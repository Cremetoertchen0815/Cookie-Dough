Imports System.Collections.Generic
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.Common
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Nez.Console
Imports Nez.Tweens

Namespace Game.DuoCard
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class GameRoom
        Inherits Scene
        Implements ICardRendererWindow

        'Instance flags
        Friend Spielers As Player() 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend PlCount As Integer 'Gibt an wieviele Spieler das Spiel enthält
        Friend NetworkMode As Boolean = False 'Gibt an, ob das Spiel über das Netzwerk kommunuziert
        Friend SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
        Friend Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private StopWhenRealStart As Boolean = False
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private Timer As TimeSpan 'Misst die Zeit seit dem Anfang des Spiels
        Private LastTimer As TimeSpan 'Gibt den Timer des vergangenen Frames an
        Private TimeOver As Boolean = False 'Gibt an, ob die registrierte Zeit abgelaufen ist

        'Game flags
        Private MoveActive As Boolean = False 'Gibt an, ob eine Figuranimation in Gange ist

        'Assets
        Private Fanfare As Song
        Private DamDamDaaaam As Song
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont

        'Renderer
        Friend Renderer As CardRenderer
        Friend Psyground As PsygroundRenderer

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtnB As Button
        'Private WithEvents HUDBtnC As Button
        'Private WithEvents HUDBtnD As Button
        Private WithEvents HUDChat As TextscrollBox
        Private WithEvents HUDChatBtn As Button
        Private WithEvents HUDInstructions As Label
        Private WithEvents HUDNameBtn As Button
        Private WithEvents HUDFullscrBtn As Button
        Private WithEvents HUDMusicBtn As Button
        Private WithEvents HUDdbgLabel As Label
        Private WithEvents HUDmotdLabel As Label
        'Private WithEvents HUDDiceBtn As GameRenderable
        Private InstructionFader As ITween(Of Color)
        Private Chat As List(Of (String, Color))

        'Keystack & other debug shit
        Private keysa As New List(Of Keys)
        Private ButtonStack As New List(Of Keys)
        Private oldpress As New List(Of Keys)
        Private Shared dbgKickuser As Integer = -1
        Private Shared dbgExecSync As Boolean = False
        Private Shared dbgPlaceCmd As (Integer, Integer, Integer)
        Private Shared dbgPlaceSet As Boolean = False
        Private Shared dbgEnd As Boolean = False
        Private Shared dbgCamFree As Boolean = False
        Private Shared dbgCam As Keyframe3D = Nothing
        Private Shared dbgLoguser As Integer = -1

        'Spielfeld
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        Private Center As Vector2 'Gibt den Mittelpunkt des Screen-Viewports des Spielfelds an
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False)} 'Bewegt die Kamera 
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False) 'Gibt die Standard-Position der Kamera an

        'Konstanten
        Private Const FigurSpeed As Integer = 450
        Private Const ErrorCooldown As Integer = 1
        Private Const CPUThinkingTime As Single = 0.6
        Private Const CamSpeed As Integer = 1300
        Sub New()
            'Bereite Flags und Variablen vor
            Status = SpielStatus.WarteAufOnlineSpieler
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            Status = SpielStatus.WarteAufOnlineSpieler
            SpielerIndex = -1
            MoveActive = False

            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ChatText"))
            Fanfare = Content.Load(Of Song)("bgm\fanfare")
            DamDamDaaaam = Content.Load(Of Song)("sfx\DamDamDaaam")

            'Lade HUD
            HUD = New GuiSystem
            'HUDDiceBtn = New GameRenderable(Me) : HUD.Controls.Add(HUDDiceBtn)
            HUDBtnB = New Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnB)
            'HUDBtnC = New Button("Anger", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnC)
            'HUDBtnD = New Button("Sacrifice", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnD)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            HUDdbgLabel = New Label(Function() FigurFaderCamera.Value.ToString, New Vector2(500, 120)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDdbgLabel)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDmotdLabel)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = hudcolors(0)

            Renderer = AddRenderer(New CardRenderer(Me, -1))
            Psyground = AddRenderer(New PsygroundRenderer(0, 0.3))
            AddRenderer(New DefaultRenderer(1))

            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            Center = New Rectangle(500, 70, 950, 950).Center.ToVector2
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(LoopType.PingPong, -1).Start()

            'Load sounds and MOTDs
            Dim sf As SoundEffect() = {GetLocalAudio(My.Settings.SoundA), GetLocalAudio(My.Settings.SoundB, True)}
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                Select Case pl.Typ
                    Case SpielerTyp.Local
                        Spielers(i).CustomSound = sf
                        Spielers(i).MOTD = My.Settings.MOTD
                    Case SpielerTyp.CPU
                        Spielers(i).MOTD = CPU_MOTDs(i)
                        If i <> 0 Then
                            Spielers(i).CustomSound = {GetLocalAudio(IdentType.TypeB), GetLocalAudio(IdentType.TypeA)}
                        Else
                            Dim sff = SoundEffect.FromFile("Content\prep\tele.wav")
                            Spielers(i).CustomSound = {sff, sff}
                        End If
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

            'Set cam
            If dbgCam <> Nothing Then
                FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = dbgCam}
                dbgCam = Nothing
            End If

            'Move cam
            If dbgCamFree Then FigurFaderCamera.Value = FigurFaderCamera.Value + New Keyframe3D(If(kstate.IsKeyDown(Keys.A), -1, 0) + If(kstate.IsKeyDown(Keys.D), 1, 0), If(kstate.IsKeyDown(Keys.S), -1, 0) + If(kstate.IsKeyDown(Keys.W), 1, 0), If(kstate.IsKeyDown(Keys.LeftShift), -1, 0) + If(kstate.IsKeyDown(Keys.Space), 1, 0), If(kstate.IsKeyDown(Keys.J), -0.01, 0) + If(kstate.IsKeyDown(Keys.L), 0.01, 0), If(kstate.IsKeyDown(Keys.K), -0.01, 0) + If(kstate.IsKeyDown(Keys.I), 0.01, 0), If(kstate.IsKeyDown(Keys.RightShift), -0.01, 0) + If(kstate.IsKeyDown(Keys.Enter), 0.01, 0), True) : HUDdbgLabel.Active = True

            If Not StopUpdating Then

            End If

            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
                If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Disconnected! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
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
            If MoveActive Then Return

            Dim data As String() = LocalClient.ReadStream()
            For Each element In data
                Dim source As Integer = CInt(element(0).ToString)
                Dim command As Char = element(1)
                Select Case command
                    Case "a"c 'Player arrived
                        Dim txt As String() = element.Substring(2).Split("|")
                        Spielers(source).Name = txt(0)
                        Spielers(source).MOTD = txt(1)
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " arrived!", Color.White)
                        SendPlayerArrived(source, Spielers(source).Name, Spielers(source).MOTD)
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
                        If Not StopUpdating And Status <> SpielStatus.SpielZuEnde And Status <> SpielStatus.WarteAufOnlineSpieler Then PostChat("The game is being suspended!", Color.White)
                        If Status <> SpielStatus.WarteAufOnlineSpieler Then StopUpdating = True
                        'If Renderer.BeginTriggered Then StopWhenRealStart = True

                        SendPlayerLeft(source)
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(2)
                        PostChat(msg, Color.White)
                    Case "n"c 'Switch player
                        SwitchPlayer()
                    Case "r"c 'Player is back
                        Dim txt As String() = element.Substring(2).Split("|")
                        Spielers(source).Name = txt(0)
                        Spielers(source).MOTD = txt(1)
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " is back!", Color.White)
                        SendPlayerBack(source)
                        If SpielerIndex = source Then SendNewPlayerActive(SpielerIndex)
                        'Check if players are still missing, if not, send the signal to continue the game
                        Dim everythere As Boolean = True
                        For Each pl In Spielers
                            If Not pl.Bereit Then everythere = False
                        Next
                        If everythere And Status <> SpielStatus.WarteAufOnlineSpieler Then StopUpdating = False : SendGameActive()
                        If everythere And StopWhenRealStart Then StopWhenRealStart = False
                    Case "y"c
                        SendSync()
                    Case "z"c
                        Dim IdentSound As IdentType = CInt(element(2).ToString)
                        Dim SoundNr As Integer = CInt(element(3).ToString)
                        Dim dat As String = element.Substring(4).Replace("_TATA_", "")
                        Try
                            'Receive sound
                            If IdentSound = IdentType.Custom Then
                                IO.File.WriteAllBytes("Cache\server\" & Spielers(source).Name & SoundNr.ToString & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                                Spielers(source).CustomSound(SoundNr) = SoundEffect.FromFile("Cache\server\" & Spielers(source).Name & SoundNr.ToString & ".wav")
                            Else
                                Spielers(source).CustomSound(SoundNr) = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                            End If
                            SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & SoundNr.ToString & "_TATA_" & dat)
                        Catch ex As Exception
                            'Data damaged, send standard sound
                            IdentSound = If(SoundNr = 0, IdentType.TypeB, IdentType.TypeA)
                            Spielers(source).CustomSound(SoundNr) = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                            SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & SoundNr.ToString & "_TATA_")
                        End Try
                End Select
            Next
        End Sub

        ' ---Methoden um Daten via den Server an die Clients zu senden---
        Private Sub SendPlayerArrived(index As Integer, name As String, MOTD As String)
            SendNetworkMessageToAll("a" & index.ToString & name & "|" & MOTD)
        End Sub
        Private Sub SendBeginGaem()
            Dim appendix As String = ""
            For i As Integer = 0 To Spielers.Length - 1
                If Spielers(i).Typ = SpielerTyp.Local Or Spielers(i).Typ = SpielerTyp.CPU Then appendix &= i.ToString
            Next
            SendNetworkMessageToAll("b" & appendix)
            SendSync()
            SendSoundFile()
        End Sub
        Private Sub SendChatMessage(index As Integer, text As String)
            SendNetworkMessageToAll("c" & index.ToString & text)
        End Sub
        Private Sub SendPlayerLeft(index As Integer)
            LocalClient.WriteStream("e" & index)
        End Sub
        'Private Sub SendHighscore()
        '    Dim pls As New List(Of (String, Integer))
        '    For i As Integer = 0 To Spielers.Length - 1
        '        If Spielers(i).Typ = SpielerTyp.Local Or Spielers(i).Typ = SpielerTyp.Online Then
        '            pls.Add((Spielers(i).Name, GetScore(i)))
        '        End If
        '    Next
        '    SendNetworkMessageToAll("h" & 0.ToString & Newtonsoft.Json.JsonConvert.SerializeObject(pls))
        'End Sub
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
        Private Sub SendPlayerBack(index As Integer)
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers))
            SendNetworkMessageToAll("r" & index.ToString & str)
            SendSoundFile()
        End Sub

        Private Sub SendFigureTransition(who As Integer, figur As Integer, destination As Integer)
            SendNetworkMessageToAll("s" & who.ToString & figur.ToString & destination.ToString)
        End Sub
        Private Sub SendWinFlag()
            SendSync()
            SendNetworkMessageToAll("w")
        End Sub
        Private Sub SendGameActive()
            SendNetworkMessageToAll("x")
        End Sub

        Private Sub SendSync()
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers))
            SendNetworkMessageToAll("y" & str)
        End Sub
        Private Sub SendSoundFile()
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                If pl.Typ = SpielerTyp.Local Or pl.Typ = SpielerTyp.CPU Then
                    'Send Sound A
                    Dim txt As String
                    Dim snd As IdentType = GetPlayerAudio(i, False, txt)
                    SendNetworkMessageToAll("z" & i.ToString & CInt(snd).ToString & "0" & "_TATA_" & txt) 'Suffix "_TATA_" is to not print out in console

                    'Send Sound B
                    snd = GetPlayerAudio(i, True, txt)
                    LocalClient.WriteStream("z" & i.ToString & CInt(snd).ToString & "1" & "_TATA_" & txt)
                End If
            Next
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

        ''' <summary>
        ''' Prüft nach dem Würfeln, wie der Zug weitergeht(Ist Zug möglich, muss Figur ausgewählt werden, ...)
        ''' </summary>
        Private Sub CalcMoves()

        End Sub

#Region "Hilfsfunktionen"
        Private Function GetLocalAudio(ident As IdentType, Optional IsSoundB As Boolean = False) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content\prep\audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache\client\sound" & If(IsSoundB, "B", "A") & ".audio")
            End If
        End Function

        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

        Private Sub SwitchPlayer()

            'Setze benötigte Flags
            SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Do While Spielers(SpielerIndex).Typ = SpielerTyp.None
                SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Loop
            'Setze HUD flags
            If Spielers(SpielerIndex).Typ <> SpielerTyp.Online Then Status = SpielStatus.Würfel Else Status = SpielStatus.Waitn
            SendNewPlayerActive(SpielerIndex)
            If Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex
            HUD.Color = hudcolors(UserIndex)
            'Reset camera if not already moving
            If FigurFaderCamera.State <> TransitionState.InProgress Then FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = StdCam}
            'Set game flags
            StopUpdating = False
            SendGameActive()
            HUDInstructions.Text = "Lol"
        End Sub
#End Region
#Region "Knopfgedrücke"

        Dim chatbtnpressed As Boolean = False

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
            If Not chatbtnpressed Then
                chatbtnpressed = True
                SFX(2).Play()
                Dim txt As String = Microsoft.VisualBasic.InputBox("Enter your message: ", "Send message", "")
                If txt <> "" Then
                    SendChatMessage(UserIndex, txt)
                    PostChat("[" & Spielers(UserIndex).Name & "]: " & txt, hudcolors(UserIndex))
                End If
                chatbtnpressed = False
            End If
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
                Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
            End If
        End Sub
#End Region
#Region "Debug Commands"

        <Command("bv-eject", "Removes a specific user from the game.")>
        Public Shared Sub dbgEjectUser(nr As Integer)
            dbgKickuser = nr
        End Sub

        <Command("bv-sync", "Removes a specific user from the game.")>
        Public Shared Sub dbgSyncData()
            dbgExecSync = True
        End Sub

        <Command("bv-place", "Places a player's figure on a certain position.")>
        Public Shared Sub dbgPlaceFigure(player As Integer, figure As Integer, value As Integer)
            dbgPlaceCmd = (player, figure, value)
            dbgPlaceSet = True
        End Sub

        <Command("bv-info", "Gives information over a specific player.")>
        Public Shared Sub dbgPlayerInfo(nr As Integer)
            dbgLoguser = nr
        End Sub

        <Command("bv-end", "Ends the game.")>
        Public Shared Sub dbgEndGame()
            dbgEnd = True
        End Sub

        <Command("bv-cam-place", "Sets up the camera in a specific way.")>
        Public Shared Sub dbgCamPlace(x As Single, y As Single, z As Single, yaw As Single, pitch As Single, roll As Single)
            dbgCam = New Keyframe3D(x, y, z, yaw, pitch, roll, False)
        End Sub

        <Command("bv-cam-free", "Sets up the camera in a specific way.")>
        Public Shared Sub dbgCamFreeS()
            dbgCamFree = True
        End Sub
#End Region
#Region "Schnittstellenimplementation"


        Private ReadOnly Property IGameWindow_Spielers As Player() Implements IGameWindow.Spielers
            Get
                Return Spielers
            End Get
        End Property

        Private ReadOnly Property IGameWindow_Status As SpielStatus Implements IGameWindow.Status
            Get
                Return Status
            End Get
        End Property

        Private ReadOnly Property IGameWindow_SelectFader As Single Implements IGameWindow.SelectFader
            Get
                Return SelectFader
            End Get
        End Property

        Private ReadOnly Property IGameWindow_SpielerIndex As Integer Implements IGameWindow.SpielerIndex
            Get
                Return SpielerIndex
            End Get
        End Property

        Private ReadOnly Property IGameWindow_UserIndex As Integer Implements IGameWindow.UserIndex
            Get
                Return UserIndex
            End Get
        End Property

        Public ReadOnly Property BGTexture As Texture2D Implements IGameWindow.BGTexture
            Get
                Return Renderer.RenderTexture
            End Get
        End Property

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function
#End Region
    End Class
End Namespace