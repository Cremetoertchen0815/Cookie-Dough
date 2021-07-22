Imports System.Collections.Generic
Imports Cookie_Dough.Framework.UI
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

        'Gameplay fields
        Friend OtherSpielers As List(Of OtherPlayer)
        Friend EgoPlayer As EgoPlayer
        Friend SpielerIndex As Integer = 0
        Friend UserIndex As Integer = 0
        Friend PlayerIndexIndex As Integer
        Friend PlayerIndexList As Integer() = {0}
        Friend StopUpdating As Boolean = False
        Friend NetworkMode As Boolean = False
        Friend Map As Map = Map.Classic
        Friend Status As GameStatus
        Friend CanStart As Boolean = False
        Friend WaitingTimeFlag As Boolean = False
        Private lastmstate As MouseState



        '3D movement & interaction
        Friend Colliders As BoundingBox()
        Friend ObjectHandler As Object3DHandler
        Friend Table As Table
        Friend Crosshair As CrosshairRenderable

        'Assets & rendering
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont
        Private Renderer As Renderer3D
        Private MinimapRenderer As RenderLayerRenderer

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtnA As Controls.Button
        Private WithEvents HUDBtnB As Controls.Button
        'Private WithEvents HUDBtnC As Controls.Button
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

        Public Sub New()
            Chat = New List(Of (String, Color))
            SpielerIndex = -1
            PlayerIndexIndex = -1
            SwitchPlayer()
            Status = GameStatus.WaitingForOnlinePlayers
            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            'If LocalClient.Connected Then
            '    Dim name As String = ""

            '    LaunchInputBox(Sub(x) Networking.ExtGame.CreateGame(LocalClient, x), ChatFont, "Enter a name for the round:", "Start Round")
            '    NetworkMode = True
            'Else
            '    NetworkMode = False
            '    Microsoft.VisualBasic.MsgBox("Client not connected!")
            'End If

        End Sub

        Public Overrides Sub Unload()
            Framework.Networking.Client.OutputDelegate = Sub(x) Return
        End Sub

        Public Overrides Sub Initialize()
            MyBase.Initialize()

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
            Renderer.GenerateMapMatrices(TileMap)

            'Load minimap renderer
            MinimapRenderer = AddRenderer(New RenderLayerRenderer(0, 5) With {.RenderTexture = New Textures.RenderTexture, .RenderTargetClearColor = Color.Transparent})
            CreateEntity("minimap").SetScale(0.4).SetPosition(New Vector2(1500, 700)).AddComponent(New TargetRendererable(MinimapRenderer))

            'Load players
            OtherSpielers = New List(Of OtherPlayer)

            EgoPlayer = CreateEntity("EgoPlayer").AddComponent(New EgoPlayer(TileMap))
            CreateEntity("Map").AddComponent(New TiledMapRenderer(TileMap, "Collision")).SetRenderLayer(5)

            'Create entities and components
            'AddSceneComponent(New Object3DHandler(Spielers(UserIndex), Me))
            Crosshair = CreateEntity("crosshair").AddComponent(Of CrosshairRenderable)()
            Table = CreateEntity("table").AddComponent(Of Table)()

            'Load HUD
            HUD = New GuiSystem()
            HUDBtnA = New Controls.Button("Exit Game", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnA)
            HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnB)
            'HUDBtnC = New Controls.Button("Anger", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnC)
            HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Controls.Label("Click on the totem to start the game...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDFullscrBtn = New Controls.Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Controls.Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)

            'Set colliders
            Colliders = {Table.BoundingBox}
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
                EgoPlayer.Enabled = Not EgoPlayer.Enabled
                Core.Instance.IsMouseVisible = Not EgoPlayer.Enabled
                Crosshair.Enabled = EgoPlayer.Enabled
            End If

            'Check if game can be started
            If Status = GameStatus.WaitingForOnlinePlayers Then
                CanStart = True
                For i As Integer = 1 To OtherSpielers.Count - 1
                    If Not OtherSpielers(i).Bereit Then CanStart = False : Exit For
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


            If CType(Core.Instance, Game1).GetStackKeystroke({Keys.F, Keys.O, Keys.V}) Then Renderer.Projection = Matrix.CreatePerspectiveFieldOfView(3, CSng(Core.Instance.Window.ClientBounds.Width) / CSng(Core.Instance.Window.ClientBounds.Height), 0.01, 500)

            'Set HUD color
            HUDColor = playcolor(UserIndex)
            HUDInstructions.Active = Status <> GameStatus.GameActive OrElse (OtherSpielers(SpielerIndex).Typ = SpielerTyp.Local)

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
                        OtherSpielers.Add(New OtherPlayer(SpielerTyp.Online))
                        OtherSpielers(source).Name = element.Substring(2)
                        OtherSpielers(source).Bereit = True
                        PostChat(OtherSpielers(source).Name & " arrived!", Color.White)
                        SendPlayerArrived(source, OtherSpielers(source).Name)
                    Case "c"c 'Sent chat message
                        Dim text As String = element.Substring(2)
                        PostChat("[" & OtherSpielers(source).Name & "]: " & text, playcolor(source))
                        SendChatMessage(source, text)
                    Case "e"c 'Suspend gaem
                        If Status <> GameStatus.WaitingForOnlinePlayers Then StopUpdating = True
                        PostChat(OtherSpielers(source).Name & " left!", Color.White)
                        PostChat("The game is being suspended!", Color.White)
                        SendPlayerLeft(source)
                    Case "g"c
                        Dim txt As Vector3() = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Vector3())(element.Substring(2))
                        Dim user = OtherSpielers(source)
                        user.Location = txt(0)
                        user.Direction = txt(1)
                    Case "n"c
                        SwitchPlayer()
                    Case "r"c 'Player is back
                        OtherSpielers(source).Bereit = True
                        PostChat(OtherSpielers(source).Name & " is back!", Color.White)
                        SendPlayerBack(source)
                        StopUpdating = False
                        If SpielerIndex = source Then SendNewPlayerActive(SpielerIndex)

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
        Private Sub SendPlayerMoved(index As Integer, position As Vector3, direction As Vector3)
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject({position, direction})
            LocalClient.WriteStream("g" & index.ToString & str)
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
        Friend Sub SendCardPlaced(who As Integer, card As Card)
            SendNetworkMessageToAll("p" & who.ToString & card.ToString)
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

        Friend Sub SwitchPlayer()
            PlayerIndexIndex = (PlayerIndexIndex + 1) Mod PlayerIndexList.Length
            SpielerIndex = PlayerIndexList(PlayerIndexIndex)
            SendNewPlayerActive(SpielerIndex)
            'TD: Send switch player command to clients
        End Sub
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
                               PostChat("[" & OtherSpielers(UserIndex).Name & "]: " & x, hudcolors(UserIndex))
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

    End Class
End Namespace