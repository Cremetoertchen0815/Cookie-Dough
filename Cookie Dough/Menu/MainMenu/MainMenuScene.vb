Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Menu.MainMenu
    Public Class MainMenuScene
        Inherits Scene

        Friend GameList As (String, String, Boolean)() '(IG name, orig. name, is available)()

        Private rend As MainMenuRenderer
        Private lastmstate As MouseState

        Private ChangeNameButtonPressed As Boolean = False
        Friend Blocked As Boolean = False
        Friend Submenu As Integer = 0
        Friend SM1Scroll As Single = 0
        Friend SM2Scroll As Single = 0
        Friend SM4Scroll As Single = 0

        'Networking
        Friend Const ServerAutoRefresh As Integer = 500
        Private MenschenOnline As Integer = 0
        Private ServerRefreshTimer As Integer = 0
        Private lasthostname As String = "127.0.0.1"
        Friend OnlineGameInstances As OnlineGameInstance() = {}
        Friend AvailableServerList As New List(Of String)
        Friend ConnectedUsers As New List(Of String)


        Protected Schwarzblende As New Transition(Of Single)

        Public Overrides Sub Initialize()
            MyBase.Initialize()
            AddRenderer(New DefaultRenderer)
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            rend = CreateEntity("Renderer").AddComponent(New MainMenuRenderer(Me))

            GameList = {("Betreten Verboten", "Lido", True), ("Timestein", "Mühle", False), ("Corridor", "Chess", True), ("pain.", "Schlafmütze", False), ("DuoCard", "Uno", True),
                        ("DooDoo-Head", "Durak", False), ("Megäaaa", "Jungle Speed", True), ("Barrelled", "Pac Man/Catch", True), ("Drop Trop", "Just try it out already.", True)}
        End Sub

        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScreenTransformMatrix)).ToPoint
            Dim OneshotPressed As Boolean = mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released

            If Blocked Then Return

            Select Case Submenu
                Case 0
                    If New Rectangle(560, 275, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(1)
                    If New Rectangle(560, 425, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(2)
                    If New Rectangle(560, 575, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(3)
                    If New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                    If New Rectangle(560, 725, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then Core.Exit()
                Case 1
                    'Scroll game list
                    Dim scrollval = (mstate.ScrollWheelValue - lastmstate.ScrollWheelValue) / 120.0F
                    If New Rectangle(1396, 296, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = Time.DeltaTime * 20
                    If New Rectangle(1396, 906, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = -Time.DeltaTime * 20
                    SM1Scroll = Mathf.Clamp(SM1Scroll - scrollval * 30, 0, 375 + GameList.Length * 150 - 1050)

                    For i As Integer = 0 To GameList.Length
                        If mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released AndAlso New Rectangle(560, 275 + i * 150 - CInt(SM1Scroll), 800, 100).Contains(mpos) Then
                            Select Case i
                                Case GameType.BetretenVerboten
                                    Core.StartSceneTransition(New FadeTransition(Function() New Game.BetretenVerboten.CreatorMenu))
                                Case GameType.Megäa
                                    Core.StartSceneTransition(New FadeTransition(Function() New Game.Megäa.GameRoom))
                                Case GameType.DuoCard
                                    Core.StartSceneTransition(New FadeTransition(Function() New Game.DuoCard.CreatorMenu))
                                Case GameType.DropTrop
                                    Core.StartSceneTransition(New FadeTransition(Function() New Game.DropTrop.CreatorMenu))
                                Case GameType.Barrelled
                                    Core.StartSceneTransition(New FadeTransition(Function() New Game.Barrelled.CreatorMenu))
                                Case GameType.Corridor
                                    Core.StartSceneTransition(New FadeTransition(Function() New Game.Corridor.GameRoom))
                                Case GameList.Length
                                    SwitchToSubmenu(0)
                            End Select
                        End If
                    Next
                    If New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                Case 2
                    'Scroll online game list
                    Dim scrollval = (mstate.ScrollWheelValue - lastmstate.ScrollWheelValue) / 120.0F
                    If New Rectangle(1396, 296, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = Time.DeltaTime * 20
                    If New Rectangle(1396, 906, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = -Time.DeltaTime * 20
                    SM2Scroll = Mathf.Clamp(SM2Scroll - scrollval * 30, 0, Math.Max(375 + OnlineGameInstances.Length * 150 - 1050, 0))

                    For i As Integer = 0 To OnlineGameInstances.Length
                        If mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released AndAlso New Rectangle(560, 275 + i * 150 - CInt(SM2Scroll), 800, 100).Contains(mpos) Then
                            Select Case i
                                Case OnlineGameInstances.Length 'Back button
                                    SwitchToSubmenu(0)
                                Case Else
                                    OpenGaemViaNetwork(OnlineGameInstances(i))
                            End Select
                        End If
                    Next
                    If New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                Case 3
                    If New Rectangle(560, 275, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then My.Settings.Schwierigkeitsgrad = (My.Settings.Schwierigkeitsgrad + 1) Mod 2 : My.Settings.Save()
                    If New Rectangle(560, 425, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(5)
                    If New Rectangle(560, 575, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                    If New Rectangle(560, 725, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(0)
                Case 4
                    'Scroll online game list
                    Dim ln As Integer = If(IsConnectedToServer And ServerActive, ConnectedUsers, AvailableServerList).Count
                    Dim scrollval = (mstate.ScrollWheelValue - lastmstate.ScrollWheelValue) / 120.0F
                    If New Rectangle(1396, 296, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = Time.DeltaTime * 20
                    If New Rectangle(1396, 906, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = -Time.DeltaTime * 20
                    SM4Scroll = Mathf.Clamp(SM4Scroll - scrollval * 30, 0, Math.Max(375 + (ln + 2) * 150 - 1050, 0))

                    For i As Integer = -2 To ln
                        If ((mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) Or (mstate.RightButton = ButtonState.Pressed And lastmstate.RightButton = ButtonState.Released)) AndAlso New Rectangle(560, 275 + (i + 2) * 150 - CInt(SM4Scroll), 800, 100).Contains(mpos) Then
                            Select Case i
                                Case ln
                                    SwitchToSubmenu(0)
                                Case -2 'Dual button
                                    If mpos.X <= 960 Then
                                        'Left button
                                        If Not IsConnectedToServer Then LaunchInputBox(Sub(x)
                                                                                           My.Settings.Servers.Add(x)
                                                                                           UpdateServerList()
                                                                                       End Sub, rend.MediumFont, "Enter IP-adress:", "Add server", My.Settings.IP) : SFX(2).Play() Else SFX(0).Play()
                                    Else
                                        'Right button
                                        If IsConnectedToServer Then
                                            LocalClient.Disconnect()
                                            SFX(2).Play()
                                        Else
                                            If Not ServerActive Then
                                                'If LocalClient.Connected Then LocalClient.Disconnect()
                                                If LocalClient.TryConnect Then
                                                    MsgBoxer.EnqueueMsgbox("Other server already active on this port")
                                                Else
                                                    StartServer()
                                                    LocalClient.Connect("127.0.0.1", My.Settings.Username)
                                                End If
                                                SFX(2).Play()
                                            Else
                                                SFX(0).Play()
                                            End If
                                        End If
                                    End If
                                Case -1
                                Case Else
                                    If Not (IsConnectedToServer And ServerActive) And mstate.LeftButton = ButtonState.Pressed Then
                                        LocalClient.Connect(AvailableServerList(i), My.Settings.Username)
                                        lasthostname = AvailableServerList(i)
                                        SFX(2).Play()
                                    ElseIf Not IsConnectedToServer And mstate.RightButton = ButtonState.Pressed Then
                                        My.Settings.Reload()
                                        If My.Settings.Servers.Contains(AvailableServerList(i)) Then My.Settings.Servers.Remove(AvailableServerList(i))
                                        My.Settings.Save()
                                        UpdateServerList()
                                    End If
                            End Select
                        End If
                    Next

                    If New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                Case 5

                    'Nick name
                    If New Rectangle(960, 230 + 0 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then LaunchInputBox(Sub(x)
                                                                                                                                                    If IsConnectedToServer Then
                                                                                                                                                        LocalClient.Disconnect()
                                                                                                                                                        My.Settings.Username = x
                                                                                                                                                        My.Settings.Save()
                                                                                                                                                        LocalClient.Connect(lasthostname, My.Settings.Username)
                                                                                                                                                    Else
                                                                                                                                                        My.Settings.Username = x
                                                                                                                                                        My.Settings.Save()
                                                                                                                                                    End If
                                                                                                                                                End Sub, rend.MediumFont, "Enter the new username: ", "Change username", My.Settings.Username)

                    'MOTD
                    If New Rectangle(960, 230 + 1 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then LaunchInputBox(Sub(x)
                                                                                                                                                    My.Settings.MOTD = x
                                                                                                                                                    My.Settings.Save()
                                                                                                                                                End Sub, rend.MediumFont, "Enter your new ""Message of the day"": ", "Change MOTD", My.Settings.MOTD)

                    'PFP
                    If New Rectangle(1350, 245 + 2 * 80, 100, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And My.Settings.Thumbnail Then
                        Dim ofd As New Windows.Forms.OpenFileDialog() With {.Filter = "PNG-File|*.png", .Title = "Select profile picture"}
                        Dim res = ofd.ShowDialog
                        If res = Windows.Forms.DialogResult.OK AndAlso New IO.FileInfo(ofd.FileName).Length <= 5000000 Then
                            IO.File.Copy(ofd.FileName, "Cache/client/pp.png", True)
                        ElseIf res = Windows.Forms.DialogResult.OK Then
                            MsgBoxer.EnqueueMsgbox("File too big!")
                        End If
                    ElseIf New Rectangle(960, 230 + 2 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                        My.Settings.Thumbnail = Not My.Settings.Thumbnail
                        My.Settings.Save()
                        If My.Settings.Thumbnail AndAlso Not IO.File.Exists("Cache/client/pp.png") Then IO.File.Copy("Content/prep/plc.png", "Cache/client/pp.png")
                    End If

                    'Spawn Sound
                    If New Rectangle(1350, 245 + 3 * 80, 100, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And My.Settings.SoundA = IdentType.Custom Then
                        Dim ofd As New Windows.Forms.OpenFileDialog() With {.Filter = "Wavefile|*.wav", .Title = "Select sound effect"}
                        Dim res = ofd.ShowDialog
                        If res = Windows.Forms.DialogResult.OK AndAlso New IO.FileInfo(ofd.FileName).Length <= 5000000 Then
                            IO.File.Copy(ofd.FileName, "Cache/client/soundA.audio", True)
                        ElseIf res = Windows.Forms.DialogResult.OK Then
                            MsgBoxer.EnqueueMsgbox("File too big!")
                        End If
                        Try
                            PlayAudio(IdentType.Custom)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
                    ElseIf New Rectangle(960, 230 + 3 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                        My.Settings.SoundA = (My.Settings.SoundA + 1) Mod 7
                        My.Settings.Save()
                        If My.Settings.SoundA = IdentType.Custom AndAlso Not IO.File.Exists("Cache/client/soundA.audio") Then IO.File.Copy("Content/prep/plc.wav", "Cache/client/soundA.audio")
                        Try
                            PlayAudio(My.Settings.SoundA)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
                    End If

                    'Kick Sound
                    If New Rectangle(1350, 245 + 4 * 80, 100, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And My.Settings.SoundB = IdentType.Custom Then
                        Dim ofd As New Windows.Forms.OpenFileDialog() With {.Filter = "Wavefile|*.wav", .Title = "Select sound effect"}
                        Dim res = ofd.ShowDialog
                        If res = Windows.Forms.DialogResult.OK AndAlso New IO.FileInfo(ofd.FileName).Length <= 5000000 Then
                            IO.File.Copy(ofd.FileName, "Cache/client/soundB.audio", True)
                        ElseIf res = Windows.Forms.DialogResult.OK Then
                            MsgBoxer.EnqueueMsgbox("File too big!")
                        End If
                        Try
                            PlayAudio(IdentType.Custom, True)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
                    ElseIf New Rectangle(960, 230 + 4 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                        My.Settings.SoundB = (My.Settings.SoundB + 1) Mod 7
                        My.Settings.Save()
                        If My.Settings.SoundB = IdentType.Custom AndAlso Not IO.File.Exists("Cache/client/soundB.audio") Then IO.File.Copy("Content/prep/plc.wav", "Cache/client/soundB.audio")
                        Try
                            PlayAudio(My.Settings.SoundB, True)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
                    End If

                    If New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                    If New Rectangle(560, 955, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(0)
            End Select



            'Wechsel Benutzername
            If New Rectangle(New Point(20, 40), rend.MediumFont.MeasureString("Username: " & My.Settings.Username).ToPoint).Contains(mpos) And OneshotPressed Then SwitchToSubmenu(5)

            'Grab data from Server in a set interval
            If IsConnectedToServer And LocalClient.AutomaticRefresh Then
                ServerRefreshTimer += CInt(Time.DeltaTime * 1000)
                If ServerRefreshTimer > ServerAutoRefresh Then
                    ServerRefreshTimer = 0
                    MenschenOnline = LocalClient.GetOnlineMemberCount
                    OnlineGameInstances = LocalClient.GetGamesList
                    UpdateServerList()
                End If
            End If

            'Read users on server
            If ServerActive Then
                ConnectedUsers.Clear()
                Server.FillListWithConnectedUsers(ConnectedUsers)
            End If

            lastmstate = mstate
            MyBase.Update()
        End Sub

        Private Sub SwitchToSubmenu(submenu As Integer, Optional InBetweenOperation As Action = Nothing)
            'Spiele Sound
            SFX(2).Play()
            Blocked = True
            Select Case submenu
                Case 1
                    SM1Scroll = 0
                Case 2
                    SM2Scroll = 0
                Case 4
                    SM4Scroll = 0
                    UpdateServerList()
            End Select

            'Blende über
            Schwarzblende = New Transition(Of Single)(New TransitionTypes.TransitionType_Linear(500), Schwarzblende.Value, 1.0F, Sub()
                                                                                                                                     If InBetweenOperation IsNot Nothing Then InBetweenOperation()
                                                                                                                                     Me.Submenu = submenu
                                                                                                                                     Blocked = False
                                                                                                                                     Schwarzblende = New Transition(Of Single)(New TransitionTypes.TransitionType_Linear(1000), 1.0F, 0.0F, Nothing)
                                                                                                                                     Automator.Add(Schwarzblende)
                                                                                                                                 End Sub)
            Automator.Add(Schwarzblende)
        End Sub

        Private Sub PlayAudio(ident As IdentType, Optional SoundB As Boolean = False)
            If ident <> IdentType.Custom Then
                SoundEffect.FromFile("Content/prep/audio_" & CInt(ident).ToString & ".wav").Play()
            Else
                SoundEffect.FromFile("Cache/client/sound" & If(SoundB, "B", "A") & ".audio").Play()
            End If
        End Sub

        Private Sub UpdateServerList()
            AvailableServerList.Clear()
            If ServerActive Then AvailableServerList.Add("localhost")
            For Each element In My.Settings.Servers
                AvailableServerList.Add(element)
            Next
        End Sub


        Private ReadOnly Property IsConnectedToServer() As Boolean
            Get
                Return LocalClient.Connected
            End Get
        End Property

        Private BlockOnlineJoin As Boolean = False
        Private Sub OpenGaemViaNetwork(ins As OnlineGameInstance)
            If BlockOnlineJoin Then Return
            Select Case ins.Type
                Case GameType.Barrelled
                    Try
                        BlockOnlineJoin = True
                        Dim client As New Game.Barrelled.SlaveWindow(ins)
                        If client.NetworkMode Then Core.StartSceneTransition(New FadeTransition(Function() client)).OnTransitionCompleted = AddressOf client.SendArrived : BlockOnlineJoin = False Else MsgBoxer.EnqueueMsgbox("Error connecting!") : BlockOnlineJoin = False
                    Catch ex As Exception
                        BlockOnlineJoin = False
                        MsgBoxer.EnqueueMsgbox("Error connecting!" & ex.ToString)
                    End Try
                Case GameType.BetretenVerboten
                    Try
                        BlockOnlineJoin = True
                        Dim client As New Game.BetretenVerboten.SlaveWindow(ins)
                        If client.NetworkMode Then Core.StartSceneTransition(New FadeTransition(Function() client)).OnTransitionCompleted = AddressOf client.SendArrived : BlockOnlineJoin = False Else MsgBoxer.EnqueueMsgbox("Error connecting!") : BlockOnlineJoin = False
                    Catch ex As Exception
                        BlockOnlineJoin = False
                        MsgBoxer.EnqueueMsgbox("Error connecting!" & ex.ToString)
                    End Try
                Case GameType.DropTrop
                    Try
                        BlockOnlineJoin = True
                        Dim client As New Game.DropTrop.SlaveWindow(ins)
                        If client.NetworkMode Then Core.StartSceneTransition(New FadeTransition(Function() client)).OnTransitionCompleted = AddressOf client.SendArrived : BlockOnlineJoin = False Else MsgBoxer.EnqueueMsgbox("Error connecting!") : BlockOnlineJoin = False
                    Catch ex As Exception
                        BlockOnlineJoin = False
                        MsgBoxer.EnqueueMsgbox("Error connecting!" & ex.ToString)
                    End Try
                Case GameType.DuoCard
                    Try
                        BlockOnlineJoin = True
                        Dim client As New Game.DuoCard.SlaveWindow(ins)
                        If client.NetworkMode Then Core.StartSceneTransition(New FadeTransition(Function() client)).OnTransitionCompleted = AddressOf client.SendArrived : BlockOnlineJoin = False Else MsgBoxer.EnqueueMsgbox("Error connecting!") : BlockOnlineJoin = False
                    Catch ex As Exception
                        BlockOnlineJoin = False
                        MsgBoxer.EnqueueMsgbox("Error connecting! " & ex.ToString)
                    End Try
            End Select
        End Sub


        'RENDERER
        Private Class MainMenuRenderer
            Inherits RenderableComponent
            Public Overrides ReadOnly Property Height As Single = 1080
            Public Overrides ReadOnly Property Width As Single = 1920.0F

            Private CounterScene As MainMenuScene
            Friend FgColor As Color = Color.Lime

            'Assets
            Friend TitleFont As NezSpriteFont
            Friend MediumFont As NezSpriteFont
            Friend SmolFont As NezSpriteFont
            Friend Arrow As Texture2D

            Public Sub New(Counterpart As MainMenuScene)
                MyBase.New()
                CounterScene = Counterpart
            End Sub

            Public Overrides Sub Initialize()
                MyBase.Initialize()

                TitleFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuTitle"))
                MediumFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuMain"))
                SmolFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuSmol"))
                Arrow = Core.Content.LoadTexture("arrow_left")
                Material = New Material With {.SamplerState = SamplerState.LinearClamp}
            End Sub

            Public Overrides Sub Render(batcher As Batcher, camera As Camera)

                'Zeichne Menü
                Select Case CounterScene.Submenu
                    Case 0 'Root
                        'Draw heading
                        batcher.DrawString(TitleFont, "Cookie Dough", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Cookie Dough").X / 2, 50), FgColor)
                        'Draw rectangles
                        batcher.DrawHollowRect(New Rectangle(560, 275, 800, 100), FgColor)
                        batcher.DrawHollowRect(New Rectangle(560, 425, 800, 100), FgColor)
                        batcher.DrawHollowRect(New Rectangle(560, 575, 800, 100), FgColor)
                        batcher.DrawHollowRect(New Rectangle(560, 725, 800, 100), FgColor)
                        'Draw text
                        batcher.DrawString(MediumFont, "Start Game", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Start Game").X / 2, 300), FgColor)
                        batcher.DrawString(MediumFont, "Join Round", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Join Round").X / 2, 450), FgColor) 'If(IsConnectedToServer, FgColor, Color.Red)
                        batcher.DrawString(MediumFont, "Settings", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Settings").X / 2, 600), FgColor)
                        batcher.DrawString(MediumFont, "Exit Game", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Exit Game").X / 2, 750), FgColor)
                    Case 1 'Start Round
                        'Draw scroll arrows
                        batcher.Draw(Arrow, New Rectangle(1420, 320, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.None, 0)
                        batcher.Draw(Arrow, New Rectangle(1420, 930, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.FlipHorizontally, 0)
                        'Draw games
                        Dim len As Integer = CounterScene.GameList.Length
                        For i As Integer = 0 To len - 1
                            Dim gameNameA As String = CounterScene.GameList(i).Item1
                            Dim gameNameB As String = "(" & CounterScene.GameList(i).Item2 & ")"
                            Dim color As Color = If(CounterScene.GameList(i).Item3, Color.Lime, Color.Red)
                            batcher.DrawHollowRect(New Rectangle(560, 275 + i * 150 - CounterScene.SM1Scroll, 800, 100), color)
                            batcher.DrawString(MediumFont, gameNameA, New Vector2(560, 300 + i * 150 - CounterScene.SM1Scroll), color)
                            batcher.DrawString(SmolFont, gameNameB, New Vector2(1360 - SmolFont.MeasureString(gameNameB).X, 310 + i * 150 - CounterScene.SM1Scroll), color)
                        Next
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 + len * 150 - CInt(CounterScene.SM1Scroll), 800, 100), Color.Lime)
                        batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back").X / 2, 300 + len * 150 - CounterScene.SM1Scroll), Color.Lime)

                        'Draw heading
                        batcher.DrawRect(New Rectangle(0, 0, 1920, 220), Color.Black)
                        batcher.DrawString(TitleFont, "Select game", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Select game").X / 2, 50), FgColor)
                    Case 2 'Join Round
                        'Draw scroll arrows
                        batcher.Draw(Arrow, New Rectangle(1420, 320, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.None, 0)
                        batcher.Draw(Arrow, New Rectangle(1420, 930, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.FlipHorizontally, 0)
                        'Draw games
                        Dim len As Integer = CounterScene.OnlineGameInstances.Length
                        For i As Integer = 0 To len - 1
                            Dim gameNameA As String = CounterScene.OnlineGameInstances(i).Name
                            Dim gameNameB As String = "(" & GetGameTitle(CounterScene.OnlineGameInstances(i).Type) & ")"
                            Dim color As Color = If(CounterScene.GameList(i).Item3, Color.Lime, Color.Red)
                            batcher.DrawHollowRect(New Rectangle(560, 275 + i * 150 - CounterScene.SM2Scroll, 800, 100), color)
                            batcher.DrawString(MediumFont, gameNameA, New Vector2(560, 300 + i * 150 - CounterScene.SM2Scroll), color)
                            batcher.DrawString(SmolFont, gameNameB, New Vector2(1360 - SmolFont.MeasureString(gameNameB).X, 310 + i * 150 - CounterScene.SM2Scroll), color)
                        Next
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 + len * 150 - CInt(CounterScene.SM2Scroll), 800, 100), Color.Lime)
                        batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back").X / 2, 300 + len * 150 - CounterScene.SM2Scroll), Color.Lime)

                        'Draw heading
                        batcher.DrawRect(New Rectangle(0, 0, 1920, 220), Color.Black)
                        batcher.DrawString(TitleFont, "Join round", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Join round").X / 2, 50), FgColor)
                    Case 3 'Settings
                        'Draw heading
                        batcher.DrawString(TitleFont, "Settings", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Settings").X / 2, 50), FgColor)
                        'Draw rectangles
                        batcher.DrawHollowRect(New Rectangle(560, 275, 800, 100), FgColor)
                        batcher.DrawHollowRect(New Rectangle(560, 425, 800, 100), FgColor)
                        batcher.DrawHollowRect(New Rectangle(560, 575, 800, 100), FgColor)
                        batcher.DrawHollowRect(New Rectangle(560, 725, 800, 100), FgColor)
                        'Draw text
                        Dim txtA As String = "CPU Difficulty: " & CType(My.Settings.Schwierigkeitsgrad, Difficulty).ToString
                        batcher.DrawString(MediumFont, txtA, New Vector2(1920.0F / 2 - MediumFont.MeasureString(txtA).X / 2, 300), FgColor)
                        batcher.DrawString(MediumFont, "User Settings", New Vector2(1920.0F / 2 - MediumFont.MeasureString("User Settings").X / 2, 450), FgColor) 'If(IsConnectedToServer, FgColor, Color.Red)
                        batcher.DrawString(MediumFont, "Server Settings", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Server Settings").X / 2, 600), FgColor)
                        batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back").X / 2, 750), FgColor)
                    Case 4 'Server
                        'Draw scroll arrows
                        batcher.Draw(Arrow, New Rectangle(1420, 320, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.None, 0)
                        batcher.Draw(Arrow, New Rectangle(1420, 930, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.FlipHorizontally, 0)
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 - CInt(CounterScene.SM4Scroll), 800, 100), Color.Lime)
                        batcher.DrawLine(New Vector2(960, 275 - CInt(CounterScene.SM4Scroll)), New Vector2(960, 375 - CInt(CounterScene.SM4Scroll)), FgColor)
                        batcher.DrawString(MediumFont, "Add Server", New Vector2(760 - MediumFont.MeasureString("Add Server").X / 2, 300 - CounterScene.SM4Scroll), If(CounterScene.IsConnectedToServer, Color.Red, Color.Lime))
                        batcher.DrawString(MediumFont, If(CounterScene.IsConnectedToServer, "Disconnect", "Start server"), New Vector2(1160 - MediumFont.MeasureString(If(CounterScene.IsConnectedToServer, "Disconnect", "Start server")).X / 2, 300 - CounterScene.SM4Scroll), If(Not CounterScene.IsConnectedToServer And ServerActive, Color.Red, Color.Lime))
                        batcher.DrawString(SmolFont, If(ServerActive And CounterScene.IsConnectedToServer, "Connected players:", "Available servers:"), New Vector2(560, 300 - CounterScene.SM4Scroll + 170), Color.Lime)
                        Dim len As Integer
                        If ServerActive And CounterScene.IsConnectedToServer Then
                            'Draw servers
                            len = CounterScene.ConnectedUsers.Count
                            For i As Integer = 0 To len - 1
                                Dim ireal As Integer = i + 2
                                Dim gameNameA As String = CounterScene.ConnectedUsers(i)
                                Dim color As Color = If(My.Settings.Username = gameNameA, Color.Cyan, Color.Lime)
                                batcher.DrawHollowRect(New Rectangle(560, 275 + ireal * 150 - CounterScene.SM4Scroll, 800, 100), color)
                                If gameNameA IsNot Nothing Then batcher.DrawString(SmolFont, gameNameA, New Vector2(560, 300 + ireal * 150 - CounterScene.SM4Scroll), color)
                            Next
                        Else
                            'Draw servers
                            len = CounterScene.AvailableServerList.Count
                            For i As Integer = 0 To len - 1
                                Dim ireal As Integer = i + 2
                                Dim gameNameA As String = CounterScene.AvailableServerList(i)
                                Dim color As Color = If(LocalClient.Hostname = gameNameA, Color.Cyan, Color.Lime)
                                batcher.DrawHollowRect(New Rectangle(560, 275 + ireal * 150 - CounterScene.SM4Scroll, 800, 100), Color.Lime)
                                batcher.DrawString(SmolFont, gameNameA, New Vector2(560, 300 + ireal * 150 - CounterScene.SM4Scroll), Color.Lime)
                            Next
                        End If
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 + (len + 2) * 150 - CInt(CounterScene.SM4Scroll), 800, 100), Color.Lime)
                        batcher.DrawString(MediumFont, "Back to Main Menu", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back to Main Menu").X / 2, 300 + (len + 2) * 150 - CounterScene.SM4Scroll), Color.Lime)

                        'Draw heading
                        batcher.DrawRect(New Rectangle(0, 0, 1920, 220), Color.Black)
                        batcher.DrawString(TitleFont, "Server settings", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Server settings").X / 2, 50), FgColor)
                    Case 5 'User settings

                        'Draw heading
                        batcher.DrawString(TitleFont, "User", New Vector2(1920.0F / 2 - TitleFont.MeasureString("User").X / 2, 50), FgColor)

                        batcher.DrawLine(New Vector2(1920 / 2, 230), New Vector2(1920 / 2, 790), Color.Cyan, 4)
                        batcher.DrawHollowRect(New Rectangle(450, 230, 1920 - 2 * 450, 560), Color.Red, 3)

                        DrawUserMenuTableString("Name", My.Settings.Username, 0, batcher)
                        DrawUserMenuTableString("MOTD", If(My.Settings.MOTD.Length > 15, My.Settings.MOTD.Substring(0, 13) & "...", My.Settings.MOTD), 1, batcher)
                        DrawUserMenuTableString("Profile Picture", If(My.Settings.Thumbnail, "Custom", "Disabled"), 2, batcher)
                        DrawUserMenuTableString("Spawn Sound", CType(My.Settings.SoundA, IdentType).ToString, 3, batcher)
                        DrawUserMenuTableString("Kick Sound", CType(My.Settings.SoundB, IdentType).ToString, 4, batcher)
                        DrawUserMenuTableString("Games Played", My.Settings.GamesLost + My.Settings.GamesWon - 2, 5, batcher)
                        DrawUserMenuTableString("K/D", Math.Round(My.Settings.GamesWon / My.Settings.GamesLost, 3), 6, batcher)

                        'File dialogue boxes
                        If My.Settings.Thumbnail Then
                            batcher.DrawHollowRect(New Rectangle(1350, 245 + 2 * 80, 100, 50), FgColor)
                            batcher.DrawString(MediumFont, "...", New Vector2(1360, 240 + 2 * 80), FgColor)
                        End If
                        If My.Settings.SoundA = IdentType.Custom Then
                            batcher.DrawHollowRect(New Rectangle(1350, 245 + 3 * 80, 100, 50), FgColor)
                            batcher.DrawString(MediumFont, "...", New Vector2(1360, 240 + 3 * 80), FgColor)
                        End If
                        If My.Settings.SoundB = IdentType.Custom Then
                            batcher.DrawHollowRect(New Rectangle(1350, 245 + 4 * 80, 100, 50), FgColor)
                            batcher.DrawString(MediumFont, "...", New Vector2(1360, 240 + 4 * 80), FgColor)
                        End If

                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 955, 800, 100), FgColor)
                        batcher.DrawString(MediumFont, "Back to Main Menu", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back to Main Menu").X / 2, 975), FgColor)
                End Select


                If CounterScene.Submenu <> 4 Then
                    'Zeichne Server-Info
                    Dim txtA As String = "Username: " & My.Settings.Username
                    Dim txtB As String = If(CounterScene.IsConnectedToServer, "Connected to: " & Environment.NewLine & LocalClient.Hostname & Environment.NewLine & CounterScene.MenschenOnline & " human(s) online", "No server connected")
                    batcher.DrawString(SmolFont, txtA, New Vector2(20, 40), FgColor)
                    batcher.DrawString(SmolFont, txtB, New Vector2(1920.0F - SmolFont.MeasureString(txtB).X - 20, 40), FgColor)
                End If
                batcher.DrawRect(New Rectangle(0, 0, 1920, 1080), Color.Black * CounterScene.Schwarzblende.Value)

            End Sub

            Private Sub DrawUserMenuTableString(txtA As String, txtB As String, i As Integer, batcher As Batcher, Optional red As Boolean = False)
                batcher.DrawString(MediumFont, txtA, New Vector2(1920 / 2 - MediumFont.MeasureString(txtA).X - 80, 250 + i * 80), Color.Lime)
                batcher.DrawString(MediumFont, txtB, New Vector2(1920 / 2 + 80, 250 + i * 80), If(red, Color.Red, Color.Lime))
                If i = 0 Then Return
                batcher.DrawLine(New Vector2(450, 230 + i * 80), New Vector2(1920 - 450, 230 + i * 80), Color.Yellow, 1)
            End Sub

            Private Function GetGameTitle(gaem As GameType) As String
                Select Case gaem
                    Case GameType.BetretenVerboten
                        Return "Betreten Verboten"
                    Case GameType.Pain
                        Return "pain."
                    Case GameType.DooDooHead
                        Return "DooDoo-Head"
                    Case GameType.Megäa
                        Return "Megäaaa"
                    Case GameType.DropTrop
                        Return "Drop Trop"
                    Case Else
                        Return gaem.ToString
                End Select
            End Function
        End Class
    End Class
End Namespace