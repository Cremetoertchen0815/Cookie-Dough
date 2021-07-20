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
        Friend Spielers As List(Of Player)
        Friend UserIndex As Integer = 0
        Friend SpielerIndex As Integer = 0
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
        Private GameFocused As Boolean = True
        Private MovementBtn As VirtualJoystick
        Private JumpBtn As VirtualButton
        Private VerticalVel As Single
        Friend Colliders As BoundingBox()
        Friend ObjectHandler As Object3DHandler
        Friend Table As Table
        Friend Crosshair As CrosshairRenderable

        'Assets & rendering
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont
        Private Renderer As Renderer3D

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtnA As Controls.Button
        Private WithEvents HUDBtnB As Controls.Button
        'Private WithEvents HUDBtnC As Controls.Button
        Private WithEvents HUDChat As Controls.TextscrollBox
        Private WithEvents HUDChatBtn As Controls.Button
        Private WithEvents HUDInstructions As Controls.Label
        Private WithEvents HUDNameBtn As Controls.Button
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
        Private Const MouseSensivity As Single = 232
        Private Const Speed As Single = 12
        Private Const JumpHeight As Single = 20
        Private Const Gravity As Single = 65

        Public Sub New()
            Chat = New List(Of (String, Color))
            SpielerIndex = -1
            PlayerIndexIndex = -1
            SwitchPlayer()
            Status = GameStatus.WaitingForOnlinePlayers
            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            If LocalClient.Connected Then
                Dim name As String = ""

                LaunchInputBox(Sub(x) Networking.ExtGame.CreateGame(LocalClient, x), ChatFont, "Enter a name for the round:", "Start Round")
                NetworkMode = True
            Else
                NetworkMode = False
                Microsoft.VisualBasic.MsgBox("Client not connected!")
            End If

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
            AddRenderer(New PsygroundRenderer(0, 0.85F))
            Renderer = AddRenderer(New Renderer3D(Me, 1))
            AddRenderer(New DefaultRenderer(2))
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.55F).SetThreshold(0.45F)

            'Load Map
            TileMap = Content.LoadTiledMap("Maps\Barrelled\" & Map.ToString & ".tmx")
            Player.Map = TileMap
            Renderer.GenerateMapMatrices(TileMap)


            'Load players
            Spielers = New List(Of Player)
            Spielers.AddRange({New Player(SpielerTyp.Local), New Player(SpielerTyp.CPU) With {.Location = New Vector3(-6, 0, 0), .Direction = Vector3.Right},
                               New Player(SpielerTyp.CPU) With {.Location = New Vector3(0, 0, -6), .Direction = Vector3.Forward}, New Player(SpielerTyp.CPU) With {.Location = New Vector3(6, 0, 0), .Direction = Vector3.Left}})

            CreateEntity("Player0").AddComponent(Spielers(0))

            'Create entities and components
            AddSceneComponent(New Object3DHandler(Spielers(UserIndex), Me))
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
            HUDNameBtn = New Controls.Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Yellow} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Controls.Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Controls.Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)

            'Assign virtual buttons
            MovementBtn = New VirtualJoystick(True, New VirtualJoystick.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D, Keys.W, Keys.S))
            JumpBtn = New VirtualButton(New VirtualButton.KeyboardKey(Keys.Space))

            'Set colliders
            Colliders = {Table.BoundingBox}
        End Sub

        Public Overrides Sub OnStart()
            MyBase.OnStart()


        End Sub

        Public Overrides Sub Update()
            MyBase.Update()

            If StopUpdating Then Return

            Dim user As Player = Spielers(0)
            Dim mstate As MouseState = Mouse.GetState
            Dim SPEEEN As New Vector3
            Dim delta As Single = Math.Min(Time.DeltaTime, 0.1F) ' Limit the max. delta time, so the player can't clip through collision

            'Apply gravity
            VerticalVel += Gravity * delta


            'Grab jump
            If JumpBtn.IsPressed And GameFocused Then VerticalVel = -JumpHeight

            'Calculate 3D shit for user
            With user
                'Get horizontal movement vector
                Dim movDir As Vector3 = user.Direction
                movDir.Y = 0
                movDir.Normalize()

                'Move player
                If GameFocused Then 'When focussed
                    SPEEEN += MovementBtn.Value.Y * movDir * Speed * delta
                    SPEEEN += MovementBtn.Value.X * Vector3.Cross(Vector3.Up, movDir) * New Vector3(Speed, 0, Speed) * delta
                    If .Location.Y <= 0 And Not JumpBtn.IsPressed Then .Location = New Vector3(.Location.X, 0, .Location.Z) : VerticalVel = 0
                    SPEEEN += New Vector3(0, VerticalVel * delta, 0)
                Else 'When unfocussed
                    If .Location.Y <= 0 And Not JumpBtn.IsPressed Then .Location = New Vector3(.Location.X, 0, .Location.Z) : VerticalVel = 0
                    SPEEEN += New Vector3(0, VerticalVel * delta, 0)
                End If

                'Collision check with Colliders
                Dim checkX As New BoundingBox(.Location + New Vector3(-2 - SPEEEN.X, 0, -2), .Location + New Vector3(2 - SPEEEN.X, 5, 2))
                Dim checkY As New BoundingBox(.Location + New Vector3(-2, 0 - VerticalVel * delta, -2), .Location + New Vector3(2, 5 - VerticalVel * delta, 2))
                Dim checkZ As New BoundingBox(.Location + New Vector3(-2, 0, -2 - SPEEEN.Z), .Location + New Vector3(2, 5, 2 - SPEEEN.Z))
                For Each cl In Colliders
                    If checkX.Intersects(cl) Then SPEEEN.X = 0
                    If checkY.Intersects(cl) Then SPEEEN.Y = 0 : VerticalVel = 0
                    If checkZ.Intersects(cl) Then SPEEEN.Z = 0
                Next
                .Location = .Location - SPEEEN

                'Clamp position
                .Location = New Vector3(.Location.X, Mathf.Clamp(.Location.Y, 0, 6), .Location.Z)

                'Generate view matrix and ray
                Dim camShift As Vector3 = Spielers(UserIndex).Direction : camShift.Y = 0 : camShift.Normalize() : camShift *= 0.5
                Dim campos As Vector3 = Spielers(UserIndex).Location + camShift + New Vector3(0, 5.5, 0)
                Renderer.View = Matrix.CreateLookAt(campos, campos + Spielers(UserIndex).Direction, Vector3.Up)

                'Calculate direction from mouse
                If Core.Instance.IsActive And GameFocused Then
                    Dim nudirection As Vector3 = .Direction
                    nudirection = Vector3.Transform(nudirection, Matrix.CreateFromAxisAngle(Vector3.Up, (-MathHelper.PiOver4 / MouseSensivity) * (mstate.X - lastmstate.X)))
                    nudirection = Vector3.Transform(nudirection, Matrix.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, nudirection), (MathHelper.PiOver4 / MouseSensivity) * (mstate.Y - lastmstate.Y)))
                    nudirection.Normalize()
                    .Direction = nudirection

                    Dim pos = Core.Instance.Window.ClientBounds.Size
                    Mouse.SetPosition(CInt(pos.X / 2), CInt(pos.Y / 2))
                End If

                If NetworkMode Then SendPlayerMoved(UserIndex, .Location, .Direction)
            End With

            'Focus/Unfocus game
            If mstate.RightButton = ButtonState.Pressed And lastmstate.RightButton = ButtonState.Released Then
                GameFocused = Not GameFocused
                Core.Instance.IsMouseVisible = Not GameFocused
                Crosshair.Enabled = GameFocused
            End If

            'Check if game can be started
            If Status = GameStatus.WaitingForOnlinePlayers Then
                CanStart = True
                For i As Integer = 1 To Spielers.Count - 1
                    If Not Spielers(i).Bereit Then CanStart = False : Exit For
                Next
            Else
                CanStart = False
            End If

            'Network stuff
            If Not LocalClient.Connected Or LocalClient.LeaveFlag Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("Connection lost! Game was ended!")
                Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
                NetworkMode = False
            End If
            ReadAndProcessInputData()

            'Set HUD color
            HUDColor = playcolor(UserIndex)
            HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name, "")
            HUDInstructions.Active = Status <> GameStatus.GameActive OrElse (Spielers(SpielerIndex).Typ = SpielerTyp.Local)

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
                        If Spielers.Count <> source Then Console.WriteLine("ALAAAARRRRM! ALAAAAAAARRRRRM!") : Return
                        Spielers.Add(New Player(SpielerTyp.Online))
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
                        Dim user As Player = Spielers(source)
                        user.Location = txt(0)
                        user.Direction = txt(1)
                    Case "n"c
                        SwitchPlayer()
                    Case "r"c 'Player is back
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " is back!", Color.White)
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

    End Class
End Namespace