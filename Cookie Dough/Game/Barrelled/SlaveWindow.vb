﻿Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
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
    Public Class SlaveWindow
        Inherits Scene
        Implements IGameWindow

        'Gameplay fields
        Friend Spielers As CommonPlayer()
        Friend EgoPlayer As EgoPlayer
        Friend Rejoin As Boolean = False
        Friend UserIndex As Integer = 0
        Friend PlCount As Integer = 2
        Friend PlayerIndexIndex As Integer
        Friend PlayerIndexList As Integer() = {0}
        Friend StopUpdating As Boolean = False
        Friend NetworkMode As Boolean = False
        Friend Map As Map = Map.Mainland
        Friend Status As GameStatus
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Friend Difficulty As Difficulty 'Declares the difficulty of the CPU
        Friend CanStart As Boolean = False
        Friend WaitingTimeFlag As Boolean = False
        Private lastmstate As MouseState



        '3D movement & interaction
        Friend Colliders As BoundingBox()
        Friend ObjectHandler As Object3DHandler
        Friend Crosshair As CrosshairRenderable
        Friend PlayerSpawn As Vector2

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
        Private WithEvents HUDInstructions As Controls.Label
        Private WithEvents HUDFullscrBtn As Controls.Button
        Private WithEvents HUDMusicBtn As Controls.Button
        Private InstructionFader As ITween(Of Color)
        Private ShowDice As Boolean = False
        Private HUDColor As Color
        Private Chat As List(Of (String, Color))
        Private InputBoxFlag As Boolean = False

        'Map
        Public TileMap As TmxMap

        'Constants
        Private Const WaitinTime As Integer = 1500

        Sub New(ins As OnlineGameInstance)
            LocalClient.AutomaticRefresh = False
            NetworkMode = False

            If Not LocalClient.JoinGame(ins, Sub(x)
                                                 'Load map info
                                                 Map = CInt(x())
                                                 PlCount = GetMapSize(Map)
                                                 GameMode = If(CBool(x()), GameMode.Casual, GameMode.Competetive)

                                                 'Load player info
                                                 ReDim Spielers(PlCount - 1)
                                                 UserIndex = CInt(x())
                                                 For i As Integer = 0 To PlCount - 1
                                                     Dim type As SpielerTyp = CInt(x())
                                                     Dim name As String = x()
                                                     If i <> UserIndex Then
                                                         Spielers(i) = New OtherPlayer(If(type = SpielerTyp.None, type, SpielerTyp.Online)) With {.Name = name}
                                                     Else
                                                         Spielers(i) = New EgoPlayer(SpielerTyp.Online) With {.Name = My.Settings.Username}
                                                     End If
                                                 Next

                                                 'Set rejoin flag
                                                 Rejoin = x() = "Rejoin"
                                             End Sub) Then LocalClient.AutomaticRefresh = True : Return

            'Bereite Flags und Variablen vor

            Chat = New List(Of (String, Color))
            PlayerIndexIndex = -1
            PlCount = GetMapSize(Map)
            Status = GameStatus.WaitingForOnlinePlayers
            Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            LoadContent()
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
            AddRenderer(New PsygroundRenderer(1, 0.85F))
            Renderer = AddRenderer(New Renderer3D(Me, 2))
            AddRenderer(New RenderLayerExcludeRenderer(3, 5))
            'AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.55F).SetThreshold(0.45F)

            'Load Map
            TileMap = Content.LoadTiledMap("Maps\Barrelled\" & Map.ToString & ".tmx")
            CommonPlayer.CollisionLayers = {TileMap.GetLayer(Of TmxLayer)("Collision"), TileMap.GetLayer(Of TmxLayer)("High")}
            Renderer.GenerateMapMatrices(TileMap)
            Renderer.Floorsize = New Vector2(TileMap.Properties("floor_size_X"), TileMap.Properties("floor_size_Y"))
            For Each element In TileMap.GetObjectGroup("Objects").Objects
                Select Case element.Type
                    Case "spawn"
                        PlayerSpawn = New Vector2(element.X, element.Y)
                End Select
            Next

            'Load minimap renderer
            MinimapRenderer = AddRenderer(New RenderLayerRenderer(0, 5) With {.RenderTexture = New Textures.RenderTexture, .RenderTargetClearColor = Color.Transparent})
            CreateEntity("minimap").SetScale(0.4).SetPosition(New Vector2(1500, 700)).AddComponent(New TargetRendererable(MinimapRenderer))

            EgoPlayer = CreateEntity("EgoPlayer").SetPosition(PlayerSpawn).AddComponent(Spielers(UserIndex))
            CreateEntity("Map").AddComponent(New TiledMapRenderer(TileMap, "Collision")).SetRenderLayer(5)

            'Create entities and components
            'AddSceneComponent(New Object3DHandler(Spielers(UserIndex), Me))
            Crosshair = CreateEntity("crosshair").AddComponent(Of CrosshairRenderable)().SetRenderLayer(6)

            'Load HUD
            HUD = New GuiSystem()
            HUDBtnA = New Controls.Button("Exit Game", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnA)
            HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnB)
            HUDSprintBar = New Controls.ProgressBar(New Vector2(500, 100), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .Progress = Function() EgoPlayer.SprintLeft} : HUD.Controls.Add(HUDSprintBar)
            HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Controls.Label("Click on the totem to start the game...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDFullscrBtn = New Controls.Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Controls.Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)

            'Set colliders
            Colliders = {}
        End Sub

        Public Overrides Sub OnStart()
            MyBase.OnStart()

        End Sub

        Public Overrides Sub Update()
            MyBase.Update()

            If StopUpdating Then Return

            Dim mstate As MouseState = Mouse.GetState

            Renderer.View = Matrix.CreateLookAt(EgoPlayer.CameraPosition, EgoPlayer.CameraPosition + EgoPlayer.Direction, Vector3.Up)


            'If NetworkMode Then SendPlayerMoved(UserIndex, EgoPlayer.GetLocation, EgoPlayer.Direction)

            'Focus/Unfocus game
            If mstate.RightButton = ButtonState.Pressed And lastmstate.RightButton = ButtonState.Released Then
                EgoPlayer.Focused = Not EgoPlayer.Focused
                Core.Instance.IsMouseVisible = Not EgoPlayer.Focused
                Crosshair.Enabled = EgoPlayer.Focused
            End If

            'Check if game can be started
            If Status = GameStatus.WaitingForOnlinePlayers Then
                CanStart = True
                For i As Integer = 1 To Spielers.Length - 1
                    If Not Spielers(i).Bereit Then CanStart = False : Exit For
                Next
            Else
                CanStart = False
            End If

            'Network stuff
            If NetworkMode And (Not LocalClient.Connected Or LocalClient.LeaveFlag) Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("Connection lost! Game was ended!")
                Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
                NetworkMode = False
            End If
            ReadAndProcessInputData()


            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> CardGameState.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
                If LocalClient.LeaveFlag And Status <> CardGameState.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Disconnected! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
            End If

            If NetworkMode Then ReadAndProcessInputData()

            'FOVVVVVVVVVVVVV
            If CType(Core.Instance, Game1).GetStackKeystroke({Keys.F, Keys.O, Keys.V}) Then fov = Math.Min(Math.PI - 0.001F, fov + 0.2) : Renderer.Projection = Matrix.CreatePerspectiveFieldOfView(fov, CSng(Core.Instance.Window.ClientBounds.Width) / CSng(Core.Instance.Window.ClientBounds.Height), 0.01, 500)

            'Set HUD color
            HUDColor = playcolor(UserIndex)
            HUDInstructions.Active = Status <> GameStatus.GameActive OrElse (Spielers(UserIndex).Typ = SpielerTyp.Local)

            lastmstate = Mouse.GetState
        End Sub

#Region "Netzwerkfunktionen"
        ''' <summary>
        ''' Liest die Daten aus dem Stream des Servers
        ''' </summary>
        Private Sub ReadAndProcessInputData()
            'If MoveActive Then Return

            Dim data As String() = LocalClient.ReadStream()
            For Each element In data
                Dim source As Integer = CInt(element(0).ToString)
                Dim command As Char = element(1)
                Select Case command
                    Case "a"c 'Player arrived
                        CreateEntity(element.Substring(2)).AddComponent(Spielers(source))
                        Spielers(source).Name = element.Substring(2)
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " arrived!", Color.White)
                        SendPlayerArrived(source, Spielers(source).Name)
                    Case "c"c 'Sent chat message
                        Dim text As String = element.Substring(2)
                        PostChat("[" & Spielers(source).Name & "]: " & text, playcolor(source))
                        SendChatMessage(source, text)
                    Case "e"c 'Suspend gaem
                        If Status <> GameStatus.WaitingForOnlinePlayers Then StopUpdating = True
                        PostChat(Spielers(source).Name & " left!", Color.White)
                        PostChat("The game is being suspended!", Color.White)
                        SendPlayerLeft(source)
                    Case "g"c
                        Dim txt As Vector3() = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Vector3())(element.Substring(2))
                        Dim user = Spielers(source)
                        user.Location = txt(0)
                        user.Direction = txt(1)
                    Case "r"c 'Player is back
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " is back!", Color.White)
                        SendPlayerBack(source)
                        StopUpdating = False

                End Select
            Next
        End Sub

        ' ---Methoden um Daten via den Server an die Clients zu senden---
        Private Sub SendPlayerArrived(index As Integer, name As String)
            SendNetworkMessageToAll("a" & index.ToString & name)
        End Sub
        Private Sub SendBeginGaem()
            SendNetworkMessageToAll("b")
        End Sub
        Private Sub SendChatMessage(index As Integer, text As String)
            SendNetworkMessageToAll("c" & index.ToString & text)
        End Sub
        Private Sub SendPlayerLeft(index As Integer)
            LocalClient.WriteStream("e" & index)
        End Sub
        Private Sub SendGameClosed()
            SendNetworkMessageToAll("l")
        End Sub
        Private Sub SendMessage(msg As String)
            SendNetworkMessageToAll("m" & msg)
        End Sub
        Private Sub SendPlayerBack(index As Integer)
            'Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers, SaucerFields))
            'SendNetworkMessageToAll("r" & index.ToString & str)
        End Sub
        Private Sub SendWinFlag()
            SendNetworkMessageToAll("w")
        End Sub

        Private Sub SendNetworkMessageToAll(message As String)
            If NetworkMode Then LocalClient.WriteStream(message)
        End Sub
#End Region
        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

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
#End Region
    End Class
End Namespace