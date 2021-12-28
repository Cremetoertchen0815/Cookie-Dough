Imports System.Collections.Generic
Imports System.Linq
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.CarCrash.Rendering
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Newtonsoft.Json
Imports Nez.Console
Imports Nez.Sprites
Imports Nez.Tweens

Namespace Game.CarCrash
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class GameRoom
        Inherits Scene
        Implements IGameWindow

        'Instance fields
        Friend Spielers As Player() 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend NetworkMode As Boolean = False 'Gibt an, ob das Spiel über das Netzwerk kommunuziert
        Friend SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
        Friend Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private StopWhenRealStart As Boolean = False 'Notices, that the game is supposed to be interrupted, as soon as it's being started
        Private MultiPlayer As Boolean

        'Assets
        Private Fanfare As Song
        Private DamDamDaaaam As Song
        Private ExplodeSound As SoundEffect
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont
        Private DataTransmissionThread As Threading.Thread

        'Emulation
        Private EmuEntity As Entity
        Private Emulator As ConsoleEmulator
        Private TestCard As SpriteRenderer

        'Renderer
        Friend Psyground As PsygroundRenderer
        Friend EmulationRenderer As RenderLayerRenderer
        Friend Renderer As Renderer3D

        'Spielfeld
        Friend CamPos As Keyframe3D = New Keyframe3D(0, 0, 5000, 1, 0, 0.2, True) 'Bewegt die Kamera 
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtnB As Button
        Private WithEvents HUDChat As TextscrollBox
        Private WithEvents HUDChatBtn As Button
        Private WithEvents HUDInstructions As Label
        Private WithEvents HUDFullscrBtn As Button
        Private WithEvents HUDMusicBtn As Button
        Private WithEvents HUDdbgLabel As Label
        Private WithEvents HUDmotdLabel As Label
        Private InstructionFader As ITween(Of Color)
        Private ShowDice As Boolean = False
        Private Chat As List(Of (String, Color))

        'Buttons
        Private BtnColorChange As VirtualButton

        'Konstanten
        Sub New(onlineMode As Boolean)
            'Bereite Flags und Variablen vor
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            MultiPlayer = onlineMode
            Status = SpielStatus.WarteAufOnlineSpieler
            SpielerIndex = -1
            UserIndex = -1
            Global.Carcrash.Shared.Username = My.Settings.Username
            Global.Carcrash.Shared.ID = My.Settings.UniqueIdentifier
            Global.Carcrash.Shared.WriteData = AddressOf SendGeneralPurposeData
            Global.Carcrash.Shared.TriggerExplosionSound = AddressOf PlayExplosion
            Global.Carcrash.Shared.Difficulty = If(My.Settings.Schwierigkeitsgrad < 1, Global.Carcrash.Game.Difficulty.Easy, Global.Carcrash.Game.Difficulty.Hard)
            Global.Carcrash.Shared.RequestLeaderboardUpdate = Sub(x)
                                                                  Spielers(0).Score = x
                                                                  Status = SpielStatus.SpielZuEnde
                                                              End Sub

            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ChatText"))
            Fanfare = Content.Load(Of Song)("bgm/fanfare")
            DamDamDaaaam = Content.Load(Of Song)("sfx/DamDamDaaam")
            ExplodeSound = Content.LoadSoundEffect("games/CC/explosion")

            'Create emulation renderer
            EmulationRenderer = AddRenderer(New RenderLayerRenderer(0, -3) With {.RenderTexture = New Textures.RenderTexture(1920, 1080)})
            EmuEntity = CreateEntity("emulation")
            TestCard = EmuEntity.AddComponent(New SpriteRenderer(Content.LoadTexture("games/CC/color_bars")) With {.RenderLayer = -3, .LayerDepth = 0F, .LocalOffset = New Vector2(1920, 1080) / 2})
            EmuEntity.AddComponent(New PrototypeSpriteRenderer(1920, 1080) With {.Color = Color.Black, .LocalOffset = New Vector2(1920, 1080) / 2, .RenderLayer = -3, .LayerDepth = 1.0F})

            'Creater other renderers
            Psyground = AddRenderer(New PsygroundRenderer(1, 0.3))
            Renderer = AddRenderer(New Renderer3D(Me, 2))

            'Create virtual gamescreen
            AddRenderer(New RenderLayerRenderer(3, -2, -1)) 'HUD renderer
            CreateEntity("gamescreen").AddComponent(New ScreenRenderer(Renderer.RenderTexture) With {.RenderLayer = -1})

            'Assign buttons
            BtnColorChange = New VirtualButton(New VirtualButton.KeyboardKey(Keys.C))

            'Lade HUD
            Dim glass = New Color(5, 5, 5, 185)
            HUD = New GuiSystem With {.RenderLayer = -2}
            HUDBtnB = New Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDBtnB)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35, .RedrawBackground = True} : HUD.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDChatBtn)
            HUDInstructions = New Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Add(HUDInstructions)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Add(HUDmotdLabel)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = Color.White
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(LoopType.PingPong, -1).Start()

            'Screen content renderable
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            GuiControl.BackgroundImage = Renderer.BlurredContents
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            'Load sounds and MOTDs
            Dim sf As SoundEffect() = {GetLocalAudio(My.Settings.SoundA), GetLocalAudio(My.Settings.SoundB, True)}
            Dim thumb = Texture2D.FromFile(Dev, "Cache/client/pp.png")
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                Select Case pl.Typ
                    Case SpielerTyp.Local
                        Spielers(i).CustomSound = sf
                        Spielers(i).MOTD = My.Settings.MOTD
                        If My.Settings.Thumbnail Then Spielers(i).Thumbnail = thumb
                End Select
            Next
        End Sub

        Public Overrides Sub Unload()
            Framework.Networking.Client.OutputDelegate = Sub(x) Return
        End Sub

        ''' <summary>
        ''' Berechnet die Spielelogik.
        ''' </summary>
        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState()
            Dim kstate As KeyboardState = If(DebugConsole.Instance.IsOpen, Nothing, Keyboard.GetState())
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

            If Not StopUpdating Then

                'Update die Spielelogik
                Select Case Status

                    Case SpielStatus.WarteAufOnlineSpieler
                        HUDInstructions.Text = "Waiting for all players to connect..."

                        'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                        For Each sp In Spielers
                            If sp Is Nothing OrElse Not sp.Bereit Then Exit Select 'Falls ein Spieler noch nicht belegt/bereit, breche Spielstart ab
                        Next

                        'Falls vollzählig, starte Spiel
                        StopUpdating = True
                        Core.Schedule(0.8, Sub()
                                               PostChat("The game has started!", Color.White)
                                               CamPos = New Keyframe3D
                                               'Start game
                                               Renderer.TriggerStartAnimation(Sub()
                                                                                  TestCard.Enabled = False
                                                                                  Emulator = EmuEntity.AddComponent(New ConsoleEmulator(AddressOf LaunchGame) With {.RenderLayer = -3, .LayerDepth = 0.5F, .TransformMatrix = Matrix.CreateScale(1.2F, 0.9F, 1.0F) * Matrix.CreateTranslation(20, 180, 0)})
                                                                                  HUDInstructions.Text = " "
                                                                                  Status = SpielStatus.SpielAktiv
                                                                                  StopUpdating = False
                                                                              End Sub)
                                               SendBeginGaem()
                                           End Sub)
                    Case SpielStatus.SpielAktiv
                        If BtnColorChange.IsPressed Then
                            Select Case Emulator.FixedForegroundTint
                                Case ConsoleColor.Cyan
                                    Emulator.FixedForegroundTint = ConsoleColor.Red
                                Case ConsoleColor.Red
                                    Emulator.FixedForegroundTint = ConsoleColor.White
                                Case Else
                                    Emulator.FixedForegroundTint = ConsoleColor.Cyan
                            End Select

                        End If
                    Case SpielStatus.SpielZuEnde
                        'Check for highscore submission
                        For Each element In Spielers
                            If element.Score < 0 Then Exit Select
                        Next

                        StopUpdating = True

                        If MultiPlayer Or LocalClient.Connected Then
                            'Submit highscore
                            Dim hscore As New List(Of (String, Double))
                            For Each element In Spielers
                                hscore.Add((element.ID, element.Score))
                            Next
                            SendHighscore(hscore)
                        Else
                            Global.Carcrash.Shared.CallbackLeaderboard(Nothing, True)
                        End If

                        Status = SpielStatus.Waitn
                End Select
            End If

            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : MsgBoxer.EnqueueMsgbox("Connection lost!", Sub() Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu)), {"Oh man"})
                If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : MsgBoxer.EnqueueMsgbox("Disconnected! Game was ended!", Sub() Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu)), {"Oh man"})
            End If

            If NetworkMode Then ReadAndProcessInputData()

            'Misc things
            If kstate.IsKeyDown(Keys.Escape) And lastkstate.IsKeyUp(Keys.Escape) Then MenuButton()
            lastmstate = mstate
            lastkstate = kstate
            MyBase.Update()
        End Sub

#Region "Netzwerkfunktionen"
        ''' <summary>
        ''' Liest die Daten aus dem Stream des Servers
        ''' </summary>
        Private Sub ReadAndProcessInputData()


            Try
                Dim data As String() = LocalClient.ReadStream()
                For Each element In data
                    Dim source As Integer = CInt(element(0).ToString)
                    Dim command As Char = element(1)
                    Select Case command
                        Case "a"c 'Player arrived
                            Dim txt As String() = element.Substring(2).Split("|")
                            Spielers(source).Name = txt(0)
                            Spielers(source).MOTD = txt(1)
                            Spielers(source).ID = txt(2)
                            Spielers(source).Bereit = True
                            PostChat(Spielers(source).Name & " arrived!", Color.White)
                            SendPlayerArrived(source, Spielers(source).Name, Spielers(source).MOTD, Spielers(source).ID)
                        Case "c"c 'Sent chat message
                            Dim text As String = element.Substring(2)
                            If source = 9 Then
                                PostChat("[Guest]: " & text, Color.Gray)
                                SendChatMessage(source, text)
                            Else
                                PostChat("[" & Spielers(source).Name & "]: " & text, playcolor(source))
                                SendChatMessage(source, text)
                            End If
                        Case "d"c 'Receiving game-related data from the client
                            Global.Carcrash.Shared.ReadData(element.Substring(2)) 'Redirect data
                        Case "e"c 'Suspend gaem
                            LocalClient.LeaveFlag = True
                            StopUpdating = False
                        Case "h"c
                            Dim dat = element.Substring(2)
                            Global.Carcrash.Shared.CallbackLeaderboard(JsonConvert.DeserializeObject(Of (String, String, Double)())(dat), False)
                            If MultiPlayer Then SendScoreboards(dat)
                        Case "m"c 'Sent chat message
                            Dim msg As String = element.Substring(2)
                            PostChat(msg, Color.White)
                        Case "p"c
                            Dim score As Integer = CInt(element.Substring(2))
                            Spielers(source).Score = score
                        Case Else
                            Throw New Exception("Wat")
                    End Select
                Next

            Catch ex As Exception

            End Try
        End Sub

        ' ---Methoden um Daten via den Server an die Clients zu senden---
        Private Sub SendPlayerArrived(index As Integer, name As String, MOTD As String, ID As String)
            SendNetworkMessageToAll("a" & index.ToString & name & "|" & MOTD & "|" & ID)
        End Sub
        Private Sub SendBeginGaem()
            Dim appendix As String = ""
            For i As Integer = 0 To Spielers.Length - 1
                If Spielers(i).Typ = SpielerTyp.Local Or Spielers(i).Typ = SpielerTyp.CPU Then appendix &= i.ToString
            Next
            SendNetworkMessageToAll("b" & appendix)
        End Sub
        Private Sub SendChatMessage(index As Integer, text As String)
            SendNetworkMessageToAll("c" & index.ToString & text)
        End Sub
        Private Sub SendGeneralPurposeData(data As String)
            SendNetworkMessageToAll("d" & data)
        End Sub
        Private Sub SendHighscore(data As List(Of (String, Double)))
            Dim diff = If(Global.Carcrash.Shared.Difficulty = Global.Carcrash.Game.Difficulty.Easy, 0, 2 - Global.Carcrash.Shared.Difficulty)

            If MultiPlayer Then
                'Registered round
                SendNetworkMessageToAll("h" & CInt(GameType.CarCrash) & diff & "0" & JsonConvert.SerializeObject(data))
            Else
                'Idle commands for single player leaderboard registration
                LocalClient.WriteStream("leaderboard_submit")
                LocalClient.WriteStream(CInt(GameType.CarCrash))
                LocalClient.WriteStream(diff)
                LocalClient.WriteStream(0)
                LocalClient.WriteStream(JsonConvert.SerializeObject(data))
                'Receive data from server
                Global.Carcrash.Shared.CallbackLeaderboard(JsonConvert.DeserializeObject(Of (String, String, Double)())(LocalClient.ReadString()), False)
                If LocalClient.ReadString() <> "Yoshaas!" Then Throw New Exception("W A T")
            End If
        End Sub
        Private Sub SendGameClosed()
            SendNetworkMessageToAll("l")
        End Sub
        Private Sub SendMessage(msg As String)
            SendNetworkMessageToAll("m" & msg)
        End Sub
        Private Sub SendScoreboards(data As String)
            SendNetworkMessageToAll("p" & data)
        End Sub

        Private Function GetPlayerAudio(i As Integer, IsB As Boolean, ByRef txt As String) As IdentType
            txt = ""
            Dim ret As IdentType
            Select Case Spielers(i).Typ
                Case SpielerTyp.Local
                    If IsB Then
                        ret = My.Settings.SoundB
                        If ret = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundB.audio")))
                    Else
                        ret = My.Settings.SoundA
                        If ret = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundA.audio")))
                    End If
                Case SpielerTyp.CPU
                    Select Case i
                        Case 0
                            ret = IdentType.Custom
                            txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Content/prep/tele.wav")))
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

        Private Sub LaunchGame()
            If NetworkMode Then
                Dim GameInstance = New Global.Carcrash.Game.OnlineGame.Host()
                GameInstance.Run()
            Else
                Dim GameInstance = New Global.Carcrash.GameLoop()
                GameInstance.Run()
            End If
        End Sub

        Private Function GetLocalAudio(ident As IdentType, Optional IsSoundB As Boolean = False) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content/prep/audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache/client/sound" & If(IsSoundB, "B", "A") & ".audio")
            End If
        End Function

        Private Sub PlayExplosion()
            Dim volume_buffer = Volume
            MediaPlayer.Volume = 0
            Dim inst = ExplodeSound.CreateInstance()
            inst.Play()
            Core.Schedule(5, Sub() Tween("Volume", volume_buffer, 2.0F).Start())
        End Sub

        Private Property Volume As Single
            Get
                Return MediaPlayer.Volume
            End Get
            Set(value As Single)
                MediaPlayer.Volume = value
            End Set
        End Property

        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

#End Region
#Region "Knopfgedrücke"

        Dim chatbtnpressed As Boolean = False

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
            SFX(2).Play()
            LaunchInputBox(Sub(x)
                               If UserIndex >= 0 Then
                                   SendChatMessage(UserIndex, x)
                                   PostChat("[" & Spielers(UserIndex).Name & "]: " & x, hudcolors(UserIndex))
                               Else
                                   SendMessage("[ADMIN]:" & x)
                                   PostChat("[ADMIN]: " & x, Color.White)
                               End If
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
            MsgBoxer.OpenMsgbox("Do you really want to leave?", Sub(x)
                                                                    If x = 1 Then Return
                                                                    SFX(2).Play()
                                                                    SendGameClosed()
                                                                    NetworkMode = False
                                                                    Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
                                                                End Sub, {"Yeah", "Nope"})
        End Sub
#End Region

#Region "Schnittstellen"

        Private ReadOnly Property IGameWindow_Status As SpielStatus Implements IGameWindow.Status
            Get
                Return Status
            End Get
        End Property

        Public ReadOnly Property BGTexture As Texture2D Implements IGameWindow.BGTexture
            Get
                Return Psyground.RenderTexture
            End Get
        End Property

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            Return CamPos
        End Function

        Public ReadOnly Property EmuTexture As Texture2D Implements IGameWindow.EmuTexture
            Get
                Return EmulationRenderer.RenderTexture
            End Get
        End Property
#End Region

    End Class
End Namespace