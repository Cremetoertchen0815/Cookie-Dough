Imports System.Collections.Generic
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Game.Barrelled.Networking
Imports Cookie_Dough.Game.Barrelled.Players
Imports Cookie_Dough.Game.Barrelled.Renderers
Imports Cookie_Dough.Game.Common
Imports Cookie_Dough.Menu.MainMenu
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Nez.Tiled
Imports Nez.Tweens

Namespace Game.Barrelled
    Public Class GameRoom
        Inherits Scene
        Implements IGameWindow

        'Gameplay fields
        Friend Spielers As CommonPlayer()
        Friend EgoPlayer As EgoPlayer
        Friend UserIndex As Integer = 0
        Friend PlCount As Integer = 2
        Friend StopUpdating As Boolean = False
        Friend NetworkMode As Boolean = False
        Friend Map As Map = Map.Mainland
        Friend Status As GameStatus
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Friend Difficulty As Difficulty 'Declares the difficulty of the CPU
        Friend CanStart As Boolean = False
        Friend WaitingTimeFlag As Boolean = False
        Private lastmstate As MouseState
        Private PrisonPeople As New List(Of Integer)

        'Networking
        Private SyncPosCounter As Single = 0

        '3D movement & interaction
        Friend Colliders As BoundingBox()
        Friend ObjectHandler As Object3DHandler
        Friend Crosshair As CrosshairRenderable

        'Assets & rendering
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont
        Private Renderer As Renderer3D
        Private MinimapRenderer As RenderLayerRenderer

        'Debug and eastereggs
        Private fov As Single = 1.15

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtnA As Controls.Button
        Private WithEvents HUDBtnB As Controls.Button
        Private WithEvents HUDSprintBar As Controls.ProgressBar
        Private WithEvents HUDChat As Controls.TextscrollBox
        Private WithEvents HUDChatBtn As Controls.Button
        Private WithEvents HUDNameBtn As Controls.Button
        Private WithEvents HUDInstructions As Controls.Label
        Private WithEvents HUDFullscrBtn As Controls.Button
        Private WithEvents HUDMusicBtn As Controls.Button
        Private AdditionalHUDRend As AdditionalHUDRendererable
        Private InstructionFader As ITween(Of Color)
        Private ShowDice As Boolean = False
        Private HUDColor As Color
        Private Chat As List(Of (String, Color))
        Private InputBoxFlag As Boolean = False

        'Map
        Public TileMap As TmxMap

        'Constants
        Private Const WaitinTime As Integer = 1500
        Private Const SyncLoc As Single = 0.05F

        Public Sub New(map As Map)
            Chat = New List(Of (String, Color))
            Me.Map = map
            PlCount = GetMapSize(map)
            Status = GameStatus.WaitingForOnlinePlayers
            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)
        End Sub

        Public Overrides Sub Unload()
            Framework.Networking.Client.OutputDelegate = Sub(x) Return
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ChatText"))

            'Prepare Nez scene
            Core.Instance.IsMouseVisible = False
            ClearColor = Color.Transparent
            'Rendereres
            AddRenderer(New PsygroundRenderer(1, 0.85F))
            Renderer = AddRenderer(New Renderer3D(Me, 2))
            AddRenderer(New RenderLayerExcludeRenderer(3, 5) With {.WantsToRenderAfterPostProcessors = True})
            'Postprocessing
            AddPostProcessor(New QualityBloomPostProcessor(2)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6F).SetThreshold(0.3F)
            AddPostProcessor(New VignettePostProcessor(1) With {.Power = 2.0F, .Radius = 1.0F})

            'Load Map
            TileMap = Content.LoadTiledMap("Maps\Barrelled\" & Map.ToString & ".tmx")

            EgoPlayer = CreateEntity("EgoPlayer").AddComponent(Spielers(0))
            CreateEntity("Map").AddComponent(New TiledMapRenderer(TileMap, "Collision")).SetRenderLayer(5)

            Dim minimapRect As RectangleF = RectangleF.Empty
            CommonPlayer.CollisionLayers = {TileMap.GetLayer(Of TmxLayer)("Collision"), TileMap.GetLayer(Of TmxLayer)("High")}
            Renderer.GenerateMapMatrices(TileMap)
            Renderer.Floorsize = New Vector2(TileMap.Properties("floor_size_X"), TileMap.Properties("floor_size_Y"))
            For Each element In TileMap.GetObjectGroup("Objects").Objects
                Select Case element.Type
                    Case "spawn"
                        CommonPlayer.PlayerSpawn = New Vector2(element.X, element.Y)
                    Case "prison"
                        CommonPlayer.PrisonPosition = New Rectangle(element.X, element.Y, element.Width, element.Height)
                    Case "minimap"
                        minimapRect = New RectangleF(New Vector2(element.X, element.Y), New Vector2(CSng(element.Properties("scale").Replace("."c, ","c))))
                End Select
            Next

            'Load minimap renderer
            MinimapRenderer = AddRenderer(New RenderLayerRenderer(0, 5) With {.RenderTexture = New Textures.RenderTexture, .RenderTargetClearColor = Color.Transparent})
            AdditionalHUDRend = CreateEntity("addition_render").SetPosition(minimapRect.Location).SetScale(minimapRect.Size).AddComponent(New AdditionalHUDRendererable(MinimapRenderer))

            'Create entities and components
            'AddSceneComponent(New Object3DHandler(Spielers(UserIndex), Me))
            Crosshair = CreateEntity("crosshair").AddComponent(Of CrosshairRenderable)().SetRenderLayer(6).SetEnabled(False)

            'Load HUD
            HUD = New GuiSystem() With {.Color = PlayerHUDColors(EgoPlayer.Mode)}
            HUDBtnA = New Controls.Button("Exit Game", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnA)
            HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnB)
            HUDSprintBar = New Controls.ProgressBar(New Vector2(500, 100), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .Progress = Function() EgoPlayer.SprintLeft, .Active = False} : HUD.Controls.Add(HUDSprintBar)
            HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(330, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 28} : HUD.Controls.Add(HUDChat)
            HUDNameBtn = New Controls.Button(EgoPlayer.Mode.ToString, New Vector2(500, 40), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Controls.Label("Run around and do stuff!", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDFullscrBtn = New Controls.Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Controls.Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)

            'Set colliders
            ObjectHandler = AddSceneComponent(New Object3DHandler(EgoPlayer, Me))
            For i As Integer = 0 To Spielers.Length - 1
                If i = UserIndex Then Continue For
                ObjectHandler.Objects.Add(Spielers(i))
            Next
            Colliders = {}
        End Sub

        Public Overrides Sub OnStart()
            MyBase.OnStart()

        End Sub

        Public Overrides Sub Update()
            MyBase.Update()

            Dim mstate As MouseState = Mouse.GetState

            'Update da good stuff
            If Not StopUpdating Then

                If NetworkMode Then SendPlayerData()

                Select Case Status
                    Case GameStatus.WaitingForOnlinePlayers
                        HUDInstructions.Text = "Waiting for all players to connect..."

                        'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                        For Each sp In Spielers
                            If sp Is Nothing OrElse Not sp.Bereit Then Exit Select 'Falls ein Spieler noch nicht belegt/bereit, breche Spielstart ab
                        Next

                        'Falls vollzählig, starte Spiel
                        Status = GameStatus.Waitn
                        Core.Schedule(0.8, Sub()
                                               PostChat("The game has started!", Color.White)
                                               SendGameActive()

                                               Select Case EgoPlayer.Mode
                                                   Case PlayerMode.Chased
                                                       ActivateGame()
                                                   Case PlayerMode.Chaser
                                                       Core.Schedule(5, AddressOf ActivateGame)

                                               End Select
                                               SendBeginGaem()
                                           End Sub)
                End Select

            End If

            'Generate View matrix for Renderer3D
            Renderer.View = Matrix.CreateLookAt(EgoPlayer.CameraPosition, EgoPlayer.CameraPosition + EgoPlayer.Direction, Vector3.Up)

            'Focus/Unfocus game
            If mstate.RightButton = ButtonState.Pressed And lastmstate.RightButton = ButtonState.Released Then
                EgoPlayer.Focused = Not EgoPlayer.Focused
                Core.Instance.IsMouseVisible = Not EgoPlayer.Focused
                Crosshair.Enabled = EgoPlayer.Focused And Status <> GameStatus.WaitingForOnlinePlayers
            End If

            'Check if game can be started
            Select Case Status
                Case GameStatus.WaitingForOnlinePlayers
                    CanStart = True
                    For i As Integer = 1 To Spielers.Length - 1
                        If Not Spielers(i).Bereit Then CanStart = False : Exit For
                    Next
                Case GameStatus.GameActive
                    'Free prisoners
                    If False Then FreePerson(UserIndex)
                    CanStart = False
                Case Else
                    CanStart = False
            End Select

            'Network stuff
            If NetworkMode And (Not LocalClient.Connected Or LocalClient.LeaveFlag) Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("Connection lost! Game was ended!")
                Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
                NetworkMode = False
            End If
            ReadAndProcessInputData()


            'FOVVVVVVVVVVVVV
            If CType(Core.Instance, Game1).GetStackKeystroke({Keys.F, Keys.O, Keys.V}) Then fov = Math.Min(Math.PI - 0.001F, fov + 0.2) : Renderer.Projection = Matrix.CreatePerspectiveFieldOfView(fov, Core.Instance.Window.ClientBounds.Width / CSng(Core.Instance.Window.ClientBounds.Height), 0.01, 500)

            lastmstate = Mouse.GetState
        End Sub

#Region "Netzwerkfunktionen"
        ''' <summary>
        ''' Liest die Daten aus dem Stream des Servers
        ''' </summary>

        Private Sub ReadAndProcessInputData()

            Dim data As String() = LocalClient.ReadStream()
            For Each element In data
                Dim source As Integer = element(0).ToString
                Dim command As Char = element(1)
                Select Case command
                    Case "a"c 'Player arrived
                        Dim txt As String() = element.Substring(2).Split("|")
                        Spielers(source).Name = txt(0)
                        Spielers(source).MOTD = txt(1)
                        Spielers(source).ID = txt(2)
                        'TRANSMIT MODE
                        Spielers(source).Bereit = True
                        Spielers(source).SetColor(PlayerColors(Spielers(source).Mode))
                        CreateEntity(txt(0)).AddComponent(Spielers(source))
                        PostChat(Spielers(source).Name & " arrived!", Color.White)
                        SendPlayerArrived(source)
                    Case "c"c 'Sent chat message
                        Dim text As String = element.Substring(2)
                        If source = 9 Then
                            PostChat("[Guest]: " & text, Color.Gray)
                            SendChatMessage(source, text)
                        Else
                            PostChat("[" & Spielers(source).Name & "]: " & text, playcolor(source))
                            SendChatMessage(source, text)
                        End If
                    Case "e"c 'Suspend gaem
                        If Spielers(source).Typ = SpielerTyp.None Then Continue For
                        Spielers(source).Bereit = False
                        PostChat(Spielers(source).Name & " left!", Color.White)
                        If Not StopUpdating And Status <> CardGameState.SpielZuEnde And Status <> CardGameState.WarteAufOnlineSpieler Then PostChat("The game is being suspended!", Color.White)
                        If Status <> CardGameState.WarteAufOnlineSpieler Then StopUpdating = True
                        'If Renderer.BeginTriggered Then StopWhenRealStart = True

                        SendPlayerLeft(source)
                    Case "f"c
                        FreePerson(source)
                    Case "g"c
                        Dim tx As String = element.Substring(2)
                        Dim dat = Newtonsoft.Json.JsonConvert.DeserializeObject(Of (Vector3, Vector3, Vector3, PlayerStatus))(tx)
                        Spielers(source).Location = dat.Item1
                        Spielers(source).Direction = dat.Item2
                        Spielers(source).ThreeDeeVelocity = dat.Item3
                        Spielers(source).RunningMode = dat.Item4
                        LocalClient.WriteStream("g" & source.ToString & tx)
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(2)
                        PostChat(msg, Color.White)
                    Case "p"c 'Player pressed
                        Dim who As Integer = element(2).ToString
                        If who = UserIndex Then
                            'Local player was touched by online player
                            If Spielers(who).Mode = PlayerMode.Chased And Spielers(source).Mode = PlayerMode.Chaser And Not PrisonPeople.Contains(who) Then EgoPlayer.Entity.Position = CommonPlayer.PlayerSpawn : EgoPlayer.PrisonEnabled = True : PrisonPeople.Add(who)
                        Else
                            'Online player touched online player
                            SendPlayerPressed(who, source)
                            If Spielers(who).Mode = PlayerMode.Chased And Spielers(source).Mode = PlayerMode.Chaser And Not PrisonPeople.Contains(who) Then PrisonPeople.Add(who)
                        End If
                    Case "r"c 'Player is back
                        Dim txt As String() = element.Substring(2).Split("|")
                        Spielers(source).Name = txt(0)
                        Spielers(source).MOTD = txt(1)
                        Spielers(source).ID = txt(2)
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " is back!", Color.White)
                        SendPlayerBack(source)
                        'Check if players are still missing, if not, send the signal to continue the game
                        Dim everythere As Boolean = True
                        For Each pl In Spielers
                            If Not pl.Bereit Then everythere = False
                        Next
                        If everythere And Status <> CardGameState.WarteAufOnlineSpieler Then StopUpdating = False : SendGameActive()
                    Case "y"c
                        SendSync()
                End Select
            Next
        End Sub

        ' ---Methoden um Daten via den Server an die Clients zu senden---
        Private Sub SendPlayerArrived(index As Integer)
            SendNetworkMessageToAll("a" & index.ToString & CInt(Spielers(index).Mode).ToString & Spielers(index).Name & "|" & Spielers(index).MOTD)
        End Sub
        Private Sub SendBeginGaem()
            Dim appendix As String = ""
            For i As Integer = 0 To Spielers.Length - 1
                If Spielers(i).Typ = SpielerTyp.Local Or Spielers(i).Typ = SpielerTyp.CPU Then appendix &= i.ToString
            Next
            SendNetworkMessageToAll("b" & appendix)
            SendSync()
        End Sub
        Private Sub SendChatMessage(index As Integer, text As String)
            SendNetworkMessageToAll("c" & index.ToString & text)
        End Sub
        Private Sub SendDrawCard(card As Card)
            LocalClient.WriteStream("d" & CInt(card.Suit).ToString & CInt(card.Type).ToString)
        End Sub
        Private Sub SendPlayerLeft(index As Integer)
            LocalClient.WriteStream("e" & index)
        End Sub

        Private Sub SendFreed(indx As Integer)
            LocalClient.WriteStream("f" & indx.ToString)
        End Sub
        Private Sub SendHighscore()
            'Dim pls As New List(Of (String, Integer))
            'For i As Integer = 0 To Spielers.Length - 1
            '    If Spielers(i).Typ = SpielerTyp.Local Or Spielers(i).Typ = SpielerTyp.Online Then
            '        pls.Add((Spielers(i).Name, GetScore(i)))
            '    End If
            'Next
            'SendNetworkMessageToAll("h" & 0.ToString & Newtonsoft.Json.JsonConvert.SerializeObject(pls))
        End Sub
        Private Sub SendPlayerData()
            Dim element = Spielers(UserIndex)

            SyncPosCounter += Time.DeltaTime
            If SyncPosCounter > SyncLoc Then
                SyncPosCounter = 0
                LocalClient.WriteStream("g" & UserIndex.ToString & Newtonsoft.Json.JsonConvert.SerializeObject((element.Location, element.Direction, element.ThreeDeeVelocity, element.RunningMode)))
            End If
        End Sub
        Private Sub SendKick(player As Integer, figur As Integer)
            SendNetworkMessageToAll("k" & player.ToString & figur.ToString)
        End Sub
        Private Sub SendGameClosed()
            SendNetworkMessageToAll("l")
        End Sub
        Private Sub SendMessage(msg As String)
            SendNetworkMessageToAll("m" & msg)
        End Sub
        Private Sub SendNewPlayerActive(who As Integer)
            SendNetworkMessageToAll("n" & who.ToString)
        End Sub
        Private Sub SendPlayerPressed(ID As String) Implements IGameWindow.PlayerPressed
            For i As Integer = 0 To Spielers.Length - 1
                If Spielers(i).ID = ID And i <> UserIndex Then
                    SendNetworkMessageToAll("p" & i.ToString & UserIndex.ToString)
                    Exit For
                End If
            Next
        End Sub

        Private Sub SendPlayerPressed(index As Integer, source As Integer)
            SendNetworkMessageToAll("p" & index.ToString & source.ToString)
        End Sub
        Private Sub SendPlayerBack(index As Integer)
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject((Spielers(index).Mode, Spielers(index).Location))
            SendNetworkMessageToAll("r" & index.ToString & str)
        End Sub
        Private Sub SendWinFlag()
            SendSync()
            SendNetworkMessageToAll("w")
        End Sub
        Private Sub SendGameActive()
            SendNetworkMessageToAll("x")
        End Sub

        Private Sub SendSync()
            Dim lst As SyncMessage() = New SyncMessage(Spielers.Length - 1) {}
            For i As Integer = 0 To Spielers.Length - 1
                lst(i) = New SyncMessage(Spielers(i).Name, Spielers(i).MOTD, Spielers(i).ID, Spielers(i).Typ, Spielers(i).Mode)
            Next
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(lst)
            SendNetworkMessageToAll("y" & str)
        End Sub

        Private Function GetPlayerAudio(i As Integer, IsB As Boolean, ByRef txt As String) As IdentType
            txt = ""
            Dim ret As IdentType
            Select Case Spielers(i).Typ
                Case SpielerTyp.Local
                    If IsB Then
                        ret = My.Settings.SoundB
                        If ret = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache\client\soundB.audio")))
                    Else
                        ret = My.Settings.SoundA
                        If ret = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache\client\soundA.audio")))
                    End If
                Case SpielerTyp.CPU
                    Select Case i
                        Case 0
                            ret = IdentType.Custom
                            txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Content\prep\tele.wav")))
                        Case Else
                            ret = If(IsB, IdentType.TypeA, IdentType.TypeB)
                    End Select
            End Select
            Return ret
        End Function

        Private Sub SendNetworkMessageToAll(message As String)
            If NetworkMode Then LocalClient.WriteStream(message)
        End Sub
#End Region

#Region "Hilfsfunktionen"
        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

        Private Sub FreePerson(by As Integer)
            If Spielers(by).Mode = PlayerMode.Chased And Not PrisonPeople.Contains(by) And PrisonPeople.Count > 0 And False Then
                Dim freed As Integer = PrisonPeople(0) : PrisonPeople.RemoveAt(0)
                If freed = UserIndex Then
                    EgoPlayer.PrisonEnabled = False
                Else
                    SendFreed(freed)
                End If
            End If
        End Sub

        Private Sub ActivateGame()
            AdditionalHUDRend.TriggerStartAnimation({"5", "4", "3", "2", "1", "Go!"}, Sub()
                                                                                          Crosshair.Enabled = True
                                                                                          EgoPlayer.PrisonEnabled = False
                                                                                          Status = GameStatus.GameActive
                                                                                          HUDSprintBar.Active = True
                                                                                      End Sub)
        End Sub
#End Region

#Region "Knopfgedrücke"
        Private Sub ExitButton() Handles HUDBtnA.Clicked
            If Microsoft.VisualBasic.MsgBox("Do you really want to leave?", Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                SFX(2).Play()
                SendGameClosed()
                NetworkMode = False
                Core.Exit()
            End If
        End Sub

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked

            LaunchInputBox(Sub(x)
                               SendChatMessage(UserIndex, x)
                               PostChat("[" & Spielers(UserIndex).Name & "]: " & x, hudcolors(UserIndex))
                           End Sub, ChatFont, "Enter your message: ", "Send message")
        End Sub
        Private Sub VolumeButton() Handles HUDMusicBtn.Clicked
            MediaPlayer.Volume = If(MediaPlayer.Volume > 0F, 0F, 0.1F)
        End Sub
        Private Sub FullscrButton() Handles HUDFullscrBtn.Clicked
            Screen.IsFullscreen = Not Screen.IsFullscreen
            Screen.ApplyChanges()
        End Sub
        Private Sub MenuButton() Handles HUDBtnB.Clicked
            If Microsoft.VisualBasic.MsgBox("Do you really want to leave?", Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                SFX(2).Play()
                SendGameClosed()
                NetworkMode = False
                Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
            End If
        End Sub

#End Region

#Region "Schnittstellenimplementation"
        Private ReadOnly Property IGameWindow_EgoPlayer As EgoPlayer Implements IGameWindow.EgoPlayer
            Get
                Return EgoPlayer
            End Get
        End Property

        Private ReadOnly Property IGameWindow_Spielers As CommonPlayer() Implements IGameWindow.Spielers
            Get
                Return Spielers
            End Get
        End Property

        Private ReadOnly Property IGameWindow_UserIndex As Integer Implements IGameWindow.UserIndex
            Get
                Return UserIndex
            End Get
        End Property
#End Region



    End Class
End Namespace