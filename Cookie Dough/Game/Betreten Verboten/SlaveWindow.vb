Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Framework.UI.Controls
Imports Cookie_Dough.Game.BetretenVerboten.Networking
Imports Cookie_Dough.Game.BetretenVerboten.Rendering
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media

Namespace Game.BetretenVerboten
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class SlaveWindow
        Inherits Scene
        Implements IGameWindow

        'Spiele-Flags und Variables
        Friend Spielers As Player() = {Nothing, Nothing, Nothing, Nothing} 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend Rejoin As Boolean = False
        Friend PlCount As Integer
        Friend FigCount As Integer
        Friend SpceCount As Integer
        Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
        Friend Map As GaemMap 'Gibt die Map an, die verwendet wird
        Friend NetworkMode As Boolean = False 'Gibt an, ob das SPiel online ist
        Friend GameMode As GameMode 'Gibt an, ob der Sieg/Verlust zur K/D gezählt werden soll
        Private SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Private Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Private WürfelAktuelleZahl As Integer 'Speichert den WErt des momentanen Würfels
        Private WürfelWerte As Integer() 'Speichert die Werte der Würfel
        Private WürfelTimer As Double 'Wird genutzt um den Würfelvorgang zu halten
        Private WürfelAnimationTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
        Private WürfelTriggered As Boolean 'Gibt an ob gerade gewürfelt wird
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private Fahrzahl As Integer 'Anzahl der Felder die gefahren werden kann
        Private DontKickSacrifice As Boolean 'Gibt an, ob die zu opfernde Figur nicht gekickt werden soll
        Private DreifachWürfeln As Boolean 'Gibt an(am Anfang des Spiels), dass ma drei Versuche hat um eine 6 zu bekommen
        Private CanGoAFK As Boolean
        Private lastmstate As MouseState
        Private lastkstate As KeyboardState
        Private MoveActive As Boolean = False
        Private StopDiceWhenFinished As Boolean = False 'Verhindert den Skip-Bug
        Private SaucerFields As New List(Of Integer)
        Friend TeamMode As Boolean
        Friend TeamNameA As String = "A"
        Friend TeamNameB As String = "B"

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
        Private WithEvents HUDAfkBtn As Button
        Private WithEvents HUDDiceBtn As GameRenderable
        Private WithEvents HUDmotdLabel As Label
        Private WithEvents HUDScores As CustomControl
        Private InstructionFader As PropertyTransition
        Private Chat As List(Of (String, Color))

        'Keystack
        Private keysa As New List(Of Keys)
        Private ButtonStack As New List(Of Keys)
        Private oldpress As New List(Of Keys)

        'Spielfeld
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        Private Center As Vector2
        Friend FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
        Friend FigurFaderKickZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
        Friend FigurFaderEnd As Single
        Friend FigurFaderXY As Transition(Of Vector2)
        Friend FigurFaderZ As Transition(Of Integer)
        Friend FigurFaderScales As New Dictionary(Of (Integer, Integer), Transition(Of Single))
        Friend FigurFaderCamera As New Transition(Of Keyframe3D)
        Friend CamRotation As Single
        Friend StdCam As New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False) 'Gibt die Standard-Position der Kamera an
        Friend PlayStompSound As Boolean

        Private Const WürfelDauer As Integer = 320
        Private Const WürfelAnimationCooldown As Integer = 4
        Private Const FigurSpeed As Integer = 450
        Private Const ErrorCooldown As Integer = 1
        Private Const RollDiceCooldown As Single = 0.5
        Private Const CPUThinkingTime As Single = 0.6
        Private Const DopsHöhe As Integer = 150
        Private Const CamSpeed As Integer = 1300
        Private Const SacrificeWait As Integer = 5

        Public Sub New(ins As OnlineGameInstance)
            LocalClient.AutomaticRefresh = False
            NetworkMode = False

            If Not LocalClient.JoinGame(ins, Sub(x)
                                                 'Load map info
                                                 Map = CInt(x())
                                                 GameMode = If(x(), GameMode.Casual, GameMode.Competetive)
                                                 TeamMode = CBool(x())

                                                 Select Case Map
                                                     Case GaemMap.Plus
                                                         Player.DefaultArray = {-1, -1, -1, -1}
                                                         FigCount = 4
                                                         PlCount = 4
                                                         SpceCount = 10
                                                     Case GaemMap.Star
                                                         Player.DefaultArray = {-1, -1}
                                                         FigCount = 2
                                                         PlCount = 6
                                                         SpceCount = 8
                                                     Case GaemMap.Octagon
                                                         Player.DefaultArray = {-1, -1}
                                                         FigCount = 2
                                                         PlCount = 8
                                                         SpceCount = 7
                                                     Case GaemMap.Snakes
                                                         Player.DefaultArray = {-1}
                                                         FigCount = 1
                                                         PlCount = 4
                                                         SpceCount = 100
                                                 End Select

                                                 'Load player info
                                                 ReDim Spielers(GetMapSize(Map) - 1)
                                                 UserIndex = CInt(x())
                                                 For i As Integer = 0 To GetMapSize(Map) - 1
                                                     Dim type As SpielerTyp = CInt(x())
                                                     Dim name As String = x()
                                                     Spielers(i) = New Player(If(type = SpielerTyp.None, type, SpielerTyp.Online)) With {.Name = If(i = UserIndex, My.Settings.Username, name)}
                                                     If type = SpielerTyp.CPU Then
                                                         If i > 5 Then
                                                             Spielers(i).CustomSound = {GetLocalAudio(IdentType.TypeB), GetLocalAudio(IdentType.TypeA), SFX(9)}
                                                         Else
                                                             Spielers(i).CustomSound = {Content.LoadSoundEffect("prep/cpu_" & i & "_0"), Content.LoadSoundEffect("prep/cpu_" & i & "_1"), SFX(9)}
                                                         End If
                                                     End If
                                                 Next

                                                 'Set rejoin flag
                                                 Rejoin = x() = "Rejoin"

                                                 'Load camera info
                                                 If UserIndex > -1 Then
                                                     Select Case Map
                                                         Case GaemMap.Plus
                                                             CamRotation = UserIndex / 2 * Math.PI
                                                         Case GaemMap.Star
                                                             CamRotation = Math.Round(UserIndex / 1.5) / 2 * Math.PI
                                                         Case GaemMap.Octagon
                                                             CamRotation = Math.Floor(UserIndex / 2) / 2 * Math.PI
                                                     End Select
                                                     StdCam = New Keyframe3D(-30, -20, -50, Math.PI * 2 - CamRotation, 0.75, 0, False)
                                                 Else
                                                     StdCam = New Keyframe3D(-30, -20, -50, 0, 0.75, 0, False)
                                                 End If
                                                 FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = If(Rejoin, StdCam, New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False))}
                                             End Sub) Then LocalClient.AutomaticRefresh = True : Return

            'Bereite Flags und Variablen vor
            Status = SpielStatus.WarteAufOnlineSpieler
            WürfelTimer = 0
            LocalClient.LeaveFlag = False
            NetworkMode = True
            SpielerIndex = -1
            LocalClient.IsHost = False
            Chat = New List(Of (String, Color))
            MoveActive = False
            If UserIndex > -1 Then Spielers(UserIndex).CustomSound = {GetLocalAudio(My.Settings.SoundA, 0), GetLocalAudio(My.Settings.SoundB, 1), GetLocalAudio(My.Settings.SoundB, 2)}
            If UserIndex > -1 Then Spielers(UserIndex).Thumbnail = If(My.Settings.Thumbnail, Texture2D.FromFile(Dev, "Cache/client/pp.png"), ReferencePixelTrans)


            'Adapt colors to team mode
            If TeamMode Then
                hudcolors = {Color.Lerp(Color.Red, Color.Yellow, 0F), Color.Lerp(Color.Turquoise, Color.Navy, 0F), Color.Lerp(Color.Red, Color.Yellow, 0.2F), Color.Lerp(Color.Turquoise, Color.Navy, 0.5F), Color.Lerp(Color.Red, Color.Yellow, 0.55F), Color.Lerp(Color.Turquoise, Color.Navy, 1.0F), Color.Lerp(Color.Red, Color.Yellow, 1.0F), New Color(50, 0, 100)}
                playcolor = hudcolors
                Farben = {"Kamerad A1", "Kamerad B1", "Kamerad A2", "Kamerad B2", "Kamerad A3", "Kamerad B3", "Kamerad A4", "Kamerad B4"}
            Else
                hudcolors = {Color.Magenta, Color.Lime, Color.Cyan, Color.Orange, New Color(255, 32, 32), New Color(48, 48, 255), Color.Teal, New Color(85, 120, 20)}
                playcolor = {Color.Magenta, Color.Lime, Color.Cyan, Color.Yellow, Color.Maroon * 1.5F, New Color(0, 0, 200), New Color(0, 80, 80), New Color(85, 120, 20)}
                Farben = {"Telekom", "Lime", "Cyan", "Yellow", "Red", "Olive", "Teal", "Blue"}
            End If

            Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            LoadContent()
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Content.Load(Of SpriteFont)("font/ButtonText"))
            ChatFont = New NezSpriteFont(Content.Load(Of SpriteFont)("font/ChatText"))
            Fanfare = Content.Load(Of Song)("bgm/fanfare")
            DamDamDaaaam = Content.Load(Of Song)("sfx/DamDamDaaam")

            'Lade HUD
            Dim glass = New Color(0, 0, 0, 125)
            HUD = New GuiSystem
            HUDDiceBtn = New GameRenderable(Me) With {.RedrawBackground = True, .BackgroundColor = glass, .Trigger = AddressOf RollDiceTrigger} : HUD.Add(HUDDiceBtn)
            HUDBtnB = New Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDBtnB)
            HUDBtnC = New Button("Anger", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDBtnC)
            HUDBtnD = New Button("Sacrifice", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDBtnD)
            HUDChat = New TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .RedrawBackground = True, .LenLimit = 35} : HUD.Add(HUDChat)
            HUDChatBtn = New Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDChatBtn)
            HUDInstructions = New Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Add(HUDInstructions)
            InstructionFader = New PropertyTransition(New TransitionTypes.TransitionType_EaseInEaseOut(700), HUDInstructions, "Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(InstructionFader)
            HUDNameBtn = New Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText")), .BackgroundColor = Color.Transparent, .Border = New ControlBorder(glass, 0), .Color = Color.Transparent} : HUD.Add(HUDNameBtn)
            HUDmotdLabel = New Label("", New Vector2(400, 750)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond, .Active = False} : HUD.Add(HUDmotdLabel)
            HUDAfkBtn = New Button("AFK", New Vector2(220, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDAfkBtn)
            HUDFullscrBtn = New Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDFullscrBtn)
            HUDMusicBtn = New Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent, .RedrawBackground = True} : HUD.Add(HUDMusicBtn)
            HUDScores = New CustomControl(AddressOf RenderScore, Sub() Return, New Vector2(1600, 700), New Vector2(270, 300)) With {.Font = ChatFont, .BackgroundColor = glass, .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .Active = False, .RedrawBackground = True} : HUD.Add(HUDScores)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = If(UserIndex > -1, hudcolors(UserIndex), Color.White)

            Renderer = AddRenderer(New Renderer3D(Me, 1))
            Psyground = AddRenderer(New PsygroundRenderer(0, 0.25))
            AddRenderer(New DefaultRenderer(2))
            GuiControl.BackgroundImage = Renderer.BlurredContents

            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.62F).SetThreshold(0)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            Center = New Rectangle(500, 70, 950, 950).Center.ToVector2
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(Tweens.LoopType.PingPong, -1).Start()
        End Sub
        Public Overrides Sub Unload()
            Client.OutputDelegate = Sub(x) Return
        End Sub

        Private Sub StartMoverSub(destination As Integer)
            'Set values
            MoveActive = True
            FigurFaderEnd = destination
            Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))
            Status = SpielStatus.FahreFelder
            PlayStompSound = False

            'Initiate
            If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
                Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
                If (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1) Or (Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1 = 0) Then
                    Dim kickID As Integer = CheckKick(1)
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

            Dim FigurFaderVectors = (GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2), GetSpielfeldVector(FigurFaderZiel.Item1, FigurFaderZiel.Item2, 1))

            If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) < FigurFaderEnd Then
                'Play sound
                If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = 0 Then
                    Spielers(FigurFaderZiel.Item1).CustomSound(0).Play()
                Else
                    SFX(3).Play()
                End If
                If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
                    Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
                    Dim trans As Transition(Of Single)
                    If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1 Then
                        Dim kickID As Integer = CheckKick(1)
                        'Make the figure duck
                        trans = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                                 Spielers(FigurFaderZiel.Item1).CustomSound(1).Play() 'Play kick sound
                                                                                                                                 If kickID = key.Item2 Then Spielers(key.Item1).Spielfiguren(key.Item2) = -1 'Reset piece position
                                                                                                                                 If FigurFaderScales.ContainsKey(key) Then FigurFaderScales.Remove(key)
                                                                                                                                 'Make it appear again
                                                                                                                                 Dim transB As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 0, 1, Nothing)
                                                                                                                                 Automator.Add(transB)
                                                                                                                                 FigurFaderScales.Add(key, transB)
                                                                                                                             End Sub)
                    Else
                        'Make the figure duck
                        trans = New Transition(Of Single)(New TransitionTypes.TransitionType_Bounce(FigurSpeed * 2), 1, 0, Nothing)
                    End If
                    If key.Item1 >= 0 And key.Item2 >= 0 Then Automator.Add(trans) : FigurFaderScales.Add(key, trans)
                End If
                FigurFaderXY = New Transition(Of Vector2)(New TransitionTypes.TransitionType_Linear(FigurSpeed), FigurFaderVectors.Item1, FigurFaderVectors.Item2, AddressOf MoverSub) : Automator.Add(FigurFaderXY)
                FigurFaderZ = New Transition(Of Integer)(New TransitionTypes.TransitionType_Parabole(FigurSpeed), 0, DopsHöhe, Nothing) : Automator.Add(FigurFaderZ)

            Else
                If Not PlayStompSound Then SFX(2).Play()

                MoveActive = False
            End If
        End Sub

        Private scheiß As New List(Of (Integer, Integer))

        Public Overrides Sub Update()
            MyBase.Update()

            Dim mstate As MouseState = Mouse.GetState()
            Dim kstate As KeyboardState = Keyboard.GetState()
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

            If Not StopUpdating And UserIndex > -1 And Not Spielers(UserIndex).IsAFK Then

                'Update die Spielelogik
                Select Case Status
                    Case SpielStatus.Würfel
                        If StopDiceWhenFinished Then Exit Select

                        'Manuelles Würfeln für lokalen Spieler
                        'Prüft und speichert, ob der Würfel-Knopf gedrückt wurde
                        If (New Rectangle(1570, 700, 300, 300).Contains(mpos) And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released) Or (kstate.IsKeyDown(Keys.Space) And lastkstate.IsKeyUp(Keys.Space)) Then
                            CanGoAFK = False
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

                    Case SpielStatus.WähleFigur

                        Dim pl As Player = Spielers(SpielerIndex)

                        Dim ichmagzüge As New List(Of Integer)
                        Dim defaultmov As Integer
                        For i As Integer = 0 To FigCount - 1
                            defaultmov = pl.Spielfiguren(i)
                            If defaultmov > -1 And defaultmov + Fahrzahl <= If(Map > 2, SpceCount, PlCount * SpceCount) + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
                        Next

                        If ichmagzüge.Count = 1 Then
                            'Move camera
                            FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)

                            SFX(2).Play()
                            Dim k As Integer = ichmagzüge(0)
                            'Setze flags
                            Status = SpielStatus.Waitn
                            defaultmov = Spielers(SpielerIndex).Spielfiguren(k)
                            SubmitResults(k, defaultmov + Fahrzahl)
                            StopUpdating = False
                        ElseIf ichmagzüge.Count = 0 Then
                            StopUpdating = True
                            HUDInstructions.Text = "No move possible!"
                            Core.Schedule(1, Sub()
                                                 SubmitResults(0, -2)
                                                 StopUpdating = False
                                             End Sub)
                        Else
                            'Manuelle Auswahl für lokale Spieler
                            For k As Integer = 0 To FigCount - 1
                                defaultmov = Spielers(SpielerIndex).Spielfiguren(k)

                                'Prüfe Figur nach Mouse-Klick
                                Dim rotato As Vector2 = RotateAboutOrigin(mpos.ToVector2, Center, CamRotation)
                                If GetFigureRectangle(Map, SpielerIndex, k, Spielers, Center).Contains(rotato.ToPoint) And Spielers(SpielerIndex).Spielfiguren(k) > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                                    If Not ichmagzüge.Contains(k) Then
                                        HUDInstructions.Text = "Incorrect move!"
                                    Else
                                        'Move camera
                                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)

                                        SFX(2).Play()
                                        'Setze flags
                                        Status = SpielStatus.Waitn
                                        SubmitResults(k, defaultmov + Fahrzahl)
                                        StopUpdating = False
                                    End If
                                    Exit For
                                End If
                            Next
                        End If
                    Case SpielStatus.WähleOpfer

                        Dim pl As Player = Spielers(SpielerIndex)

                        Dim ichmagzüge As New List(Of Integer)
                        For i As Integer = 0 To FigCount - 1
                            Dim defaultmov = pl.Spielfiguren(i)
                            If defaultmov > -1 And defaultmov < If(Map > 2, SpceCount, PlCount * SpceCount) Then ichmagzüge.Add(i)
                        Next

                        If ichmagzüge.Count = 1 Then
                            StopUpdating = True
                            SendGod(ichmagzüge(0))
                            'Move camera
                            FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
                        ElseIf ichmagzüge.Count = 0 Then
                            StopUpdating = True
                            HUDInstructions.Text = "No sacrificable piece!"
                            Core.Schedule(1, Sub() SubmitResults(0, -2))
                            'Move camera
                            FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
                        Else
                            'Manuelle Auswahl für lokale Spieler
                            For k As Integer = 0 To FigCount - 1
                                'Prüfe Figur nach Mouse-Klick
                                Dim rotato As Vector2 = RotateAboutOrigin(mpos.ToVector2, Center, CamRotation)
                                If GetFigureRectangle(Map, SpielerIndex, k, Spielers, Center).Contains(rotato.ToPoint) And Spielers(SpielerIndex).Spielfiguren(k) > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
                                    If Not ichmagzüge.Contains(k) Then
                                        HUDInstructions.Text = "Can't select this piece!"
                                    Else
                                        StopUpdating = True
                                        SendGod(k)
                                        'Move camera
                                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
                                    End If
                                    Exit For
                                End If
                            Next
                        End If
                    Case SpielStatus.SpielZuEnde
                        StopUpdating = True
                End Select

                'Set HUD color
                If Not Renderer.BeginTriggered Then HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name & "(" & GetScore(SpielerIndex) & ")", "")
                If Not Renderer.BeginTriggered Then HUDNameBtn.Color = hudcolors(If(SpielerIndex > -1, SpielerIndex, 0))
                HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (SpielerIndex = UserIndex)
            ElseIf Not StopUpdating And UserIndex > -1 Then
                'Set HUD color
                If Not Renderer.BeginTriggered Then HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name & "(" & GetScore(SpielerIndex) & ")", "")
                If Not Renderer.BeginTriggered Then HUDNameBtn.Color = hudcolors(If(SpielerIndex > -1, SpielerIndex, 0))
                HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (SpielerIndex = UserIndex)
            End If


            For Each element In FigurFaderScales
                If element.Value.State = TransitionState.Done Then scheiß.Add(element.Key)
            Next

            For Each element In scheiß
                FigurFaderScales.Remove(element)
            Next
            scheiß.Clear()

            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : MsgBoxer.EnqueueMsgbox("Connection lost!", Sub() Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene)), {"Oh man"})
                If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : MsgBoxer.EnqueueMsgbox("Host left! Game was ended!", Sub() Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene)), {"Oh man"})
            End If

            If NetworkMode Then ReadAndProcessInputData()

            'Misc stuff
            If kstate.IsKeyDown(Keys.Escape) And lastkstate.IsKeyUp(Keys.Escape) Then MenuButton()
            lastmstate = mstate
            lastkstate = kstate
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
                        Dim appendix As String() = element.Substring(1).Split("|"c)
                        For i As Integer = 0 To Spielers.Length - 1
                            If appendix(0).Contains(i) Then Spielers(i).Typ = SpielerTyp.Local
                        Next
                        'Set team names
                        TeamNameA = appendix(1)
                        TeamNameB = appendix(2)

                        'Init game
                        SendSoundFile()
                        StopUpdating = False
                        Status = SpielStatus.Waitn
                        PostChat("The game has started!", Color.White)
                        FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = StdCam}
                        Renderer.TriggerStartAnimation(TeamMode, Sub() Return)
                    Case "c"c 'Sent chat message
                        Dim source As Integer = element(1).ToString
                        If source = 9 Then
                            Dim text As String = element.Substring(2)
                            PostChat("[Guest]: " & text, Color.Gray)
                        Else
                            PostChat("[" & Spielers(source).Name & "]: " & element.Substring(2), playcolor(source))
                        End If
                    Case "d"c
                        Dim source As Integer = element(1).ToString
                        Dim figure As Integer = element(2).ToString
                        Dim aim As Integer = element.Substring(3)
                        Spielers(source).Spielfiguren(figure) = aim
                    Case "e"c 'Suspend gaem
                        Dim who As Integer = element(1).ToString
                        StopUpdating = True
                        Spielers(who).Bereit = False
                        PostChat(Spielers(who).Name & " left!", Color.White)
                        PostChat("The game is being suspended!", Color.White)
                    Case "f"c 'Trigger flying saucer field
                        'Grab data and set flags
                        Dim pl As Integer = element(1).ToString
                        Dim figur As Integer = element(2).ToString
                        Dim distance As Integer = element.Substring(3).Split("&"c)(0)
                        Dim Field As Integer = Spielers(pl).Spielfiguren(figur)
                        Status = SpielStatus.SaucerFlight


                        'MY NAME IS NATHAN FIELDING
                        Dim Nathan_Fielding As Integer = element.Substring(3).Split("&"c)(1)


                        If SaucerFields.Contains(Nathan_Fielding) Then SaucerFields.Remove(Nathan_Fielding)
                        Renderer.TriggerSaucerAnimation(FigurFaderZiel, Sub() Spielers(pl).Spielfiguren(figur) = distance, Nothing)
                    Case "g"c 'Generate flying saucer field
                        Dim pos As Integer = element.Substring(1)
                        SaucerFields.Add(pos)
                    Case "k"c 'Kick player by god
                        Dim pl As Integer = element(1).ToString
                        Dim fig As Integer = element(2).ToString
                        KickedByGod(pl, fig)
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(1)
                        PostChat(msg, Color.White)
                    Case "n"c 'New player active
                        Dim who As Integer = element(1).ToString
                        SpielerIndex = who
                        HUDBtnC.Active = CanAnger(UserIndex) And Not Spielers(UserIndex).IsAFK And SpielerIndex = UserIndex
                        HUDBtnD.Active = SpielerIndex = UserIndex And Not Spielers(UserIndex).IsAFK And Map <> GaemMap.Snakes
                        HUDScores.Active = UserIndex <> SpielerIndex
                        HUDNameBtn.Active = True
                        CanGoAFK = True
                        If UserIndex < 0 Then Continue For
                        If who = UserIndex Then
                            PrepareMove()
                        Else
                            Status = SpielStatus.Waitn
                        End If
                    Case "o"c
                        Dim pl As Integer = element(1).ToString
                        Dim figur As Integer = element(2).ToString
                        Dim aim As Integer = element.Substring(3)
                        Renderer.TriggerSlideAnimation((pl, figur), aim, Sub() Return)
                    Case "p"c
                        Dim who As Integer = element(1).ToString
                        Spielers(who).AngerCount -= 1
                    Case "q"c 'Swap players
                        Dim plA As Integer = element(1).ToString
                        Dim plB As Integer = element(2).ToString

                        Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Bounce(FigurSpeed * 2), 1, 0, Nothing)
                        Automator.Add(trans) : FigurFaderScales.Add((plA, 0), trans)
                        trans = New Transition(Of Single)(New TransitionTypes.TransitionType_Bounce(FigurSpeed * 2), 1, 0, Nothing)
                        Automator.Add(trans) : FigurFaderScales.Add((plB, 0), trans)

                        Core.Schedule(FigurSpeed / 1000, Sub()
                                                             Dim buffer As Integer = Spielers(plA).Spielfiguren(0)
                                                             Spielers(plA).Spielfiguren(0) = Spielers(plB).Spielfiguren(0)
                                                             Spielers(plB).Spielfiguren(0) = buffer
                                                         End Sub)


                    Case "r"c 'Player returned and sync every player
                        Dim source As Integer = element(1).ToString
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " is back!", Color.White)
                        HUDInstructions.Text = "Welcome back!"
                        SendSoundFile()
                    Case "s"c 'Create transition
                        Dim playr As Integer = element(1).ToString
                        Dim figur As Integer = element(2).ToString
                        Dim destination As Integer = element.Substring(3).ToString

                        Status = SpielStatus.FahreFelder
                        FigurFaderZiel = (playr, figur)
                        'Animiere wie die Figur sich nach vorne bewegt, anschließend kehre zurück zum nichts tun
                        StartMoverSub(destination)
                    Case "w"c 'Spieler hat gewonnen
                        HUDDiceBtn.Active = False
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
                        If Map <> GaemMap.Snakes Then Core.Schedule(1, Sub()
                                                                           If TeamMode Then

                                                                               'Get ranks
                                                                               Dim teamA As Integer = 0
                                                                               Dim teamB As Integer = 0
                                                                               For i As Integer = 0 To PlCount / 2 - 1
                                                                                   teamA += GetScore(i * 2)
                                                                                   teamB += GetScore(i * 2 + 1)
                                                                               Next

                                                                               If teamA > teamB Then
                                                                                   Core.Schedule(2, Sub() PostChat("Team A won(" & teamA & ") points)!", Color.Red))
                                                                                   Core.Schedule(3, Sub() PostChat("Team B lost(" & teamB & ") points)", Color.Cyan))
                                                                               ElseIf teamB > teamA Then
                                                                                   Core.Schedule(2, Sub() PostChat("Team B won(" & teamB & ") points)!", Color.Cyan))
                                                                                   Core.Schedule(3, Sub() PostChat("Team A lost(" & teamA & ") points)", Color.Cyan))
                                                                               Else
                                                                                   Core.Schedule(2, Sub() PostChat("Draw(" & teamA & ")!", Color.Gray))
                                                                               End If

                                                                               If GameMode = GameMode.Competetive Then
                                                                                   'Update K/D
                                                                                   If (teamA >= teamB And Mathf.IsEven(UserIndex)) Or (teamB >= teamA And Mathf.IsOdd(UserIndex)) Then My.Settings.GamesWon += 1 Else My.Settings.GamesLost += 1
                                                                                   My.Settings.Save()
                                                                               End If
                                                                           Else
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
                                                                           End If
                                                                       End Sub)
                        'Set flags
                        Status = SpielStatus.SpielZuEnde
                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                        Renderer.AdditionalZPos = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(5000), 0, 1234, Nothing)
                        Automator.Add(Renderer.AdditionalZPos)
                    Case "x"c 'Continue with game
                        StopUpdating = CInt(element.Substring(1))
                    Case "y"c 'Synchronisiere Daten
                        Dim str As String = element.Substring(1)
                        Dim sp As SyncMessage = Newtonsoft.Json.JsonConvert.DeserializeObject(Of SyncMessage)(str)
                        For i As Integer = 0 To PlCount - 1
                            For j As Integer = 0 To FigCount - 1
                                Spielers(i).Spielfiguren(j) = sp.Spielers(i).Spielfiguren(j)
                            Next
                            Spielers(i).Name = sp.Spielers(i).Name
                            Spielers(i).OriginalType = sp.Spielers(i).OriginalType
                            Spielers(i).MOTD = sp.Spielers(i).MOTD
                            Spielers(i).AdditionalPoints = sp.Spielers(i).AdditionalPoints
                            Spielers(i).AngerCount = sp.Spielers(i).AngerCount
                            Spielers(i).SacrificeCounter = sp.Spielers(i).SacrificeCounter
                            Spielers(i).SuicideField = sp.Spielers(i).SuicideField
                            Spielers(i).IsAFK = sp.Spielers(i).IsAFK
                        Next
                        If UserIndex > -1 AndAlso Not CanAnger(UserIndex) Then HUDBtnC.Active = False
                        If UserIndex > -1 Then HUDAfkBtn.Text = If(Spielers(UserIndex).IsAFK, "Back Again", "AFK")
                        SaucerFields = sp.SaucerFields
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
                                                                             'Receive image
                                                                             If IdentSound = IdentType.Custom Then
                                                                                 File.WriteAllBytes("Cache/client/" & Spielers(source).Name & "_pp.png", Decompress(Convert.FromBase64String(dat)))
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
        Private Sub SendGameClosed()
            LocalClient.WriteStream("e")
        End Sub
        Private Sub SendAfkSignal() Handles HUDAfkBtn.Clicked
            If CanGoAFK Then LocalClient.WriteStream("i")
        End Sub
        Private Sub SendGod(figur As Integer)
            LocalClient.WriteStream("j" & figur.ToString)
        End Sub
        Private Sub SendAngered()
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
                                                       If My.Settings.SoundC = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache/client/soundC.audio")))
                                                       LocalClient.WriteStream("z" & My.Settings.SoundC.ToString & "2" & "_TATA_" & txt)

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
        Private Sub CalcMoves()
            Dim homebase As Integer = GetHomebaseIndex(SpielerIndex) 'Eine Spielfigur-ID, die sich in der Homebase befindet(-1, falls Homebase leer ist)
            Dim startfd As Boolean = IsFieldCoveredByOwnFigure(SpielerIndex, 0) 'Ob das Start-Feld blockiert ist
            HUDDiceBtn.Active = False
            Fahrzahl = GetNormalDiceSum() 'Setzt die Anzahl der zu fahrenden Felder im voraus(kann im Fall einer vollen Homebase überschrieben werden)

            If Is6InDiceList() And homebase > -1 And Not startfd Then 'Falls Homebase noch eine Figur enthält und 6 gewürfelt wurde, setze Figur auf Feld 0 und fahre anschließend x Felder nach vorne
                'Bereite das Homebase-verlassen vor
                Fahrzahl = GetSecondDiceAfterSix()
                HUDInstructions.Text = "Move Character out of your homebase and move him " & Fahrzahl & " spaces!"
                FigurFaderZiel = (SpielerIndex, homebase)
                'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                If Not IsFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl) Then
                    StopDiceWhenFinished = True
                    SubmitResults(homebase, Fahrzahl)
                Else
                    StopUpdating = True
                    HUDInstructions.Text = "Field already covered! Move with the other piece!"
                    Core.Schedule(ErrorCooldown, Sub()
                                                     'Move camera
                                                     FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, MathHelper.TwoPi - CamRotation, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                                                     Status = SpielStatus.WähleFigur
                                                     StopUpdating = False
                                                 End Sub)
                End If
            ElseIf Is6InDiceList() And homebase > -1 And startfd Then 'Gibt an, dass das Start-Feld von einer eigenen Figur belegt ist(welche nicht gekickt werden kann) und dass selbst beim Wurf einer 6 keine weitere Figur die Homebase verlassen kann
                HUDInstructions.Text = "Start field blocked! Move pieces out of the way first!"



                If IsFutureFieldCoveredByOwnFigure(SpielerIndex, 0, -1) AndAlso Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl, -1) Then 'Spieler auf dem Start-Feld muss wenn mögl.  bewegt werden
                    homebase = GetFieldID(SpielerIndex, 0).Item2
                    FigurFaderZiel = (SpielerIndex, homebase)
                    StopDiceWhenFinished = True
                    SubmitResults(homebase, Fahrzahl)
                Else 'We can't so s$*!, also schieben wir unsere Probleme einfach auf den nächst besten Deppen, der gleich dran ist

                    'Move camera
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, MathHelper.TwoPi - CamRotation, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)

                    Status = SpielStatus.WähleFigur
                    StopUpdating = True
                    Core.Schedule(ErrorCooldown, Sub() StopUpdating = False)
                End If

                Status = SpielStatus.WähleFigur
            ElseIf (GetHomebaseCount(SpielerIndex) = FigCount And Not Is6InDiceList()) OrElse Not CanDoAMove() Then 'Falls Homebase komplett voll ist(keine Figur auf Spielfeld) und keine 6 gewürfelt wurde(oder generell kein Zug mehr möglich ist), ist kein Zug möglich und der nächste Spieler ist an der Reihe
                StopUpdating = True
                HUDInstructions.Text = "No move possible!"
                Core.Schedule(ErrorCooldown, Sub()
                                                 SubmitResults(0, -2)
                                                 StopUpdating = False
                                             End Sub)
            Else 'Ansonsten fahre x Felder nach vorne mit der Figur, die anschließend ausgewählt wird
                'TODO: Add code for handling normal dice rolls and movement, as well as kicking
                HUDInstructions.Text = "Select piece to be moved " & Fahrzahl & " spaces!"
                Status = SpielStatus.WähleFigur

                'Move camera
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, MathHelper.TwoPi - CamRotation, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
            End If
        End Sub

        Private Function CheckKick(Optional Increment As Integer = 0) As Integer
            'Berechne globale Spielfeldposition der rauswerfenden Figur
            Dim playerA As Integer = FigurFaderZiel.Item1
            Dim fieldA As Integer = Spielers(playerA).Spielfiguren(FigurFaderZiel.Item2) + Increment
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

                        'Implement BV bonus
                        If fieldA = 0 Then
                            Core.Schedule(1, Sub()
                                                 PostChat("BETRETEN VERBOTEN!", Color.White)
                                                 PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White)
                                             End Sub)
                            Spielers(playerA).AdditionalPoints += 50 * If(Mathf.IsEven(playerA) = Mathf.IsEven(playerB), -1, 1)
                        Else
                            Core.Schedule(1, Sub() PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White))
                            Spielers(playerA).AdditionalPoints += 25 * If(Mathf.IsEven(playerA) = Mathf.IsEven(playerB), -1, 1)
                        End If
                        If Mathf.IsEven(playerA) = Mathf.IsEven(playerB) Then Spielers(playerB).CustomSound(2).Play()
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
            Spielers(player).CustomSound(2).Play()
        End Sub

        Private Function GetNormalDiceSum() As Integer
            Dim sum As Integer = 0
            For i As Integer = 0 To WürfelWerte.Length - 1
                sum += WürfelWerte(i)
                If WürfelWerte(i) <> 6 Then Exit For
            Next
            Return sum
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

        Private Function IsÜberholingInSeHaus(defaultmov As Integer) As Boolean
            If defaultmov + Fahrzahl < If(Map > 2, SpceCount, PlCount * SpceCount) Then Return False

            For i As Integer = defaultmov + 1 To defaultmov + Fahrzahl
                If IsFieldCovered(SpielerIndex, -1, i) And i >= If(Map > 2, SpceCount, PlCount * SpceCount) Then Return True
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
                    If fieldB > -1 And ((fieldA < If(Map > 2, SpceCount, PlCount * SpceCount) AndAlso player <> i And fb = fa) OrElse (player = i And figur <> j And fieldA = fieldB)) Then Return True
                Next
            Next

            Return False
        End Function

        Public Function CanAnger(index As Integer) As Boolean

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
        Private Function GetLocalAudio(ident As IdentType, Optional SoundNr As Integer = 0) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content/prep/audio_" & CInt(ident).ToString & ".wav")
            Else
                Select Case SoundNr
                    Case 1
                        Return SoundEffect.FromFile("Cache/client/soundB.audio")
                    Case 2
                        Return SoundEffect.FromFile("Cache/client/soundC.audio")
                    Case Else
                        Return SoundEffect.FromFile("Cache/client/soundA.audio")
                End Select
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

        Private Function GetScore(pl As Integer) As Integer
            Dim ret As Single = Spielers(pl).AngerCount * 5
            For Each element In Spielers(pl).Spielfiguren
                If element >= 0 Then ret += element
            Next
            Return CInt(ret * 10) + Spielers(pl).AdditionalPoints
        End Function
        Private Function GetSpielfeldVector(player As Integer, figur As Integer, Optional increment As Integer = 0) As Vector2
            Return GetMapVectorPos(Map, player, figur, Spielers(player).Spielfiguren(figur) + increment)
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
            HUDChat.ScrollDown = True
        End Sub
        Private Sub RollDiceTrigger()
            WürfelTriggered = True
            WürfelTimer = 0
            WürfelAnimationTimer = -1
        End Sub

        Private Sub PrepareMove()
            'Set HUD flags
            SpielerIndex = UserIndex
            Status = SpielStatus.Würfel
            HUDDiceBtn.Active = True
            StopDiceWhenFinished = False
            HUDInstructions.Text = "Roll the Dice!"
            HUDBtnD.Text = If(Spielers(SpielerIndex).SacrificeCounter <= 0, "Sacrifice", "(" & Spielers(SpielerIndex).SacrificeCounter & ")")
            'Reset camera if not already moving
            If FigurFaderCamera.State <> TransitionState.InProgress Then FigurFaderCamera = New Transition(Of Keyframe3D) With {.Value = StdCam}
            'Set game flags
            If Spielers(SpielerIndex).SacrificeCounter > 0 Then Spielers(SpielerIndex).SacrificeCounter -= 1 'Reduziere Sacrifice counter
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

        Private chatbtnpressed As Boolean = False

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
            SFX(2).Play()
            LaunchInputBox(AddressOf SendChatMessage, ChatFont, "Enter your message: ", "Send message")
        End Sub


        Private Sub VolumeButton() Handles HUDMusicBtn.Clicked
            MediaPlayer.Volume = If(MediaPlayer.Volume > 0F, 0F, 0.1F)
        End Sub
        Private Sub FullscrButton() Handles HUDFullscrBtn.Clicked
            Screen.IsFullscreen = Not Screen.IsFullscreen
            Screen.ApplyChanges()
        End Sub
        Private Sub MenuButton() Handles HUDBtnB.Clicked
            If Not Renderer.BeginTriggered Then MsgBoxer.EnqueueMsgbox("Do you really want to leave?", Sub(x)
                                                                                                           If x = 1 Then Return
                                                                                                           SFX(2).Play()
                                                                                                           LocalClient.blastmode = False
                                                                                                           SendGameClosed()
                                                                                                           NetworkMode = False
                                                                                                           Core.StartSceneTransition(New FadeTransition(Function() New Menu.MainMenu.MainMenuScene))
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
                CanGoAFK = False
                WürfelWerte(0) = If(aim > 6, 6, aim)
                WürfelWerte(1) = If(aim > 6, aim - 6, 0)
                CalcMoves()
                SendAngered()
                SFX(2).Play()
            Catch
                MsgBoxer.EnqueueMsgbox("Alright, then don't.", Nothing, {"Bitch!"})
            End Try
        End Sub

        Private Sub SacrificeButton() Handles HUDBtnD.Clicked
            If Status = SpielStatus.Würfel And Not StopUpdating And Spielers(UserIndex).SacrificeCounter <= 0 And UserIndex > -1 Then
                MsgBoxer.EnqueueMsgbox("You can sacrifice one of your players to the holy BV gods. The further your player is, the higher is the chance to recieve a positive effect.", Nothing, {"OK"})
                MsgBoxer.EnqueueMsgbox("You really want to sacrifice one of your precious players?", Sub(x)
                                                                                                         If x = 0 Then
                                                                                                             CanGoAFK = False
                                                                                                             Status = SpielStatus.WähleOpfer
                                                                                                             DontKickSacrifice = Spielers(UserIndex).SacrificeCounter < 0
                                                                                                             Spielers(UserIndex).SacrificeCounter = SacrificeWait
                                                                                                             HUDBtnD.Text = "(" & SacrificeWait & ")"
                                                                                                             'Move camera
                                                                                                             FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, MathHelper.TwoPi - CamRotation, 0, 0, False), Nothing) : Automator.Add(FigurFaderCamera)
                                                                                                         Else
                                                                                                             MsgBoxer.EnqueueMsgbox("Dann halt nicht.", Nothing, {"OK"})
                                                                                                         End If
                                                                                                     End Sub, {"Yeah", "Nah, mate"})
            Else
                SFX(0).Play()
            End If
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

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function

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

        Public ReadOnly Property BGTexture As Texture2D Implements IGameWindow.BGTexture
            Get
                Return Psyground.RenderTexture
            End Get
        End Property

        Public ReadOnly Property StartCamPoses As Keyframe3D() Implements IGameWindow.StartCamPoses
            Get
                Return {New Keyframe3D(0, 0, 0, MathHelper.TwoPi - CamRotation, 0, 0, False), StdCam}
            End Get
        End Property

        Public ReadOnly Property TeamNames As String() Implements IGameWindow.TeamNames
            Get
                Return {TeamNameA, TeamNameB}
            End Get
        End Property

        Public ReadOnly Property GameTexture As Texture2D Implements IGameWindow.GameTexture
            Get
                Return Renderer.RenderTexture
            End Get
        End Property
#End Region
    End Class
End Namespace