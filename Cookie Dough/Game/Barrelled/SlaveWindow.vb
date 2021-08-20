Imports System.Collections.Generic
Imports System.IO
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Game.Barrelled.Networking
Imports Cookie_Dough.Game.Barrelled.Players
Imports Cookie_Dough.Game.Barrelled.Renderers
Imports Cookie_Dough.Game.Common
Imports Cookie_Dough.Menu.MainMenu
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
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
        Friend WaitingTimeFlag As Boolean = False
        Private lastmstate As MouseState

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

        Public Sub New(ins As OnlineGameInstance)
            LocalClient.AutomaticRefresh = False
            NetworkMode = False

            If Not LocalClient.JoinGame(ins, Sub(x)
                                                 'Load map info
                                                 Map = CInt(x())
                                                 PlCount = GetMapSize(Map)
                                                 GameMode = If(x(), GameMode.Casual, GameMode.Competetive)

                                                 'Load player info
                                                 ReDim Spielers(PlCount - 1)
                                                 UserIndex = CInt(x())
                                                 For i As Integer = 0 To PlCount - 1
                                                     Dim readde As Boolean = x() = 1 Or i = 0 'Is player already joined to the game
                                                     Dim type As SpielerTyp = CInt(x())
                                                     Dim name As String = x()
                                                     If i <> UserIndex Then
                                                         Spielers(i) = New OtherPlayer(If(type = SpielerTyp.None, type, SpielerTyp.Online)) With {.Name = name, .Bereit = readde}
                                                         If readde Then CreateEntity(Spielers(i).Name).AddComponent(Spielers(i))
                                                     Else
                                                         Spielers(i) = New EgoPlayer(SpielerTyp.Online) With {.Name = My.Settings.Username}
                                                         EgoPlayer = CreateEntity("EgoPlayer").AddComponent(Spielers(UserIndex))
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
            NetworkMode = True

            LoadContent()
        End Sub

        Public Overrides Sub Unload()
            Framework.Networking.Client.OutputDelegate = Sub(x) Return
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ChatText"))

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
            Dim minimapRect As RectangleF = RectangleF.Empty
            TileMap = Content.LoadTiledMap("Maps/Barrelled/" & Map.ToString & ".tmx")
            CommonPlayer.CollisionLayers = {TileMap.GetLayer(Of TmxLayer)("Collision"), TileMap.GetLayer(Of TmxLayer)("High")}
            Renderer.GenerateMapMatrices(TileMap)
            Renderer.Floorsize = New Vector2(TileMap.Properties("floor_size_X"), TileMap.Properties("floor_size_Y"))
            For Each element In TileMap.GetObjectGroup("Objects").Objects
                Select Case element.Type
                    Case "spawn"
                        CommonPlayer.PlayerSpawn = New Vector2(element.X, element.Y)
                    Case "prison"
                        CommonPlayer.PrisonPosition = New Rectangle(element.X, element.Y, element.Width, element.Height)
                        BarrierRectangle = New RectangleF(element.Properties("barr_X").Replace("."c, ","c), element.Properties("barr_Y").Replace("."c, ","c), element.Properties("barr_Width").Replace("."c, ","c), element.Properties("barr_Height").Replace("."c, ","c))
                    Case "minimap"
                        minimapRect = New RectangleF(New Vector2(element.X, element.Y), New Vector2(CSng(element.Properties("scale").Replace("."c, ","c))))
                End Select
            Next

            'Load minimap renderer
            MinimapRenderer = AddRenderer(New RenderLayerRenderer(0, 5) With {.RenderTexture = New Textures.RenderTexture, .RenderTargetClearColor = Color.Transparent})
            AdditionalHUDRend = CreateEntity("addition_render").SetPosition(minimapRect.Location).SetScale(minimapRect.Size).AddComponent(New AdditionalHUDRendererable(MinimapRenderer))
            CreateEntity("Map").AddComponent(New TiledMapRenderer(TileMap, "Collision")).SetRenderLayer(5)

            'Create entities and components
            'AddSceneComponent(New Object3DHandler(Spielers(UserIndex), Me))
            Crosshair = CreateEntity("crosshair").AddComponent(Of CrosshairRenderable)().SetRenderLayer(6)

            'Load HUD
            HUD = New GuiSystem() With {.Color = PlayerHUDColors(PlayerMode.Ghost)}
            HUDBtnA = New Controls.Button("Exit Game", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnA)
            HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnB)
            HUDSprintBar = New Controls.ProgressBar(New Vector2(500, 100), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .Progress = Function() EgoPlayer.SprintLeft} : HUD.Controls.Add(HUDSprintBar)
            HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(330, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .LenLimit = 28} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDNameBtn = New Controls.Button("", New Vector2(500, 40), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDInstructions = New Controls.Label("Click on the totem to start the game...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
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
            'Add prison collider
            Dim loc = New Vector3(BarrierRectangle.X, -2, BarrierRectangle.Y)
            Dim siz = New Vector3(BarrierRectangle.Width * 2, 25, BarrierRectangle.Height * 2)
            ObjectHandler.Objects.Add(New RoomTriggerBox(New BoundingBox(loc, loc + siz), AddressOf SendRequestFreeing))
        End Sub

        Public Overrides Sub OnStart()
            MyBase.OnStart()

        End Sub

        Public Overrides Sub Update()
            MyBase.Update()

            If StopUpdating Then Return

            Dim mstate As MouseState = Mouse.GetState

            Renderer.View = Matrix.CreateLookAt(EgoPlayer.CameraPosition, EgoPlayer.CameraPosition + EgoPlayer.Direction, Vector3.Up)


            If NetworkMode Then SendPlayerData()

            'Focus/Unfocus game
            If mstate.RightButton = ButtonState.Pressed And lastmstate.RightButton = ButtonState.Released Then
                EgoPlayer.Focused = Not EgoPlayer.Focused
                Core.Instance.IsMouseVisible = Not EgoPlayer.Focused
                Crosshair.Enabled = EgoPlayer.Focused
            End If

            'Check if game can be started
            Select Case Status
                Case GameStatus.GameActive
                    'Free prisoners
                    If False Then SendRequestFreeing()
            End Select

            'Network stuff
            If NetworkMode And (Not LocalClient.Connected Or LocalClient.LeaveFlag) Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("Connection lost! Game was ended!")
                Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
                NetworkMode = False
            End If


            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> GameStatus.GameFinished Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene))
                If LocalClient.LeaveFlag And Status <> GameStatus.GameFinished Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Host left! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene))
            End If

            If NetworkMode Then ReadAndProcessInputData()

            'FOVVVVVVVVVVVVV
            If CType(Core.Instance, Game1).GetStackKeystroke({Keys.F, Keys.O, Keys.V}) Then fov = Math.Min(Math.PI - 0.001F, fov + 0.2) : Renderer.Projection = Matrix.CreatePerspectiveFieldOfView(fov, Core.Instance.Window.ClientBounds.Width / CSng(Core.Instance.Window.ClientBounds.Height), 0.01, 500)

            'Set HUD color
            HUDInstructions.Active = Status <> GameStatus.GameActive OrElse (Spielers(UserIndex).Typ = SpielerTyp.Local)

            lastmstate = Mouse.GetState
        End Sub

#Region "Netzwerkfunktionen"
        ''' <summary>
        ''' Liest die Daten aus dem Stream des Servers
        ''' </summary>
        Private Sub ReadAndProcessInputData()
            'Implement move active
            Dim data As String() = LocalClient.ReadStream()
            For Each element In data
                Dim command As Char = element(0)
                Select Case command
                    Case "a"c 'Player arrived
                        Dim source As Integer = element(1).ToString
                        Dim MODE As Integer = CInt(element(2).ToString)
                        Dim txt As String() = element.Substring(3).Split("|")
                        Spielers(source).Name = txt(0)
                        Spielers(source).MOTD = txt(1)
                        Spielers(source).Bereit = True
                        Spielers(source).Mode = MODE
                        If source = UserIndex And Spielers(source).Mode = PlayerMode.Ghost Then EgoPlayer.PrisonEnabled = False
                        If source <> UserIndex Then CreateEntity(Spielers(source).Name).AddComponent(Spielers(source))
                        HUD.Color = PlayerHUDColors(EgoPlayer.Mode)
                        HUDNameBtn.Text = EgoPlayer.Mode.ToString
                        PostChat(Spielers(source).Name & " arrived!", Color.White)
                    Case "b"c 'Begin gaem
                        'Set local vs online players
                        Dim stuff As String = element.Substring(1)
                        For i As Integer = 0 To Spielers.Length - 1
                            If stuff.Contains(i) Then Spielers(i).Typ = SpielerTyp.Local
                        Next
                        'Init game
                        SendSoundFile()
                        StopUpdating = False
                        Status = CardGameState.Waitn
                        PostChat("The game has started!", Color.White)
                    Case "c"c 'Sent chat message
                        Dim source As Integer = element(1).ToString
                        If source = 9 Then
                            Dim text As String = element.Substring(2)
                            PostChat("[Guest]: " & text, Color.Gray)
                        Else
                            PostChat("[" & Spielers(source).Name & "]: " & element.Substring(2), playcolor(source))
                        End If
                    Case "f"c
                        Dim pl As Integer = element.Substring(1)
                        If pl = UserIndex And EgoPlayer.Mode = PlayerMode.Chased Then EgoPlayer.PrisonEnabled = False
                    Case "e"c 'Suspend gaem
                        Dim who As Integer = element(1).ToString
                        StopUpdating = True
                        Spielers(who).Bereit = False
                        PostChat(Spielers(who).Name & " left!", Color.White)
                        PostChat("The game is being suspended!", Color.White)
                    Case "g"c
                        Dim pl As Integer = element(1).ToString
                        If pl = UserIndex Then Exit Select
                        Dim dat = Newtonsoft.Json.JsonConvert.DeserializeObject(Of (Vector3, Vector3, Vector3, PlayerStatus))(element.Substring(8))
                        Spielers(pl).Location = dat.Item1
                        Spielers(pl).Direction = dat.Item2
                        Spielers(pl).ThreeDeeVelocity = dat.Item3
                        Spielers(pl).RunningMode = dat.Item4
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(1)
                        PostChat(msg, Color.White)
                    Case "p"c 'Player pressed
                        Dim who As Integer = CInt(element(1).ToString)
                        Dim source As Integer = CInt(element(2).ToString)
                        If who = UserIndex Then
                            'Some chaser touched local, chased player
                            If Spielers(who).Mode = PlayerMode.Chased And Spielers(source).Mode = PlayerMode.Chaser Then EgoPlayer.Entity.Position = CommonPlayer.PlayerSpawn : EgoPlayer.PrisonEnabled = True
                        End If
                    Case "r"c 'Player returned and sync every player
                        Dim source As Integer = element(1).ToString
                        Dim dat As (PlayerMode, Vector3) = Newtonsoft.Json.JsonConvert.DeserializeObject(Of (PlayerMode, Vector3))(element.Substring(2))
                        Spielers(source).Mode = dat.Item1
                        Spielers(source).Entity.Position = New Vector2(dat.Item2.X, dat.Item2.Z)
                        PostChat(Spielers(source).Name & " is back!", Color.White)
                        HUDInstructions.Text = "Welcome back!"
                        SendSoundFile()
                    Case "w"c 'Spieler hat gewonnen
                        'HUDInstructions.Text = "Game over!"
                        'If MediaPlayer.IsRepeating Then
                        '    MediaPlayer.Play(DamDamDaaaam)
                        '    MediaPlayer.Volume = 0.8
                        'Else
                        '    MediaPlayer.Play(Fanfare)
                        '    MediaPlayer.Volume = 0.3
                        'End If
                        'MediaPlayer.IsRepeating = False

                        ''Berechne Rankings
                        'Core.Schedule(1, Sub()
                        '                     Dim ranks As New List(Of (Integer, Integer)) '(Spieler ID, Score)
                        '                     For i As Integer = 0 To PlCount - 1
                        '                         ranks.Add((i, GetScore(i)))
                        '                     Next
                        '                     ranks = ranks.OrderBy(Function(x) x.Item2).ToList()
                        '                     ranks.Reverse()

                        '                     For i As Integer = 0 To ranks.Count - 1
                        '                         Dim ia As Integer = i

                        '                         Select Case i
                        '                             Case 0
                        '                                 Core.Schedule(i, Sub() PostChat("1st place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                        '                             Case 1
                        '                                 Core.Schedule(i, Sub() PostChat("2nd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                        '                             Case 2
                        '                                 Core.Schedule(i, Sub() PostChat("3rd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                        '                             Case Else
                        '                                 Core.Schedule(i, Sub() PostChat((ia + 1) & "th place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                        '                         End Select
                        '                     Next

                        '                     'Update K/D
                        '                     If ranks(0).Item1 = UserIndex Then
                        '                         If GameMode = GameMode.Competetive Then My.Settings.GamesWon += 1
                        '                     Else
                        '                         If GameMode = GameMode.Competetive Then My.Settings.GamesLost += 1
                        '                     End If
                        '                     My.Settings.Save()
                        '                 End Sub)
                        ''Set flags
                        'Status = CardGameState.SpielZuEnde
                        'FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                    Case "x"c 'Continue with game
                        Select Case EgoPlayer.Mode
                            Case PlayerMode.Chased
                                ActivateGame()
                            Case PlayerMode.Chaser
                                Core.Schedule(3, AddressOf ActivateGame)

                        End Select
                    Case "y"c 'Synchronisiere Daten
                        Dim str As String = element.Substring(1)
                        Dim sp As SyncMessage() = Newtonsoft.Json.JsonConvert.DeserializeObject(Of SyncMessage())(str)
                        For i As Integer = 0 To PlCount - 1
                            Spielers(i).Name = sp(i).Name
                            Spielers(i).Typ = sp(i).Typ
                            Spielers(i).MOTD = sp(i).MOTD
                            Spielers(i).Mode = sp(i).Mode
                            Spielers(i).ID = sp(i).ID
                            If i <> UserIndex Then
                                Spielers(i).SetColor(PlayerColors(Spielers(i).Mode))
                                If Spielers(i).Bereit AndAlso FindEntity(Spielers(i).Name) Is Nothing Then CreateEntity(Spielers(i).Name).AddComponent(Spielers(i))
                            End If
                        Next
                    Case "z"c 'Receive sound
                        Dim dataReceiver As New Threading.Thread(Sub()
                                                                     Dim source As Integer = element(1).ToString
                                                                     Dim IdentSound As IdentType = CInt(element(2).ToString)
                                                                     Dim SoundNr As Integer = element(3).ToString
                                                                     Dim dat As String = element.Substring(4).Replace("_TATA_", "")
                                                                     If source = UserIndex Then Exit Sub
                                                                     Dim sound As SoundEffect

                                                                     If SoundNr = 9 Then
                                                                         Try
                                                                             'Receive sound
                                                                             If IdentSound = IdentType.Custom Then
                                                                                 File.WriteAllBytes("Cache/client/" & Spielers(source).Name & "_pp.png", Compress.Decompress(Convert.FromBase64String(dat)))
                                                                                 Spielers(source).Thumbnail = Texture2D.FromFile(Dev, "Cache/client/" & Spielers(source).Name & "_pp.png")
                                                                             End If
                                                                         Catch ex As Exception
                                                                         End Try
                                                                     Else
                                                                         Try
                                                                             'Receive sound
                                                                             If IdentSound = IdentType.Custom Then
                                                                                 File.WriteAllBytes("Cache/client/" & Spielers(source).Name & SoundNr.ToString & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                                                                                 sound = SoundEffect.FromFile("Cache/client/" & Spielers(source).Name & SoundNr.ToString & ".wav")
                                                                             Else
                                                                                 sound = SoundEffect.FromFile("Content/prep/audio_" & CInt(IdentSound).ToString & ".wav")
                                                                             End If
                                                                         Catch ex As Exception
                                                                             'Data damaged, send standard sound
                                                                             IdentSound = If(SoundNr = 0, IdentType.TypeB, IdentType.TypeA)
                                                                             sound = SoundEffect.FromFile("Content/prep/audio_" & CInt(IdentSound).ToString & ".wav")
                                                                         End Try

                                                                         'Set sound for player
                                                                         Spielers(source).CustomSound(SoundNr) = sound
                                                                     End If
                                                                 End Sub) With {.Priority = Threading.ThreadPriority.BelowNormal}
                        dataReceiver.Start()

                End Select
            Next
        End Sub

        ' ---Methoden um Daten via den Server an die Clients zu senden---

        Friend Sub SendArrived()
            If UserIndex < 0 Then
                SendNetworkMessageToAll("y") 'request refresh
                Return
            End If

            If Rejoin Then
                SendNetworkMessageToAll("r" & My.Settings.Username & "|" & My.Settings.MOTD & "|" & My.Settings.UniqueIdentifier) 'Rejoin
            Else
                SendNetworkMessageToAll("a" & My.Settings.Username & "|" & My.Settings.MOTD & "|" & My.Settings.UniqueIdentifier) 'Nujoin
            End If
        End Sub
        Private Sub SendChatMessage(index As Integer, text As String)
            SendNetworkMessageToAll("c" & index.ToString & text)
        End Sub
        Private Sub SendPlayerLeft(index As Integer)
            LocalClient.WriteStream("e" & index)
        End Sub
        Private Sub SendRequestFreeing()
            SendNetworkMessageToAll("f")
        End Sub
        Private Sub SendPlayerData()
            Dim element = Spielers(UserIndex)

            SyncPosCounter += Time.DeltaTime
            If SyncPosCounter > SyncLoc Then
                SyncPosCounter = 0
                SendNetworkMessageToAll("g" & "_TATA_" & Newtonsoft.Json.JsonConvert.SerializeObject((element.Location, element.Direction, element.ThreeDeeVelocity, element.RunningMode)))
            End If
        End Sub
        Private Sub SendGameClosed()
            SendNetworkMessageToAll("l")
        End Sub
        Private Sub SendMessage(msg As String)
            SendNetworkMessageToAll("m" & msg)
        End Sub
        Private Sub SendPlayerPressed(ID As String) Implements IGameWindow.PlayerPressed
            For i As Integer = 0 To Spielers.Length - 1
                If Spielers(i).ID = ID And i <> UserIndex Then
                    SendNetworkMessageToAll("p" & i.ToString)
                    Exit For
                End If
            Next
        End Sub
        Private Sub SendPlayerBack(index As Integer)
            'Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers, SaucerFields))
            'SendNetworkMessageToAll("r" & index.ToString & str)
        End Sub
        Private Sub SendWinFlag()
            SendNetworkMessageToAll("w")
        End Sub
        Private Sub SendSoundFile()
            If UserIndex < 0 Then Return

            Dim dataSender As New Threading.Thread(Sub()
                                                       Dim txt As String = ""
                                                       If My.Settings.SoundA = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundA.audio")))
                                                       SendNetworkMessageToAll("z" & My.Settings.SoundA.ToString & "0" & "_TATA_" & txt)

                                                       txt = ""
                                                       If My.Settings.SoundB = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundB.audio")))
                                                       SendNetworkMessageToAll("z" & My.Settings.SoundB.ToString & "1" & "_TATA_" & txt)

                                                       txt = ""
                                                       If My.Settings.Thumbnail Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/pp.png")))
                                                       SendNetworkMessageToAll("z" & If(My.Settings.Thumbnail, IdentType.Custom, 0).ToString & "9" & "_TATA_" & txt)
                                                   End Sub) With {.Priority = Threading.ThreadPriority.BelowNormal}
            dataSender.Start()
        End Sub

        Private Sub SendNetworkMessageToAll(message As String)
            If NetworkMode Then LocalClient.WriteStream(message)
        End Sub
#End Region
        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

        Private Sub ActivateGame()
            AdditionalHUDRend.TriggerStartAnimation({"5", "4", "3", "2", "1", "Go!"}, Sub()
                                                                                          Crosshair.Enabled = True
                                                                                          EgoPlayer.PrisonEnabled = False
                                                                                          Status = GameStatus.GameActive
                                                                                          HUDSprintBar.Active = True
                                                                                      End Sub)
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

        Private ReadOnly Property IGameWindow_UserIndex As Integer Implements IGameWindow.UserIndex
            Get
                Return UserIndex
            End Get
        End Property

        Public Property BarrierRectangle As RectangleF Implements IGameWindow.BarrierRectangle
#End Region
    End Class
End Namespace