Imports System.Collections.Generic
Imports System.Linq
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.BetretenVerboten.Renderers
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Nez.Console
Imports Nez.Tweens

Namespace Game.BetretenVerboten
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class GameRoom
        Inherits Scene
        Implements IGameWindow

        'Instance flags
        Friend Spielers As Player() 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend PlCount As Integer 'Gibt an wieviele Spieler das Spiel enthält
        Friend FigCount As Integer 'Gibt an wieviele Figuren jeder Spieler hat
        Friend SpceCount As Integer 'Gibt an wieviele Felder jeder Spieler besitzt
        Friend NetworkMode As Boolean = False 'Gibt an, ob das Spiel über das Netzwerk kommunuziert
        Friend SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
        Friend Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Friend Map As GaemMap 'Gibt die Map an, die verwendet wird
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private Timer As TimeSpan 'Misst die Zeit seit dem Anfang des Spiels
        Private LastTimer As TimeSpan 'Gibt den Timer des vergangenen Frames an
        Private TimeOver As Boolean = False 'Gibt an, ob die registrierte Zeit abgelaufen ist

        'Game flags
        Private WürfelAktuelleZahl As Integer 'Speichert den WErt des momentanen Würfels
        Private WürfelWerte As Integer() 'Speichert die Werte der Würfel
        Private WürfelTimer As Double 'Wird genutzt um den Würfelvorgang zu halten
        Private WürfelAnimationTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
        Private WürfelTriggered As Boolean 'Gibt an ob gerade gewürfelt wird
        Private DreifachWürfeln As Boolean 'Gibt an(am Anfang des Spiels), dass ma drei Versuche hat um eine 6 zu bekommen
        Private Fahrzahl As Integer 'Anzahl der Felder die gefahren werden kann
        Private MoveActive As Boolean = False 'Gibt an, ob eine Figuranimation in Gange ist
        Private SaucerFields As New List(Of Integer)
        Private DontKickSacrifice As Boolean 'Gibt an, ob die zu opfernde Figur nicht gekickt werden soll

        'Assets
        Private Fanfare As Song
        Private DamDamDaaaam As Song
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont

        'Renderer
        Friend Renderer As Renderer3D
        Friend Psyground As PsygroundRenderer

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtnB As Button
        Private WithEvents HUDBtnC As Button
        Private WithEvents HUDBtnD As Button
        Private WithEvents HUDChat As TextscrollBox
        Private WithEvents HUDChatBtn As Button
        Private WithEvents HUDInstructions As Label
        Private WithEvents HUDNameBtn As Button
        Private WithEvents HUDFullscrBtn As Button
        Private WithEvents HUDMusicBtn As Button
        Private WithEvents HUDdbgLabel As Label
        Private WithEvents HUDmotdLabel As Label
        Private WithEvents HUDDiceBtn As GameRenderable
        Private InstructionFader As ITween(Of Color)
        Private ShowDice As Boolean = False
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
        Friend FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
        Friend FigurFaderEnd As Single 'Gibt an auf welchem Feld der Zug enden soll
        Friend FigurFaderXY As Transition(Of Vector2) 'Bewegt die zu animierende Figur auf der X- und Y-Achse
        Friend FigurFaderZ As Transition(Of Integer)  'Bewegt die zu animierende Figur auf der Z-Achse
        Friend FigurFaderScales As New Dictionary(Of (Integer, Integer), Transition(Of Single)) 'Gibt die Skalierung für einzelne Figuren an Key: (Spieler ID, Figur ID) Value: Transition(Z)
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(0, 0, 0, 0, 0, 0, False)} 'Bewegt die Kamera 
        Friend CPUTimer As Single 'Timer-Flag um der CPU etwas "Überlegzeit" zu geben
        Friend PlayStompSound As Boolean 'Gibt an, ob der Stampf-Sound beim Landen(Kicken) gespielt werden soll
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False) 'Gibt die Standard-Position der Kamera an

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
        Sub New(Map As GaemMap)
            'Bereite Flags und Variablen vor
            Status = SpielStatus.WarteAufOnlineSpieler
            WürfelTimer = 0
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            Status = SpielStatus.WarteAufOnlineSpieler
            SpielerIndex = -1
            MoveActive = False
            Me.Map = Map

            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            Select Case Map
                Case GaemMap.Default4Players
                    Timer = New TimeSpan(0, 1, 11, 11, 11)
                    Player.DefaultArray = {-1, -1, -1, -1}
                    FigCount = 4
                    PlCount = 4
                    SpceCount = 10
                    SaucerChance = 18
                Case GaemMap.Default6Players
                    Timer = New TimeSpan(0, 1, 11, 11, 11)
                    Player.DefaultArray = {-1, -1} '{-1, -1}
                    FigCount = 2
                    PlCount = 6
                    SpceCount = 8
                    SaucerChance = 14
                Case GaemMap.Default8Players
                    Timer = New TimeSpan(0, 1, 11, 11, 11)
                    Player.DefaultArray = {-1, -1}
                    FigCount = 2
                    PlCount = 8
                    SpceCount = 7
                    SaucerChance = 10
            End Select
            LastTimer = Timer

            If Spielers Is Nothing Then Spielers = {New Player(SpielerTyp.Local, Difficulty.Smart), New Player(SpielerTyp.Local, Difficulty.Smart), New Player(SpielerTyp.Local, Difficulty.Smart), New Player(SpielerTyp.Local, Difficulty.Smart)}
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
            HUDBtnC = New Button("Anger", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnC)
            HUDBtnD = New Button("Sacrifice", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnD)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            HUDdbgLabel = New Label(Function() FigurFaderCamera.Value.ToString, New Vector2(500, 120)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDdbgLabel)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDmotdLabel)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Button("", New Vector2(500, 700), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = hudcolors(0)

            Renderer = AddRenderer(New Renderer3D(Me, -1))
            Psyground = AddRenderer(New PsygroundRenderer(0, 0.3))
            AddRenderer(New DefaultRenderer(1))

            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            Center = New Rectangle(500, 70, 950, 950).Center.ToVector2
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(LoopType.PingPong, -1).Start()

            Dim sf As SoundEffect() = {GetLocalAudio(My.Settings.SoundA), GetLocalAudio(My.Settings.SoundB, True)}
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                Select Case pl.Typ
                    Case SpielerTyp.Local
                        Spielers(i).CustomSound = sf
                    Case SpielerTyp.CPU
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
            Dim kstate As KeyboardState = Keyboard.GetState()
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

            'Eject command
            If dbgKickuser > -1 Then
                Dim everythere As Boolean = StopUpdating
                For i As Integer = 0 To Spielers.Length - 1
                    Dim pl = Spielers(i)
                    If i = dbgKickuser Then
                        pl.Typ = SpielerTyp.None
                        For j As Integer = 0 To pl.Spielfiguren.Length - 1
                            SendSetFigurePosition(i, j, -1)
                            pl.Spielfiguren(j) = -1
                        Next
                        ' "Bring player back to the game" if left
                        If Not pl.Bereit Then
                            pl.Bereit = True
                            SendPlayerBack(i)
                        End If
                        If SpielerIndex = i Then SwitchPlayer()
                    ElseIf pl.Bereit = False Then
                        everythere = False
                    End If
                Next
                If everythere Then StopUpdating = False : SendGameActive()
                dbgKickuser = -1
            End If

            'Sync command
            If dbgExecSync Then
                dbgExecSync = False
                SendSync()
            End If

            'Place command
            If dbgPlaceSet Then
                Spielers(dbgPlaceCmd.Item1).Spielfiguren(dbgPlaceCmd.Item2) = dbgPlaceCmd.Item3
                SendSync()
                dbgPlaceSet = False
            End If

            'Log command
            If dbgLoguser > -1 Then
                DebugConsole.Instance.Log(Newtonsoft.Json.JsonConvert.SerializeObject(Spielers(dbgLoguser)))
                dbgLoguser = -1
            End If

            'Set cam
            If dbgCam <> Nothing Then
                FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = dbgCam}
                dbgCam = Nothing
            End If

            'Move cam
            If dbgCamFree Then FigurFaderCamera.Value = FigurFaderCamera.Value + New Keyframe3D(If(kstate.IsKeyDown(Keys.A), -1, 0) + If(kstate.IsKeyDown(Keys.D), 1, 0), If(kstate.IsKeyDown(Keys.S), -1, 0) + If(kstate.IsKeyDown(Keys.W), 1, 0), If(kstate.IsKeyDown(Keys.LeftShift), -1, 0) + If(kstate.IsKeyDown(Keys.Space), 1, 0), If(kstate.IsKeyDown(Keys.J), -0.01, 0) + If(kstate.IsKeyDown(Keys.L), 0.01, 0), If(kstate.IsKeyDown(Keys.K), -0.01, 0) + If(kstate.IsKeyDown(Keys.I), 0.01, 0), If(kstate.IsKeyDown(Keys.RightShift), -0.01, 0) + If(kstate.IsKeyDown(Keys.Enter), 0.01, 0), True) : HUDdbgLabel.Active = True

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
                    ShowDice = False
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
                    Status = SpielStatus.SpielZuEnde
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                    Renderer.AdditionalZPos = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(5000), 0, 1234, Nothing)
                    Automator.Add(Renderer.AdditionalZPos)
                End If



                'Setze den lokalen Spieler
                If SpielerIndex > -1 AndAlso Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex

                'Update die Spielelogik
                Select Case Status
                    Case SpielStatus.Würfel

                        Select Case Spielers(SpielerIndex).Typ
                            Case SpielerTyp.Local
                                'Manuelles Würfeln für lokalen Spieler
                                'Prüft und speichert, ob der Würfel-Knopf gedrückt wurde
                                If (New Rectangle(1570, 700, 300, 300).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) Or (kstate.IsKeyDown(Keys.Space) And lastkstate.IsKeyUp(Keys.Space)) Then
                                    WürfelTriggered = True
                                    WürfelTimer = 0
                                    WürfelAnimationTimer = -1
                                End If

                                'Solange Knopf gedrückt, generiere zufällige Zahl in einem Intervall von 50ms
                                If WürfelTriggered Then

                                    WürfelTimer += Time.DeltaTime * 1000
                                    'Implementiere einen Cooldown für die Würfelanimation
                                    If Math.Floor(WürfelTimer / WürfelAnimationCooldown) <> WürfelAnimationTimer Then WürfelAktuelleZahl = RollDice() : WürfelAnimationTimer = Math.Floor(WürfelTimer / WürfelAnimationCooldown) : SFX(7).Play()

                                    If WürfelTimer > WürfelDauer Then
                                        WürfelTimer = 0
                                        WürfelTriggered = False
                                        'Gebe Würfe-Ergebniss auf dem Bildschirm aus
                                        HUDInstructions.Text = "You got a " & WürfelAktuelleZahl.ToString & "!"
                                        StopUpdating = True

                                        For i As Integer = 0 To WürfelWerte.Length - 1

                                            If WürfelWerte(i) = 0 Then
                                                'Speiechere Würfel-Wert nach kurzer Pause und wiederhole
                                                Dim it As Integer = i 'Zwischenvariable zur problemlosen Verwendung von i im Lambda-Ausdruck in der nächsten Zeile
                                                Core.Schedule(RollDiceCooldown, Sub()
                                                                                    WürfelWerte(it) = WürfelAktuelleZahl
                                                                                    StopUpdating = False
                                                                                    'Prüfe, ob Würfeln beendet werden soll
                                                                                    If it >= WürfelWerte.Length - 1 Or (Not DreifachWürfeln And WürfelAktuelleZahl < 6) Or (DreifachWürfeln And it > 0 And WürfelAktuelleZahl < 6 AndAlso WürfelWerte(it - 1) >= 6) Or (DreifachWürfeln And it >= 2 And WürfelWerte(2) < 6) Then CalcMoves()
                                                                                    WürfelAktuelleZahl = 0
                                                                                End Sub)

                                                'Beende Schleife
                                                Exit For
                                            End If
                                        Next
                                    End If
                                End If
                            Case SpielerTyp.CPU
                                'Automatisches Würfeln im Hintergrund für CPU
                                WürfelTimer += Time.DeltaTime
                                If WürfelTimer > CPUThinkingTime Then
                                    'Nach kurzem Delay, fülle Würfel-Array mit Zufallszahlen
                                    For it As Integer = 0 To WürfelWerte.Length - 1
                                        'Speiechere Würfel-Wert nach kurzer Pause und wiederhole
                                        WürfelAktuelleZahl = RollDice()
                                        WürfelWerte(it) = WürfelAktuelleZahl
                                        StopUpdating = False
                                        'Prüfe, ob Würfeln beendet werden soll
                                        If it >= WürfelWerte.Length - 1 Or (Not DreifachWürfeln And WürfelAktuelleZahl < 6) Or ((DreifachWürfeln Or GetHomebaseCount(SpielerIndex) > 0) And it > 0 And WürfelAktuelleZahl < 6 AndAlso WürfelWerte(it - 1) >= 6) Or (DreifachWürfeln And it >= 2 And WürfelWerte(2) < 6) Then CalcMoves() : Exit For
                                    Next
                                End If
                        End Select


                    Case SpielStatus.WähleFigur

                        Dim pl As Player = Spielers(SpielerIndex)
                        Select Case pl.Typ
                            Case SpielerTyp.Local

                                Dim ichmagzüge As New List(Of Integer)
                                Dim defaultmov As Integer
                                For i As Integer = 0 To FigCount - 1
                                    defaultmov = pl.Spielfiguren(i)
                                    If defaultmov > -1 And defaultmov + Fahrzahl <= PlCount * SpceCount + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
                                Next

                                If ichmagzüge.Count = 1 Then
                                    'Move camera
                                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)

                                    SFX(2).Play()
                                    Dim k As Integer = ichmagzüge(0)
                                    'Setze flags
                                    Status = SpielStatus.FahreFelder
                                    FigurFaderZiel = (SpielerIndex, k)
                                    'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                                    StartMoverSub()
                                    defaultmov = Spielers(SpielerIndex).Spielfiguren(k)
                                    SendFigureTransition(SpielerIndex, k, defaultmov + Fahrzahl)
                                    StopUpdating = False
                                ElseIf ichmagzüge.Count = 0 Then
                                    StopUpdating = True
                                    HUDInstructions.Text = "No move possible!"
                                    Core.Schedule(1, Sub()
                                                         SwitchPlayer()
                                                         StopUpdating = False
                                                     End Sub)
                                Else
                                    'Manuelle Auswahl für lokale Spieler
                                    For k As Integer = 0 To FigCount - 1
                                        defaultmov = Spielers(SpielerIndex).Spielfiguren(k)

                                        'Prüfe Figur nach Mouse-Klick
                                        If GetFigureRectangle(Map, SpielerIndex, k, Spielers, Center).Contains(mpos) And Spielers(SpielerIndex).Spielfiguren(k) > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                                            If Not ichmagzüge.Contains(k) Then
                                                HUDInstructions.Text = "Incorrect move!"
                                            Else
                                                'Move camera
                                                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)

                                                SFX(2).Play()
                                                'Setze flags
                                                Status = SpielStatus.FahreFelder
                                                FigurFaderZiel = (SpielerIndex, k)
                                                'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                                                StartMoverSub()
                                                SendFigureTransition(SpielerIndex, k, defaultmov + Fahrzahl)
                                                StopUpdating = False
                                            End If
                                            Exit For
                                        End If
                                    Next
                                End If

                            Case SpielerTyp.CPU
                                'TODO: FÜge CPU-Code ein(Auswahl, welcher Zug optimal ist)
                                CPUTimer += Time.DeltaTime
                                If CPUTimer > CPUThinkingTime Then
                                    CPUTimer = 0

                                    Dim k As Integer
                                    Dim ichmagzüge As New List(Of Integer)
                                    Dim defaultmov As Integer
                                    For i As Integer = 0 To FigCount - 1
                                        defaultmov = pl.Spielfiguren(i)
                                        If defaultmov > -1 And defaultmov + Fahrzahl <= PlCount * SpceCount + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
                                    Next
                                    'Prüfe ob Zug möglich
                                    If ichmagzüge.Count = 0 Then SwitchPlayer() : Exit Select

                                    Select Case Spielers(SpielerIndex).Schwierigkeit
                                        Case Difficulty.Brainless

                                            'Berechne zufällig das zu fahrende Feld
                                            k = ichmagzüge(Nez.Random.Range(0, ichmagzüge.Count))

                                        Case Difficulty.Smart

                                            Dim Scores As New Dictionary(Of Integer, Single) ' im INteger ist der Index der FIgur und im Single der Score
                                            For Each element In ichmagzüge
                                                Scores.Add(element, 1)
                                            Next

                                            'Spielfigurimportans: eine Figur die näher am Ziel ist ist wichtiger
                                            Dim counts As New List(Of (Integer, Integer))
                                            For Each element In ichmagzüge
                                                counts.Add((element, Spielers(SpielerIndex).Spielfiguren(element)))
                                            Next
                                            counts = counts.OrderBy(Function(x) x.Item2).ToList()
                                            For i As Integer = 0 To counts.Count - 1
                                                Scores(counts(i).Item1) *= 1 + i * 0.1F
                                            Next

                                            For Each element In ichmagzüge
                                                ' Safety:ist eine Figur höchstens 6 felder vor einer Feindlichen Figur entfernt, ist sie in einer Gefahrenzone die avoidet werden soll
                                                Dim locpos As Integer() = {Spielers(SpielerIndex).Spielfiguren(element), Spielers(SpielerIndex).Spielfiguren(element) + Fahrzahl}
                                                Dim Globpos As Integer() = {PlayerFieldToGlobalField(locpos(0), SpielerIndex), PlayerFieldToGlobalField(locpos(1), SpielerIndex)}
                                                For ALVSP As Integer = 0 To FigCount - 1
                                                    If ALVSP <> SpielerIndex Then
                                                        For ALVSPF As Integer = 0 To FigCount - 1
                                                            Dim locposB As Integer = Spielers(ALVSP).Spielfiguren(ALVSPF)
                                                            Dim GlobposB As Integer = PlayerFieldToGlobalField(locposB, ALVSP)
                                                            'Falls momentane Position in Feindlichiem Feld, verbessere Score(fliehen)
                                                            If GlobposB < Globpos(0) And GlobposB >= Globpos(0) - 6 Then
                                                                Scores(element) *= 1.4F
                                                            ElseIf GlobposB < Globpos(1) And GlobposB >= Globpos(1) - 6 And locpos(1) < PlCount * SpceCount And locposB > -1 And Not (GlobposB < Globpos(0) And GlobposB >= Globpos(0) - 6) Then
                                                                'Falls momentanes Feld nicht in feindlichem Gebiet, aber zukünftiges, verschlechtere Score
                                                                Scores(element) /= 1.5F
                                                            End If
                                                        Next
                                                    End If
                                                Next

                                                ' Destiny: landet der zug im Haus? 
                                                If locpos(1) >= PlCount * SpceCount Then
                                                    Scores(element) *= 10.0F
                                                End If

                                                ' Attackopportunity: kann der zug einen Feindlichen spieler eleminieren? 
                                                Dim Ergebnis As (Integer, Integer) = GetKickFigur(SpielerIndex, element, Fahrzahl)
                                                If Ergebnis.Item1 <> -1 And Ergebnis.Item2 <> -1 Then
                                                    Scores(element) *= 2.2F
                                                End If


                                                'Risk: nicht auf das Startfeld/den Eingangsbereich eines gegners stellen da eine neue figur erscheinen könnte.
                                                Dim aimpl As Integer = Math.Floor(Globpos(1) / SpceCount)
                                                Dim ishomeregionbusy As Boolean = locpos(1) < PlCount * SpceCount And aimpl <> SpielerIndex AndAlso GetHomebaseIndex(aimpl) > -1
                                                If locpos(1) > 0 And (locpos(1) Mod SpceCount) = 0 And ishomeregionbusy Then
                                                    Scores(element) /= 4
                                                ElseIf locpos(1) > 6 And (locpos(1) Mod SpceCount) < 7 And Not (locpos(0) Mod SpceCount) < 7 And ishomeregionbusy Then
                                                    Scores(element) /= 2
                                                End If

                                                'Flee: führt der Zug die Figur aus einem Startbereich heraus
                                                If locpos(0) > 6 And (locpos(0) Mod SpceCount) < 7 And (locpos(1) Mod SpceCount) > 6 Then
                                                    Scores(element) *= 1.3F
                                                End If
                                            Next

                                            'Sortieren und besten Zug filtern
                                            Dim NeueLIsteweilIChsehrcreativebin As New List(Of (Integer, Single))
                                            For Each element In ichmagzüge
                                                NeueLIsteweilIChsehrcreativebin.Add((element, Scores(element)))
                                            Next
                                            NeueLIsteweilIChsehrcreativebin = NeueLIsteweilIChsehrcreativebin.OrderBy(Function(x) x.Item2).ToList()
                                            NeueLIsteweilIChsehrcreativebin.Reverse()
                                            k = NeueLIsteweilIChsehrcreativebin(0).Item1
                                    End Select


                                    defaultmov = pl.Spielfiguren(k)
                                    'Setze flags
                                    Status = SpielStatus.FahreFelder
                                    FigurFaderZiel = (SpielerIndex, k)

                                    'Move camera
                                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)

                                    'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                                    StartMoverSub()
                                    SendFigureTransition(SpielerIndex, k, defaultmov + Fahrzahl)
                                    StopUpdating = False
                                End If
                        End Select

                    Case SpielStatus.WähleOpfer

                        Dim pl As Player = Spielers(SpielerIndex)
                        If pl.Typ = SpielerTyp.Local Then

                            Dim ichmagzüge As New List(Of Integer)
                            Dim defaultmov As Integer
                            For i As Integer = 0 To FigCount - 1
                                defaultmov = pl.Spielfiguren(i)
                                If defaultmov > -1 And defaultmov + Fahrzahl < PlCount * SpceCount Then ichmagzüge.Add(i)
                            Next

                            If ichmagzüge.Count = 1 Then
                                Sacrifice(SpielerIndex, ichmagzüge(0))
                                'Move camera
                                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
                            ElseIf ichmagzüge.Count = 0 Then
                                StopUpdating = True
                                HUDInstructions.Text = "No sacrificable piece!"
                                Core.Schedule(1, Sub()
                                                     SwitchPlayer()
                                                     StopUpdating = False
                                                 End Sub)
                                'Move camera
                                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
                            Else
                                'Manuelle Auswahl für lokale Spieler
                                For k As Integer = 0 To FigCount - 1
                                    'Prüfe Figur nach Mouse-Klick
                                    If GetFigureRectangle(Map, SpielerIndex, k, Spielers, Center).Contains(mpos) And Spielers(SpielerIndex).Spielfiguren(k) > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                                        If Not ichmagzüge.Contains(k) Then
                                            HUDInstructions.Text = "Can't select this piece!"
                                        Else
                                            Sacrifice(SpielerIndex, k)
                                            'Move camera
                                            FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
                                        End If
                                        Exit For
                                    End If
                                Next
                            End If
                        End If

                    Case SpielStatus.WarteAufOnlineSpieler
                        HUDInstructions.Text = "Waiting for all players to connect..."

                        'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                        For Each sp In Spielers
                            If sp Is Nothing OrElse Not sp.Bereit Then Exit Select 'Falls ein SPieler noch nicht belegt/bereit, breche Spielstart ab
                        Next

                        'Falls vollzählig, starte Spiel
                        StopUpdating = True
                        Core.Schedule(0.8, Sub()
                                               PostChat("The game has started!", Color.White)
                                               SendBeginGaem()
                                               FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = StdCam}
                                               'Launch start animation
                                               Renderer.TriggerStartAnimation(AddressOf SwitchPlayer)
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
                HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name & "(" & GetScore(SpielerIndex) & ")", "")
                If Not Renderer.BeginTriggered Then HUDNameBtn.Color = hudcolors(If(SpielerIndex > -1, SpielerIndex, 0))
                HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (SpielerIndex > -1 AndAlso Spielers(SpielerIndex).Typ = SpielerTyp.Local)
                End If

                Dim scheiß As New List(Of (Integer, Integer))
            For Each element In FigurFaderScales
                If element.Value.State = TransitionState.Done Then scheiß.Add(element.Key)
            Next

            For Each element In scheiß
                FigurFaderScales.Remove(element)
            Next

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
                        SendPlayerLeft(source)
                    Case "j"c 'God got activated
                        Dim figur As Integer = CInt(element(2).ToString)
                        DontKickSacrifice = Spielers(source).SacrificeCounter < 0
                        Spielers(source).SacrificeCounter = SacrificeWait
                        Sacrifice(source, figur)
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(2)
                        PostChat(msg, Color.White)
                    Case "n"c 'Switch player
                        SwitchPlayer()
                    Case "p"c 'Player angered
                        Spielers(source).Angered = True
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
                        If everythere Then StopUpdating = False : SendGameActive()
                    Case "s"c 'Move figure
                        Dim figur As Integer = CInt(element(2).ToString)
                        Dim destination As Integer = CInt(element.Substring(3).ToString)
                        SendFigureTransition(source, figur, destination)
                        'Animiere wie die Figur sich nach vorne bewegt, anschließend kehre zurück zum nichts tun
                        Dim defaultmov As Integer = Math.Max(Spielers(source).Spielfiguren(figur), 0)
                        Status = SpielStatus.FahreFelder
                        FigurFaderZiel = (source, figur)
                        StartMoverSub(destination)
                    Case "y"c
                        SendSync()
                    Case "z"c
                        Dim IdentSound As IdentType = CInt(element(2).ToString)
                        Dim SoundNr As Integer = CInt(element(3).ToString)
                        Dim dat As String = element.Substring(4).Replace("_TATA_", "")

                        If IdentSound = IdentType.Custom Then
                            IO.File.WriteAllBytes("Cache\server\" & Spielers(source).Name & SoundNr.ToString & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                            Spielers(source).CustomSound(SoundNr) = SoundEffect.FromFile("Cache\server\" & Spielers(source).Name & SoundNr.ToString & ".wav")
                        Else
                            Spielers(source).CustomSound(SoundNr) = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                        End If
                        SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & SoundNr.ToString & "_TATA_" & dat)
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
        Private Sub SendSetFigurePosition(pl As Integer, fig As Integer, pos As Integer)
            SendNetworkMessageToAll("d" & pl.ToString & fig.ToString & pos.ToString)
        End Sub
        Private Sub SendPlayerLeft(index As Integer)
            LocalClient.WriteStream("e" & index)
        End Sub
        Private Sub SendFlyingSaucerActive(last As Integer, distance As Integer)
            SendNetworkMessageToAll("f" & FigurFaderZiel.Item1.ToString & FigurFaderZiel.Item2.ToString & distance.ToString & "&" & last.ToString)
        End Sub
        Private Sub SendFlyingSaucerAdded(fields As Integer)
            SendNetworkMessageToAll("g" & fields.ToString)
        End Sub
        Private Sub SendHighscore()
            Dim pls As New List(Of (String, Integer))
            For i As Integer = 0 To Spielers.Length - 1
                If Spielers(i).Typ = SpielerTyp.Local Or Spielers(i).Typ = SpielerTyp.Online Then
                    pls.Add((Spielers(i).Name, GetScore(i)))
                End If
            Next
            SendNetworkMessageToAll("h" & 0.ToString & CInt(Map).ToString & Newtonsoft.Json.JsonConvert.SerializeObject(pls))
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
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers, SaucerFields))
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
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers, SaucerFields))
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
            Dim homebase As Integer = GetHomebaseIndex(SpielerIndex) 'Eine Spielfigur-ID, die sich in der Homebase befindet(-1, falls Homebase leer ist)
            Dim startfd As Boolean = IsFieldCoveredByOwnFigure(SpielerIndex, 0) 'Ob das Start-Feld blockiert ist
            ShowDice = False
            Fahrzahl = GetNormalDiceSum() 'Setzt die Anzahl der zu fahrenden Felder im voraus(kann im Fall einer vollen Homebase überschrieben werden)

            If Is6InDiceList() And homebase > -1 And Not startfd Then 'Falls Homebase noch eine Figur enthält und 6 gewürfelt wurde, setze Figur auf Feld 0 und fahre anschließend x Felder nach vorne
                'Bereite das Homebase-verlassen vor
                Fahrzahl = GetSecondDiceAfterSix(SpielerIndex)
                HUDInstructions.Text = "Move Character out of your homebase and move him " & Fahrzahl & " spaces!"
                FigurFaderZiel = (SpielerIndex, homebase)
                'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                If Not IsFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl) Then
                    StartMoverSub()
                    SendFigureTransition(SpielerIndex, homebase, Fahrzahl)
                Else
                    StopUpdating = True
                    HUDInstructions.Text = "Field already covered! Move with the other piece!"
                    Core.Schedule(ErrorCooldown, Sub()
                                                     'Move camera
                                                     FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                                                     Status = SpielStatus.WähleFigur
                                                     StopUpdating = False
                                                 End Sub)
                End If
            ElseIf Is6InDiceList() And homebase > -1 And startfd Then 'Gibt an, dass das Start-Feld von einer eigenen Figur belegt ist(welche nicht gekickt werden kann) und dass selbst beim Wurf einer 6 keine weitere Figur die Homebase verlassen kann
                HUDInstructions.Text = "Start field blocked! Move pieces out of the way first!"

                If IsFutureFieldCoveredByOwnFigure(SpielerIndex, 0, -1) AndAlso Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl, -1) Then 'Spieler auf dem Start-Feld muss wenn mögl.  bewegt werden
                    homebase = GetFieldID(SpielerIndex, 0).Item2
                    FigurFaderZiel = (SpielerIndex, homebase)
                    StartMoverSub()
                    SendFigureTransition(SpielerIndex, homebase, Fahrzahl)
                Else 'We can't so s$*!, also schieben wir unsere Probleme einfach auf den nächst besten Deppen, der gleich dran ist

                    'Move camera
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)

                    Status = SpielStatus.WähleFigur
                    StopUpdating = True
                    Core.Schedule(ErrorCooldown, Sub() StopUpdating = False)
                End If
            ElseIf (GetHomebaseCount(SpielerIndex) = FigCount And Not Is6InDiceList()) OrElse Not CanDoAMove() Then 'Falls Homebase komplett voll ist(keine Figur auf Spielfeld) und keine 6 gewürfelt wurde(oder generell kein Zug mehr möglich ist), ist kein Zug möglich und der nächste Spieler ist an der Reihe
                StopUpdating = True
                HUDInstructions.Text = "No move possible!"
                Core.Schedule(1, Sub()
                                     SwitchPlayer()
                                     StopUpdating = False
                                 End Sub)
            Else 'Ansonsten fahre x Felder nach vorne mit der Figur, die anschließend ausgewählt wird
                HUDInstructions.Text = "Select piece to be moved " & Fahrzahl & " spaces!"
                Status = SpielStatus.WähleFigur

                'Move camera
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
            End If
        End Sub

#Region "Hilfsfunktionen"
        Private Function CheckKick(playerA As Integer, figur As Integer, Optional Increment As Integer = 0) As Integer
            'Berechne globale Spielfeldposition der rauswerfenden Figur
            Dim fieldA As Integer = Spielers(playerA).Spielfiguren(figur) + Increment
            Dim fa As Integer = PlayerFieldToGlobalField(fieldA, playerA)
            'Loope durch andere Spieler
            For i As Integer = playerA + 1 To playerA + PlCount - 1
                'Überspringe falls Spieler nicht aktiv
                Dim playerB As Integer = i Mod PlCount
                If Spielers(playerB).Typ = SpielerTyp.None Then Continue For
                'Loope durch alle Spielfiguren eines jeden Spielers
                For j As Integer = 0 To FigCount - 1
                    'Berechne globale Spielfeldposition der rauszuwerfenden Spielfigur
                    Dim fieldB As Integer = Spielers(playerB).Spielfiguren(j)
                    Dim fb As Integer = PlayerFieldToGlobalField(fieldB, playerB)
                    'Falls globale Spielfeldposition identisch und 
                    If fieldB >= 0 And fieldB < PlCount * SpceCount And fb = fa Then
                        Core.Schedule(1, Sub() PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White))
                        Spielers(playerA).AdditionalPoints += 25
                        Return j
                    End If
                Next
            Next
            Return -1
        End Function
        Private Sub KickedByGod(player As Integer, figur As Integer)
            Dim key = (player, figur)
            If FigurFaderScales.ContainsKey(key) Then FigurFaderScales.Remove(key)
            Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                          Spielers(player).Spielfiguren(figur) = -1
                                                                                                                          If FigurFaderScales.ContainsKey(key) Then FigurFaderScales.Remove(key)
                                                                                                                          Dim transB As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 0, 1, Nothing)
                                                                                                                          Automator.Add(transB)
                                                                                                                          FigurFaderScales.Add(key, transB)
                                                                                                                      End Sub)
            Automator.Add(trans)
            FigurFaderScales.Add(key, trans)
            SendKick(player, figur)
        End Sub

        Private Function GetKickFigur(player As Integer, figur As Integer, Optional Increment As Integer = 0) As (Integer, Integer)
            'Berechne globale Spielfeldposition der rauswerfenden Figur
            Dim playerA As Integer = player
            Dim fieldA As Integer = Spielers(playerA).Spielfiguren(figur) + Increment
            Dim fa As Integer = PlayerFieldToGlobalField(fieldA, playerA)
            'Loope durch andere Spieler
            For i As Integer = playerA + 1 To playerA + PlCount - 1
                'Überspringe falls Spieler nicht aktiv
                Dim playerB As Integer = i Mod PlCount
                If Spielers(playerB).Typ = SpielerTyp.None Then Continue For
                'Loope durch alle Spielfiguren eines jeden Spielers
                For j As Integer = 0 To FigCount - 1
                    'Berechne globale Spielfeldposition der rauszuwerfenden Spielfigur
                    Dim fieldB As Integer = Spielers(playerB).Spielfiguren(j)
                    Dim fb As Integer = PlayerFieldToGlobalField(fieldB, playerB)
                    'Falls globale Spielfeldposition identisch und 
                    If fieldB >= 0 And fieldB <= PlCount * SpceCount And fb = fa Then Return (i, j)
                Next
            Next
            Return (-1, -1)
        End Function

        Private Sub TriggerSaucer(last As Integer)
            SaucerFields.Remove(last)
            Status = SpielStatus.SaucerFlight
            Dim distance As Integer = Nez.Random.Range(-6, 7)
            Do While IsFieldCovered(SpielerIndex, -1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + distance) Or distance = 0 Or Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + distance < 0 ' Or ((Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + distance) Mod SpceCount) = 0
                distance += 1
            Loop

            Dim nr As Integer = Math.Min(Math.Max(distance + Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), 0), PlCount * SpceCount + FigCount - 1)
            If NetworkMode Then SendFlyingSaucerActive(last, nr)
            Renderer.TriggerSaucerAnimation(FigurFaderZiel, Sub()
                                                                Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = nr
                                                                HUDInstructions.Text = If(distance > 0, "+", "") & distance
                                                            End Sub, Sub()
                                                                         Dim saucertrigger As Boolean = False
                                                                         For Each element In SaucerFields
                                                                             If PlayerFieldToGlobalField(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), FigurFaderZiel.Item1) = element Then
                                                                                 saucertrigger = True
                                                                                 nr = element
                                                                             End If
                                                                         Next

                                                                         'Trigger UFO, falls auf Feld gelandet
                                                                         If saucertrigger And Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < PlCount * SpceCount Then TriggerSaucer(nr) Else If Status <> SpielStatus.SpielZuEnde Then SwitchPlayer()
                                                                     End Sub)
        End Sub

        Private Function GetNormalDiceSum() As Integer
            Dim sum As Integer = 0
            For i As Integer = 0 To WürfelWerte.Length - 1
                sum += WürfelWerte(i)
                If WürfelWerte(i) <> 6 Then Exit For
            Next
            Return sum
        End Function

        Private Function CheckWin() As Boolean
            For i As Integer = 0 To PlCount - 1
                Dim pl As Player = Spielers(i)
                Dim check As Boolean = True
                For j As Integer = 0 To FigCount - 1
                    If pl.Spielfiguren(j) < PlCount * SpceCount Then check = False
                Next
                If check Then Return True
            Next
            Return False
        End Function

        Private Function CanDoAMove() As Boolean
            Dim pl As Player = Spielers(SpielerIndex)

            'Wähle alle möglichen Zügen aus
            Dim ichmagzüge As New List(Of Integer)
            Dim defaultmov As Integer
            For i As Integer = 0 To FigCount - 1
                defaultmov = pl.Spielfiguren(i)
                'Prüfe ob Zug mit dieser Figur möglich ist(Nicht in homebase, nicht über Ziel hinaus und Zielfeld nicht mit eigener Figur belegt
                If defaultmov > -1 And defaultmov + Fahrzahl <= PlCount * SpceCount + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
            Next

            'Prüfe ob Zug möglich
            Return ichmagzüge.Count > 0
        End Function

        Private Function GetScore(pl As Integer) As Integer
            Dim ret As Single = If(Spielers(pl).Angered, 0, 5)
            For Each element In Spielers(pl).Spielfiguren
                If element >= 0 Then ret += element
            Next
            Return CInt(ret * 10) + Spielers(pl).AdditionalPoints
        End Function

        Private Function IsÜberholingInSeHaus(defaultmov As Integer) As Boolean
            If defaultmov + Fahrzahl < PlCount * SpceCount Then Return False

            For i As Integer = defaultmov + 1 To defaultmov + Fahrzahl
                If IsFieldCovered(SpielerIndex, -1, i) And i >= PlCount * SpceCount Then Return True
            Next

            Return False
        End Function


        Private Function IsFieldCovered(player As Integer, figur As Integer, fieldA As Integer) As Boolean
            If fieldA < 0 Then Return False

            Dim fa As Integer = PlayerFieldToGlobalField(fieldA, player)
            For i As Integer = 0 To PlCount - 1
                'Loope durch alle Spielfiguren eines jeden Spielers
                For j As Integer = 0 To FigCount - 1
                    'Berechne globale Spielfeldposition der rauszuwerfenden Spielfigur
                    Dim fieldB As Integer = Spielers(i).Spielfiguren(j)
                    Dim fb As Integer = PlayerFieldToGlobalField(fieldB, i)
                    'Falls globale Spielfeldposition identisch und 
                    If fieldB > -1 And ((fieldA < PlCount * SpceCount AndAlso (player <> i Or figur <> j) And fb = fa) OrElse (player = i And figur <> j And fieldA = fieldB)) Then Return True
                Next
            Next

            Return False
        End Function

        Private Function GetFieldID(player As Integer, field As Integer) As (Integer, Integer)
            Dim fa As Integer = PlayerFieldToGlobalField(field, player)
            For j As Integer = 0 To PlCount - 1
                For i As Integer = 0 To FigCount - 1
                    Dim fieldB As Integer = Spielers(j).Spielfiguren(i)
                    If fieldB >= 0 And fieldB < PlCount * SpceCount And fa = PlayerFieldToGlobalField(fieldB, j) Then Return (j, i)
                Next
            Next
            Return (-1, -1)
        End Function
        Private Function GetLocalAudio(ident As IdentType, Optional IsSoundB As Boolean = False) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content\prep\audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache\client\sound" & If(IsSoundB, "B", "A") & ".audio")
            End If
        End Function

        'Prüft, ob man dreimal würfeln darf
        Private Function CanRollThrice(player As Integer) As Boolean
            Dim fieldlst As New List(Of Integer)
            For i As Integer = 0 To FigCount - 1
                Dim tm As Integer = Spielers(player).Spielfiguren(i)
                If tm >= 0 And tm < PlCount * SpceCount Then Return False 'Falls sich Spieler auf dem Spielfeld befindet, ist dreimal würfeln unmöglich
                If tm >= PlCount * SpceCount Then fieldlst.Add(tm) 'Merke Figuren, die sich im Haus befinden
            Next

            'Wenn nicht alle FIguren bis an den Anschlag gefahren wurden, darf man nicht dreifach würfeln
            For i As Integer = PlCount * SpceCount + FigCount - 1 To (PlCount * SpceCount + FigCount - fieldlst.Count) Step -1
                If Not fieldlst.Contains(i) Then Return False
            Next

            Return True
        End Function

        Private Function IsFieldCoveredByOwnFigure(player As Integer, field As Integer) As Boolean
            For i As Integer = 0 To FigCount - 1
                If Spielers(player).Spielfiguren(i) = field Then Return True
            Next
            Return False
        End Function

        Private Function IsFutureFieldCoveredByOwnFigure(player As Integer, futurefield As Integer, fieldindx As Integer) As Boolean
            For i As Integer = 0 To FigCount - 1
                If Spielers(player).Spielfiguren(i) = futurefield And i <> fieldindx Then Return True
            Next
            Return False
        End Function

        'Gibt den Index ein Spielfigur zurück, die sich noch in der Homebase befindet. Falls keine Figur mehr in der Homebase, gibt die Fnkt. -1 zurück.
        Private Function GetHomebaseIndex(player As Integer) As Integer
            For i As Integer = 0 To FigCount - 1
                If Spielers(player).Spielfiguren(i) = -1 Then Return i
            Next
            Return -1
        End Function

        Private Function GetHomebaseCount(player As Integer) As Integer
            Dim count As Integer = 0
            For i As Integer = 0 To FigCount - 1
                If Spielers(player).Spielfiguren(i) = -1 Then count += 1
            Next
            Return count
        End Function

        Private Function Is6InDiceList() As Boolean
            For i As Integer = 0 To WürfelWerte.Length - 2
                If WürfelWerte(i) = 6 Then Return True
            Next
            Return False
        End Function

        Private Function PlayerFieldToGlobalField(field As Integer, player As Integer) As Integer
            Return (field + player * SpceCount) Mod (PlCount * SpceCount)
        End Function

        Private Function GetSecondDiceAfterSix(player As Integer) As Integer
            Dim findex As Integer = -1
            Dim sum As Integer = 0
            For i As Integer = 0 To WürfelWerte.Length - 1
                If findex = -1 And WürfelWerte(i) = 6 Then findex = i : Continue For
                If findex > -1 Then sum += WürfelWerte(i)
            Next
            Return sum
        End Function

        Private Sub StartMoverSub(Optional destination As Integer = -1)
            'Set values
            MoveActive = True
            FigurFaderEnd = If(destination < 0, Math.Max(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), 0) + Fahrzahl, destination)
            Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))
            Status = SpielStatus.FahreFelder
            PlayStompSound = False

            'Initiate
            If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
                Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
                If (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1) Or (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1 = 0) Then
                    Dim kickID As Integer = CheckKick(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1)
                    Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                                  Spielers(FigurFaderZiel.Item1).CustomSound(1).Play()
                                                                                                                                  If kickID = key.Item2 Then Spielers(key.Item1).Spielfiguren(key.Item2) = -1
                                                                                                                                  If FigurFaderScales.ContainsKey(key) Then FigurFaderScales.Remove(key)
                                                                                                                                  Dim transB As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 0, 1, Nothing)
                                                                                                                                  Automator.Add(transB)
                                                                                                                                  FigurFaderScales.Add(key, transB)
                                                                                                                              End Sub)
                    If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
                Else
                    Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Bounce(FigurSpeed * 2), 1, 0, Nothing)
                    If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
                End If
            End If
            FigurFaderXY = New Transition(Of Vector2)(New TransitionTypes.TransitionType_EaseInEaseOut(FigurSpeed), FigurFaderVectors.Item1, FigurFaderVectors.Item2, AddressOf MoverSub) : Automator.Add(FigurFaderXY)
            FigurFaderZ = New Transition(Of Integer)(New TransitionTypes.TransitionType_Parabole(FigurSpeed), 0, DopsHöhe, Nothing) : Automator.Add(FigurFaderZ)
        End Sub

        Private Sub MoverSub()
            Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) += 1

            Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 0), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))

            If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < FigurFaderEnd Then
                'Play sound
                If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = 0 Then
                    Spielers(FigurFaderZiel.Item1).CustomSound(0).Play()
                Else
                    SFX(3).Play()
                End If
                If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
                    Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
                    If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1 Then
                        Dim kickID As Integer = CheckKick(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1)
                        PlayStompSound = True
                        Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                                      Spielers(FigurFaderZiel.Item1).CustomSound(1).Play()
                                                                                                                                      If kickID = key.Item2 Then Spielers(key.Item1).Spielfiguren(key.Item2) = -1
                                                                                                                                      If FigurFaderScales.ContainsKey(key) Then FigurFaderScales.Remove(key)
                                                                                                                                      Dim transB As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 0, 1, Nothing)
                                                                                                                                      Automator.Add(transB)
                                                                                                                                      FigurFaderScales.Add(key, transB)
                                                                                                                                  End Sub)
                        If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
                    Else
                        Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Bounce(FigurSpeed * 2), 1, 0, Nothing)
                        If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
                    End If
                End If
                FigurFaderXY = New Transition(Of Vector2)(New TransitionTypes.TransitionType_Linear(FigurSpeed), FigurFaderVectors.Item1, FigurFaderVectors.Item2, AddressOf MoverSub) : Automator.Add(FigurFaderXY)
                FigurFaderZ = New Transition(Of Integer)(New TransitionTypes.TransitionType_Parabole(FigurSpeed), 0, DopsHöhe, Nothing) : Automator.Add(FigurFaderZ)
            Else
                If Not PlayStompSound Then SFX(2).Play()

                Dim saucertrigger As Boolean = False
                Dim nr As Integer
                For Each element In SaucerFields
                    If PlayerFieldToGlobalField(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), FigurFaderZiel.Item1) = element Then
                        saucertrigger = True
                        nr = element
                    End If
                Next

                'Trigger UFO, falls auf Feld gelandet
                If saucertrigger And Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < PlCount * SpceCount Then TriggerSaucer(nr) Else SwitchPlayer()

                MoveActive = False
            End If

        End Sub

        Private Function GetSpielfeldVector(player As Integer, figur As Integer, Optional increment As Integer = 0) As Vector2
            Return GetMapVectorPos(Map, player, figur, Spielers(player).Spielfiguren(figur) + increment)
        End Function

        Private Function GetWürfelSourceRectangle(augenzahl As Integer) As Rectangle
            Select Case augenzahl
                Case 1
                    Return New Rectangle(0, 0, 260, 260)
                Case 2
                    Return New Rectangle(260, 0, 260, 260)
                Case 3
                    Return New Rectangle(520, 0, 260, 260)
                Case 4
                    Return New Rectangle(0, 260, 260, 260)
                Case 5
                    Return New Rectangle(260, 260, 260, 260)
                Case 6
                    Return New Rectangle(520, 260, 260, 260)
                Case Else
                    Return New Rectangle(0, 0, 0, 0)
            End Select
        End Function


        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

        Private Sub Sacrifice(pl As Integer, figur As Integer)
            Spielers(pl).AdditionalPoints += 25
            StopUpdating = True
            Status = SpielStatus.Waitn
            PostChat(Spielers(pl).Name & " offered one of his pieces to the gods...", Color.White)
            SendMessage(Spielers(pl).Name & " offered one of his pieces to the gods...")
            If Not DontKickSacrifice Then KickedByGod(pl, figur) 'Kick sacrifice
            Dim progress = Spielers(pl).Spielfiguren(figur) / (PlCount * SpceCount)
            Dim pogfactor = progress * 0.45F + 0.45F 'Field 0: Chance of sth good: 45%;  Field max.: Chance of sth good: 90%
            Core.Schedule(2, Sub() 'Wait a sec
                                 Dim plsdont As Boolean = False

                                 Do
                                     Dim RNG = Nez.Random.NextFloat
                                     If RNG <= pogfactor Then 'Positive effect
                                         Select Case Nez.Random.Range(0, 5)
                                             Case 0
                                                 Try

                                                     'Boost random figure
                                                     Dim fig = Nez.Random.Range(0, FigCount)
                                                     Dim boost = Nez.Random.Range(1, PlCount * 5)
                                                     Dim futurefield = Spielers(pl).Spielfiguren(fig) + boost
                                                     If futurefield < PlCount * SpceCount AndAlso Not IsFutureFieldCoveredByOwnFigure(pl, futurefield, fig) Then
                                                         PostChat("You're lucky! A random figure of yours is being boosted!", Color.White)
                                                         SendMessage("You're lucky! A random figure of yours is being boosted!")
                                                         plsdont = True
                                                         FigurFaderZiel = (pl, fig)
                                                         StartMoverSub(futurefield)
                                                         SendFigureTransition(pl, fig, futurefield)
                                                         Exit Do
                                                     End If
                                                 Catch ex As Exception

                                                 End Try
                                             Case 1
                                                 'Kick random enemy figure
                                                 Dim pla = Nez.Random.Range(0, PlCount)
                                                 Dim fig = Nez.Random.Range(0, FigCount)
                                                 Dim dont = False
                                                 Dim count = 0
                                                 Do While Spielers(pla).Spielfiguren(fig) < 0 And Spielers(pla).Spielfiguren(fig) >= PlCount * SpceCount
                                                     pla = Nez.Random.Range(0, PlCount)
                                                     fig = Nez.Random.Range(0, FigCount)
                                                     count += 1
                                                     If count > 20 Then dont = True : Exit Do
                                                 Loop
                                                 If pla <> pl And Not dont Then
                                                     PostChat("You're lucky! A random enemy figure got kicked!", Color.White)
                                                     SendMessage("You're lucky! A random enemy figure got kicked!")
                                                     KickedByGod(pla, fig)
                                                     Exit Do
                                                 End If
                                             Case 2
                                                 'Reset anger button
                                                 If Not Spielers(pl).Angered Then Continue Do
                                                 PostChat("You're lucky! Your anger button got reset!", Color.White)
                                                 SendMessage("You're lucky! Your anger button got reset!")
                                                 Spielers(pl).Angered = False
                                                 Exit Do
                                             Case 3
                                                 'Add points
                                                 PostChat("You're lucky! You gained 75 points!", Color.White)
                                                 SendMessage("You're lucky! You gained 75 points!")
                                                 Spielers(pl).AdditionalPoints += 75
                                                 Exit Do
                                         End Select
                                     ElseIf RNG > pogfactor + (1 - pogfactor) / 5 * 3 Then 'Negative effect
                                         Select Case Nez.Random.Range(0, 3)
                                             Case 0
                                                 'Subtract points
                                                 PostChat("Oh ooh! You lost 75 points!", Color.White)
                                                 SendMessage("Oh ooh! You lost 75 points!")
                                                 Spielers(pl).AdditionalPoints -= 75
                                                 Exit Do
                                             Case 1
                                                 'Kick random figure
                                                 Dim fig = Nez.Random.Range(0, FigCount)
                                                 Dim dont = False
                                                 Dim count = 0
                                                 Do While Spielers(pl).Spielfiguren(fig) < 0 And Spielers(pl).Spielfiguren(fig) >= PlCount * SpceCount
                                                     fig = Nez.Random.Range(0, FigCount)
                                                     count += 1
                                                     If count > 20 Then dont = True : Exit Do
                                                 Loop
                                                 If Not dont Then
                                                     PostChat("Oh ooh! Another one of your piece died!", Color.White)
                                                     SendMessage("Oh ooh! Another one of your piece died!")
                                                     KickedByGod(pl, fig)
                                                     Exit Do
                                                 End If
                                             Case 2
                                                 'Set anger button
                                                 If Spielers(pl).Angered Then Continue Do
                                                 PostChat("Oh ooh! Your anger button got deleteted!", Color.White)
                                                 SendMessage("Oh ooh! Your anger button got deleteted!")
                                                 Spielers(pl).Angered = True
                                                 Exit Do
                                         End Select
                                     Else
                                         PostChat("You got spared.", Color.White)
                                         SendMessage("You got spared.")
                                         Exit Do
                                     End If
                                 Loop
                                 SendSync()

                                 'Switch player
                                 If Not plsdont Then Core.Schedule(2, Sub() SwitchPlayer())
                             End Sub)
        End Sub

        Private Sub SwitchPlayer()
            'Generate SaucerFields
            If Nez.Random.Range(0, SaucerChance) = 5 Then
                Dim nr As Integer = Nez.Random.Range(0, PlCount * SpceCount)
                Do While ((nr Mod SpceCount) = 0 Or SaucerFields.Contains(nr) Or IsFieldCovered(SpielerIndex, -1, nr)) And nr > 0
                    nr -= 1
                Loop
                If nr >= 0 Then
                    SaucerFields.Add(nr)
                    SendFlyingSaucerAdded(nr)
                End If
            End If

            'Setze benötigte Flags
            SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Do While Spielers(SpielerIndex).Typ = SpielerTyp.None
                SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Loop
            'Setze HUD flags
            If Spielers(SpielerIndex).SacrificeCounter > 0 Then Spielers(SpielerIndex).SacrificeCounter -= 1 'Reduziere Sacrifice counter
            If Spielers(SpielerIndex).Typ <> SpielerTyp.Online Then Status = SpielStatus.Würfel Else Status = SpielStatus.Waitn
            SendNewPlayerActive(SpielerIndex)
            If Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex
            HUDBtnC.Active = Not Spielers(SpielerIndex).Angered And SpielerIndex = UserIndex
            HUDBtnD.Active = SpielerIndex = UserIndex
            HUDBtnD.Text = If(Spielers(SpielerIndex).SacrificeCounter <= 0, "Sacrifice", "(" & Spielers(SpielerIndex).SacrificeCounter & ")")
            HUD.Color = hudcolors(UserIndex)
            'Reset camera if not already moving
            If FigurFaderCamera.State <> TransitionState.InProgress Then FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = StdCam}
            'Set game flags
            ShowDice = True
            StopUpdating = False
            SendGameActive()
            HUDInstructions.Text = "Roll the Dice!"
            DreifachWürfeln = CanRollThrice(SpielerIndex) 'Falls noch alle Figuren un der Homebase sind
            WürfelTimer = 0
            WürfelAktuelleZahl = 0
            ReDim WürfelWerte(5)
            For i As Integer = 0 To WürfelWerte.Length - 1
                WürfelWerte(i) = 0
            Next
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
        Private Sub AngerButton() Handles HUDBtnC.Clicked
            If Status = SpielStatus.Würfel And Not StopUpdating Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("You get angry, because you suck at this game.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "You suck!")
                If Microsoft.VisualBasic.MsgBox("You are granted a single Joker. Do you want to utilize it now?", Microsoft.VisualBasic.MsgBoxStyle.YesNo, "You suck!") = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                    Dim res As String = Microsoft.VisualBasic.InputBox("How far do you want to move? (12 fields are the maximum and 1 field the minimum)", "You suck!")
                    Try
                        Dim aim As Integer = CInt(res)
                        Do Until aim < 13 And aim > 0
                            res = Microsoft.VisualBasic.InputBox("Screw you! I said 1 <= x <= 12 FIELDS!", "You suck!")
                            aim = CInt(res)
                        Loop
                        WürfelWerte(0) = If(aim > 6, 6, aim)
                        WürfelWerte(1) = If(aim > 6, aim - 6, 0)
                        CalcMoves()
                        Spielers(UserIndex).Angered = True
                        HUDBtnC.Active = False
                        SFX(2).Play()
                    Catch
                        Microsoft.VisualBasic.MsgBox("Alright, then don't.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "You suck!")
                    End Try
                End If
                StopUpdating = False
            Else
                SFX(0).Play()
            End If
        End Sub

        Private Sub SacrificeButton() Handles HUDBtnD.Clicked
            If Status = SpielStatus.Würfel And Not StopUpdating And Spielers(UserIndex).SacrificeCounter <= 0 Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("You can sacrifice one of your players to the holy BV gods. The further your player is, the higher is the chance to recieve a positive effect.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "YEET")
                If Microsoft.VisualBasic.MsgBox("You really want to sacrifice one of your precious players?", Microsoft.VisualBasic.MsgBoxStyle.YesNo, "YEET") = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                    Status = SpielStatus.WähleOpfer
                    DontKickSacrifice = Spielers(UserIndex).SacrificeCounter < 0
                    Spielers(UserIndex).SacrificeCounter = SacrificeWait
                    HUDBtnD.Text = "(" & SacrificeWait & ")"
                    'Move camera
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                Else
                    Microsoft.VisualBasic.MsgBox("Dann halt nicht.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "You suck!")
                End If
                StopUpdating = False
            Else
                SFX(0).Play()
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

        Private ReadOnly Property IGameWindow_FigurFaderScales As Dictionary(Of (Integer, Integer), Transition(Of Single)) Implements IGameWindow.FigurFaderScales
            Get
                Return FigurFaderScales
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

        Private ReadOnly Property IGameWindow_FigurFaderZiel As (Integer, Integer) Implements IGameWindow.FigurFaderZiel
            Get
                Return FigurFaderZiel
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

        Private ReadOnly Property IGameWindow_FigurFaderXY As Transition(Of Vector2) Implements IGameWindow.FigurFaderXY
            Get
                Return FigurFaderXY
            End Get
        End Property

        Private ReadOnly Property IGameWindow_FigurFaderZ As Transition(Of Integer) Implements IGameWindow.FigurFaderZ
            Get
                Return FigurFaderZ
            End Get
        End Property

        Public ReadOnly Property MapRet As GaemMap Implements IGameWindow.Map
            Get
                Return Map
            End Get
        End Property

        Private ReadOnly Property IGameWindow_SaucerFields As List(Of Integer) Implements IGameWindow.SaucerFields
            Get
                Return SaucerFields
            End Get
        End Property

        Private ReadOnly Property IGameWindow_ShowDice As Boolean Implements IGameWindow.ShowDice
            Get
                Return ShowDice
            End Get
        End Property

        Private ReadOnly Property IGameWindow_WürfelAktuelleZahl As Integer Implements IGameWindow.WürfelAktuelleZahl
            Get
                Return WürfelAktuelleZahl
            End Get
        End Property

        Private ReadOnly Property IGameWindow_WürfelWerte As Integer() Implements IGameWindow.WürfelWerte
            Get
                Return WürfelWerte
            End Get
        End Property

        Private ReadOnly Property IGameWindow_DreifachWürfeln As Boolean Implements IGameWindow.DreifachWürfeln
            Get
                Return DreifachWürfeln
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
#End Region
    End Class
End Namespace