Imports System.Collections.Generic
Imports System.Linq
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.CommonCards
Imports Cookie_Dough.Game.DuoCard.Rendering
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
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Private SelectionState As SelectionMode = SelectionMode.Standard
        Private stat As CardGameState 'Speichert den aktuellen Status des Spiels
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private StopWhenRealStart As Boolean = False
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private Timer As TimeSpan 'Misst die Zeit seit dem Anfang des Spiels
        Private LastTimer As TimeSpan 'Gibt den Timer des vergangenen Frames an
        Private TimeOver As Boolean = False 'Gibt an, ob die registrierte Zeit abgelaufen ist
        Private CardStack As List(Of Card)
        Private BeSkipped As Boolean = False
        Private DrawForces As Integer = 0

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
        ' Private WithEvents HUDBtnC As Button
        'Private WithEvents HUDBtnD As Button
        Private WithEvents HUDArrowUp As TextureButton
        Private WithEvents HUDArrowDown As TextureButton
        Private WithEvents HUDChat As TextscrollBox
        Private WithEvents HUDChatBtn As Button
        Private WithEvents HUDInstructions As Label
        Private WithEvents HUDNameBtn As Button
        Private WithEvents HUDFullscrBtn As Button
        Private WithEvents HUDMusicBtn As Button
        Private WithEvents HUDdbgLabel As Label
        Private WithEvents HUDmotdLabel As Label
        Private WithEvents HUDSoftBtn As GameRenderable
        Private WithEvents HUDAfkBtn As Button
        Private DeckScroll As Integer
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
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(0, 0, 0, 0, 0, 0, False)} 'Bewegt die Kamera New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False)
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False) 'Gibt die Standard-Position der Kamera an

        'Konstanten
        Private Const FigurSpeed As Integer = 450
        Private Const ErrorCooldown As Integer = 1
        Private Const CPUThinkingTime As Single = 0.6
        Private Const CamSpeed As Integer = 1300

        Sub New()
            'Bereite Flags und Variablen vor
            stat = CardGameState.WarteAufOnlineSpieler
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            SpielerIndex = -1
            PlCount = 4
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
            HUDSoftBtn = New GameRenderable(Me) : HUD.Controls.Add(HUDSoftBtn)
            HUDBtnB = New Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnB)
            HUDArrowUp = New TextureButton(DebugTexture, New Vector2(935, 700), New Vector2(50, 20)) With {.Active = False} : HUD.Controls.Add(HUDArrowUp)
            HUDArrowDown = New TextureButton(DebugTexture, New Vector2(935, 970), New Vector2(50, 20)) With {.Active = False} : HUD.Controls.Add(HUDArrowDown)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            HUDdbgLabel = New Label(Function() FigurFaderCamera.Value.ToString, New Vector2(500, 120)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDdbgLabel)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDmotdLabel)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
            HUDAfkBtn = New Button("AFK", New Vector2(220, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDAfkBtn)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = Color.White

            Renderer = AddRenderer(New CardRenderer(Me, -1))
            Psyground = AddRenderer(New PsygroundRenderer(0, 0.3))

            AddRenderer(New DefaultRenderer(1))
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.4).SetThreshold(0.15)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            Center = New Rectangle(500, 70, 950, 950).Center.ToVector2
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(LoopType.PingPong, -1).Start()

            'Generate card stack
            CardStack = New List(Of Card)
            For i As Integer = 1 To 13
                For j As Integer = 0 To 3
                    CardStack.Add(New Card(i, j))
                Next
            Next

            'Load sounds, MOTDs and decks
            Dim sf As SoundEffect() = {GetLocalAudio(My.Settings.SoundA), GetLocalAudio(My.Settings.SoundB, True)}
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                Select Case pl.Typ
                    Case SpielerTyp.Local
                        Spielers(i).MOTD = My.Settings.MOTD
                    Case SpielerTyp.CPU
                        Spielers(i).MOTD = CPU_MOTDs(i)
                End Select
                If Spielers(i).Typ <> SpielerTyp.None Then Spielers(i).HandDeck = New List(Of Card) From {DrawRandomCard(), DrawRandomCard(), DrawRandomCard(), DrawRandomCard(), DrawRandomCard(), DrawRandomCard(), DrawRandomCard()}
            Next
            'Set table card
            TableCard = DrawRandomCard()
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


            'Move cam
            If dbgCamFree Then FigurFaderCamera.Value += New Keyframe3D(If(kstate.IsKeyDown(Keys.A), -1, 0) + If(kstate.IsKeyDown(Keys.D), 1, 0), If(kstate.IsKeyDown(Keys.S), -1, 0) + If(kstate.IsKeyDown(Keys.W), 1, 0), If(kstate.IsKeyDown(Keys.LeftShift), -1, 0) + If(kstate.IsKeyDown(Keys.Space), 1, 0), If(kstate.IsKeyDown(Keys.J), -0.01, 0) + If(kstate.IsKeyDown(Keys.L), 0.01, 0), If(kstate.IsKeyDown(Keys.K), -0.01, 0) + If(kstate.IsKeyDown(Keys.I), 0.01, 0), If(kstate.IsKeyDown(Keys.RightShift), -0.01, 0) + If(kstate.IsKeyDown(Keys.Enter), 0.01, 0), True) : HUDdbgLabel.Active = True

            If Not StopUpdating Then

                'Prüfe, ob die Runde gewonnen wurde und beende gegebenenfalls die Runde
                If CheckWin() Or TimeOver Or dbgEnd Then
                    'Cue music
                    If MediaPlayer.IsRepeating Then
                        MediaPlayer.Play(DamDamDaaaam)
                        MediaPlayer.Volume = 0.8
                    Else
                        MediaPlayer.Play(Fanfare)
                        MediaPlayer.Volume = 0.3
                    End If
                    MediaPlayer.IsRepeating = False
                    StopUpdating = True
                    dbgEnd = False
                    HUDInstructions.Text = "Game over!"

                    'Berechne Rankings
                    Dim ranks As New List(Of (Integer, Integer)) '(Spieler ID, Score)
                    For i As Integer = 0 To PlCount - 1
                        ranks.Add((i, GetScore(i)))
                    Next
                    ranks = ranks.OrderBy(Function(x) x.Item2).ToList()
                    ranks.Reverse()

                    'Display ranks
                    For i As Integer = 0 To ranks.Count - 1
                        Dim ia As Integer = i
                        Select Case i
                            Case 0
                                Core.Schedule(1 + i, Sub() PostChat("1st place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                            Case 1
                                Core.Schedule(1 + i, Sub() PostChat("2nd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                            Case 2
                                Core.Schedule(1 + i, Sub() PostChat("3rd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                            Case Else
                                Core.Schedule(1 + i, Sub() PostChat((ia + 1) & "th place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                        End Select
                    Next

                    If GameMode = GameMode.Competetive Then
                        'Update highscores
                        Core.Schedule(ranks.Count + 1, AddressOf SendHighscore)
                        'Update K/D
                        If Spielers(ranks(0).Item1).Typ = SpielerTyp.Local Then My.Settings.GamesWon += 1 Else My.Settings.GamesLost += 1
                        My.Settings.Save()
                    End If

                    'Set flags
                    SendWinFlag()
                    Status = CardGameState.SpielZuEnde
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                End If

                'TODO: Show remaining cards in player title
                Select Case Status
                    Case CardGameState.SelectAction
                        Select Case SelectionState
                            Case SelectionMode.Standard
                                'Lay down card
                                For i As Integer = 0 To 6
                                    Dim card_nr As Integer = i + 7 * DeckScroll
                                    If card_nr >= HandDeck.Count Then Exit For

                                    If (mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) AndAlso New Rectangle(i * 140 + 470, 740, 120, 200).Contains(mpos) Then
                                        Dim card = Spielers(UserIndex).HandDeck(card_nr)
                                        If IsLayingCardValid(card) Then
                                            'Read and pop card from hand deck
                                            Spielers(UserIndex).HandDeck.RemoveAt(card_nr)
                                            DebugConsole.Instance.Log(card.ToString)
                                            LayCard(card)
                                        Else
                                            HUDInstructions.Text = "Card invalid!"
                                        End If
                                        Exit For
                                    End If
                                Next
                            Case SelectionMode.Suit
                                For i As Integer = 0 To 3
                                    If (mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) AndAlso New Rectangle(i * 140 + 470, 740, 120, 200).Contains(mpos) Then
                                        LayCard(SuitStack(i))
                                        Exit For
                                    End If
                                Next
                        End Select

                        'Card stack pressed
                        If (mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) AndAlso New Rectangle(710, 420, 220, 270).Contains(mpos) AndAlso CardStack.Count > 0 Then
                            If BeSkipped Then
                                'Skip player
                                StopUpdating = True
                                BeSkipped = False
                                Core.Schedule(0.5, AddressOf SwitchPlayer)
                            ElseIf DrawForces > 0 Then
                                'Draw cards that you're forced to
                                StopUpdating = True

                                If DrawForces > 0 Then
                                    DrawForces -= 1
                                    Renderer.TriggerDeckPullAnimation(AddressOf CheckDrawForces)
                                Else
                                    Core.Schedule(0.5, AddressOf SwitchPlayer)
                                End If
                            Else
                                'Draw card
                                If Renderer.card_deck_top_pos Is Nothing OrElse Renderer.card_deck_top_pos.State <> TransitionState.InProgress Then Renderer.TriggerDeckPullAnimation(AddressOf CheckDrawForces)
                            End If
                        End If
                    Case CardGameState.WarteAufOnlineSpieler

                        HUDInstructions.Text = "Waiting for all players to connect..."

                        'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                        For Each sp In Spielers
                            If sp Is Nothing OrElse Not sp.Bereit Then Exit Select 'Falls ein Spieler noch nicht belegt/bereit, breche Spielstart ab
                        Next

                        'Falls vollzählig, starte Spiel
                        StopUpdating = True
                        Core.Schedule(0.8, Sub()
                                               PostChat("The game has started!", Color.White)
                                               FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(1500), New Keyframe3D, StdCam, Sub()
                                                                                                                                                                                    SwitchPlayer()
                                                                                                                                                                                    If StopWhenRealStart Then StopUpdating = True
                                                                                                                                                                                End Sub)
                                               Automator.Add(FigurFaderCamera)
                                               SendBeginGaem()
                                           End Sub)
                End Select
            End If

            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> CardGameState.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
                If LocalClient.LeaveFlag And Status <> CardGameState.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Disconnected! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
            End If

            If NetworkMode Then ReadAndProcessInputData()

            'Misc things
            If kstate.IsKeyDown(Keys.Escape) And lastkstate.IsKeyUp(Keys.Escape) Then MenuButton()
            lastmstate = mstate
            lastkstate = kstate
            DeckScroll = Mathf.Clamp(DeckScroll, 0, CInt(Math.Floor((HandDeck.Count - 1) / 7.0F)))
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
                        If Not StopUpdating And Status <> CardGameState.SpielZuEnde And Status <> CardGameState.WarteAufOnlineSpieler Then PostChat("The game is being suspended!", Color.White)
                        If Status <> CardGameState.WarteAufOnlineSpieler Then StopUpdating = True
                        'If Renderer.BeginTriggered Then StopWhenRealStart = True

                        SendPlayerLeft(source)
                    Case "f"c 'Slave layed down card
                        Dim card_nr As Integer = CInt(element.Substring(2))
                        Dim card As Card = Spielers(source).HandDeck(card_nr)
                        'Place card
                        Spielers(SpielerIndex).HandDeck.RemoveAt(card_nr)
                        DebugConsole.Instance.Log(Card.ToString)
                        LayCard(card)
                    Case "g"c
                        Dim card As New Card(CInt(element.Substring(3)), CInt(element(2).ToString))
                        TableCard = card
                        StopUpdating = True
                        Core.Schedule(0.5, AddressOf SwitchPlayer)
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(2)
                        PostChat(msg, Color.White)
                    Case "n"c 'Switch player
                        SwitchPlayer()
                    Case "p"c 'Card stack pressed
                        If BeSkipped Then
                            'Skip player
                            BeSkipped = False
                            Core.Schedule(0.5, AddressOf SwitchPlayer)
                        ElseIf DrawForces > 0 Then
                            'Draw cards that you're forced to
                            StopUpdating = True

                            DrawForces -= 1
                            Renderer.TriggerDeckPullAnimation(AddressOf CheckDrawForces)
                        Else
                            'Draw card
                            If Renderer.card_deck_top_pos Is Nothing OrElse Renderer.card_deck_top_pos.State <> TransitionState.InProgress Then Renderer.TriggerDeckPullAnimation(AddressOf CheckDrawForces)
                        End If
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
                        If everythere And Status <> CardGameState.WarteAufOnlineSpieler Then StopUpdating = False : SendGameActive()
                        If everythere And StopWhenRealStart Then StopWhenRealStart = False
                    Case "y"c
                        SendSync()
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

        Private Sub SendLayCard(card As Card)
            LocalClient.WriteStream("f" & CInt(card.Suit).ToString & CInt(card.Type).ToString)
        End Sub
        Private Sub SendHighscore()
            Dim pls As New List(Of (String, Integer))
            For i As Integer = 0 To Spielers.Length - 1
                If Spielers(i).Typ = SpielerTyp.Local Or Spielers(i).Typ = SpielerTyp.Online Then
                    pls.Add((Spielers(i).Name, GetScore(i)))
                End If
            Next
            SendNetworkMessageToAll("h" & 0.ToString & Newtonsoft.Json.JsonConvert.SerializeObject(pls))
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
        Private Sub SendParamUpdate()
            SendNetworkMessageToAll("s" & If(BeSkipped, 1, 0).ToString & DrawForces.ToString)
        End Sub
        Private Sub SendPlayerBack(index As Integer)
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers))
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
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers) With {.TableCard = TableCard})
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

        Private Function IsLayingCardValid(card As Card) As Boolean
            If BeSkipped And card.Type <> CardType.Eight Then Return False
            If DrawForces > 0 And card.Type <> CardType.Seven Then Return False
            Return card.Suit = TableCard.Suit Or card.Type = TableCard.Type Or card.Type = CardType.Jack
        End Function
        Private Function GetScore(i As Integer) As Integer
            Return 0
        End Function

        Private Function CheckWin() As Boolean
            For Each element In Spielers
                If element.Typ <> SpielerTyp.None AndAlso element.HandDeck.Count = 0 Then Return True
            Next
            Return False
        End Function

        Private Sub CheckDrawForces()
            Dim res As Card = DrawRandomCard()
            Spielers(SpielerIndex).HandDeck.Add(res)
            SendDrawCard(res)
            StopUpdating = True

            If DrawForces > 0 Then
                DrawForces -= 1
                Renderer.TriggerDeckPullAnimation(AddressOf CheckDrawForces)
            Else
                Core.Schedule(0.5, AddressOf SwitchPlayer)
            End If
        End Sub

        Private Sub LayCard(card As Card)
            TableCard = card
            If CardStack.Contains(card) Then CardStack.Remove(card)
            Status = CardGameState.CardAnimationActive
            SendLayCard(card)

            Select Case card.Type
                Case CardType.Jack
                    If SpielerIndex = UserIndex Then Core.Schedule(0.5, Sub()
                                                                            HUDInstructions.Text = "Select wishing suit!"
                                                                            SelectionState = SelectionMode.Suit
                                                                            Status = CardGameState.SelectAction
                                                                        End Sub)
                Case CardType.Seven
                    DrawForces += 2
                    StopUpdating = True
                    Core.Schedule(0.5, AddressOf SwitchPlayer)
                Case CardType.Eight
                    BeSkipped = True
                    StopUpdating = True
                    Core.Schedule(0.5, AddressOf SwitchPlayer)
                Case CardType.Nine
                    If SpielerIndex = UserIndex Then Status = CardGameState.SelectAction : SelectionState = SelectionMode.Standard
                Case Else
                    StopUpdating = True
                    Core.Schedule(0.5, AddressOf SwitchPlayer)
            End Select
        End Sub

        Private Function DrawRandomCard() As Card
            Dim indx As Integer = Nez.Random.Range(0, CardStack.Count)
            Dim ret As Card = CardStack(indx)
            CardStack.RemoveAt(indx)
            Return ret
        End Function

        Private Sub SwitchPlayer()

            'Setze benötigte Flags
            SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Do While Spielers(SpielerIndex).Typ = SpielerTyp.None
                SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Loop
            'Setze HUD flags
            If Spielers(SpielerIndex).Typ <> SpielerTyp.Online Then Status = CardGameState.SelectAction Else Status = CardGameState.Waitn
            SendNewPlayerActive(SpielerIndex)
            If Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex
            HUD.Color = hudcolors(UserIndex)
            'Reset camera if not already moving
            If FigurFaderCamera.State <> TransitionState.InProgress Then FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = StdCam}
            'Set game flags
            StopUpdating = False
            SelectionState = SelectionMode.Standard
            HUDArrowUp.Active = Spielers(UserIndex).Typ = SpielerTyp.Local
            HUDArrowDown.Active = Spielers(UserIndex).Typ = SpielerTyp.Local
            SendParamUpdate()
            SendGameActive()
            ResetHUD()
            HUDInstructions.Text = "Lol"
        End Sub

        Private Sub ResetHUD()
            'HUDBtnC.Active = Not Spielers(SpielerIndex).Angered And SpielerIndex = UserIndex
            'HUDBtnD.Active = SpielerIndex = UserIndex
            'HUDBtnD.Text = If(Spielers(SpielerIndex).SacrificeCounter <= 0, "Sacrifice", "(" & Spielers(SpielerIndex).SacrificeCounter & ")")
            HUDAfkBtn.Text = If(Spielers(SpielerIndex).IsAFK, "Back Again", "AFK")
            HUD.TweenColorTo(If(UserIndex >= 0, hudcolors(UserIndex), Color.White), 0.5).SetEaseType(EaseType.CubicInOut).Start()
            HUDNameBtn.Active = True
        End Sub
#End Region
#Region "Knopfgedrücke"

        Dim chatbtnpressed As Boolean = False

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
            SFX(2).Play()
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
                Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
            End If
        End Sub
        Private Sub ScrollUp() Handles HUDArrowUp.Clicked
            DeckScroll = Math.Max(0, DeckScroll - 1)
        End Sub
        Private Sub ScrollDown() Handles HUDArrowDown.Clicked
            DeckScroll = Math.Min(Math.Floor((HandDeck.Count - 1) / 7), DeckScroll + 1)
        End Sub
#End Region
#Region "Debug Commands"


        <Command("dc-cam-free", "Sets up the camera in a specific way.")>
        Public Shared Sub dbgCamFreeS()
            dbgCamFree = True
        End Sub
#End Region
#Region "Schnittstellenimplementation"


        Private ReadOnly Property IGameWindow_SelectFader As Single Implements ICardRendererWindow.SelectFader
            Get
                Return SelectFader
            End Get
        End Property

        Private ReadOnly Property IGameWindow_SpielerIndex As Integer Implements ICardRendererWindow.SpielerIndex
            Get
                Return SpielerIndex
            End Get
        End Property

        Private ReadOnly Property IGameWindow_UserIndex As Integer Implements ICardRendererWindow.UserIndex
            Get
                Return UserIndex
            End Get
        End Property

        Public ReadOnly Property BGTexture As Texture2D Implements ICardRendererWindow.BGTexture
            Get
                Return Renderer.RenderTexture
            End Get
        End Property

        Private SuitStack = New List(Of Card) From {New Card(CardType.Clear, CardSuit.Clubs), New Card(CardType.Clear, CardSuit.Diamonds), New Card(CardType.Clear, CardSuit.Hearts), New Card(CardType.Clear, CardSuit.Spades)}
        Public ReadOnly Property HandDeck As List(Of Card) Implements ICardRendererWindow.HandDeck
            Get
                If SelectionState = SelectionMode.Suit Then Return SuitStack
                Return Spielers(UserIndex).HandDeck
            End Get
        End Property

        Public Property TableCard As Card Implements ICardRendererWindow.TableCard

        Private ReadOnly Property ICardRendererWindow_DeckScroll As Integer Implements ICardRendererWindow.DeckScroll
            Get
                Return DeckScroll
            End Get
        End Property

        Public Function GetCamPos() As Keyframe3D Implements ICardRendererWindow.GetCamPos
            If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function

        Friend Property Status As CardGameState Implements ICardRendererWindow.State
            Get
                Return stat
            End Get
            Set(value As CardGameState)
                stat = value
                HUDArrowUp.Active = value = CardGameState.SelectAction
                HUDArrowDown.Active = value = CardGameState.SelectAction
            End Set
        End Property
#End Region
    End Class
End Namespace