Imports System.Collections.Generic
Imports Cookie_Dough.Game.Barrelled.Networking
Imports Cookie_Dough.Game.Barrelled.Players
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Game.Barrelled
    ''' <summary>
    ''' Enthällt das Menu des Spiels und verwaltet die Spiele-Session
    ''' </summary>
    Public Class CreatorMenu
        Inherits Scene

        'Menü Flags
        Private MenuAktiviert As Boolean = True
        Private lastmstate As MouseState
        Private ChangeNameButtonPressed As Boolean = False
        Private PlayerCount As Integer = 4
        Private PlayerSel As Integer = 0
        Private Map As Map = Map.Classic
        Private Mode As GameMode = GameMode.Competetive
        Friend Arrow As Texture2D
        Protected AllUser As New List(Of (String, String)) '(ID, Name)
        Protected NewGamePlayers As PlayerMode()
        Protected Whitelist As Integer() = {0, 0, 0, 0}
        Protected SecondScreen As Boolean = False
        Protected SM4Scroll As Single
        Protected Schwarzblende As New Transition(Of Single)

        'Konstanten
        Friend Const FadeOverTime As Integer = 500
        Friend Const ServerAutoRefresh As Integer = 500

        Public Overrides Sub Initialize()
            MyBase.Initialize()

            'Lade Assets
            Arrow = Core.Content.LoadTexture("arrow_left")
            Dev = Core.GraphicsDevice
            ClearColor = Color.Black

            'Init values
            NewGamePlayers = {PlayerMode.Ghost, PlayerMode.Ghost, PlayerMode.Ghost}
            Map = Map.Classic
            PlayerCount = 4
            PlayerSel = 0
            MenuAktiviert = True

            AddRenderer(New DefaultRenderer)
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            CreateEntity("Renderer").AddComponent(New MenuRenderer(Me))
        End Sub

        Public Overrides Sub Update()
            Dim IsActive = Core.Instance.IsActive
            Dim mstate As MouseState = If(IsActive, Mouse.GetState(), New MouseState)
            Dim kstate As KeyboardState = Keyboard.GetState()
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint
            Dim OneshotPressed As Boolean = mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released

            If MenuAktiviert And Not ChangeNameButtonPressed Then

                If Not SecondScreen Then

                    If New Rectangle(560, 200, 800, 100).Contains(mpos) And OneshotPressed Then
                        Map = (Map + 1) Mod 2
                        ReDim NewGamePlayers(GetMapSize(Map) - 1)
                        ReDim Whitelist(GetMapSize(Map) - 1)
                        SFX(2).Play()
                    End If
                    If New Rectangle(560, 350, 800, 100).Contains(mpos) And OneshotPressed Then Mode = If(Mode = GameMode.Casual, GameMode.Competetive, GameMode.Casual)
                    If New Rectangle(560, 500, 400, 100).Contains(mpos) And OneshotPressed Then PlayerSel -= 1 : SFX(2).Play()
                    If New Rectangle(960, 500, 400, 100).Contains(mpos) And OneshotPressed Then PlayerSel += 1 : SFX(2).Play()
                    If New Rectangle(560, 650, 800, 100).Contains(mpos) And OneshotPressed Then NewGamePlayers(PlayerSel) = (NewGamePlayers(PlayerSel) + 1) Mod 3 : SFX(2).Play()
                    If New Rectangle(560, 900, 400, 100).Contains(mpos) And OneshotPressed Then Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene)) : MenuAktiviert = False
                    If New Rectangle(960, 900, 400, 100).Contains(mpos) And OneshotPressed Then
                        SFX(2).Play()

                        If IsConnectedToServer Then
                            SwitchToOtherScreen(Sub()
                                                    AllUser = New List(Of (String, String)) From {("", "Open")}
                                                    AllUser.AddRange(LocalClient.GetAllUsers)
                                                    SM4Scroll = 0
                                                End Sub)
                        Else
                            Microsoft.VisualBasic.MsgBox("Server connection required!")
                        End If
                    End If
                Else
                    Dim scrollval = (mstate.ScrollWheelValue - lastmstate.ScrollWheelValue) / 120.0F
                    If New Rectangle(1396, 296, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = Time.DeltaTime * 20
                    If New Rectangle(1396, 906, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = -Time.DeltaTime * 20
                    SM4Scroll = Mathf.Clamp(SM4Scroll - scrollval * 30, 0, Math.Max(375 + (Whitelist.Length - 1) * 150 - 1050, 0))

                    'Upper buttons
                    If New Rectangle(560, 200, 400, 100).Contains(mpos) And OneshotPressed Then OpenInputbox("Enter a name for the round:", "Start Round", AddressOf StartNewRound)
                    If New Rectangle(960, 200, 400, 100).Contains(mpos) And OneshotPressed Then SwitchToOtherScreen()

                    'Whitelist items
                    For i As Integer = 0 To NewGamePlayers.Length - 1
                        If i > 0 Then
                            If New Rectangle(560, 350 + (i - 1) * 150 - CInt(SM4Scroll), 800, 100).Contains(mpos) And OneshotPressed Then
                                'Increment player
                                Dim once As Boolean = True
                                Do While once Or IsIDtaken(i)
                                    Whitelist(i) = (Whitelist(i) + 1) Mod AllUser.Count
                                    once = False
                                Loop
                            End If
                        End If
                    Next
                End If

                PlayerCount = GetMapSize(Map)
                PlayerSel = Math.Min(Math.Max(PlayerSel, 0), PlayerCount - 1)
            End If

            lastmstate = mstate
            MyBase.Update()
        End Sub
        Private Function IsIDtaken(i As String) As Boolean
            For j As Integer = 0 To Whitelist.Length - 1
                If Whitelist(j) = Whitelist(i) And i <> j And AllUser(Whitelist(i)).Item1 <> "" Then Return True
            Next
            Return False
        End Function
        Private Sub StartNewRound(servername As String)
            If Not MenuAktiviert Then Return

            LocalClient.AutomaticRefresh = False

            Dim local_count As Integer = 1
            Dim AktuellesSpiel As New GameRoom(Map)
            ReDim AktuellesSpiel.Spielers(AktuellesSpiel.PlCount - 1)
            AktuellesSpiel.GameMode = Mode
            AktuellesSpiel.NetworkMode = False
            AktuellesSpiel.Difficulty = My.Settings.Schwierigkeitsgrad
            For i As Integer = 0 To AktuellesSpiel.PlCount - 1
                If i = 0 Then
                    AktuellesSpiel.Spielers(i) = New EgoPlayer(SpielerTyp.Local, NewGamePlayers(i)) With {.Name = My.Settings.Username & If(local_count > 1, "-" & local_count.ToString, "")}
                Else
                    AktuellesSpiel.Spielers(i) = New OtherPlayer(SpielerTyp.Online, NewGamePlayers(i)) With {.Bereit = False}
                End If
            Next
            AktuellesSpiel.LoadContent()

            'Blende über
            Core.StartSceneTransition(New FadeTransition(Function() AktuellesSpiel)).OnScreenObscured = Sub()
                                                                                                            Dim wtlst As String() = New String(Whitelist.Length - 1) {}
                                                                                                            For i As Integer = 0 To Whitelist.Length - 1
                                                                                                                wtlst(i) = AllUser(Whitelist(i)).Item1
                                                                                                            Next
                                                                                                            If Not ExtGame.CreateGame(LocalClient, servername, Map, AktuellesSpiel.Spielers, wtlst, Mode = GameMode.Casual) Then Microsoft.VisualBasic.MsgBox("Somethings wrong, mate!") Else AktuellesSpiel.NetworkMode = True
                                                                                                        End Sub

            MenuAktiviert = False
        End Sub

        Private Sub OpenInputbox(message As String, title As String, finalaction As Action(Of String), Optional defaultvalue As String = "")
            If Not ChangeNameButtonPressed Then
                ChangeNameButtonPressed = True
                Dim txt As String = Microsoft.VisualBasic.InputBox(message, title, defaultvalue)
                If txt <> "" Then
                    finalaction.Invoke(txt)
                End If
                ChangeNameButtonPressed = False
            End If
        End Sub

        Private ReadOnly Property IsConnectedToServer() As Boolean
            Get
                Return LocalClient.Connected
            End Get
        End Property

        Private Sub SwitchToOtherScreen(Optional InBetweenOperation As Action = Nothing)
            'Spiele Sound
            SFX(2).Play()

            'Blende über
            Schwarzblende = New Transition(Of Single)(New TransitionTypes.TransitionType_Linear(500), Schwarzblende.Value, 1.0F, Sub()
                                                                                                                                     If InBetweenOperation IsNot Nothing Then InBetweenOperation()
                                                                                                                                     SecondScreen = Not SecondScreen
                                                                                                                                     Schwarzblende = New Transition(Of Single)(New TransitionTypes.TransitionType_Linear(1000), 1.0F, 0.0F, Nothing)
                                                                                                                                     Automator.Add(Schwarzblende)
                                                                                                                                 End Sub)
            Automator.Add(Schwarzblende)
        End Sub

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

                TitleFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\MenuTitle"))
                MediumFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\MenuMain"))
                Material = New Material With {.SamplerState = SamplerState.LinearClamp, .BlendState = BlendState.AlphaBlend}
            End Sub

            Public Overrides ReadOnly Property Bounds As RectangleF
                Get
                    Return New RectangleF(0, 0, 1920, 1080)
                End Get
            End Property

            Public Overrides Sub Render(batcher As Batcher, camera As Camera)


                If Not instance.SecondScreen Then
                    'Draw rects
                    batcher.DrawHollowRect(New Rectangle(560, 200, 800, 100), FgColor)
                    batcher.DrawHollowRect(New Rectangle(560, 350, 800, 100), FgColor)
                    batcher.DrawHollowRect(New Rectangle(560, 500, 800, 100), FgColor)
                    batcher.DrawHollowRect(New Rectangle(560, 650, 800, 100), FgColor)

                    'Draw contents
                    batcher.DrawLine(New Vector2(1920.0F / 2, 200), New Vector2(1920.0F / 2, 300), FgColor)
                    batcher.DrawString(MediumFont, "Map: " & GetMapName(instance.Map), New Vector2(1920.0F / 2 - 200 - MediumFont.MeasureString("Map: " & GetMapName(instance.Map)).X / 2, 225), FgColor)
                    batcher.DrawString(MediumFont, instance.PlayerCount.ToString & " Player", New Vector2(1920.0F / 2 + 200 - MediumFont.MeasureString(instance.PlayerCount.ToString & " Player").X / 2, 225), FgColor)

                    batcher.DrawString(MediumFont, "Mode: " & instance.Mode.ToString, New Vector2(1920.0F / 2 - MediumFont.MeasureString("Mode: " & instance.Mode.ToString).X / 2, 375), FgColor)

                    batcher.DrawLine(New Vector2(1920.0F / 2, 500), New Vector2(1920.0F / 2, 600), FgColor)
                    batcher.DrawString(MediumFont, "←", New Vector2(1920.0F / 2 - 200 - MediumFont.MeasureString("←").X / 2, 525), FgColor)
                    batcher.DrawString(MediumFont, "→", New Vector2(1920.0F / 2 + 200 - MediumFont.MeasureString("→").X / 2, 525), FgColor)

                    Dim line_str = If(instance.PlayerSel <= 0, "Local", "Online") & " Player " & If(instance.PlayerSel > 0, instance.PlayerSel.ToString, "").ToString & ": " & instance.NewGamePlayers(instance.PlayerSel).ToString
                    batcher.DrawString(MediumFont, line_str, New Vector2(1920.0F / 2 - MediumFont.MeasureString(line_str).X / 2, 675), FgColor)
                    batcher.DrawHollowRect(New Rectangle(560, 900, 800, 100), FgColor)

                    batcher.DrawLine(New Vector2(1920.0F / 2, 900), New Vector2(1920.0F / 2, 1000), FgColor)
                    batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - 200 - MediumFont.MeasureString("Back").X / 2, 925), FgColor)
                    batcher.DrawString(MediumFont, "Start Round", New Vector2(1920.0F / 2 + 200 - MediumFont.MeasureString("Start Round").X / 2, 925), FgColor)
                Else
                    'Draw top button
                    batcher.DrawHollowRect(New Rectangle(560, 200 - CInt(instance.SM4Scroll), 800, 100), FgColor)
                    batcher.DrawLine(New Vector2(1920.0F / 2, 200 - CInt(instance.SM4Scroll)), New Vector2(1920.0F / 2, 300 - CInt(instance.SM4Scroll)), FgColor)
                    batcher.DrawString(MediumFont, "Start", New Vector2(1920.0F / 2 - 200 - MediumFont.MeasureString("Start").X / 2, 225 - CInt(instance.SM4Scroll)), FgColor)
                    batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 + 200 - MediumFont.MeasureString("Back").X / 2, 225 - CInt(instance.SM4Scroll)), FgColor)
                    'Draw scroll arrows
                    batcher.Draw(instance.Arrow, New Rectangle(1420, 320, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.None, 0)
                    batcher.Draw(instance.Arrow, New Rectangle(1420, 930, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.FlipHorizontally, 0)
                    'Draw online player
                    For i As Integer = 1 To instance.NewGamePlayers.Length - 1
                        Dim str As String = "Player " & i.ToString & ": " & instance.AllUser(instance.Whitelist(i - 1)).Item2
                        batcher.DrawHollowRect(New Rectangle(560, 350 + (i - 1) * 150 - CInt(instance.SM4Scroll), 800, 100), FgColor)
                        batcher.DrawString(MediumFont, str, New Vector2(1920.0F / 2 - MediumFont.MeasureString(str).X / 2, 375 + (i - 1) * 150 - CInt(instance.SM4Scroll)), FgColor)
                    Next
                End If

                'Draw heading
                batcher.DrawRect(New Rectangle(0, 0, 1920, 150), Color.Black)
                batcher.DrawString(TitleFont, "Barrelled", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Barrelled").X / 2, 50), FgColor)

                batcher.DrawRect(New Rectangle(0, 0, 1920, 1080), Color.Black * instance.Schwarzblende.Value)
            End Sub
        End Class

    End Class
End Namespace