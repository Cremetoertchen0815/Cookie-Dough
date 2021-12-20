Imports System.Collections.Generic
Imports System.Linq
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.BetretenVerboten.Rendering
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

        'Instance fields
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
        Friend Difficulty As Difficulty 'Declares the difficulty of the CPU
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private StopWhenRealStart As Boolean = False 'Notices, that the game is supposed to be interrupted, as soon as it's being started
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private Timer As TimeSpan 'Misst die Zeit seit dem Anfang des Spiels
        Private LastTimer As TimeSpan 'Gibt den Timer des vergangenen Frames an
        Private TimeOver As Boolean = False 'Gibt an, ob die registrierte Zeit abgelaufen ist
        Private IsAskingForNameWish As Boolean = False 'Gibt an, ob das Spiel gerade einen Teamnamenswunsch von allen Spielern erwartet
        Private SlideFields As Dictionary(Of Integer, Integer) 'Enthält alle Slide-Felder als Key und deren Ziel als Value
        Friend TeamMode As Boolean
        Friend TeamNameA As String = "A"
        Friend TeamNameB As String = "B"

        'Game fields
        Private WürfelAktuelleZahl As Integer 'Speichert den Wert des momentanen Würfels
        Private WürfelWerte As Integer() 'Speichert die Werte der Würfel
        Private WürfelTimer As Double 'Wird genutzt um den Würfelvorgang zu halten
        Private WürfelAnimationTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
        Private WürfelTriggered As Boolean 'Gibt an ob gerade gewürfelt wird
        Private DreifachWürfeln As Boolean 'Gibt an(am Anfang des Spiels), dass ma drei Versuche hat um eine 6 zu bekommen
        Private Fahrzahl As Integer 'Anzahl der Felder die gefahren werden kann
        Private MoveActive As Boolean = False 'Gibt an, ob eine Figuranimation in Gange ist
        Private SaucerFields As New List(Of Integer) 'Keeps track of which fields are covered by a UFO fields
        Private DontKickSacrifice As Boolean 'Gibt an, ob die zu opfernde Figur nicht gekickt werden soll

        'Assets
        Private Fanfare As Song
        Private DamDamDaaaam As Song
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont
        Private DataTransmissionThread As Threading.Thread

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
        Private WithEvents HUDAfkBtn As Button
        Private WithEvents HUDdbgLabel As Label
        Private WithEvents HUDmotdLabel As Label
        Private WithEvents HUDDiceBtn As GameRenderable
        Private WithEvents HUDScores As CustomControl
        Private InstructionFader As ITween(Of Color)
        Private ShowDice As Boolean = False
        Private Chat As List(Of (String, Color))

        'Debug shit
        Private LogPath As String = "Log/chat/" & Date.Now.ToShortDateString & " " & Date.Now.ToShortTimeString.Replace(":"c, "."c) & ".log"
        Private Shared dbgKickuser As Integer = -1
        Private Shared dbgExecSync As Boolean = False
        Private Shared dbgPlaceCmd As (Integer, Integer, Integer)
        Private Shared dbgPlaceSet As Boolean = False
        Private Shared dbgEnd As Boolean = False
        Private Shared dbgCamFree As Boolean = False
        Private Shared dbgCam As Keyframe3D = Nothing
        Private Shared dbgLoguser As Integer = -1
        Private Shared dbgEndlessAnger As Boolean = False
        Private Shared dbgLoadState As Boolean = False
        Private Shared dbgSaveState As Boolean = False

        'Spielfeld
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        Private Center As Vector2 'Gibt den Mittelpunkt des Screen-Viewports des Spielfelds an
        Friend FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
        Friend FigurFaderEnd As Single 'Gibt an auf welchem Feld der Zug enden soll
        Friend FigurFaderXY As Transition(Of Vector2) 'Bewegt die zu animierende Figur auf der X- und Y-Achse
        Friend FigurFaderZ As Transition(Of Integer)  'Bewegt die zu animierende Figur auf der Z-Achse
        Friend FigurFaderScales As New Dictionary(Of (Integer, Integer), Transition(Of Single)) 'Gibt die Skalierung für einzelne Figuren an Key: (Spieler ID, Figur ID) Value: Transition(Z)
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False)} 'Bewegt die Kamera 
        Friend CPUTimer As Single 'Timer-Flag um der CPU etwas "Überlegzeit" zu geben
        Friend PlayStompSound As Boolean 'Gibt an, ob der Stampf-Sound beim Landen(Kicken) gespielt werden soll
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False) 'Gibt die Standard-Position der Kamera an

        'Konstanten
        Private Const WürfelDauer As Integer = 320
        Private Const WürfelAnimationCooldown As Integer = 4
        Private Const FigurSpeed As Integer = 400
        Private Const ErrorCooldown As Integer = 1
        Private Const RollDiceCooldown As Single = 0.5
        Private Const CPUThinkingTime As Single = 0.5
        Private Const DopsHöhe As Integer = 130
        Private Const CamSpeed As Integer = 1200
        Private Const SacrificeWait As Integer = 4
        Private SaucerChance As Integer = 18

        Public Sub New(Map As GaemMap, TeamMode As Boolean)
            'Bereite Flags und Variablen vor
            Status = SpielStatus.WarteAufOnlineSpieler
            WürfelTimer = 0
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            Status = SpielStatus.WarteAufOnlineSpieler
            SpielerIndex = -1
            UserIndex = -1
            MoveActive = False
            Me.Map = Map

            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            Select Case Map
                Case GaemMap.Plus
                    Timer = New TimeSpan(0, 1, 11, 11, 11)
                    Player.DefaultArray = {-1, -1, -1, -1}
                    FigCount = 4
                    PlCount = 4
                    SpceCount = 10
                    SaucerChance = 18
                Case GaemMap.Star
                    Timer = New TimeSpan(0, 1, 11, 11, 11)
                    Player.DefaultArray = {-1, -1} '{-1, -1}
                    FigCount = 2
                    PlCount = 6
                    SpceCount = 8
                    SaucerChance = 14
                Case GaemMap.Octagon
                    Timer = New TimeSpan(0, 1, 11, 11, 11)
                    Player.DefaultArray = {-1, -1}
                    FigCount = 2
                    PlCount = 8
                    SpceCount = 7
                    SaucerChance = 10
                Case GaemMap.Snakes
                    Timer = New TimeSpan(0, 1, 11, 11, 11)
                    Player.DefaultArray = {-1}
                    FigCount = 1
                    PlCount = 4
                    SpceCount = 100
                    SaucerChance = 10
            End Select

            'GEILES MINT: New Color(0, 255, 100)

            'Adapt colors to team mode
            Me.TeamMode = TeamMode
            If TeamMode Then
                hudcolors = {Color.Lerp(Color.Red, Color.Yellow, 0F), Color.Lerp(Color.Turquoise, Color.Navy, 0F), Color.Lerp(Color.Red, Color.Yellow, 0.2F), Color.Lerp(Color.Turquoise, Color.Navy, 0.5F), Color.Lerp(Color.Red, Color.Yellow, 0.55F), Color.Lerp(Color.Turquoise, Color.Navy, 1.0F), Color.Lerp(Color.Red, Color.Yellow, 1.0F), New Color(50, 0, 100)}
                playcolor = hudcolors
                Farben = {"Kamerad A1", "Kamerad B1", "Kamerad A2", "Kamerad B2", "Kamerad A3", "Kamerad B3", "Kamerad A4", "Kamerad B4"}
            Else
                hudcolors = {Color.Magenta, Color.Lime, Color.Cyan, Color.Orange, New Color(255, 32, 32), New Color(48, 48, 255), Color.Teal, New Color(85, 120, 20)}
                playcolor = {Color.Magenta, Color.Lime, Color.Cyan, Color.Yellow, Color.Maroon * 1.5F, New Color(0, 0, 200), New Color(0, 80, 80), New Color(85, 120, 20)}
                Farben = {"Magenta", "Lime", "Cyan", "Yellow", "Red", "Blue", "Teal", "Olive"}
            End If

            'Load slide fields
            Dim slides = GetSnakeFields(Map)
            SlideFields = New Dictionary(Of Integer, Integer)
            For Each element In slides
                SlideFields.Add(element.Item1, element.Item2)
            Next
            LastTimer = Timer
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ChatText"))
            Fanfare = Content.Load(Of Song)("bgm/fanfare")
            DamDamDaaaam = Content.Load(Of Song)("sfx/DamDamDaaam")

            'Lade HUD
            Dim glass = New Color(5, 5, 5, 185)
            HUD = New GuiSystem
            HUDDiceBtn = New GameRenderable(Me) With {.RedrawBackground = True, .BackgroundColor = glass} : HUD.Controls.Add(HUDDiceBtn)
            HUDBtnB = New Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Controls.Add(HUDBtnB)
            HUDBtnC = New Button("Anger", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Controls.Add(HUDBtnC)
            HUDBtnD = New Button("Sacrifice", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Controls.Add(HUDBtnD)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .RedrawBackground = True, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            HUDdbgLabel = New Label(Function() FigurFaderCamera.Value.ToString, New Vector2(500, 120)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDdbgLabel)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Controls.Add(HUDmotdLabel)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/MenuTitle")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Controls.Add(HUDMusicBtn)
            HUDAfkBtn = New Button("AFK", New Vector2(220, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Controls.Add(HUDAfkBtn)
            HUDScores = New CustomControl(AddressOf RenderScore, Sub() Return, New Vector2(1600, 700), New Vector2(270, 300)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .Active = False} : HUD.Controls.Add(HUDScores)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = Color.White

            Renderer = AddRenderer(New Renderer3D(Me, 1))
            Psyground = AddRenderer(New PsygroundRenderer(0, 0.25))
            AddRenderer(New DefaultRenderer(2))
            GuiControl.BackgroundImage = Renderer.BlurredContents

            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.62F).SetThreshold(0)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            Center = New Rectangle(500, 70, 950, 950).Center.ToVector2
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(LoopType.PingPong, -1).Start()

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
                    Case SpielerTyp.CPU
                        Spielers(i).MOTD = CPU_MOTDs(i)
                        If i > 5 Then
                            Spielers(i).CustomSound = {GetLocalAudio(IdentType.TypeB), GetLocalAudio(IdentType.TypeA)}
                        Else
                            Spielers(i).CustomSound = {Content.LoadSoundEffect("prep/cpu_" & i & "_0"), Content.LoadSoundEffect("prep/cpu_" & i & "_1")}
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
            If dbgCamFree Then FigurFaderCamera.Value += New Keyframe3D(If(kstate.IsKeyDown(Keys.A), -1, 0) + If(kstate.IsKeyDown(Keys.D), 1, 0), If(kstate.IsKeyDown(Keys.S), -1, 0) + If(kstate.IsKeyDown(Keys.W), 1, 0), If(kstate.IsKeyDown(Keys.LeftShift), -1, 0) + If(kstate.IsKeyDown(Keys.Space), 1, 0), If(kstate.IsKeyDown(Keys.J), -0.01, 0) + If(kstate.IsKeyDown(Keys.L), 0.01, 0), If(kstate.IsKeyDown(Keys.K), -0.01, 0) + If(kstate.IsKeyDown(Keys.I), 0.01, 0), If(kstate.IsKeyDown(Keys.RightShift), -0.01, 0) + If(kstate.IsKeyDown(Keys.Enter), 0.01, 0), True) : HUDdbgLabel.Active = True

            'Load map
            If dbgLoadState Then
                dbgLoadState = False
                If IO.File.Exists("Cache\client\bv_game_cache.dat") Then
                    Dim data = Newtonsoft.Json.JsonConvert.DeserializeObject(Of (player_data As Player(), saucer_fields As Integer(), time_left As TimeSpan))(IO.File.ReadAllText("Cache\client\bv_game_cache.dat"))
                    For i As Integer = 0 To PlCount - 1
                        Spielers(i).AdditionalPoints = data.player_data(i).AdditionalPoints
                        Spielers(i).AngerCount = data.player_data(i).AngerCount
                        Spielers(i).SacrificeCounter = data.player_data(i).SacrificeCounter
                        Spielers(i).Spielfiguren = data.player_data(i).Spielfiguren
                        Spielers(i).SuicideField = data.player_data(i).SuicideField
                    Next
                    SaucerFields.Clear()
                    For Each element In data.saucer_fields
                        SaucerFields.Add(element)
                    Next
                    Timer = data.time_left
                    SendSync()
                    DebugConsole.Instance.Log("Game cache loaded!")
                End If
                DebugConsole.Instance.Log("Game cache file not present!")
            End If


            'Save map
            If dbgSaveState Then
                dbgSaveState = False
                Dim data = (Spielers, SaucerFields.ToArray(), Timer)
                IO.File.WriteAllText("Cache\client\bv_game_cache.dat", Newtonsoft.Json.JsonConvert.SerializeObject(data))
                DebugConsole.Instance.Log("Game cache saved!")
            End If

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

                    If TeamMode Then
                        'Get ranks
                        Dim teamA As Integer = 0
                        Dim teamB As Integer = 0
                        For i As Integer = 0 To PlCount / 2 - 1
                            teamA += GetScore(i * 2)
                            teamB += GetScore(i * 2 + 1)
                        Next

                        If teamA > teamB Then
                            Core.Schedule(2, Sub() PostChat("Team " & TeamNameA & " won(" & teamA & " points)!", Color.Red))
                            Core.Schedule(3, Sub() PostChat("Team " & TeamNameB & " lost(" & teamB & " points)", Color.Cyan))
                        ElseIf teamB > teamA Then
                            Core.Schedule(2, Sub() PostChat("Team " & TeamNameB & " won(" & teamB & " points)!", Color.Cyan))
                            Core.Schedule(3, Sub() PostChat("Team " & TeamNameA & " lost(" & teamA & " points)", Color.Cyan))
                        Else
                            Core.Schedule(2, Sub() PostChat("Draw(" & TeamNameA & ")!", Color.Gray))
                        End If

                        If GameMode = GameMode.Competetive Then
                            'Update highscores
                            Core.Schedule(5, AddressOf SendHighscore)
                            'Update K/D
                            If (teamA >= teamB And Mathf.IsEven(UserIndex)) Or (teamB >= teamA And Mathf.IsOdd(UserIndex)) Then My.Settings.GamesWon += 1 Else My.Settings.GamesLost += 1
                            My.Settings.Save()
                        End If
                    Else
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
                                    If defaultmov > -1 And defaultmov + Fahrzahl <= If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
                                Next

                                If ichmagzüge.Count = 1 Then
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
                                CPUTimer += Time.DeltaTime
                                If CPUTimer > CPUThinkingTime Then
                                    CPUTimer = 0

                                    Dim k As Integer
                                    Dim ichmagzüge As New List(Of Integer)
                                    Dim defaultmov As Integer
                                    Dim dontmove As Boolean = False
                                    For i As Integer = 0 To FigCount - 1
                                        defaultmov = pl.Spielfiguren(i)
                                        If defaultmov > -1 And defaultmov + Fahrzahl <= If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
                                    Next
                                    'Prüfe ob Zug möglich
                                    If ichmagzüge.Count = 0 Then SwitchPlayer() : Exit Select

                                    Select Case Difficulty
                                        Case Difficulty.Brainless

                                            'Berechne zufällig das zu fahrende Feld
                                            k = ichmagzüge(Nez.Random.Range(0, ichmagzüge.Count))

                                        Case Difficulty.Smart
                                            Dim behaviour As CpuBehaviour = CpuBehaviour.Behaviour(SpielerIndex)
                                            Dim Scores As New Dictionary(Of Integer, Single) ' im INteger ist der Index der FIgur und im Single der Score
                                            For Each element In ichmagzüge
                                                Scores.Add(element, 1)
                                            Next

                                            'Sacrifice: Check for figure sacrificing
                                            If behaviour.SacrificeCondition.HasFlag(CPUSacrificeCondition.EndGameWithNoWin) Then
                                                Dim house_count As Integer = 0
                                                Dim missing_fig As Integer
                                                Dim limit As Integer = If(Map > 2, SpceCount, PlCount * SpceCount)
                                                'Check for current state of figures
                                                For j As Integer = 0 To FigCount - 1
                                                    If pl.Spielfiguren(j) > limit Then
                                                        house_count += 1
                                                    Else
                                                        missing_fig = j
                                                    End If
                                                Next

                                                'If exactly one figure is not yet in it's house and the next move will land in it's house, but it's lacking in points, sacrifice
                                                If house_count = FigCount - 1 AndAlso pl.Spielfiguren(missing_fig) + Fahrzahl = limit Then
                                                    If TeamMode Then
                                                        Dim teamA As Integer = 0
                                                        Dim teamB As Integer = 0
                                                        For i As Integer = 0 To PlCount / 2 - 1
                                                            teamA += GetScore(i * 2)
                                                            teamB += GetScore(i * 2 + 1)
                                                        Next
                                                        If If(SpielerIndex Mod 2 = 0, teamB, teamA) > If(SpielerIndex Mod 2 = 1, teamB, teamA) + Fahrzahl * 10 Then
                                                            Sacrifice(SpielerIndex, ichmagzüge(0))
                                                            dontmove = True
                                                        End If
                                                    Else
                                                        Dim max_pnt As Integer = 0
                                                        For i As Integer = 0 To PlCount - 1
                                                            max_pnt = Math.Max(GetScore(i), max_pnt)
                                                        Next
                                                        If max_pnt >= GetScore(SpielerIndex) + Fahrzahl * 10 Then
                                                            Sacrifice(SpielerIndex, ichmagzüge(0))
                                                            dontmove = True
                                                        End If
                                                    End If
                                                End If
                                            End If

                                            'Spielfigurimportans: eine Figur die näher am Ziel ist ist wichtiger
                                            Dim counts As New List(Of (Integer, Integer))
                                            For Each element In ichmagzüge
                                                counts.Add((element, Spielers(SpielerIndex).Spielfiguren(element)))
                                            Next
                                            counts = counts.OrderBy(Function(x) x.Item2).ToList()
                                            For i As Integer = 0 To counts.Count - 1
                                                Scores(counts(i).Item1) *= 1 + i * behaviour.DistanceMultiplier
                                            Next

                                            For Each element In ichmagzüge
                                                ' Safety:ist eine Figur höchstens 6 felder vor einer Feindlichen Figur entfernt, ist sie in einer Gefahrenzone die avoidet werden soll
                                                Dim locpos As Integer() = {Spielers(SpielerIndex).Spielfiguren(element), Spielers(SpielerIndex).Spielfiguren(element) + Fahrzahl}
                                                Dim Globpos As Integer() = {PlayerFieldToGlobalField(locpos(0), SpielerIndex), PlayerFieldToGlobalField(locpos(1), SpielerIndex)}
                                                Dim playerTeam As Integer = SpielerIndex Mod 2
                                                For ALVSP As Integer = 0 To FigCount - 1
                                                    If ALVSP <> SpielerIndex Then
                                                        For ALVSPF As Integer = 0 To FigCount - 1
                                                            Dim locposB As Integer = Spielers(ALVSP).Spielfiguren(ALVSPF)
                                                            Dim GlobposB As Integer = PlayerFieldToGlobalField(locposB, ALVSP)
                                                            'Falls momentane Position in Feindlichiem Feld, verbessere Score(fliehen)
                                                            If GlobposB < Globpos(0) And GlobposB >= Globpos(0) - 6 Then
                                                                Scores(element) *= behaviour.PieceDzLeavingMultiplier
                                                            ElseIf GlobposB < Globpos(1) And GlobposB >= Globpos(1) - 6 And locpos(1) < If(Map > 2, SpceCount, PlCount * SpceCount) And locposB > -1 And Not (GlobposB < Globpos(0) And GlobposB >= Globpos(0) - 6) Then
                                                                'Falls momentanes Feld nicht in feindlichem Gebiet, aber zukünftiges, verschlechtere Score
                                                                Scores(element) *= behaviour.PieceDzEnteringMultiplier
                                                            End If
                                                        Next
                                                    End If
                                                Next

                                                ' Destiny: landet der zug im Haus? 
                                                If locpos(1) >= If(Map > 2, SpceCount, PlCount * SpceCount) Then
                                                    Scores(element) *= behaviour.ManifestDestinyMultiplier
                                                End If

                                                ' Attackopportunity: kann der zug einen Feindlichen spieler eleminieren? 
                                                Dim Ergebnis As (Integer, Integer) = GetKickFigur(SpielerIndex, element, Fahrzahl)
                                                If Ergebnis.Item1 <> -1 And Ergebnis.Item2 <> -1 Then
                                                    Dim targetTeam As Integer = Ergebnis.Item1 Mod 2
                                                    Scores(element) *= If(playerTeam = targetTeam, behaviour.AttackPartyMemberMultiplier, behaviour.AttackOpportunityMultiplier) 'Worsen score if kicking party member
                                                End If


                                                'Risk: nicht auf das Startfeld/den Eingangsbereich eines gegners stellen da eine neue figur erscheinen könnte.
                                                Dim aimpl As Integer = Math.Floor(Globpos(1) / SpceCount) 'The player who's section the figure will land on
                                                Dim landinginhaus As Boolean = locpos(1) >= If(Map > 2, SpceCount, PlCount * SpceCount) 'Determines whether the player will land in his haus
                                                Dim ishomeregionbusy As Boolean = aimpl <> SpielerIndex AndAlso GetHomebaseIndex(aimpl) > -1 'The home base linked to the area the piece is gonna land in, houses playing pieces.
                                                Dim isfieldcoveredbyUFO As Boolean = SaucerFields.Contains(PlayerFieldToGlobalField(locpos(1), SpielerIndex))
                                                If locpos(1) > 0 And (locpos(1) Mod SpceCount) = 0 And ishomeregionbusy And Not landinginhaus And Not isfieldcoveredbyUFO Then
                                                    Scores(element) *= behaviour.HomeFieldEnteringMultiplier
                                                ElseIf locpos(1) > 6 And (locpos(1) Mod SpceCount) < 7 And Not (locpos(0) Mod SpceCount) < 7 And ishomeregionbusy And Not landinginhaus Then
                                                    Scores(element) *= behaviour.HomeDzEnteringMultiplier
                                                End If

                                                'Anti-suicide: nicht auf das eigene(oder der eines teammates) Suicide field stellen
                                                If Not TeamMode And Globpos(1) = Spielers(SpielerIndex).SuicideField Then
                                                    Scores(element) *= behaviour.SuicideMultiplier
                                                ElseIf TeamMode Then
                                                    For i As Integer = 0 To PlCount / 2 - 1
                                                        If (Globpos(1) = Spielers(i * 2 + playerTeam).SuicideField) Then
                                                            Scores(element) *= behaviour.SuicideMultiplier
                                                            Exit For
                                                        End If
                                                    Next
                                                End If

                                                'Flee A: führt der Zug die Figur aus einem Startbereich heraus
                                                If locpos(0) > 6 And (locpos(0) Mod SpceCount) < 7 And (locpos(1) Mod SpceCount) > 6 Then
                                                    Scores(element) *= behaviour.HomeDzLeavingMultiplier
                                                End If

                                                'Flee B: führt der Zug die Figur von einem Startfeld weg
                                                If locpos(0) > 0 And (locpos(0) Mod SpceCount) = 0 And ishomeregionbusy Then
                                                    Scores(element) *= behaviour.HomeFieldLeavingMultiplier
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

                                    If dontmove Then Exit Select
                                    defaultmov = pl.Spielfiguren(k)
                                    'Setze flags
                                    Status = SpielStatus.FahreFelder
                                    FigurFaderZiel = (SpielerIndex, k)

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
                                If defaultmov > -1 And defaultmov + Fahrzahl < If(Map > 2, SpceCount, PlCount * SpceCount) Then ichmagzüge.Add(i)
                            Next

                            If ichmagzüge.Count = 1 Then
                                Sacrifice(SpielerIndex, ichmagzüge(0))
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
                        HUDInstructions.Text = If(IsAskingForNameWish, "Waiting for all players to answer their preferred team name...", "Waiting for all players to connect...")

                        If IsAskingForNameWish Or Not TeamMode Then

                            'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                            For Each sp In Spielers
                                If sp Is Nothing OrElse Not sp.Bereit OrElse (sp.TeamNameWish = String.Empty And TeamMode AndAlso (sp.Typ = SpielerTyp.Online Or sp.Typ = SpielerTyp.Local)) Then Exit Select 'Falls ein Spieler noch nicht belegt/bereit, breche Spielstart ab
                            Next

                            'Falls vollzählig, starte Spiel
                            If TeamMode Then
                                'Post message to chat
                                PostChat("Thank a lot! *bless*", Color.White)
                                PostChat("Let'sa go!", Color.White)
                                SendMessage("Thank a lot! *bless*")
                                SendMessage("Let'sa go!")
                                'Pick team names
                                Dim teamA As New List(Of String)
                                Dim teamB As New List(Of String)
                                For i As Integer = 0 To PlCount - 1
                                    Dim curent_team = If(i Mod 2 = 0, teamA, teamB)
                                    If Spielers(i).Typ = SpielerTyp.Online Or Spielers(i).Typ = SpielerTyp.Local Then curent_team.Add(Spielers(i).TeamNameWish)
                                Next
                                If teamA.Count > 0 Then TeamNameA = teamA(Nez.Random.Range(0, teamA.Count))
                                If teamB.Count > 0 Then TeamNameB = teamB(Nez.Random.Range(0, teamB.Count))
                            End If
                            StopUpdating = True
                            Core.Schedule(0.8, Sub()
                                                   PostChat("The game has started!", Color.White)
                                                   FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = StdCam}
                                                   HUDInstructions.Text = " "
                                                   'Launch start animation
                                                   Renderer.TriggerStartAnimation(TeamMode, Sub()
                                                                                                SwitchPlayer()
                                                                                                If StopWhenRealStart Then StopUpdating = True
                                                                                            End Sub)
                                                   SendBeginGaem()
                                               End Sub)

                        ElseIf Not IsAskingForNameWish Then

                            'Prüfe einer die vier Spieler nicht anwesend sind, kehre zurück
                            For Each sp In Spielers
                                If sp Is Nothing OrElse Not sp.Bereit Then Exit Select 'Falls ein Spieler noch nicht belegt/bereit, breche Spielstart ab
                            Next

                            IsAskingForNameWish = True
                            Core.Schedule(0.5F, Sub()
                                                    PostChat("Enter you wish for a team name into the chat:", Color.White)
                                                    SendMessage("Enter you wish for a team name into the chat:")
                                                    PostChat("(The winner will be picked at random)", Color.White)
                                                    SendMessage("(The winner will be picked at random)")
                                                End Sub)
                            For i As Integer = 0 To PlCount - 1
                                Dim j = i
                                If Spielers(i).Typ = SpielerTyp.Local Then MsgBoxer.EnqueueInputbox("Enter your wish team, " & Spielers(i).Name & "!", Sub(x, y) Spielers(j).TeamNameWish = x, If(i Mod 2 = 0, TeamNameA, TeamNameB))
                            Next
                        End If
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
            If MoveActive Then Return

            Try
                Dim data As String() = LocalClient.ReadStream()
                For Each element In data
                    Dim source As Integer = element(0).ToString
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
                            If IsAskingForNameWish Then Spielers(source).TeamNameWish = text
                        Case "e"c 'Suspend gaem
                            If Spielers(source).Typ = SpielerTyp.None Then Continue For
                            Spielers(source).Bereit = False
                            PostChat(Spielers(source).Name & " left!", Color.White)
                            If Not StopUpdating And Status <> SpielStatus.SpielZuEnde And Status <> SpielStatus.WarteAufOnlineSpieler Then PostChat("The game is being suspended!", Color.White)
                            If Status <> SpielStatus.WarteAufOnlineSpieler Then StopUpdating = True
                            If Renderer.BeginTriggered Then StopWhenRealStart = True

                            SendPlayerLeft(source)
                        Case "i"c 'Afk button toggled
                            If SpielerIndex = source And Status = SpielStatus.Waitn Then Status = SpielStatus.Würfel
                            Spielers(source).IsAFK = Not Spielers(source).IsAFK
                            SendSync()
                        Case "j"c 'God got activated
                            Dim figur As Integer = element(2).ToString
                            DontKickSacrifice = Spielers(source).SacrificeCounter < 0
                            Spielers(source).SacrificeCounter = SacrificeWait
                            Sacrifice(source, figur)
                        Case "m"c 'Sent chat message
                            Dim msg As String = element.Substring(2)
                            PostChat(msg, Color.White)
                        Case "n"c 'Switch player
                            SwitchPlayer()
                        Case "p"c 'Player angered
                            SetAngered(source)
                            SendSync()
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
                        Case "s"c 'Move figure
                            Dim figur As Integer = element(2).ToString
                            Dim destination As Integer = element.Substring(3).ToString
                            SendFigureTransition(source, figur, destination)
                            'Animiere wie die Figur sich nach vorne bewegt, anschließend kehre zurück zum nichts tun
                            Dim defaultmov As Integer = Math.Max(Spielers(source).Spielfiguren(figur), 0)
                            Status = SpielStatus.FahreFelder
                            FigurFaderZiel = (source, figur)
                            StartMoverSub(destination)
                        Case "y"c 'Sync requested
                            SendSync()
                        Case "z"c 'Transmit user data
                            Dim s As New Threading.Thread(Sub()
                                                              Dim IdentSound As IdentType = CInt(element(2).ToString)
                                                              Dim dataNr As Integer = element(3).ToString
                                                              Dim dat As String = element.Substring(4).Replace("_TATA_", "")
                                                              Try
                                                                  If dataNr = 9 Then
                                                                      'Receive pfp
                                                                      If IdentSound = IdentType.Custom Then
                                                                          IO.File.WriteAllBytes("Cache/server/" & Spielers(source).Name & "_pp.png", Compress.Decompress(Convert.FromBase64String(dat)))
                                                                          Spielers(source).Thumbnail = Texture2D.FromFile(Dev, "Cache/server/" & Spielers(source).Name & "_pp.png")
                                                                      End If
                                                                      SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & dataNr.ToString & "_TATA_" & dat)
                                                                  Else
                                                                      'Receive sound
                                                                      If IdentSound = IdentType.Custom Then
                                                                          IO.File.WriteAllBytes("Cache/server/" & Spielers(source).Name & dataNr.ToString & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                                                                          Spielers(source).CustomSound(dataNr) = SoundEffect.FromFile("Cache/server/" & Spielers(source).Name & dataNr.ToString & ".wav")
                                                                      Else
                                                                          Spielers(source).CustomSound(dataNr) = SoundEffect.FromFile("Content/prep/audio_" & CInt(IdentSound).ToString & ".wav")
                                                                      End If
                                                                      SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & dataNr.ToString & "_TATA_" & dat)
                                                                  End If

                                                              Catch ex As Exception
                                                                  'Data damaged, send standard sound
                                                                  If dataNr = 9 Then Exit Sub
                                                                  IdentSound = If(dataNr = 0, IdentType.TypeB, IdentType.TypeA)
                                                                  Spielers(source).CustomSound(dataNr) = SoundEffect.FromFile("Content/prep/audio_" & CInt(IdentSound).ToString & ".wav")
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
            SendNetworkMessageToAll("b" & appendix & "|" & TeamNameA & "|" & TeamNameB)
            SendPlayerData()
            SendSync()
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
            If TeamMode Then
                'Get team scores
                Dim teamA = 0
                Dim teamB = 0
                For i As Integer = 0 To PlCount / 2 - 1
                    If Spielers(i * 2).OriginalType <> SpielerTyp.None Then teamA += GetScore(i * 2)
                    If Spielers(i * 2 + 1).OriginalType <> SpielerTyp.None Then teamB += GetScore(i * 2 + 1)
                Next

                Dim pls As New List(Of (String, Integer))
                If teamA > 0 Then pls.Add(("Team " & TeamNameA, teamA))
                If teamB > 0 Then pls.Add(("Team " & TeamNameB, teamB))

                If pls.Count < 1 Then Return
                SendNetworkMessageToAll("h" & 0.ToString & CInt(Map).ToString & 1.ToString & Newtonsoft.Json.JsonConvert.SerializeObject(pls))
            Else
                Dim pls As New List(Of (String, Integer))
                For i As Integer = 0 To Spielers.Length - 1
                    If Spielers(i).OriginalType = SpielerTyp.Local Or Spielers(i).OriginalType = SpielerTyp.Online Then pls.Add((Spielers(i).ID, GetScore(i)))
                Next
                SendNetworkMessageToAll("h" & 0.ToString & CInt(Map).ToString & 0.ToString & Newtonsoft.Json.JsonConvert.SerializeObject(pls))
            End If
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
            SendSync()
        End Sub
        Private Sub SendAngered(who As Integer)
            SendNetworkMessageToAll("p" & who.ToString)
        End Sub
        Private Sub SendSlide(pl As Integer, figur As Integer, aim As Integer)
            SendNetworkMessageToAll("o" & pl.ToString & figur.ToString & aim.ToString)
        End Sub
        Private Sub SendPlayerBack(index As Integer)
            SendNetworkMessageToAll("r" & index.ToString)
            SendSync()
            SendPlayerData()
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
        Private Sub SendPlayerData()
            Dim dataSender As New Threading.Thread(Sub()
                                                       For i As Integer = 0 To Spielers.Length - 1
                                                           Dim pl = Spielers(i)
                                                           If pl.Typ = SpielerTyp.Local Then
                                                               'Send Sound A
                                                               Dim txt As String = ""
                                                               Dim snd As IdentType = GetPlayerAudio(i, False, txt)
                                                               SendNetworkMessageToAll("z" & i.ToString & CInt(snd).ToString & "0" & "_TATA_" & txt) 'Suffix "_TATA_" is to not print out in console

                                                               'Send Sound B
                                                               txt = ""
                                                               snd = GetPlayerAudio(i, True, txt)
                                                               SendNetworkMessageToAll("z" & i.ToString & CInt(snd).ToString & "1" & "_TATA_" & txt)

                                                               'Send Thumbnail
                                                               txt = ""
                                                               If My.Settings.Thumbnail And pl.Typ = SpielerTyp.Local Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/pp.png")))
                                                               SendNetworkMessageToAll("z" & i.ToString & If(My.Settings.Thumbnail And pl.Typ = Global.Cookie_Dough.SpielerTyp.Local, IdentType.Custom, 0).ToString & "9" & "_TATA_" & txt)
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
                        If ret = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundB.audio")))
                    Else
                        ret = My.Settings.SoundA
                        If ret = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundA.audio")))
                    End If
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
                Fahrzahl = GetSecondDiceAfterSix()
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

                    If Map <> GaemMap.Snakes And Spielers(SpielerIndex).Typ <> SpielerTyp.CPU Then
                        'Move camera
                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                    End If

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

                If Map <> GaemMap.Snakes And Spielers(SpielerIndex).Typ <> SpielerTyp.CPU Then
                    'Move camera
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                End If
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
                    If fieldB >= 0 And fieldB < If(Map > 2, SpceCount, PlCount * SpceCount) And fb = fa Then
                        Dim kickingAlly = Mathf.IsEven(playerA) = Mathf.IsEven(playerB) And TeamMode
                        'Implement BV bonus
                        If fieldA = 0 Then
                            Core.Schedule(1, Sub()
                                                 PostChat("BETRETEN VERBOTEN!", Color.White)
                                                 PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White)
                                             End Sub)
                            Spielers(playerA).AdditionalPoints += 50 * If(kickingAlly, -1, 1)
                        Else
                            Core.Schedule(1, Sub() PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White))
                            Spielers(playerA).AdditionalPoints += 25 * If(kickingAlly, -1, 1)
                        End If

                        If kickingAlly Then SFX(9).Play()
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
            SFX(9).Play()
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
                    If fieldB >= 0 And fieldB <= If(Map > 2, SpceCount, PlCount * SpceCount) And fb = fa Then Return (playerB, j)
                Next
            Next
            Return (-1, -1)
        End Function


        Private Sub TriggerSaucer(last As Integer)
            'If we're in snek Map, trigger god field
            Sacrifice(FigurFaderZiel.Item1, -1)
            Return

            'Trigger UFO
            SaucerFields.Remove(last)
            Status = SpielStatus.SaucerFlight
            Dim distance As Integer = Nez.Random.Range(-6, 7)
            Do While IsFieldCovered(SpielerIndex, -1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + distance) Or distance = 0 Or Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + distance < 0 Or Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + distance >= If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount
                distance = Nez.Random.Range(-6, 7)
            Loop

            Dim nr As Integer = Math.Min(Math.Max(distance + Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), 0), If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount - 1)
            If NetworkMode Then SendFlyingSaucerActive(last, nr)
            Renderer.TriggerSaucerAnimation(FigurFaderZiel, Sub()
                                                                Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = nr
                                                                HUDInstructions.Text = If(distance > 0, "+", "") & distance
                                                            End Sub, Sub()
                                                                         'Check if succesive UFO field
                                                                         Dim saucertrigger As Boolean = False
                                                                         For Each element In SaucerFields
                                                                             If PlayerFieldToGlobalField(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), FigurFaderZiel.Item1) = element Then
                                                                                 saucertrigger = True
                                                                                 nr = element
                                                                             End If
                                                                         Next

                                                                         If CheckTeamSuicide() Then
                                                                             'Trigger suicide
                                                                             saucertrigger = False
                                                                         ElseIf Not saucertrigger AndAlso CheckForSlide() Then
                                                                             'Trigger slide
                                                                             Status = SpielStatus.Waitn
                                                                             Return
                                                                         End If


                                                                         'Trigger UFO, falls auf Feld gelandet
                                                                         If saucertrigger And Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < If(Map > 2, SpceCount, PlCount * SpceCount) Then TriggerSaucer(nr) Else If Status <> SpielStatus.SpielZuEnde Then SwitchPlayer()
                                                                     End Sub)
        End Sub

        Private Function CheckTeamSuicide() As Boolean
            If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < If(Map > 2, SpceCount, PlCount * SpceCount) Then 'If figure is on regular playing field
                If TeamMode Then 'Check also for team mates' suicide fields
                    Dim team = FigurFaderZiel.Item1 Mod 2
                    For i As Integer = 0 To PlCount / 2 - 1
                        If CheckForSuicide(i * 2 + team) Then
                            Return True
                            Exit For
                        End If
                    Next
                Else  'Check only for own field
                    Return CheckForSuicide(FigurFaderZiel.Item1)
                End If
            End If

            Return False
        End Function

        Private Function CheckForSuicide(pl As Integer) As Boolean
            If Spielers(pl).SuicideField >= 0 AndAlso PlayerFieldToGlobalField(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), FigurFaderZiel.Item1) = Spielers(pl).SuicideField Then
                PostChat(Spielers(FigurFaderZiel.Item1).Name & " committed suicide!", Color.White)
                SendMessage(Spielers(FigurFaderZiel.Item1).Name & " committed suicide!")
                KickedByGod(FigurFaderZiel.Item1, FigurFaderZiel.Item2)
                Return True
            Else
                Return False
            End If
        End Function

        Private Function CheckForSlide() As Boolean
            Dim globalpos As Integer = PlayerFieldToGlobalField(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), FigurFaderZiel.Item1)
            If SlideFields.ContainsKey(globalpos) AndAlso Not IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, SlideFields(globalpos)) Then
                Status = SpielStatus.SaucerFlight
                Dim aim As Integer = SlideFields(globalpos)

                Renderer.TriggerSlideAnimation(FigurFaderZiel, aim, Sub()
                                                                        'Figure out the saucer data
                                                                        Dim saucertrigger As Boolean = False
                                                                        Dim nr As Integer
                                                                        For Each element In SaucerFields
                                                                            If PlayerFieldToGlobalField(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), FigurFaderZiel.Item1) = element Then
                                                                                saucertrigger = True
                                                                                nr = element
                                                                            End If
                                                                        Next

                                                                        If CheckTeamSuicide() Then
                                                                            'Trigger suicide
                                                                            saucertrigger = False
                                                                        ElseIf Not saucertrigger AndAlso CheckForSlide() Then
                                                                            'Trigger slide
                                                                            Status = SpielStatus.Waitn
                                                                            Return
                                                                        End If

                                                                        'Trigger UFO, falls auf Feld gelandet
                                                                        If saucertrigger And Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < If(Map > 2, SpceCount, PlCount * SpceCount) Then
                                                                            TriggerSaucer(nr)
                                                                        ElseIf Status <> SpielStatus.SpielZuEnde Then
                                                                            SwitchPlayer()
                                                                        End If
                                                                    End Sub)

                SendSlide(FigurFaderZiel.Item1, FigurFaderZiel.Item2, aim)
                Return True
            End If

            Return False
        End Function

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
                    If pl.Spielfiguren(j) < If(Map > 2, SpceCount, PlCount * SpceCount) Then check = False
                Next
                If check Then Return True
            Next
            Return False
        End Function

        Private Function GetFigureCountInHaus(spieler As Integer) As Integer
            Dim ret As Integer = 0
            Dim pl As Player = Spielers(spieler)
            For j As Integer = 0 To FigCount - 1
                If pl.Spielfiguren(j) >= If(Map > 2, SpceCount, PlCount * SpceCount) Then ret += 1
            Next
            Return ret
        End Function

        Private Function CanDoAMove() As Boolean
            Dim pl As Player = Spielers(SpielerIndex)

            'Wähle alle möglichen Zügen aus
            Dim ichmagzüge As New List(Of Integer)
            Dim defaultmov As Integer
            For i As Integer = 0 To FigCount - 1
                defaultmov = pl.Spielfiguren(i)
                'Prüfe ob Zug mit dieser Figur möglich ist(Nicht in homebase, nicht über Ziel hinaus und Zielfeld nicht mit eigener Figur belegt
                If defaultmov > -1 And defaultmov + Fahrzahl <= If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
            Next

            'Prüfe ob Zug möglich
            Return ichmagzüge.Count > 0
        End Function

        Private Function GetScore(pl As Integer) As Integer
            Dim ret As Single = Spielers(pl).AngerCount * 5
            For Each element In Spielers(pl).Spielfiguren
                If element >= 0 Then ret += element
            Next
            Return CInt(ret * 10) + Spielers(pl).AdditionalPoints
        End Function

        Private Function IsÜberholingInSeHaus(defaultmov As Integer) As Boolean
            If defaultmov + Fahrzahl < If(Map > 2, SpceCount, PlCount * SpceCount) Then Return False

            For i As Integer = defaultmov + 1 To defaultmov + Fahrzahl
                If IsFieldCovered(SpielerIndex, -1, i) And i >= If(Map > 2, SpceCount, PlCount * SpceCount) Then Return True
            Next

            Return False
        End Function

        Private Function GetFurthestSpaceInHaus() As Integer
            Dim max As Integer = If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount
            For i As Integer = 0 To FigCount - 1
                If Spielers(SpielerIndex).Spielfiguren(i) >= If(Map > 2, SpceCount, PlCount * SpceCount) Then max = Math.Min(Spielers(SpielerIndex).Spielfiguren(i), max)
            Next
            Return max - 1
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
                    If fieldB > -1 And ((fieldA < If(Map > 2, SpceCount, PlCount * SpceCount) AndAlso (player <> i Or figur <> j) And fb = fa) OrElse (player = i And figur <> j And fieldA = fieldB)) Then Return True
                Next
            Next

            Return False
        End Function

        Private Function GetFieldID(player As Integer, field As Integer) As (Integer, Integer)
            Dim fa As Integer = PlayerFieldToGlobalField(field, player)
            For j As Integer = 0 To PlCount - 1
                For i As Integer = 0 To FigCount - 1
                    Dim fieldB As Integer = Spielers(j).Spielfiguren(i)
                    If fieldB >= 0 And fieldB < If(Map > 2, SpceCount, PlCount * SpceCount) And fa = PlayerFieldToGlobalField(fieldB, j) Then Return (j, i)
                Next
            Next
            Return (-1, -1)
        End Function
        Private Function GetLocalAudio(ident As IdentType, Optional IsSoundB As Boolean = False) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content/prep/audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache/client/sound" & If(IsSoundB, "B", "A") & ".audio")
            End If
        End Function

        'Prüft, ob man dreimal würfeln darf
        Private Function CanRollThrice(player As Integer) As Boolean
            Dim fieldlst As New List(Of Integer)
            For i As Integer = 0 To FigCount - 1
                Dim tm As Integer = Spielers(player).Spielfiguren(i)
                If tm >= 0 And tm < If(Map > 2, SpceCount, PlCount * SpceCount) Then Return False 'Falls sich Spieler auf dem Spielfeld befindet, ist dreimal würfeln unmöglich
                If tm >= If(Map > 2, SpceCount, PlCount * SpceCount) Then fieldlst.Add(tm) 'Merke Figuren, die sich im Haus befinden
            Next

            'Wenn nicht alle FIguren bis an den Anschlag gefahren wurden, darf man nicht dreifach würfeln
            For i As Integer = If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount - 1 To (If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount - fieldlst.Count) Step -1
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
        Public Function CanAnger(index As Integer) As Boolean
            If dbgEndlessAnger Then Return True 'Always return that one AngerButton is present if debug option is set

            'Check for own anger count
            If Spielers(index).AngerCount > 0 Then Return True

            'Check for anger count of team mates
            If Not TeamMode Then Return False
            For i As Integer = 0 To PlCount / 2 - 1
                Dim team As Integer = index Mod 2
                If Spielers(i * 2 + team).Typ <> SpielerTyp.None And Spielers(i * 2 + team).AngerCount > 0 Then Return True
            Next

            Return False 'Else return regular count
        End Function

        Public Sub SetAngered(index As Integer)
            If dbgEndlessAnger Then Return 'Don't influence shit when dealing with debug anger

            If Spielers(index).AngerCount > 0 Then
                'The player has an anger button, which he uses
                Spielers(index).AngerCount -= 1
                If Spielers(index).Typ = SpielerTyp.Online Then SendAngered(index)
            Else
                'The player doesn't have an anger button, so he snitches one from his team mates
                If Not TeamMode Then Return
                For i As Integer = 0 To PlCount / 2 - 1
                    Dim team As Integer = index Mod 2

                    Dim pl = i * 2 + team
                    If Spielers(pl).Typ <> SpielerTyp.None And Spielers(pl).AngerCount > 0 Then
                        Spielers(pl).AngerCount -= 1

                        If Spielers(pl).Typ = SpielerTyp.Online Then SendAngered(pl)
                        Return
                    End If
                Next
            End If
        End Sub

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

        Private Function DoesContainField(indx As Integer) As Boolean
            For Each element In Spielers
                If element.SuicideField = indx Then Return True
            Next
            Return False
        End Function

        Private Function PlayerFieldToGlobalField(field As Integer, player As Integer) As Integer
            If Map > 2 Then Return field
            Return (field + player * SpceCount) Mod (PlCount * SpceCount)
        End Function

        Private Function GetSecondDiceAfterSix() As Integer
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
                                                                                                                                      If kickID = key.Item2 Then Spielers(key.Item1).Spielfiguren(key.Item2) = -1 'Kick figure
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
                MoveActive = False

                'Figure out the saucer data
                Dim saucertrigger As Boolean = False
                Dim nr As Integer
                For Each element In SaucerFields
                    If PlayerFieldToGlobalField(Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2), FigurFaderZiel.Item1) = element Then
                        saucertrigger = True
                        nr = element
                    End If
                Next

                If CheckTeamSuicide() Then
                    'Trigger suicide
                    saucertrigger = False
                ElseIf Not saucertrigger AndAlso CheckForSlide() Then
                    'Trigger slide
                    Status = SpielStatus.Waitn
                    Return
                End If

                'Trigger UFO, falls auf Feld gelandet
                If saucertrigger And Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < If(Map > 2, SpceCount, PlCount * SpceCount) Then TriggerSaucer(nr) Else SwitchPlayer()

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

        Private Sub RenderScore(batcher As Batcher, InnerBounds As Rectangle, color As Color)
            If GuiControl.BackgroundImage IsNot Nothing Then batcher.Draw(GuiControl.BackgroundImage, InnerBounds, InnerBounds, Color.White)
            batcher.DrawRect(InnerBounds, HUDScores.BackgroundColor)
            batcher.DrawHollowRect(InnerBounds, color, HUDScores.Border.Width)
            Dim space As Integer
            For i As Integer = 0 To PlCount - 1
                If Spielers(i).Typ = SpielerTyp.None Then Continue For
                space += 1
                batcher.DrawString(HUDScores.Font, Spielers(i).Name & ": " & GetScore(i), InnerBounds.Location.ToVector2 + New Vector2(30, space * 30), hudcolors(i))
            Next
        End Sub
        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            Log(txt)
            HUDChat.ScrollDown = True
        End Sub

        Private Sub Log(text As String)
            IO.File.AppendAllText(LogPath, text & Environment.NewLine)
        End Sub

        Private Sub Sacrifice(pl As Integer, figur As Integer)
            StopUpdating = True
            Status = SpielStatus.Waitn
            Dim pogfactor As Single 'Chance of getting sth good
            Dim semipogfactor As Single 'Chance of getting sth neutral
            If figur > -1 Then
                'Sacrifice
                Spielers(pl).AdditionalPoints += 25
                PostChat(Spielers(pl).Name & " offered one of his pieces to the gods...", Color.White)
                SendMessage(Spielers(pl).Name & " offered one of his pieces to the gods...")
                If Not DontKickSacrifice Then KickedByGod(pl, figur) 'Kick sacrifice
                Dim progress = Spielers(pl).Spielfiguren(figur) / (PlCount * SpceCount)
                pogfactor = progress * 0.4F + 0.2F 'Field 0: Chance of sth good: 20%;  Field max.: Chance of sth good: 50%
                semipogfactor = 0.2F
            Else
                'God field
                SendMessage(Spielers(pl).Name & "stepped on a god field...")
                pogfactor = 0.4F
                semipogfactor = 0.2F
            End If
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
                                                     If futurefield < If(Map > 2, SpceCount, PlCount * SpceCount) AndAlso Not IsFutureFieldCoveredByOwnFigure(pl, futurefield, fig) Then
                                                         Dim txt = If(FigCount <= 1, "You're lucky! Your figure is being boosted!", "You're lucky! A random figure of yours is being boosted!")
                                                         PostChat(txt, Color.White)
                                                         SendMessage(txt)
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
                                                 If (Not TeamMode And pla <> pl) Or (TeamMode And (pla Mod 2) <> (pl Mod 2)) And Not dont Then
                                                     PostChat("You're lucky! A random enemy figure got kicked!", Color.White)
                                                     SendMessage("You're lucky! A random enemy figure got kicked!")
                                                     KickedByGod(pla, fig)
                                                     Exit Do
                                                 End If
                                             Case 2
                                                 'Reset anger button
                                                 Dim hasAnger = Spielers(pl).AngerCount > 0
                                                 If hasAnger Then
                                                     PostChat("You're lucky! You got another anger button!", Color.White)
                                                     SendMessage("You're lucky! You got another anger button!")
                                                 Else
                                                     PostChat("You're lucky! You got back your anger button!", Color.White)
                                                     SendMessage("You're lucky! You got back your anger button!")
                                                 End If
                                                 Spielers(pl).AngerCount += 1
                                                 Exit Do
                                             Case 3
                                                 'Add points
                                                 PostChat("You're lucky! You gained 75 points!", Color.White)
                                                 SendMessage("You're lucky! You gained 75 points!")
                                                 Spielers(pl).AdditionalPoints += 75
                                                 Exit Do
                                         End Select
                                     ElseIf RNG > pogfactor + semipogfactor Then 'Negative effect
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
                                                 Dim pla = Nez.Random.Range(0, PlCount)
                                                 Dim dont = False
                                                 Dim count = 0
                                                 Do While Spielers(pl).Spielfiguren(fig) < 0 And Spielers(pl).Spielfiguren(fig) >= PlCount * SpceCount
                                                     fig = Nez.Random.Range(0, FigCount)
                                                     pla = Nez.Random.Range(0, PlCount)
                                                     count += 1
                                                     If count > 20 Then dont = True : Exit Do
                                                 Loop
                                                 If Not TeamMode Or (TeamMode And pl Mod 2 = pla Mod 2) And Not dont Then
                                                     Dim txt = If(FigCount <= 1, "Oh ooh! Your piece died!", "Oh ooh! One of your " & If(TeamMode, "team's ", "") & "pieces died!")
                                                     PostChat(txt, Color.White)
                                                     SendMessage(txt)
                                                     KickedByGod(pl, fig)
                                                     Exit Do
                                                 End If
                                             Case 2
                                                 'Set anger button
                                                 If Spielers(pl).AngerCount <= 0 Then Continue Do
                                                 PostChat("Oh ooh! You've lost an anger button!", Color.White)
                                                 SendMessage("Oh ooh! You've lost an anger button!")
                                                 Spielers(pl).AngerCount -= 1
                                                 Exit Do
                                         End Select
                                     Else
                                         If figur > -1 Or Nez.Random.Range(0, 3) = 1 Then
                                             'If on god field there a 2/3 chance of getting spared, if on sacrifice theres a 100% chance
                                             PostChat("You got spared.", Color.White)
                                             SendMessage("You got spared.")
                                         Else
                                             'Swap places with figure

                                         End If
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
                Dim nr As Integer = Nez.Random.Range(0, If(Map < GaemMap.Snakes, PlCount * SpceCount, SpceCount))
                Do While ((nr Mod SpceCount) = 0 Or SaucerFields.Contains(nr) Or IsFieldCovered(SpielerIndex, -1, nr)) And nr > 0
                    nr -= 1
                Loop
                If nr >= 0 Then
                    SaucerFields.Add(nr)
                    SendFlyingSaucerAdded(nr)
                End If
            End If
            'Generate suicide field
            If SpielerIndex >= 0 AndAlso Spielers(SpielerIndex).SuicideField < 0 AndAlso GetFigureCountInHaus(SpielerIndex) > FigCount - 2 Then
                Dim indx As Integer = -1
                Do While indx < 0 OrElse DoesContainField(indx)
                    indx = Nez.Random.Range(0, If(Map > 2, SpceCount, PlCount * SpceCount))
                Loop
                Spielers(SpielerIndex).SuicideField = indx
                SendSync()
            End If
            'Increment Player Index
            SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Do While Spielers(SpielerIndex).Typ = SpielerTyp.None
                SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Loop
            'Set game flags
            If Spielers(SpielerIndex).SacrificeCounter > 0 Then Spielers(SpielerIndex).SacrificeCounter -= 1 'Reduziere Sacrifice counter
            Status = If(Spielers(SpielerIndex).Typ <> SpielerTyp.Online, SpielStatus.Würfel, SpielStatus.Waitn)
            SendNewPlayerActive(SpielerIndex) 'Transmit to slaves that new player is active
            If Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex
            ShowDice = True
            StopUpdating = False
            SendGameActive()
            DreifachWürfeln = CanRollThrice(SpielerIndex) 'Falls noch alle Figuren un der Homebase sind
            WürfelTimer = 0
            WürfelAktuelleZahl = 0
            ReDim WürfelWerte(5)
            For i As Integer = 0 To WürfelWerte.Length - 1
                WürfelWerte(i) = 0
            Next
            'Set HUD flags
            ResetHUD()
            HUDInstructions.Text = "Roll the Dice!"
            'Reset camera if not already moving
            FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
        End Sub

        Private Sub ResetHUD()
            If UserIndex < 0 Then Return
            HUDBtnC.Active = CanAnger(UserIndex) And SpielerIndex = UserIndex And Not Spielers(UserIndex).IsAFK
            HUDBtnD.Active = SpielerIndex = UserIndex And Not Spielers(UserIndex).IsAFK And Map <> GaemMap.Snakes
            HUDBtnD.Text = If(Spielers(SpielerIndex).SacrificeCounter <= 0, "Sacrifice", "(" & Spielers(SpielerIndex).SacrificeCounter & ")")
            HUDAfkBtn.Text = If(Spielers(SpielerIndex).IsAFK, "Back Again", "AFK")
            HUD.TweenColorTo(If(UserIndex >= 0, hudcolors(UserIndex), Color.White), 0.5).SetEaseType(EaseType.CubicInOut).Start()
            HUDNameBtn.Active = True
            HUDScores.Active = UserIndex <> SpielerIndex
        End Sub
#End Region
#Region "Knopfgedrücke"

        Private chatbtnpressed As Boolean = False

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
#If Not MONO Then
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
#End If
        End Sub
        Private Sub VolumeButton() Handles HUDMusicBtn.Clicked
            MediaPlayer.Volume = If(MediaPlayer.Volume > 0F, 0F, 0.1F)
        End Sub
        Private Sub FullscrButton() Handles HUDFullscrBtn.Clicked
            Screen.IsFullscreen = Not Screen.IsFullscreen
            Screen.ApplyChanges()
        End Sub
        Private Sub MenuButton() Handles HUDBtnB.Clicked
            If Not Renderer.BeginTriggered Then MsgBoxer.OpenMsgbox("Do you really want to leave?", Sub(x)
                                                                                                        If x = 1 Then Return
                                                                                                        SFX(2).Play()
                                                                                                        SendGameClosed()
                                                                                                        NetworkMode = False
                                                                                                        Core.StartSceneTransition(New FadeTransition(Function() New CreatorMenu))
                                                                                                    End Sub, {"Yeah", "Nope"})
        End Sub
        Private Sub AngerButton() Handles HUDBtnC.Clicked
            If Status = SpielStatus.Würfel And Not StopUpdating And UserIndex >= 0 Then
                If Not MsgBoxer.OpenMsgbox("You get angry, because you suck at this game.", Nothing, {"OK"}) Then Return
                MsgBoxer.EnqueueMsgbox("You are granted a single Joker. Do you want to utilize it now?", Sub(x)
                                                                                                             If x = 0 Then
                                                                                                                 MsgBoxer.EnqueueInputbox("How far do you want to move? (12 fields are the maximum and 1 field the minimum)", AddressOf AngerButtonFinal, "")
                                                                                                             Else
                                                                                                                 MsgBoxer.EnqueueMsgbox("Alright, then don't.", Nothing, {"Bitch!"})
                                                                                                             End If
                                                                                                         End Sub, {"Yeah", "Nope"})

            Else
                SFX(0).Play()
            End If
        End Sub

        Private Sub AngerButtonFinal(text As String, button As Integer)
            Try
                If button <> 0 Then Throw New Exception
                Dim aim As Integer = CInt(text)
                If Not (aim < 13 And aim > 0) Then MsgBoxer.EnqueueInputbox("Screw you! I said 1 <= x <= 12 FIELDS!", AddressOf AngerButtonFinal, "") : Return
                WürfelWerte(0) = If(aim > 6, 6, aim)
                WürfelWerte(1) = If(aim > 6, aim - 6, 0)
                CalcMoves()
                SetAngered(UserIndex)
                HUDBtnC.Active = False
                SFX(2).Play()
            Catch
                MsgBoxer.EnqueueMsgbox("Alright, then don't.", Nothing, {"Bitch!"})
            End Try
        End Sub

        Private Sub SacrificeButton() Handles HUDBtnD.Clicked
            If Status = SpielStatus.Würfel And Not StopUpdating And UserIndex >= 0 AndAlso Spielers(UserIndex).SacrificeCounter <= 0 Then

                If Not MsgBoxer.OpenMsgbox("You can sacrifice one of your players to the holy BV gods. The further your player is, the higher is the chance to recieve a positive effect.", Nothing, {"OK"}) Then Return
                MsgBoxer.EnqueueMsgbox("You really want to sacrifice one of your precious players?", Sub(x)
                                                                                                         If x = 0 Then
                                                                                                             Status = SpielStatus.WähleOpfer
                                                                                                             DontKickSacrifice = Spielers(UserIndex).SacrificeCounter < 0
                                                                                                             Spielers(UserIndex).SacrificeCounter = SacrificeWait
                                                                                                             HUDBtnD.Text = "(" & SacrificeWait & ")"
                                                                                                             'Move camera
                                                                                                             FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                                                                                                         Else
                                                                                                             MsgBoxer.EnqueueMsgbox("Dann halt nicht.", Nothing, {"OK"})
                                                                                                         End If
                                                                                                     End Sub, {"Yeah", "Nah, mate"})
            Else
                SFX(0).Play()
            End If
        End Sub

        Private Sub AwayFromKeyboardButton() Handles HUDAfkBtn.Clicked
            If SpielerIndex < 0 OrElse Spielers(SpielerIndex).OriginalType <> SpielerTyp.Local Or UserIndex < 0 Then Return
            Spielers(SpielerIndex).IsAFK = Not Spielers(SpielerIndex).IsAFK
            If Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex
            ResetHUD()
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

        <Command("bv-anger", "Enables endless anger issues.")>
        Public Shared Sub dbgAnger()
            dbgEndlessAnger = True
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

        <Command("bv-cache", "Ends the game.")>
        Public Shared Sub dbgCache(cmd As String)
            Select Case cmd
                Case "save"
                    dbgSaveState = True
                Case "load"
                    dbgLoadState = True
                Case Else
                    Nez.Console.DebugConsole.Instance.Log("Wat")
                    Nez.Console.DebugConsole.Instance.Log("Type ""save"" or ""load""!")
            End Select
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
                Return Psyground.RenderTexture
            End Get
        End Property

        Public ReadOnly Property GameTexture As Texture2D Implements IGameWindow.GameTexture
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

        Public ReadOnly Property StartCamPoses As Keyframe3D() Implements IGameWindow.StartCamPoses
            Get
                Return {New Keyframe3D, New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False)}
            End Get
        End Property

        Public ReadOnly Property TeamNames As String() Implements IGameWindow.TeamNames
            Get
                Return {TeamNameA, TeamNameB}
            End Get
        End Property

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function
#End Region
    End Class
End Namespace