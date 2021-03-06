Imports Cookie_Dough.Game.DropTrop.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Game.DropTrop
    ''' <summary>
    ''' Enthällt das Menu des Spiels und verwaltet die Spiele-Session
    ''' </summary>
    Public Class CreatorMenu
        Inherits Scene

        'Menü Flags
        Private MenuAktiviert As Boolean = True
        Private lastmstate As MouseState
        Private NewGamePlayers As SpielerTyp() = {SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local}
        Protected Whitelist As Integer() = {0, 0, 0, 0}
        Private ChangeNameButtonPressed As Boolean = False
        Private PlayerCount As Integer = 2
        Private PlayerSel As Integer = 0
        Private Map As GaemMap = GaemMap.Smol
        Private Mode As GameMode = GameMode.Competetive
        Protected Schwarzblende As New Transition(Of Single)

        'Konstanten
        Friend Const FadeOverTime As Integer = 500
        Friend Const ServerAutoRefresh As Integer = 500

        Public Overrides Sub Initialize()
            MyBase.Initialize()

            'Lade Assets
            Dev = Core.GraphicsDevice
            ClearColor = Color.Black

            'Init values
            NewGamePlayers = {SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local, SpielerTyp.Local}
            Map = 0
            PlayerCount = 4
            PlayerSel = 0
            MenuAktiviert = True

            AddRenderer(New DefaultRenderer)
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            CreateEntity("Renderer").AddComponent(New MenuRenderer(Me))

            'Setze verschiedene flags und bereite Variablen von
            'If My.Settings.Username = "" Then My.Settings.Username = Environment.UserName : My.Settings.Save()
        End Sub

        Public Overrides Sub Update()
            Dim IsActive = Core.Instance.IsActive
            Dim mstate As MouseState = If(IsActive, Mouse.GetState(), New MouseState)
            Dim kstate As KeyboardState = Keyboard.GetState()
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint
            Dim OneshotPressed As Boolean = mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released

            If MenuAktiviert And Not ChangeNameButtonPressed Then
                If New Rectangle(560, 200, 800, 100).Contains(mpos) And OneshotPressed Then
                    Map = (Map + 1) Mod 4
                    Select Case Map
                        Case GaemMap.Smol
                            ReDim NewGamePlayers(1)
                        Case GaemMap.Big
                            ReDim NewGamePlayers(3)
                        Case GaemMap.Huuuge
                            ReDim NewGamePlayers(5)
                        Case GaemMap.TREMENNNDOUS
                            ReDim NewGamePlayers(7)
                    End Select
                    ReDim Whitelist(NewGamePlayers.Length - 1)
                    PlayerCount = NewGamePlayers.Length
                    SFX(2).Play()
                End If
                If New Rectangle(560, 500, 400, 100).Contains(mpos) And OneshotPressed Then PlayerSel -= 1 : SFX(2).Play()
                If New Rectangle(960, 500, 400, 100).Contains(mpos) And OneshotPressed Then PlayerSel += 1 : SFX(2).Play()
                If New Rectangle(560, 650, 800, 100).Contains(mpos) And OneshotPressed Then
                    If PlayerSel = 0 Then
                        NewGamePlayers(PlayerSel) = (NewGamePlayers(PlayerSel) + 1) Mod 2
                    Else
                        NewGamePlayers(PlayerSel) = (NewGamePlayers(PlayerSel) + 1) Mod If(IsConnectedToServer, 4, 2)
                        If NewGamePlayers(PlayerSel) = 2 Then NewGamePlayers(PlayerSel) = 3
                    End If
                    SFX(2).Play()
                End If
                If New Rectangle(560, 900, 400, 100).Contains(mpos) And OneshotPressed Then Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene)) : MenuAktiviert = False
                If New Rectangle(960, 900, 400, 100).Contains(mpos) And OneshotPressed Then
                    SFX(2).Play()

                    Dim Internetz As Boolean = False
                    For i As Integer = 0 To PlayerCount - 1
                        If NewGamePlayers(i) = SpielerTyp.Online And IsConnectedToServer Then Internetz = True : Exit For
                    Next

                    If IsConnectedToServer And Internetz Then
                        OpenInputbox("Enter a name for the round:", "Start Round", AddressOf StartNewRound)
                    Else
                        StartNewRound("")
                    End If
                End If

                PlayerSel = Math.Min(Math.Max(PlayerSel, 0), PlayerCount - 1)
            End If

            lastmstate = mstate
            MyBase.Update()
        End Sub

        Private Sub StartNewRound(servername As String)
            If Not MenuAktiviert Then Return

            Dim Internetz As Boolean = False
            For i As Integer = 0 To PlayerCount - 1
                If NewGamePlayers(i) = SpielerTyp.Online And IsConnectedToServer Then Internetz = True : Exit For
            Next
            If Internetz Then LocalClient.AutomaticRefresh = False

            Dim AktuellesSpiel As New GameRoom(Map) With {.PlCount = PlayerCount}
            ReDim AktuellesSpiel.Spielers(AktuellesSpiel.PlCount - 1)
            AktuellesSpiel.NetworkMode = False
            AktuellesSpiel.Spielers(0) = New Player(NewGamePlayers(0)) With {.Name = If(NewGamePlayers(0) = SpielerTyp.Local, My.Settings.Username, Farben(0))}
            For i As Integer = 1 To AktuellesSpiel.PlCount - 1
                Select Case NewGamePlayers(i)
                    Case SpielerTyp.Local
                        AktuellesSpiel.Spielers(i) = New Player(SpielerTyp.Local) With {.Name = My.Settings.Username & "-" & (i + 1).ToString}
                    Case SpielerTyp.CPU
                        AktuellesSpiel.Spielers(i) = New Player(SpielerTyp.CPU) With {.Name = Farben(i)}
                    Case SpielerTyp.Online
                        AktuellesSpiel.Spielers(i) = New Player(SpielerTyp.Online) With {.Bereit = False}
                    Case SpielerTyp.None
                        AktuellesSpiel.Spielers(i) = New Player(SpielerTyp.None) With {.Bereit = True}
                End Select
            Next

            'Blende über
            Core.StartSceneTransition(New FadeTransition(Function() AktuellesSpiel)).OnScreenObscured = Sub()
                                                                                                            AktuellesSpiel.LoadContent()
                                                                                                            If Internetz Then
                                                                                                                Dim wtlst As String() = New String(Whitelist.Length - 1) {}
                                                                                                                For i As Integer = 0 To Whitelist.Length - 1
                                                                                                                    wtlst(i) = ""
                                                                                                                Next
                                                                                                                If Not ExtGame.CreateGame(LocalClient, servername, Map, AktuellesSpiel.Spielers, wtlst, Mode = GameMode.Casual) Then MsgBoxer.EnqueueMsgbox("Somethings wrong, mate!") Else AktuellesSpiel.NetworkMode = True
                                                                                                            End If
                                                                                                        End Sub

            MenuAktiviert = False
        End Sub

        Private ReadOnly Property IsConnectedToServer() As Boolean
            Get
                Return LocalClient.Connected
            End Get
        End Property

        Public Shared Function GetMapSize(map As GaemMap) As Vector2
            Select Case map
                Case GaemMap.Smol
                    Return Vector2.One * 10
                Case GaemMap.Big
                    Return Vector2.One * 12
                Case GaemMap.Huuuge
                    Return Vector2.One * 14
                Case GaemMap.TREMENNNDOUS
                    Return Vector2.One * 16
            End Select
        End Function

        Public Class MenuRenderer
            Inherits RenderableComponent

            Private TitleFont As NezSpriteFont
            Private MediumFont As NezSpriteFont
            Private instance As CreatorMenu

            Public Sub New(instance As CreatorMenu)
                MyBase.New()
                Me.instance = instance
            End Sub

            Public Overrides Sub OnAddedToEntity()
                MyBase.OnAddedToEntity()

                TitleFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuTitle"))
                MediumFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuMain"))
                Material = New Material With {.SamplerState = SamplerState.LinearClamp, .BlendState = BlendState.AlphaBlend}
            End Sub

            Public Overrides ReadOnly Property Bounds As RectangleF
                Get
                    Return New RectangleF(0, 0, 1920, 1080)
                End Get
            End Property

            Public Overrides Sub Render(batcher As Batcher, camera As Camera)

                'Draw heading
                batcher.DrawString(TitleFont, "Drop Trop", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Drop Trop").X / 2, 50), FgColor)

                'Draw rects
                batcher.DrawHollowRect(New Rectangle(560, 200, 800, 100), FgColor)
                batcher.DrawHollowRect(New Rectangle(560, 350, 800, 100), FgColor)
                batcher.DrawHollowRect(New Rectangle(560, 500, 800, 100), FgColor)
                batcher.DrawHollowRect(New Rectangle(560, 650, 800, 100), FgColor)

                'Draw contents
                batcher.DrawLine(New Vector2(1920.0F / 2, 500), New Vector2(1920.0F / 2, 600), FgColor)
                batcher.DrawString(MediumFont, "←", New Vector2(1920.0F / 2 - 200 - MediumFont.MeasureString("←").X / 2, 525), FgColor)
                batcher.DrawString(MediumFont, "→", New Vector2(1920.0F / 2 + 200 - MediumFont.MeasureString("→").X / 2, 525), FgColor)
                batcher.DrawString(MediumFont, "Map: " & instance.Map.ToString, New Vector2(1920.0F / 2 - MediumFont.MeasureString("Map: " & instance.Map).X / 2, 225), FgColor)
                batcher.DrawString(MediumFont, instance.PlayerCount.ToString & " Player", New Vector2(1920.0F / 2 - MediumFont.MeasureString(instance.PlayerCount.ToString & " Player").X / 2, 375), FgColor)
                batcher.DrawString(MediumFont, "Player " & (instance.PlayerSel + 1).ToString & ": " & instance.NewGamePlayers(instance.PlayerSel).ToString, New Vector2(1920.0F / 2 - MediumFont.MeasureString("Player " & (instance.PlayerSel + 1).ToString & ": " & instance.NewGamePlayers(instance.PlayerSel).ToString).X / 2, 675), FgColor)
                batcher.DrawHollowRect(New Rectangle(560, 900, 800, 100), FgColor)
                batcher.DrawLine(New Vector2(1920.0F / 2, 900), New Vector2(1920.0F / 2, 1000), FgColor)
                batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - 200 - MediumFont.MeasureString("Back").X / 2, 925), FgColor)
                batcher.DrawString(MediumFont, "Start Round", New Vector2(1920.0F / 2 + 200 - MediumFont.MeasureString("Start Round").X / 2, 925), FgColor)

                batcher.DrawRect(New Rectangle(0, 0, 1920, 1080), Color.Black * instance.Schwarzblende.Value)
            End Sub
        End Class

    End Class
End Namespace