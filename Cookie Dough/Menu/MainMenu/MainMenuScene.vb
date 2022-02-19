Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI.Gamepad
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
        Private _vcontroller As GpadController
        Private _refresh As Refreshinator(Of Integer)
        Private _lastFrameCounter As Integer

        Private ChangeNameButtonPressed As Boolean = False
        Friend Blocked As Boolean = False
        Friend Submenu As Integer = 0

        'Networking
        Friend Const ServerAutoRefresh As Integer = 500
        Private MenschenOnline As Integer = 0
        Private ServerRefreshTimer As Integer = 0
        Private lasthostname As String = "127.0.0.1"
        Friend OnlineGameInstances As OnlineGameInstance() = {}
        Friend AvailableServerList As New List(Of String)
        Friend ConnectedUsers As New List(Of String)

        'Leaderboards
        Friend LdbSelectedGame As GameType = GameType.BetretenVerboten
        Friend LdbSelectedMap As Integer = 0
        Friend LdbSelectedTeam As Boolean = False
        Friend LdbData As New List(Of (String, String, Double))


        Protected Schwarzblende As New Transition(Of Single)

        Public Overrides Sub Initialize()
            MyBase.Initialize()
            AddRenderer(New DefaultRenderer)
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            Core.Instance.IsMouseVisible = True

            'Generate visuals and gpad controller
            Dim rentity = CreateEntity("Renderer")
            rend = rentity.AddComponent(New MainMenuRenderer(Me)).SetLayerDepth(0.5F)
            _vcontroller = rentity.AddComponent(New GpadController).SetLayerDepth(1.0F)

            'Register vcontroller controlls
            _refresh = New Refreshinator(Of Integer)
            _refresh.EqualityComparerer = Function(a, b) a = b
            _refresh.RefreshAction = AddressOf GenerateVirtualControls
            GenerateVirtualControls()

            GameList = {("Betreten Verboten", "Lido", True), ("Car Crash", "Wheel speen sim", True), ("Corridor", "Chess", True), ("pain.", "Schlafmütze", False), ("DuoCard", "Uno", True),
                        ("Peng", "Pong on drugs", False), ("Megäaaa", "Jungle Speed", True), ("Barrelled", "Pac Man/Catch", True), ("Drop Trop", "Error 404", True)}
        End Sub

        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScreenTransformMatrix)).ToPoint
            Dim OneshotPressed As Boolean = mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released

            If Blocked Then Return

            _vcontroller.LocalOffset = Vector2.Zero

            Select Case Submenu
                Case 2
                    'Scroll online game list
                    Dim scrollval = (mstate.ScrollWheelValue - lastmstate.ScrollWheelValue) / 120.0F
                    If New Rectangle(1396, 296, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = Time.DeltaTime * 20
                    If New Rectangle(1396, 906, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = -Time.DeltaTime * 20
                    ScrollValue = Mathf.Clamp(ScrollValue - scrollval * 30, 0, Math.Max(375 + OnlineGameInstances.Length * 150 - 1050, 0))

                    For i As Integer = 0 To OnlineGameInstances.Length
                        If mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released AndAlso New Rectangle(560, 275 + i * 150 - CInt(ScrollValue), 800, 100).Contains(mpos) Then
                            Select Case i
                                Case OnlineGameInstances.Length 'Back button
                                    SwitchToSubmenu(0)
                                Case Else
                                    OpenGaemViaNetwork(OnlineGameInstances(i))
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
#If MONO Then
                        If IO.File.Exists("thumbnail.png") Then
                            If New IO.FileInfo("thumbnail.png").Length <= 5000000 Then
                                IO.File.Copy("thumbnail.png", "Cache/client/pp.png", True)
                                MsgBoxer.EnqueueMsgbox("File implemented!")
                            Else
                                MsgBoxer.EnqueueMsgbox("File too big!")
                            End If
                        Else
                            MsgBoxer.EnqueueMsgbox("Mono(specifically Mac OS) doesn't support file selection windows! Instead, copy the desired PNG image file in the root directory, name it ""thumbnail.png"" and press this button again.")
                        End If
#Else
                        Dim ofd As New Windows.Forms.OpenFileDialog() With {.Filter = "PNG-File|*.png", .Title = "Select profile picture"}
                        Dim res = ofd.ShowDialog
                        If res = Windows.Forms.DialogResult.OK AndAlso New IO.FileInfo(ofd.FileName).Length <= 5000000 Then
                            IO.File.Copy(ofd.FileName, "Cache/client/pp.png", True)
                        ElseIf res = Windows.Forms.DialogResult.OK Then
                            MsgBoxer.EnqueueMsgbox("File too big!")
                        End If
#End If
                    ElseIf New Rectangle(960, 230 + 2 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                        My.Settings.Thumbnail = Not My.Settings.Thumbnail
                        My.Settings.Save()
                        If My.Settings.Thumbnail AndAlso Not IO.File.Exists("Cache/client/pp.png") Then IO.File.Copy("Content/prep/plc.png", "Cache/client/pp.png")
                    End If

                    'Spawn Sound
                    If New Rectangle(1350, 245 + 3 * 80, 100, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And My.Settings.SoundA = IdentType.Custom Then
#If MONO Then
                        If IO.File.Exists("spawn.wav") Then
                            If New IO.FileInfo("spawn.wav").Length <= 5000000 Then
                                IO.File.Copy("spawn.wav", "Cache/client/soundA.audio", True)
                                MsgBoxer.EnqueueMsgbox("File implemented!")

                                Try
                                    PlayAudio(IdentType.Custom)
                                Catch ex As Exception
                                    MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                                End Try
                            Else
                                MsgBoxer.EnqueueMsgbox("File too big!")
                            End If
                        Else
                            MsgBoxer.EnqueueMsgbox("Mono(specifically Mac OS) doesn't support file selection windows! Instead, copy the desired wave file in the root directory, name it ""spawn.wav"" and press this button again.")
                        End If
#Else
                        Dim ofd As New Windows.Forms.OpenFileDialog() With {.Filter = "Wavefile|*.wav", .Title = "Select sound effect"}
                        Dim res = ofd.ShowDialog
                        If res = Windows.Forms.DialogResult.OK AndAlso New IO.FileInfo(ofd.FileName).Length <= 5000000 Then
                            IO.File.Copy(ofd.FileName, "Cache/client/soundA.audio", True)
                        ElseIf res = Windows.Forms.DialogResult.OK Then
                            MsgBoxer.EnqueueMsgbox("File too big!")
                        End If

                        Try
                            PlayAudio(IdentType.Custom, 0)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
#End If
                    ElseIf New Rectangle(960, 230 + 3 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                        My.Settings.SoundA = (My.Settings.SoundA + 1) Mod 7
                        My.Settings.Save()
                        If My.Settings.SoundA = IdentType.Custom AndAlso Not IO.File.Exists("Cache/client/soundA.audio") Then IO.File.Copy("Content/prep/plc.wav", "Cache/client/soundA.audio")
                        Try
                            PlayAudio(My.Settings.SoundA, 0)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
                    End If

                    'Kick Sound
                    If New Rectangle(1350, 245 + 4 * 80, 100, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And My.Settings.SoundB = IdentType.Custom Then
#If MONO Then
                        If IO.File.Exists("kick.wav") Then
                            If New IO.FileInfo("kick.wav").Length <= 5000000 Then
                                IO.File.Copy("kick.wav", "Cache/client/soundB.audio", True)
                                MsgBoxer.EnqueueMsgbox("File implemented!")
                                Try
                                    PlayAudio(IdentType.Custom, True)
                                Catch ex As Exception
                                    MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                                End Try
                            Else
                                MsgBoxer.EnqueueMsgbox("File too big!")
                            End If
                        Else
                            MsgBoxer.EnqueueMsgbox("Mono(specifically Mac OS) doesn't support file selection windows! Instead, copy the desired wave file in the root directory, name it ""kick.wav"" and press this button again.")
                        End If
#Else
                        Dim ofd As New Windows.Forms.OpenFileDialog() With {.Filter = "Wavefile|*.wav", .Title = "Select sound effect"}
                        Dim res = ofd.ShowDialog
                        If res = Windows.Forms.DialogResult.OK AndAlso New IO.FileInfo(ofd.FileName).Length <= 5000000 Then
                            IO.File.Copy(ofd.FileName, "Cache/client/soundB.audio", True)
                        ElseIf res = Windows.Forms.DialogResult.OK Then
                            MsgBoxer.EnqueueMsgbox("File too big!")
                        End If

                        Try
                            PlayAudio(IdentType.Custom, 1)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
#End If
                    ElseIf New Rectangle(960, 230 + 4 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                        My.Settings.SoundB = (My.Settings.SoundB + 1) Mod 7
                        My.Settings.Save()
                        If My.Settings.SoundB = IdentType.Custom AndAlso Not IO.File.Exists("Cache/client/soundB.audio") Then IO.File.Copy("Content/prep/plc.wav", "Cache/client/soundB.audio")
                        Try
                            PlayAudio(My.Settings.SoundB, 1)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
                    End If

                    'Death Sound
                    If New Rectangle(1350, 245 + 5 * 80, 100, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And My.Settings.SoundB = IdentType.Custom Then
#If MONO Then
                        If IO.File.Exists("death.wav") Then
                            If New IO.FileInfo("death.wav").Length <= 5000000 Then
                                IO.File.Copy("death.wav", "Cache/client/soundC.audio", True)
                                MsgBoxer.EnqueueMsgbox("File implemented!")
                                Try
                                    PlayAudio(IdentType.Custom, True)
                                Catch ex As Exception
                                    MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                                End Try
                            Else
                                MsgBoxer.EnqueueMsgbox("File too big!")
                            End If
                        Else
                            MsgBoxer.EnqueueMsgbox("Mono(specifically Mac OS) doesn't support file selection windows! Instead, copy the desired wave file in the root directory, name it ""kick.wav"" and press this button again.")
                        End If
#Else
                        Dim ofd As New Windows.Forms.OpenFileDialog() With {.Filter = "Wavefile|*.wav", .Title = "Select sound effect"}
                        Dim res = ofd.ShowDialog
                        If res = Windows.Forms.DialogResult.OK AndAlso New IO.FileInfo(ofd.FileName).Length <= 5000000 Then
                            IO.File.Copy(ofd.FileName, "Cache/client/soundC.audio", True)
                        ElseIf res = Windows.Forms.DialogResult.OK Then
                            MsgBoxer.EnqueueMsgbox("File too big!")
                        End If

                        Try
                            PlayAudio(IdentType.Custom, 2)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
#End If
                    ElseIf New Rectangle(960, 230 + 5 * 80, 510, 80).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                        My.Settings.SoundC = (My.Settings.SoundC + 1) Mod 7
                        My.Settings.Save()
                        If My.Settings.SoundC = IdentType.Custom AndAlso Not IO.File.Exists("Cache/client/soundC.audio") Then IO.File.Copy("Content/prep/plc.wav", "Cache/client/soundC.audio")
                        Try
                            PlayAudio(My.Settings.SoundC, 2)
                        Catch ex As Exception
                            MsgBoxer.EnqueueMsgbox("Invalid sound file!")
                        End Try
                    End If

                    If New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                    If New Rectangle(560, 955, 800, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(0)
                Case 6
                    'Change options
                    Dim loaddata As Boolean = False
                    If New Rectangle(20, 190, 260, 70).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then 'Change game
                        LdbSelectedGame = If(LdbSelectedGame = GameType.BetretenVerboten, GameType.CarCrash, GameType.BetretenVerboten)
                        LdbSelectedMap = 0
                        LdbSelectedTeam = False
                        loaddata = True
                    End If
                    If New Rectangle(20, 290, 260, 70).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then 'Change map
                        Select Case LdbSelectedGame
                            Case GameType.BetretenVerboten
                                LdbSelectedMap = (LdbSelectedMap + 1) Mod 4
                            Case GameType.CarCrash
                                LdbSelectedMap = (LdbSelectedMap + 1) Mod 3
                        End Select
                        loaddata = True
                    End If
                    If New Rectangle(20, 390, 260, 70).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And LdbSelectedGame = GameType.BetretenVerboten Then 'Change team
                        LdbSelectedTeam = Not LdbSelectedTeam
                        loaddata = True
                    End If

                    'Fetching data
                    If loaddata Then LdbData = LocalClient.GetLeaderboard(LdbSelectedGame, LdbSelectedMap, LdbSelectedTeam)

                    'Navigation
                    If New Rectangle(30, 950, 300, 100).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(0) 'Back
                    If New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4) 'Server settings
                Case Else
                    'Scroll game list
                    Dim scrollval = (mstate.ScrollWheelValue - lastmstate.ScrollWheelValue) / 120.0F
                    If New Rectangle(1396, 296, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = Time.DeltaTime * 20
                    If New Rectangle(1396, 906, 50, 50).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then scrollval = -Time.DeltaTime * 20
                    ScrollValue = Mathf.Clamp(ScrollValue - scrollval * 30, 0, 375 + GameList.Length * 150 - 1050)

                    If mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then _vcontroller.SimulateMousePress(mpos + New Point(0, ScrollValue))

                    If Submenu > 0 And New Rectangle(1920 - 450, 0, 450, 200).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed Then SwitchToSubmenu(4)
                    _vcontroller.LocalOffset = New Vector2(0, -ScrollValue)
            End Select

            _refresh.Update()
            _lastFrameCounter = ConnectedUsers.Count

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
            _vcontroller.Enabled = False
            If submenu = 4 Then UpdateServerList()

            'Blende über
            Schwarzblende = New Transition(Of Single)(New TransitionTypes.TransitionType_Linear(500), Schwarzblende.Value, 1.0F, Sub()
                                                                                                                                     If InBetweenOperation IsNot Nothing Then InBetweenOperation()
                                                                                                                                     Me.Submenu = submenu
                                                                                                                                     ScrollValue = 0
                                                                                                                                     GenerateVirtualControls()
                                                                                                                                     _vcontroller.Enabled = True
                                                                                                                                     Blocked = False
                                                                                                                                     Schwarzblende = New Transition(Of Single)(New TransitionTypes.TransitionType_Linear(1000), 1.0F, 0.0F, Nothing)
                                                                                                                                     Automator.Add(Schwarzblende)
                                                                                                                                 End Sub)
            Automator.Add(Schwarzblende)
        End Sub

        Private Sub GenerateVirtualControls()
            _vcontroller.DeregisterAll()
            _refresh.ClearConditions()

            Select Case Submenu
                Case 0
                    'Main Menu
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 275, 800, 100), Sub() SwitchToSubmenu(1)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 425, 800, 100), Sub() SwitchToSubmenu(2)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 575, 800, 100), Sub() SwitchToSubmenu(6, Sub() LdbData = LocalClient.GetLeaderboard(LdbSelectedGame, LdbSelectedMap, LdbSelectedTeam))))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 725, 800, 100), Sub() SwitchToSubmenu(3)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(1920 - 380, 45, 380, 100), Sub() SwitchToSubmenu(4)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(20, 40, 400, 50), Sub() SwitchToSubmenu(5)))
                    _vcontroller.ActionGoBack = Sub() Return
                    _vcontroller.ActionScroll = Sub(x) Return

                Case 1
                    'Create game
                    For i As Integer = 0 To GameList.Length
                        Dim aa As Action
                        Select Case i
                            Case GameType.BetretenVerboten
                                aa = Sub() Core.StartSceneTransition(New FadeTransition(Function() New Game.BetretenVerboten.CreatorMenu))
                            Case GameType.Megäa
                                aa = Sub() Core.StartSceneTransition(New FadeTransition(Function() New Game.Megäa.GameRoom))
                            Case GameType.DuoCard
                                aa = Sub() Core.StartSceneTransition(New FadeTransition(Function() New Game.DuoCard.CreatorMenu))
                            Case GameType.DropTrop
                                aa = Sub() Core.StartSceneTransition(New FadeTransition(Function() New Game.DropTrop.CreatorMenu))
                            Case GameType.Barrelled
                                aa = Sub() Core.StartSceneTransition(New FadeTransition(Function() New Game.Barrelled.CreatorMenu))
                            Case GameType.Corridor
                                aa = Sub() Core.StartSceneTransition(New FadeTransition(Function() New Game.Corridor.GameRoom))
                            Case GameType.CarCrash
                                aa = Sub() Core.StartSceneTransition(New FadeTransition(Function() New Game.CarCrash.CreatorMenu))
                            Case GameList.Length
                                aa = Sub() SwitchToSubmenu(0)
                            Case Else
                                aa = Sub() Return
                        End Select
                        _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 275 + i * 150 - CInt(ScrollValue), 800, 100), aa))
                    Next

                    _vcontroller.ActionGoBack = Sub() SwitchToSubmenu(0)
                    _vcontroller.ActionScroll = Sub(x) ScrollValue = Mathf.Clamp(ScrollValue - x * 5, 0, 375 + GameList.Length * 150 - 1050)

                Case 3
                    'Settings
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 275, 800, 100), Sub()
                                                                                                       My.Settings.Schwierigkeitsgrad = (My.Settings.Schwierigkeitsgrad + 1) Mod 2
                                                                                                       My.Settings.Save()
                                                                                                   End Sub))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 425, 800, 100), Sub() SwitchToSubmenu(5)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(20, 40, 400, 50), Sub() SwitchToSubmenu(5)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 575, 800, 100), Sub() SwitchToSubmenu(4)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 725, 800, 100), Sub()
                                                                                                       Dim r As Single = Nez.Random.NextFloat()
                                                                                                       Dim g As Single = Nez.Random.NextFloat()
                                                                                                       Dim b As Single = Nez.Random.NextFloat()
                                                                                                       FgColor = New Color(r, g, b)
                                                                                                       My.Settings.colorR = r * 255
                                                                                                       My.Settings.colorG = g * 255
                                                                                                       My.Settings.colorB = b * 255
                                                                                                       My.Settings.Save()
                                                                                                   End Sub))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 875, 800, 100), Sub() SwitchToSubmenu(0)))

                    'Top Controls
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(1920 - 380, 45, 380, 100), Sub() SwitchToSubmenu(4)))
                    _vcontroller.RegisterControl(New IDPControl(New Rectangle(20, 40, 400, 50), Sub() SwitchToSubmenu(5)))

                    _vcontroller.ActionGoBack = Sub() SwitchToSubmenu(0)
                    _vcontroller.ActionScroll = Sub(x) Return
                Case 4
                    'Server settings
                    Dim ln As Integer = If(IsConnectedToServer And ServerActive, ConnectedUsers, AvailableServerList).Count
                    For i As Integer = -2 To ln
                        Select Case i
                            Case ln
                                _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 275 + (i + 2) * 150, 800, 100), Sub() SwitchToSubmenu(0)))
                            Case -2 'Dual button

                                'Left button
                                _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 275, 400, 100), Sub()
                                                                                                                   If Not IsConnectedToServer Then LaunchInputBox(Sub(x)
                                                                                                                                                                      My.Settings.Servers.Add(x)
                                                                                                                                                                      UpdateServerList()
                                                                                                                                                                  End Sub, rend.MediumFont, "Enter IP-adress:", "Add server", My.Settings.IP) : SFX(2).Play() Else SFX(0).Play()
                                                                                                               End Sub))

                                'Right button
                                _vcontroller.RegisterControl(New IDPControl(New Rectangle(960, 275, 400, 100), Sub()

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
                                                                                                               End Sub))
                            Case -1 'Leave one empty row
                            Case Else
                                Dim ii = i
                                _vcontroller.RegisterControl(New IDPControl(New Rectangle(560, 275 + (i + 2) * 150, 800, 100), Sub()
                                                                                                                                   If Not (IsConnectedToServer And ServerActive) Then
                                                                                                                                       LocalClient.Connect(AvailableServerList(ii), My.Settings.Username)
                                                                                                                                       lasthostname = AvailableServerList(ii)
                                                                                                                                       SFX(2).Play()
                                                                                                                                   End If
                                                                                                                               End Sub))
                        End Select
                    Next

                    _vcontroller.ActionGoBack = Sub() SwitchToSubmenu(0)
                    _vcontroller.ActionScroll = Sub(x) ScrollValue = Mathf.Clamp(ScrollValue - x * 5, 0, 375 + GameList.Length * 150 - 1050)

                    'Add refresh conditions
                    _refresh.AddCondition(Function() IsConnectedToServer)
                    _refresh.AddCondition(Function() ServerActive)
                    _refresh.AddCondition(Function() ConnectedUsers.Count)
                    _refresh.AddCondition(Function() AvailableServerList.Count)
            End Select
        End Sub

        Private _scrolls = {0, 0, 0, 0, 0, 0, 0}
        Friend Property ScrollValue As Integer
            Get
                Return _scrolls(Submenu)
            End Get
            Set(value As Integer)
                If Submenu = 1 Or Submenu = 2 Or Submenu = 4 Then _scrolls(Submenu) = value
            End Set
        End Property

        Private Sub PlayAudio(ident As IdentType, Optional Sound As Integer = 0)
            If ident <> IdentType.Custom Then
                SoundEffect.FromFile("Content/prep/audio_" & CInt(ident).ToString & ".wav").Play()
            Else
                Select Case Sound
                    Case 0
                        SoundEffect.FromFile("Cache/client/soundA.audio").Play()
                    Case 1
                        SoundEffect.FromFile("Cache/client/soundB.audio").Play()
                    Case 2
                        SoundEffect.FromFile("Cache/client/soundC.audio").Play()
                End Select
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
                Case GameType.CarCrash
                    Try
                        BlockOnlineJoin = True
                        Dim client As New Game.CarCrash.SlaveWindow(ins)
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
                        Dim onlinecolor = If(LocalClient.Connected, FgColor, Color.Red)
                        'Draw heading
                        batcher.DrawString(TitleFont, "Cookie Dough", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Cookie Dough").X / 2, 50), FgColor)
                        'Draw rectangles
                        batcher.DrawHollowRect(New Rectangle(560, 275, 800, 100), FgColor)
                        batcher.DrawHollowRect(New Rectangle(560, 425, 800, 100), onlinecolor)
                        batcher.DrawHollowRect(New Rectangle(560, 575, 800, 100), onlinecolor)
                        batcher.DrawHollowRect(New Rectangle(560, 725, 800, 100), FgColor)
                        'Draw text
                        batcher.DrawString(MediumFont, "Start Game", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Start Game").X / 2, 300), FgColor)
                        batcher.DrawString(MediumFont, "Join Round", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Join Round").X / 2, 450), onlinecolor)
                        batcher.DrawString(MediumFont, "Leaderboard", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Leaderboard").X / 2, 600), onlinecolor)
                        batcher.DrawString(MediumFont, "Settings", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Settings").X / 2, 750), FgColor)
                    Case 1 'Start Round
                        'Draw scroll arrows
                        batcher.Draw(Arrow, New Rectangle(1420, 320, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.None, 0)
                        batcher.Draw(Arrow, New Rectangle(1420, 930, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.FlipHorizontally, 0)
                        'Draw games
                        Dim len As Integer = CounterScene.GameList.Length
                        For i As Integer = 0 To len - 1
                            Dim gameNameA As String = CounterScene.GameList(i).Item1
                            Dim gameNameB As String = "(" & CounterScene.GameList(i).Item2 & ")"
                            Dim color As Color = If(CounterScene.GameList(i).Item3, FgColor, Color.Red)
                            batcher.DrawHollowRect(New Rectangle(560, 275 + i * 150 - CounterScene.ScrollValue, 800, 100), color)
                            batcher.DrawString(MediumFont, gameNameA, New Vector2(560, 300 + i * 150 - CounterScene.ScrollValue), color)
                            batcher.DrawString(SmolFont, gameNameB, New Vector2(1360 - SmolFont.MeasureString(gameNameB).X, 310 + i * 150 - CounterScene.ScrollValue), color)
                        Next
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 + len * 150 - CInt(CounterScene.ScrollValue), 800, 100), FgColor)
                        batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back").X / 2, 300 + len * 150 - CounterScene.ScrollValue), FgColor)

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
                            Dim gameNameA As String = If(CounterScene.OnlineGameInstances(i).Name.Length > 16, CounterScene.OnlineGameInstances(i).Name.Substring(0, 13) & "...", CounterScene.OnlineGameInstances(i).Name)
                            Dim gameNameB As String = "(" & GetGameTitle(CounterScene.OnlineGameInstances(i).Type) & ")"
                            Dim color As Color = If(CounterScene.GameList(i).Item3, FgColor, Color.Red)
                            batcher.DrawHollowRect(New Rectangle(560, 275 + i * 150 - CounterScene.ScrollValue, 800, 100), color)
                            batcher.DrawString(MediumFont, gameNameA, New Vector2(560, 300 + i * 150 - CounterScene.ScrollValue), color)
                            batcher.DrawString(SmolFont, gameNameB, New Vector2(1360 - SmolFont.MeasureString(gameNameB).X, 310 + i * 150 - CounterScene.ScrollValue), color)
                        Next
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 + len * 150 - CInt(CounterScene.ScrollValue), 800, 100), FgColor)
                        batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back").X / 2, 300 + len * 150 - CounterScene.ScrollValue), FgColor)

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
                        batcher.DrawHollowRect(New Rectangle(560, 875, 800, 100), FgColor)
                        'Draw text
                        Dim txtA As String = "Difficulty: " & CType(My.Settings.Schwierigkeitsgrad, Difficulty).ToString
                        batcher.DrawString(MediumFont, txtA, New Vector2(1920.0F / 2 - MediumFont.MeasureString(txtA).X / 2, 300), FgColor)
                        batcher.DrawString(MediumFont, "User Settings", New Vector2(1920.0F / 2 - MediumFont.MeasureString("User Settings").X / 2, 450), FgColor) 'If(IsConnectedToServer, FgColor, Color.Red)
                        batcher.DrawString(MediumFont, "Server Settings", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Server Settings").X / 2, 600), FgColor)
                        batcher.DrawString(MediumFont, "Randomize Menu Color", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Randomize Menu Color").X / 2, 750), FgColor)
                        batcher.DrawString(MediumFont, "Back", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back").X / 2, 900), FgColor)
                    Case 4 'Server
                        'Draw scroll arrows
                        batcher.Draw(Arrow, New Rectangle(1420, 320, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.None, 0)
                        batcher.Draw(Arrow, New Rectangle(1420, 930, 50, 50), Nothing, Color.Orange, 0.5 * Math.PI, New Vector2(8), SpriteEffects.FlipHorizontally, 0)
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 - CInt(CounterScene.ScrollValue), 800, 100), FgColor)
                        batcher.DrawLine(New Vector2(960, 275 - CInt(CounterScene.ScrollValue)), New Vector2(960, 375 - CInt(CounterScene.ScrollValue)), FgColor)
                        batcher.DrawString(MediumFont, "Add Server", New Vector2(760 - MediumFont.MeasureString("Add Server").X / 2, 300 - CounterScene.ScrollValue), If(CounterScene.IsConnectedToServer, Color.Red, FgColor))
                        batcher.DrawString(MediumFont, If(CounterScene.IsConnectedToServer, "Disconnect", "Start server"), New Vector2(1160 - MediumFont.MeasureString(If(CounterScene.IsConnectedToServer, "Disconnect", "Start server")).X / 2, 300 - CounterScene.ScrollValue), If(Not CounterScene.IsConnectedToServer And ServerActive, Color.Red, FgColor))
                        batcher.DrawString(SmolFont, If(ServerActive And CounterScene.IsConnectedToServer, "Connected players:", "Available servers:"), New Vector2(560, 300 - CounterScene.ScrollValue + 170), FgColor)
                        Dim len As Integer
                        If ServerActive And CounterScene.IsConnectedToServer Then
                            'Draw servers
                            len = CounterScene.ConnectedUsers.Count
                            For i As Integer = 0 To len - 1
                                Dim ireal As Integer = i + 2
                                Dim gameNameA As String = CounterScene.ConnectedUsers(i)
                                Dim color As Color = If(My.Settings.Username = gameNameA, Color.Cyan, FgColor)
                                batcher.DrawHollowRect(New Rectangle(560, 275 + ireal * 150 - CounterScene.ScrollValue, 800, 100), color)
                                If gameNameA IsNot Nothing Then batcher.DrawString(SmolFont, gameNameA, New Vector2(560, 300 + ireal * 150 - CounterScene.ScrollValue), color)
                            Next
                        Else
                            'Draw servers
                            len = CounterScene.AvailableServerList.Count
                            For i As Integer = 0 To len - 1
                                Dim ireal As Integer = i + 2
                                Dim gameNameA As String = CounterScene.AvailableServerList(i)
                                Dim color As Color = If(LocalClient.Hostname = gameNameA, Color.Cyan, FgColor)
                                batcher.DrawHollowRect(New Rectangle(560, 275 + ireal * 150 - CounterScene.ScrollValue, 800, 100), FgColor)
                                batcher.DrawString(SmolFont, gameNameA, New Vector2(560, 300 + ireal * 150 - CounterScene.ScrollValue), FgColor)
                            Next
                        End If
                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 275 + (len + 2) * 150 - CInt(CounterScene.ScrollValue), 800, 100), FgColor)
                        batcher.DrawString(MediumFont, "Back to Main Menu", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back to Main Menu").X / 2, 300 + (len + 2) * 150 - CounterScene.ScrollValue), FgColor)

                        'Draw heading
                        batcher.DrawRect(New Rectangle(0, 0, 1920, 220), Color.Black)
                        batcher.DrawString(TitleFont, "Server settings", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Server settings").X / 2, 50), FgColor)
                    Case 5 'User settings

                        'Draw heading
                        batcher.DrawString(TitleFont, "User", New Vector2(1920.0F / 2 - TitleFont.MeasureString("User").X / 2, 50), FgColor)

                        batcher.DrawLine(New Vector2(1920 / 2, 230), New Vector2(1920 / 2, 870), Color.Cyan, 4)
                        batcher.DrawHollowRect(New Rectangle(450, 230, 1920 - 2 * 450, 640), New Color(0, 255, 100), 3)

                        DrawUserMenuTableString("Name", My.Settings.Username, 0, batcher)
                        DrawUserMenuTableString("MOTD", If(My.Settings.MOTD.Length > 15, My.Settings.MOTD.Substring(0, 13) & "...", My.Settings.MOTD), 1, batcher)
                        DrawUserMenuTableString("Profile Picture", If(My.Settings.Thumbnail, "Custom", "Disabled"), 2, batcher)
                        DrawUserMenuTableString("Spawn Sound", CType(My.Settings.SoundA, IdentType).ToString, 3, batcher)
                        DrawUserMenuTableString("Kick Sound", CType(My.Settings.SoundB, IdentType).ToString, 4, batcher)
                        DrawUserMenuTableString("Death Sound", CType(My.Settings.SoundC, IdentType).ToString, 5, batcher)
                        DrawUserMenuTableString("Games Played", My.Settings.GamesLost + My.Settings.GamesWon - 2, 6, batcher)
                        DrawUserMenuTableString("K/D", Math.Round(My.Settings.GamesWon / My.Settings.GamesLost, 3), 7, batcher)

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
                        If My.Settings.SoundC = IdentType.Custom Then
                            batcher.DrawHollowRect(New Rectangle(1350, 245 + 5 * 80, 100, 50), FgColor)
                            batcher.DrawString(MediumFont, "...", New Vector2(1360, 240 + 5 * 80), FgColor)
                        End If

                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(560, 955, 800, 100), FgColor)
                        batcher.DrawString(MediumFont, "Back to Main Menu", New Vector2(1920.0F / 2 - MediumFont.MeasureString("Back to Main Menu").X / 2, 975), FgColor)
                    Case 6 'Leaderboards
                        'Draw heading
                        batcher.DrawString(TitleFont, "Leaderboard", New Vector2(1920.0F / 2 - TitleFont.MeasureString("Leaderboard").X / 2, 50), FgColor)

                        'Draw settings
                        batcher.DrawString(MediumFont, "Game: " & GetShortGameTitle(CounterScene.LdbSelectedGame), New Vector2(30, 200), FgColor)
                        batcher.DrawString(MediumFont, "Map: " & GetMapName(CounterScene.LdbSelectedGame, CounterScene.LdbSelectedMap), New Vector2(30, 300), FgColor)
                        If CounterScene.LdbSelectedGame = GameType.BetretenVerboten Then batcher.DrawString(MediumFont, "Team: " & If(CounterScene.LdbSelectedTeam, "Yes", "No"), New Vector2(30, 400), FgColor)

                        Dim margin_left As Integer = 470
                        Dim margin_top As Integer = 200
                        Dim cell_height As Integer = 70
                        Dim cell_width_A As Integer = 800
                        Dim cell_width_B As Integer = 300
                        Dim cell_width_index As Integer = 80
                        'Draw table
                        batcher.DrawLine(New Vector2(margin_left + cell_width_index, margin_top + cell_height), New Vector2(margin_left + cell_width_index, margin_top + 11 * cell_height), FgColor, 3)
                        batcher.DrawLine(New Vector2(margin_left + cell_width_A, margin_top), New Vector2(margin_left + cell_width_A, margin_top + 11 * cell_height), FgColor, 3)
                        batcher.DrawLine(New Vector2(margin_left, margin_top), New Vector2(margin_left, margin_top + 11 * cell_height), FgColor, 3)
                        batcher.DrawLine(New Vector2(margin_left + cell_width_A + cell_width_B, margin_top), New Vector2(margin_left + cell_width_A + cell_width_B, margin_top + 11 * cell_height), FgColor, 3)
                        For i As Integer = 0 To 11
                            batcher.DrawLine(New Vector2(margin_left, margin_top + i * cell_height), New Vector2(margin_left + cell_width_A + cell_width_B, margin_top + i * cell_height), FgColor, 3)
                        Next

                        'Draw table headers
                        batcher.DrawString(MediumFont, "Name", New Vector2(800, 200), FgColor)
                        batcher.DrawString(MediumFont, "Score", New Vector2(1350, 200), FgColor)
                        For i As Integer = 1 To 10
                            batcher.DrawString(MediumFont, i.ToString() & ".", New Vector2(margin_left + 5, margin_top + cell_height * i), FgColor)
                        Next

                        'Draw contents
                        For i As Integer = 0 To CounterScene.LdbData.Count - 1
                            batcher.DrawString(MediumFont, CounterScene.LdbData(i).Item1, New Vector2(margin_left + cell_width_index + 10, 200 + (i + 1) * cell_height), FgColor)
                            batcher.DrawString(MediumFont, CounterScene.LdbData(i).Item3.ToString, New Vector2(margin_left + cell_width_A + 10, 200 + (i + 1) * cell_height), FgColor)
                        Next

                        'Draw back button
                        batcher.DrawHollowRect(New Rectangle(30, 950, 300, 100), FgColor, 3)
                        batcher.DrawString(MediumFont, "Back", New Vector2(120, 975), FgColor)
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
                batcher.DrawString(MediumFont, txtA, New Vector2(1920 / 2 - MediumFont.MeasureString(txtA).X - 80, 250 + i * 80), FgColor)
                batcher.DrawString(MediumFont, txtB, New Vector2(1920 / 2 + 80, 250 + i * 80), If(red, Color.Red, FgColor))
                If i = 0 Then Return
                batcher.DrawLine(New Vector2(450, 230 + i * 80), New Vector2(1920 - 450, 230 + i * 80), New Color(30, 0, 80), 2)
            End Sub


            Private Function GetShortGameTitle(gaem As GameType) As String
                Select Case gaem
                    Case GameType.BetretenVerboten
                        Return "BV"
                    Case GameType.CarCrash
                        Return "CC"
                    Case GameType.Corridor
                        Return "CR"
                    Case GameType.Pain
                        Return "PN"
                    Case GameType.DuoCard
                        Return "PE"
                    Case GameType.Peng
                        Return "DDH"
                    Case GameType.Megäa
                        Return "MG"
                    Case GameType.Barrelled
                        Return "BR"
                    Case GameType.DropTrop
                        Return "DT"
                    Case Else
                        Return ""
                End Select
            End Function

            Private Function GetMapName(gaem As GameType, map As Integer) As String
                Select Case gaem
                    Case GameType.BetretenVerboten
                        Return Game.BetretenVerboten.Maps.GetMapName(map)
                    Case GameType.CarCrash
                        Select Case map
                            Case 0
                                Return "Easy"
                            Case 1
                                Return "Medium"
                            Case Else
                                Return "Hard"
                        End Select
                    Case Else
                        Return "NaNi"
                End Select
            End Function

            Private Function GetGameTitle(gaem As GameType) As String
                Select Case gaem
                    Case GameType.BetretenVerboten
                        Return "Betreten Verboten"
                    Case GameType.Pain
                        Return "pain."
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