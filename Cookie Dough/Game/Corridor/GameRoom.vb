Imports System.Collections.Generic
Imports System.Linq
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.Corridor.Rendering
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Nez.Console
Imports Nez.Tweens

Namespace Game.Corridor
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class GameRoom
        Inherits Scene
        Implements IGameWindow

        'Instance fields
        Friend Spielers As Player() = {New Player(SpielerTyp.Local), New Player(SpielerTyp.Local)} 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend NetworkMode As Boolean = False 'Gibt an, ob das Spiel über das Netzwerk kommunuziert
        Friend SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
        Friend Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private Timer As TimeSpan 'Misst die Zeit seit dem Anfang des Spiels
        Private LastTimer As TimeSpan 'Gibt den Timer des vergangenen Frames an
        Private TimeOver As Boolean = False 'Gibt an, ob die registrierte Zeit abgelaufen ist
        Private StopWhenRealStart As Boolean = False 'Notices, that the game is supposed to be interrupted, as soon as it's being started

        'Assets
        Private Fanfare As Song
        Private DamDamDaaaam As Song
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont
        Private DataTransmissionThread As Threading.Thread

        'Renderer
        Friend Renderer As Renderer3D
        Friend Psyground As PsygroundRenderer

        'Spielfeld
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False)} 'Bewegt die Kamera 
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt

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
        Private WithEvents HUDAfkBtn As Button
        Private WithEvents HUDdbgLabel As Label
        Private WithEvents HUDmotdLabel As Label
        Private WithEvents HUDDiceBtn As GameRenderable
        Private InstructionFader As ITween(Of Color)
        Private ShowDice As Boolean = False
        Private Chat As List(Of (String, Color))

        'Konstanten
        Private Const WürfelDauer As Integer = 320
        Private Const WürfelAnimationCooldown As Integer = 4
        Private Const FigurSpeed As Integer = 450
        Private Const ErrorCooldown As Integer = 1
        Private Const RollDiceCooldown As Single = 0.5
        Private Const CPUThinkingTime As Single = 0.6
        Private Const DopsHöhe As Integer = 150
        Private Const CamSpeed As Integer = 1300
        Private Const SacrificeWait As Integer = 5
        Private SaucerChance As Integer = 18
        Sub New()
            'Bereite Flags und Variablen vor
            Status = SpielStatus.WarteAufOnlineSpieler
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            Status = SpielStatus.WarteAufOnlineSpieler
            SpielerIndex = -1
            UserIndex = -1

            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            LastTimer = Timer

            LoadContent()
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ChatText"))
            Fanfare = Content.Load(Of Song)("bgm\fanfare")
            DamDamDaaaam = Content.Load(Of Song)("sfx\DamDamDaaam")

            'Lade HUD
            HUD = New GuiSystem
            HUDDiceBtn = New GameRenderable(Me) : HUD.Controls.Add(HUDDiceBtn)
            HUDBtnB = New Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnB)
            'HUDBtnC = New Button("Anger", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnC)
            'HUDBtnD = New Button("Sacrifice", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnD)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDmotdLabel)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
            HUDAfkBtn = New Button("AFK", New Vector2(220, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDAfkBtn)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = Color.White
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(LoopType.PingPong, -1).Start()

            Renderer = AddRenderer(New Renderer3D(Me, -1))
            'Psyground = AddRenderer(New PsygroundRenderer(0, 0.3))
            AddRenderer(New DefaultRenderer(1))

            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            'Load sounds and MOTDs
            Dim sf As SoundEffect() = {GetLocalAudio(My.Settings.SoundA), GetLocalAudio(My.Settings.SoundB, True)}
            Dim thumb = Texture2D.FromFile(Dev, "Cache\client\pp.png")
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                Select Case pl.Typ
                    Case SpielerTyp.Local
                        Spielers(i).CustomSound = sf
                        Spielers(i).MOTD = My.Settings.MOTD
                        If My.Settings.Thumbnail Then Spielers(i).Thumbnail = thumb
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

            If Not StopUpdating Then




                'Setze den lokalen Spieler
                If SpielerIndex > -1 AndAlso Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex

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
                                               FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = New Keyframe3D}
                                               HUDInstructions.Text = " "
                                               'Launch start animation
                                               StopUpdating = True
                                               SendBeginGaem()
                                           End Sub)
                    Case SpielStatus.SpielZuEnde
                        StopUpdating = True
                End Select

                'Implement timer
                Timer -= TimeSpan.FromSeconds(Time.DeltaTime)
                If Timer.Hours <> LastTimer.Hours And LastTimer.Hours > 0 Then
                    PostChat(LastTimer.Hours.ToString & " hours left!", Color.White)
                    SendMessage(LastTimer.Hours.ToString & " hours left!")
                ElseIf Timer.Minutes <> LastTimer.Minutes And LastTimer.Hours = 0 And (LastTimer.Minutes = 15 Or LastTimer.Minutes = 30 Or LastTimer.Minutes = 5 Or LastTimer.Minutes = 1) Then
                    PostChat(LastTimer.Minutes.ToString & " minutes left!", Color.White)
                    SendMessage(LastTimer.Minutes.ToString & " minutes left!")
                ElseIf Timer.TotalSeconds <> LastTimer.TotalSeconds And LastTimer.Hours = 0 And LastTimer.Minutes = 0 And LastTimer.Seconds = 0 And Not TimeOver Then
                    PostChat("Time over!", Color.White)
                    SendMessage("Time over!")
                    TimeOver = True
                End If
                LastTimer = Timer

                'Set HUD color
                HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name, "")
                HUDNameBtn.Color = hudcolors(If(SpielerIndex > -1, SpielerIndex, 0))
                HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (SpielerIndex > -1 AndAlso Spielers(SpielerIndex).Typ = SpielerTyp.Local)
            End If

            ''Network stuff
            'If NetworkMode Then
            '    If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
            '    If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Disconnected! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
            'End If

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
                        Case "e"c 'Suspend gaem
                            If Spielers(source).Typ = SpielerTyp.None Then Continue For
                            Spielers(source).Bereit = False
                            PostChat(Spielers(source).Name & " left!", Color.White)
                            If Not StopUpdating And Status <> SpielStatus.SpielZuEnde And Status <> SpielStatus.WarteAufOnlineSpieler Then PostChat("The game is being suspended!", Color.White)
                            If Status <> SpielStatus.WarteAufOnlineSpieler Then StopUpdating = True
                            StopWhenRealStart = True

                            SendPlayerLeft(source)
                        Case "m"c 'Sent chat message
                            Dim msg As String = element.Substring(2)
                            PostChat(msg, Color.White)
                        Case "n"c 'Switch player
                            'SwitchPlayer()
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
                        Case "y"c 'Sync requested
                            'SendSync()
                        Case "z"c 'Transmit user data
                            Dim s As New Threading.Thread(Sub()
                                                              Dim IdentSound As IdentType = CInt(element(2).ToString)
                                                              Dim dataNr As Integer = CInt(element(3).ToString)
                                                              Dim dat As String = element.Substring(4).Replace("_TATA_", "")
                                                              Try
                                                                  If dataNr = 9 Then
                                                                      'Receive pfp
                                                                      If IdentSound = IdentType.Custom Then
                                                                          IO.File.WriteAllBytes("Cache\server\" & Spielers(source).Name & "_pp.png", Compress.Decompress(Convert.FromBase64String(dat)))
                                                                          Spielers(source).Thumbnail = Texture2D.FromFile(Dev, "Cache\server\" & Spielers(source).Name & "_pp.png")
                                                                      End If
                                                                      SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & dataNr.ToString & "_TATA_" & dat)
                                                                  Else
                                                                      'Receive sound
                                                                      If IdentSound = IdentType.Custom Then
                                                                          IO.File.WriteAllBytes("Cache\server\" & Spielers(source).Name & dataNr.ToString & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                                                                          Spielers(source).CustomSound(dataNr) = SoundEffect.FromFile("Cache\server\" & Spielers(source).Name & dataNr.ToString & ".wav")
                                                                      Else
                                                                          Spielers(source).CustomSound(dataNr) = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                                                                      End If
                                                                      SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & dataNr.ToString & "_TATA_" & dat)
                                                                  End If

                                                              Catch ex As Exception
                                                                  'Data damaged, send standard sound
                                                                  If dataNr = 9 Then Exit Sub
                                                                  IdentSound = If(dataNr = 0, IdentType.TypeB, IdentType.TypeA)
                                                                  Spielers(source).CustomSound(dataNr) = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                                                                  SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & dataNr.ToString & "_TATA_")
                                                              End Try
                                                          End Sub) With {.Priority = Threading.ThreadPriority.BelowNormal}
                            s.Start()

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
            SendPlayerData()
            'SendSync()
        End Sub
        Private Sub SendChatMessage(index As Integer, text As String)
            SendNetworkMessageToAll("c" & index.ToString & text)
        End Sub
        Private Sub SendSetFigurePosition(pl As Integer, fig As Integer, pos As Integer)
            SendNetworkMessageToAll("d" & pl.ToString & fig.ToString & pos.ToString)
        End Sub
        Private Sub SendPlayerLeft(index As Integer)
            LocalClient.WriteStream("e" & index)
        End Sub
        Private Sub SendFlyingSaucerAdded(fields As Integer)
            SendNetworkMessageToAll("g" & fields.ToString)
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
        Private Sub SendPlayerBack(index As Integer)
            SendNetworkMessageToAll("r" & index.ToString)
            'SendSync()
            SendPlayerData()
        End Sub
        Private Sub SendWinFlag()
            'SendSync()
            SendNetworkMessageToAll("w")
        End Sub
        Private Sub SendGameActive()
            SendNetworkMessageToAll("x")
        End Sub
        Private Sub SendPlayerData()
            Dim dataSender As New Threading.Thread(Sub()
                                                       For i As Integer = 0 To Spielers.Length - 1
                                                           Dim pl = Spielers(i)
                                                           If pl.Typ = SpielerTyp.Local Or pl.Typ = SpielerTyp.CPU Then
                                                               'Send Sound A
                                                               Dim txt As String = ""
                                                               Dim snd As IdentType = GetPlayerAudio(i, False, txt)
                                                               SendNetworkMessageToAll("z" & i.ToString & CInt(snd).ToString & "0" & "_TATA_" & txt) 'Suffix "_TATA_" is to not print out in console

                                                               'Send Sound B
                                                               snd = GetPlayerAudio(i, True, txt)
                                                               SendNetworkMessageToAll("z" & i.ToString & CInt(snd).ToString & "1" & "_TATA_" & txt)

                                                               'Send Thumbnail
                                                               txt = ""
                                                               If My.Settings.Thumbnail And pl.Typ = SpielerTyp.Local Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache\client\pp.png")))
                                                               SendNetworkMessageToAll("z" & i.ToString & If(My.Settings.Thumbnail And pl.Typ = SpielerTyp.Local, CInt(IdentType.Custom), 0).ToString & "9" & "_TATA_" & txt)
                                                           End If
                                                       Next
                                                   End Sub) With {.Priority = Threading.ThreadPriority.BelowNormal}
            dataSender.Start()
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
            If Microsoft.VisualBasic.MsgBox("Do you really want to leave?", Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                SFX(2).Play()
                SendGameClosed()
                NetworkMode = False
                Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene))
            End If
        End Sub
#End Region
#Region "Debug Commands"

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

        Private ReadOnly Property IGameWindow_HUDNameBtn As Button Implements IGameWindow.HUDNameBtn
            Get
                Return HUDNameBtn
            End Get
        End Property

        Public ReadOnly Property IGameWindow_HUDmotdLabel As Label Implements IGameWindow.HUDmotdLabel
            Get
                Return HUDmotdLabel
            End Get
        End Property

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function

        Private ReadOnly Property IGameWindow_SelectFader As Single Implements IGameWindow.SelectFader
            Get
                Return SelectFader
            End Get
        End Property
#End Region
    End Class
End Namespace