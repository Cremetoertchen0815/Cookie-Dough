Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.Common
Imports Cookie_Dough.Game.DuoCard.Networking
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
    Public Class SlaveWindow
        Inherits Scene
        Implements ICardRendererWindow

        'Instance flags
        Friend Spielers As BaseCardPlayer() 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend Rejoin As Boolean = False
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
        Private WithEvents HUDBtnC As Button
        Private WithEvents HUDAfkBtn As Button
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
        Private DeckScroll As Integer
        Private InstructionFader As ITween(Of Color)
        Private Chat As List(Of (String, Color))

        'Keystack
        Private keysa As New List(Of Keys)
        Private ButtonStack As New List(Of Keys)
        Private oldpress As New List(Of Keys)

        'Spielfeld
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        Private Center As Vector2 'Gibt den Mittelpunkt des Screen-Viewports des Spielfelds an
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(0, 0, 0, 0, 0, 0, False)} 'Bewegt die Kamera New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False)
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False) 'Gibt die Standard-Position der Kamera an

        'Konstanten
        Private Const SyncLoc As Single = 1 / 2
        Private Const SyncVelocity As Single = 1 / 8
        Private Const SyncDirection As Single = 1 / 4

        Public Sub New(ins As OnlineGameInstance)
            LocalClient.AutomaticRefresh = False
            NetworkMode = False

            If Not LocalClient.JoinGame(ins, Sub(x)
                                                 'Load map info
                                                 PlCount = CInt(x())
                                                 GameMode = If(x(), GameMode.Casual, GameMode.Competetive)

                                                 'Load player info
                                                 ReDim Spielers(PlCount - 1)
                                                 UserIndex = CInt(x())
                                                 For i As Integer = 0 To PlCount - 1
                                                     Dim type As SpielerTyp = CInt(x())
                                                     Dim name As String = x()
                                                     Spielers(i) = New BaseCardPlayer(If(type = SpielerTyp.None, type, SpielerTyp.Online)) With {.Name = If(i = UserIndex, My.Settings.Username, name)}
                                                 Next

                                                 'Set rejoin flag
                                                 Rejoin = x() = "Rejoin"

                                                 'Load camera info
                                                 StdCam = New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False)
                                                 FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = If(Rejoin, StdCam, New Keyframe3D)}
                                             End Sub) Then LocalClient.AutomaticRefresh = True : Return

            'Bereite Flags und Variablen vor
            stat = CardGameState.WarteAufOnlineSpieler
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            SpielerIndex = -1
            MoveActive = False
            NetworkMode = True

            Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            LoadContent()
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ChatText"))
            Fanfare = Content.Load(Of Song)("bgm/fanfare")
            DamDamDaaaam = Content.Load(Of Song)("sfx/DamDamDaaam")

            'Lade HUD
            HUD = New GuiSystem
            HUDSoftBtn = New GameRenderable(Me) : HUD.Controls.Add(HUDSoftBtn)
            HUDBtnB = New Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnB)
            HUDBtnC = New Button("Mau", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnC)
            HUDArrowUp = New TextureButton(DebugTexture, New Vector2(935, 700), New Vector2(50, 20)) With {.Active = False} : HUD.Controls.Add(HUDArrowUp)
            HUDArrowDown = New TextureButton(DebugTexture, New Vector2(935, 970), New Vector2(50, 20)) With {.Active = False} : HUD.Controls.Add(HUDArrowDown)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Label(" ", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            HUDdbgLabel = New Label(Function() FigurFaderCamera.Value.ToString, New Vector2(500, 120)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDdbgLabel)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDmotdLabel)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
            HUDAfkBtn = New Button("AFK", New Vector2(220, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDAfkBtn)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = If(UserIndex > -1, hudcolors(UserIndex), Color.White)

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

        End Sub
        Public Overrides Sub Unload()
            Client.OutputDelegate = Sub(x) Return
        End Sub

        Private scheiß As New List(Of (Integer, Integer))

        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState()
            Dim kstate As KeyboardState = If(DebugConsole.Instance.IsOpen, Nothing, Keyboard.GetState())
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

            If Not StopUpdating And SpielerIndex = UserIndex Then

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
                                            SendPlayerCardLay(card_nr)
                                        Else
                                            HUDInstructions.Text = "Card invalid!"
                                        End If
                                        Exit For
                                    End If
                                Next
                            Case SelectionMode.Suit
                                For i As Integer = 0 To 3
                                    If (mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) AndAlso New Rectangle(i * 140 + 470, 740, 120, 200).Contains(mpos) Then
                                        SendCardLay(SuitStack(i))
                                        Exit For
                                    End If
                                Next
                        End Select

                        'Card stack pressed
                        If (mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) AndAlso New Rectangle(710, 420, 220, 270).Contains(mpos) AndAlso CardStack.Count > 0 Then SendCardStackPress()
                End Select
            End If

            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> CardGameState.SpielZuEnde Then StopUpdating = True : NetworkMode = False : MsgBoxer.EnqueueMsgbox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
                If LocalClient.LeaveFlag And Status <> CardGameState.SpielZuEnde Then StopUpdating = True : NetworkMode = False : MsgBoxer.EnqueueMsgbox("Disconnected! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
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
        Private Sub ReadAndProcessInputData()
            If MoveActive Then Return

            'Implement move active
            Dim data As String() = LocalClient.ReadStream()
            For Each element In data
                Dim command As Char = element(0)
                Select Case command
                    Case "a"c 'Player arrived
                        Dim source As Integer = element(1).ToString
                        Dim txt As String() = element.Substring(2).Split("|")
                        Spielers(source).Name = txt(0)
                        Spielers(source).MOTD = txt(1)
                        Spielers(source).Bereit = True
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
                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(1500), New Keyframe3D, StdCam, Sub() StopUpdating = True)
                        Automator.Add(FigurFaderCamera)
                    Case "c"c 'Sent chat message
                        Dim source As Integer = element(1).ToString
                        If source = 9 Then
                            Dim text As String = element.Substring(2)
                            PostChat("[Guest]: " & text, Color.Gray)
                        Else
                            PostChat("[" & Spielers(source).Name & "]: " & element.Substring(2), playcolor(source))
                        End If
                    Case "d"c 'Draw card
                        Dim card As New Card(CInt(element.Substring(2)), CInt(element(1).ToString))
                        StopUpdating = True
                        Renderer.TriggerDeckPullAnimation(Sub() Spielers(UserIndex).HandDeck.Add(card))
                    Case "e"c 'Suspend gaem
                        Dim who As Integer = element(1).ToString
                        StopUpdating = True
                        Spielers(who).Bereit = False
                        PostChat(Spielers(who).Name & " left!", Color.White)
                        PostChat("The game is being suspended!", Color.White)
                    Case "f"c 'Lay card
                        Dim card As New Card(CInt(element.Substring(2)), CInt(element(1).ToString))
                        TableCard = card
                        If SpielerIndex = UserIndex And card.Type = CardType.Jack Then
                            SelectionState = SelectionMode.Suit
                            Status = CardGameState.SelectAction
                        End If
                    Case "k"c
                        Dim source As Integer = CInt(element(1).ToString)
                        Spielers(source).CustomSound(0).Play()
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(1)
                        PostChat(msg, Color.White)
                    Case "n"c 'New player active
                        Dim who As Integer = element(1).ToString
                        SpielerIndex = who
                        HUDNameBtn.Active = True
                        If UserIndex < 0 Then Continue For
                        If who = UserIndex Then
                            PrepareMove()
                        Else
                            Status = CardGameState.Waitn
                            HUDBtnC.Active = False
                        End If
                    Case "r"c 'Player returned and sync every player
                        Dim source As Integer = element(1).ToString
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " is back!", Color.White)
                        HUDInstructions.Text = "Welcome back!"
                        SendSoundFile()
                    Case "s"c
                        BeSkipped = CInt(element(1).ToString) = 1
                        DrawForces = CInt(element(2).ToString)
                    Case "w"c 'Spieler hat gewonnen
                        HUDInstructions.Text = "Game over!"
                        If MediaPlayer.IsRepeating Then
                            MediaPlayer.Play(DamDamDaaaam)
                            MediaPlayer.Volume = 0.8
                        Else
                            MediaPlayer.Play(Fanfare)
                            MediaPlayer.Volume = 0.3
                        End If
                        MediaPlayer.IsRepeating = False

                        'Berechne Rankings
                        Core.Schedule(1, Sub()
                                             Dim ranks As New List(Of (Integer, Integer)) '(Spieler ID, Score)
                                             For i As Integer = 0 To PlCount - 1
                                                 ranks.Add((i, GetScore(i)))
                                             Next
                                             ranks = ranks.OrderBy(Function(x) x.Item2).ToList()
                                             ranks.Reverse()

                                             For i As Integer = 0 To ranks.Count - 1
                                                 Dim ia As Integer = i

                                                 Select Case i
                                                     Case 0
                                                         Core.Schedule(i, Sub() PostChat("1st place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                                                     Case 1
                                                         Core.Schedule(i, Sub() PostChat("2nd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                                                     Case 2
                                                         Core.Schedule(i, Sub() PostChat("3rd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                                                     Case Else
                                                         Core.Schedule(i, Sub() PostChat((ia + 1) & "th place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", playcolor(ranks(ia).Item1)))
                                                 End Select
                                             Next

                                             'Update K/D
                                             If ranks(0).Item1 = UserIndex Then
                                                 If GameMode = GameMode.Competetive Then My.Settings.GamesWon += 1
                                             Else
                                                 If GameMode = GameMode.Competetive Then My.Settings.GamesLost += 1
                                             End If
                                             My.Settings.Save()
                                         End Sub)
                        'Set flags
                        Status = CardGameState.SpielZuEnde
                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                    Case "x"c 'Continue with game
                        StopUpdating = False
                    Case "y"c 'Synchronisiere Daten
                        Dim str As String = element.Substring(1)
                        Dim sp As SyncMessage = Newtonsoft.Json.JsonConvert.DeserializeObject(Of SyncMessage)(str)
                        For i As Integer = 0 To PlCount - 1
                            Spielers(i).HandDeck.Clear()
                            Spielers(i).HandDeck.AddRange(sp.Spielers(i).HandDeck)
                            Spielers(i).Name = sp.Spielers(i).Name
                            Spielers(i).OriginalType = sp.Spielers(i).OriginalType
                            Spielers(i).MOTD = sp.Spielers(i).MOTD
                            Spielers(i).IsAFK = sp.Spielers(i).IsAFK
                        Next
                        TableCard = sp.TableCard
                        If UserIndex > -1 Then HUDAfkBtn.Text = If(Spielers(UserIndex).IsAFK, "Back Again", "AFK")
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

        'BOOTI PLS PLAE DMC 2
        'DANTE IS GUD IS DE BÄST PLAYE DMC 2 PLSSS
        Friend Sub SendArrived()
            If UserIndex < 0 Then
                LocalClient.WriteStream("y") 'request refresh
                Return
            End If

            If Rejoin Then
                LocalClient.WriteStream("r" & My.Settings.Username & "|" & My.Settings.MOTD & "|" & My.Settings.UniqueIdentifier) 'Rejoin
            Else
                LocalClient.WriteStream("a" & My.Settings.Username & "|" & My.Settings.MOTD & "|" & My.Settings.UniqueIdentifier) 'Nujoin
            End If
        End Sub
        Private Sub SendChatMessage(text As String)
            LocalClient.WriteStream("c" & text)
        End Sub
        Private Sub SendDrawCard()
            LocalClient.WriteStream("d")
        End Sub
        Private Sub SendPlayerCardLay(card As Integer)
            LocalClient.WriteStream("f" & card.ToString)
        End Sub
        Private Sub SendGameClosed()
            LocalClient.WriteStream("e")
        End Sub
        Private Sub SendCardLay(card As Card)
            LocalClient.WriteStream("g" & CInt(card.Suit).ToString & CInt(card.Type).ToString)
        End Sub
        Private Sub SendAfkSignal() Handles HUDAfkBtn.Clicked
            LocalClient.WriteStream("i")
        End Sub
        Private Sub SendMauSignal() Handles HUDBtnC.Clicked
            If Spielers(UserIndex).HandDeck.Count <> 2 Then Return
            HUDBtnC.Active = False
            LocalClient.WriteStream("i")
        End Sub
        Private Sub SendCardStackPress()
            LocalClient.WriteStream("p")
        End Sub

        Private Sub SendSoundFile()
            If UserIndex < 0 Then Return

            Dim dataSender As New Threading.Thread(Sub()
                                                       Dim txt As String = ""
                                                       If My.Settings.SoundA = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundA.audio")))
                                                       LocalClient.WriteStream("z" & My.Settings.SoundA.ToString & "0" & "_TATA_" & txt)

                                                       txt = ""
                                                       If My.Settings.SoundB = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundB.audio")))
                                                       LocalClient.WriteStream("z" & My.Settings.SoundB.ToString & "1" & "_TATA_" & txt)

                                                       txt = ""
                                                       If My.Settings.Thumbnail Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/pp.png")))
                                                       LocalClient.WriteStream("z" & If(My.Settings.Thumbnail, IdentType.Custom, 0).ToString & "9" & "_TATA_" & txt)
                                                   End Sub) With {.Priority = Threading.ThreadPriority.BelowNormal}
            dataSender.Start()
        End Sub

        Private Sub SubmitResults(figur As Integer, destination As Integer)
            Core.Schedule(0.5, Sub()
                                   If destination < 0 Then
                                       LocalClient.WriteStream("n")
                                   Else
                                       LocalClient.WriteStream("s" & figur & destination)
                                   End If
                               End Sub)
        End Sub
#End Region

#Region "Hilfsfunktionen"

        Private Function GetLocalAudio(ident As IdentType, Optional IsSoundB As Boolean = False) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content/prep/audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache/client/sound" & If(IsSoundB, "B", "A") & ".audio")
            End If
        End Function

        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub
        Private Function GetScore(i As Integer) As Integer
            Return 0
        End Function

        Private Function CheckWin() As Boolean
            For Each element In Spielers
                If element.Typ <> SpielerTyp.None AndAlso element.HandDeck.Count = 0 Then Return True
            Next
            Return False
        End Function
        Private Function IsLayingCardValid(card As Card) As Boolean
            'Check if card has to be drawn
            If Spielers(UserIndex).HandDeck.Count < 2 And HUDBtnC.Active Then
                StopUpdating = True
                If Spielers(UserIndex).HandDeck.Count = 0 Then PostChat("You forgot to Mau Mau!", Color.White) : SendChatMessage("You forgot to Mau Mau!")
                If Spielers(UserIndex).HandDeck.Count = 1 Then PostChat("You forgot to Mau!", Color.White) : SendChatMessage("You forgot to Mau!")
                SendDrawCard()
                Return False
            End If

            If BeSkipped And card.Type <> CardType.Eight Then Return False
            If DrawForces > 0 And card.Type <> CardType.Seven Then Return False
            Return card.Suit = TableCard.Suit Or card.Type = TableCard.Type Or card.Type = CardType.Jack
        End Function

        Private Function DrawRandomCard() As Card
            Dim indx As Integer = Nez.Random.Range(0, CardStack.Count)
            Dim ret As Card = CardStack(indx)
            CardStack.RemoveAt(indx)
            Return ret
        End Function

        Private Sub PrepareMove()
            Status = CardGameState.SelectAction
            SelectionState = SelectionMode.Standard
            HUDInstructions.Text = "Place a card!"
            'Check Mau button
            HUDBtnC.Active = True
            If Spielers(UserIndex).HandDeck.Count = 1 Then HUDBtnC.Text = "Mau Mau" Else HUDBtnC.Text = "Mau"
        End Sub
#End Region

#Region "Knopfgedrücke"

        Private chatbtnpressed As Boolean = False

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
            SFX(2).Play()
            LaunchInputBox(Sub(x)
                               SendChatMessage(x)
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
            MsgBoxer.OpenMsgbox("Do you really want to leave?", Sub(x)
                                                                    If x = 1 Then Return
                                                                    SFX(2).Play()
                                                                    SendGameClosed()
                                                                    NetworkMode = False
                                                                    Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene))
                                                                End Sub, {"Yeah", "Nope"})
        End Sub
        Private Sub ScrollUp() Handles HUDArrowUp.Clicked
            DeckScroll = Math.Max(0, DeckScroll - 1)
        End Sub
        Private Sub ScrollDown() Handles HUDArrowDown.Clicked
            DeckScroll = Math.Min(Math.Floor((HandDeck.Count - 1) / 7), DeckScroll + 1)
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

        Private ReadOnly Property ICardRendererWindow_Spielers As BaseCardPlayer() Implements ICardRendererWindow.Spielers
            Get
                Return Spielers
            End Get
        End Property
#End Region
    End Class
End Namespace