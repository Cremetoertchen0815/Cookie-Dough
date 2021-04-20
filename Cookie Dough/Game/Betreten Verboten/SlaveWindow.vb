Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Game.BetretenVerboten.Networking
Imports Cookie_Dough.Game.BetretenVerboten.Renderers
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Nez

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
        Friend NetworkMode As Boolean = False
        Private SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Private Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Private WürfelAktuelleZahl As Integer 'Speichert den WErt des momentanen Würfels
        Private WürfelWerte As Integer() 'Speichert die Werte der Würfel
        Private WürfelTimer As Double 'Wird genutzt um den Würfelvorgang zu halten
        Private WürfelAnimationTimer As Double 'Implementiert einen Cooldown für die Würfelanimation
        Private WürfelTriggered As Boolean 'Gibt an ob gerade gewürfelt wird
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private Fahrzahl As Integer 'Anzahl der Felder die gefahren werden kann
        Private DreifachWürfeln As Boolean 'Gibt an(am Anfang des Spiels), dass ma drei Versuche hat um eine 6 zu bekommen
        Private lastmstate As MouseState
        Private lastkstate As KeyboardState
        Private MoveActive As Boolean = False
        Private SaucerFields As New List(Of Integer)

        'Assets
        Private WürfelAugen As Texture2D
        Private WürfelRahmen As Texture2D
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont
        Private bgm As Song
        Private Lalala As Song
        Private IsLala As Boolean = False


        'Renderer
        Friend Renderer As Renderer3D
        Friend Psyground As PsygroundRenderer

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtnB As Controls.Button
        Private WithEvents HUDBtnC As Controls.Button
        Private WithEvents HUDBtnD As Controls.Button
        Private WithEvents HUDChat As Controls.TextscrollBox
        Private WithEvents HUDChatBtn As Controls.Button
        Private WithEvents HUDInstructions As Controls.Label
        Private WithEvents HUDNameBtn As Controls.Button
        Private WithEvents HUDFullscrBtn As Controls.Button
        Private WithEvents HUDMusicBtn As Controls.Button
        Private InstructionFader As PropertyTransition
        Private ShowDice As Boolean = False
        Private HUDColor As Color
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
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(-30, -20, -50, 0, 0.75, 0)}
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0) 'Gibt die Standard-Position der Kamera an
        Friend PlayStompSound As Boolean

        Private Const WürfelDauer As Integer = 320
        Private Const WürfelAnimationCooldown As Integer = 40
        Private Const FigurSpeed As Integer = 450
        Private Const ErrorCooldown As Integer = 1
        Private Const RollDiceCooldown As Single = 0.5
        Private Const CPUThinkingTime As Integer = 600
        Private Const DopsHöhe As Integer = 150
        Private Const CamSpeed As Integer = 1300
        Private Const SacrificeWait As Integer = 5

        Sub New(ins As OnlineGameInstance)
            LocalClient.AutomaticRefresh = False
            NetworkMode = False

            If Not LocalClient.JoinGame(ins, Sub(x)
                                                 Map = CInt(x())
                                                 ReDim Spielers(GetMapSize(Map) - 1)
                                                 UserIndex = CInt(x())
                                                 For i As Integer = 0 To GetMapSize(Map) - 1
                                                     Dim type As SpielerTyp = CInt(x())
                                                     Dim name As String = x()
                                                     Spielers(i) = New Player(If(type = SpielerTyp.None, type, SpielerTyp.Online)) With {.Name = If(i = UserIndex, My.Settings.Username, name)}
                                                 Next

                                                 Rejoin = x() = "Rejoin"
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
            Spielers(UserIndex).CustomSound = GetLocalAudio(My.Settings.Sound)

            Select Case Map
                Case GaemMap.Default4Players
                    Player.DefaultArray = {-1, -1, -1, -1}
                    FigCount = 4
                    PlCount = 4
                    SpceCount = 10
                Case GaemMap.Default6Players
                    Player.DefaultArray = {-1, -1}
                    FigCount = 2
                    PlCount = 6
                    SpceCount = 8
            End Select

            Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            LoadContent()
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Content.Load(Of SpriteFont)("font\ButtonText"))
            ChatFont = New NezSpriteFont(Content.Load(Of SpriteFont)("font\ChatText"))

            'Lade HUD
            HUD = New GuiSystem
            HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnB)
            HUDBtnC = New Controls.Button("Anger", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnC)
            HUDBtnD = New Controls.Button("Sacrifice", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDBtnD)
            HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Controls.Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            InstructionFader = New PropertyTransition(New TransitionTypes.TransitionType_EaseInEaseOut(700), HUDInstructions, "Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), Nothing) With {.Repeat = RepeatJob.Reverse} : Automator.Add(InstructionFader)
            HUDNameBtn = New Controls.Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Yellow} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Controls.Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Controls.Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDMusicBtn)
            CreateEntity("HUD").AddComponent(HUD)

            Renderer = AddRenderer(New Renderer3D(Me, -1))
            Psyground = AddRenderer(New PsygroundRenderer(0))
            AddRenderer(New DefaultRenderer(1))

            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            CreateEntity("HUDRenderable").AddComponent(New GameRenderable(Me))
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
                                                                                                                                  SFX(4).Play()
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
                    Spielers(FigurFaderZiel.Item1).CustomSound.Play()
                Else
                    SFX(3).Play()
                End If
                If IsFieldCovered(FigurFaderZiel.Item1, FigurFaderZiel.Item2, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1) Then
                    Dim key As (Integer, Integer) = GetFieldID(FigurFaderZiel.Item1, Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) + 1)
                    If Spielers(FigurFaderZiel.Item1).Spielfiguren(FigurFaderZiel.Item2) = FigurFaderEnd - 1 Then
                        Dim kickID As Integer = CheckKick(1)
                        Dim trans As New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(FigurSpeed), 1, 0, Sub()
                                                                                                                                      SFX(4).Play()
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

                MoveActive = False
            End If
        End Sub

        Dim scheiß As New List(Of (Integer, Integer))

        Public Overrides Sub Update()
            MyBase.Update()

            Dim mstate As MouseState = Mouse.GetState()
            Dim kstate As KeyboardState = Keyboard.GetState()
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

            If Not StopUpdating Then

                'Update die Spielelogik
                Select Case Status
                    Case SpielStatus.Würfel

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
                                                                            If it >= WürfelWerte.Length - 1 Or (Not DreifachWürfeln And WürfelAktuelleZahl < 6) Or ((DreifachWürfeln Or GetHomebaseCount(SpielerIndex) > 0) And it > 0 And WürfelAktuelleZahl < 6 AndAlso WürfelWerte(it - 1) >= 6) Or (DreifachWürfeln And it >= 2 And WürfelWerte(2) < 6) Then CalcMoves()
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
                            If defaultmov > -1 And defaultmov + Fahrzahl <= PlCount * SpceCount + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
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
                                If GetFigureRectangle(Map, SpielerIndex, k, Spielers, Center).Contains(mpos) And Spielers(SpielerIndex).Spielfiguren(k) > -1 And mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released Then
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
                        Dim defaultmov As Integer
                        For i As Integer = 0 To FigCount - 1
                            defaultmov = pl.Spielfiguren(i)
                            If defaultmov > -1 And defaultmov + Fahrzahl <= PlCount * SpceCount + FigCount - 1 Then ichmagzüge.Add(i)
                        Next

                        If ichmagzüge.Count = 1 Then
                            StopUpdating = True
                            SendGod(ichmagzüge(0))
                            'Move camera
                            FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, StdCam, Nothing) : Automator.Add(FigurFaderCamera)
                        ElseIf ichmagzüge.Count = 0 Then
                            StopUpdating = True
                            HUDInstructions.Text = "No sacrificable piece!"
                            Core.Schedule(1, Sub()
                                                 SubmitResults(0, -2)
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
                HUDColor = Renderer3D.playcolor(UserIndex)
                HUDBtnB.Color = HUDColor : HUDBtnB.Border = New ControlBorder(HUDColor, HUDBtnB.Border.Width)
                HUDBtnC.Color = HUDColor : HUDBtnC.Border = New ControlBorder(HUDColor, HUDBtnC.Border.Width)
                HUDBtnD.Color = HUDColor : HUDBtnD.Border = New ControlBorder(HUDColor, HUDBtnD.Border.Width)
                HUDChat.Color = HUDColor : HUDChat.Border = New ControlBorder(HUDColor, HUDChat.Border.Width)
                HUDChatBtn.Color = HUDColor : HUDChatBtn.Border = New ControlBorder(HUDColor, HUDChatBtn.Border.Width)
                HUDFullscrBtn.Color = HUDColor : HUDFullscrBtn.Border = New ControlBorder(HUDColor, HUDFullscrBtn.Border.Width)
                HUDMusicBtn.Color = HUDColor : HUDMusicBtn.Border = New ControlBorder(HUDColor, HUDMusicBtn.Border.Width)
                HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name & "(" & GetScore(SpielerIndex) & ")", "")
                HUDNameBtn.Color = If(SpielerIndex > -1, Renderer3D.playcolor(SpielerIndex), Color.White)
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
                If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New GameInstance))
                If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Host left! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New GameInstance))
            End If

            If NetworkMode Then ReadAndProcessInputData()

            If GetStackKeystroke({Keys.L, Keys.A, Keys.L, Keys.A, Keys.L, Keys.A, Keys.L, Keys.A}) Then
                If IsLala Then
                    MediaPlayer.Play(bgm)
                    MediaPlayer.Volume = 0.1
                Else
                    MediaPlayer.Play(Lalala)
                    MediaPlayer.Volume = 0.6
                End If
                IsLala = Not IsLala
            End If

            'Misc stuff
            If kstate.IsKeyDown(Keys.Escape) And lastkstate.IsKeyUp(Keys.Escape) Then MenuButton()
            lastmstate = mstate
            lastkstate = kstate
        End Sub

#Region "Netzwerkfunktionen"
        Private Sub ReadAndProcessInputData()
            Dim data As String() = LocalClient.ReadStream()
            For Each element In data
                Dim command As Char = element(0)
                Select Case command
                    Case "a"c 'Player arrived
                        Dim source As Integer = CInt(element(1).ToString)
                        Spielers(source).Name = element.Substring(2)
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " arrived!", Color.White)
                    Case "b"c 'Begin gaem
                        SendSoundFile()
                        StopUpdating = False
                        Status = SpielStatus.Waitn
                        PostChat("The game has started!", Color.White)
                    Case "c"c 'Sent chat message
                        Dim source As Integer = CInt(element(1).ToString)
                        PostChat("[" & Spielers(source).Name & "]: " & element.Substring(2), Renderer3D.playcolor(source))
                    Case "d"c
                        Dim source As Integer = CInt(element(1).ToString)
                        Dim figure As Integer = CInt(element(2).ToString)
                        Dim aim As Integer = CInt(element.Substring(3))
                        Spielers(source).Spielfiguren(figure) = aim
                    Case "e"c 'Suspend gaem
                        Dim who As Integer = CInt(element(1).ToString)
                        StopUpdating = True
                        Spielers(who).Bereit = False
                        PostChat(Spielers(who).Name & " left!", Color.White)
                        PostChat("The game is being suspended!", Color.White)
                    Case "f"c 'Trigger flying saucer field
                        'Grab data and set flags
                        Dim pl As Integer = CInt(element(1).ToString)
                        Dim figur As Integer = CInt(element(2).ToString)
                        Dim distance As Integer = CInt(element.Substring(3).Split("&"c)(0))
                        Dim Field As Integer = Spielers(pl).Spielfiguren(figur)
                        Status = SpielStatus.SaucerFlight


                        'MY NAME IS NATHAN FIELDING
                        Dim Nathan_Fielding As Integer = CInt(element.Substring(3).Split("&"c)(1))


                        If SaucerFields.Contains(Nathan_Fielding) Then SaucerFields.Remove(Nathan_Fielding)
                        Renderer.TriggerSaucerAnimation(FigurFaderZiel, Sub() Spielers(pl).Spielfiguren(figur) = distance, Nothing)
                    Case "g"c 'Generate flying saucer field
                        Dim pos As Integer = CInt(element.Substring(1))
                        SaucerFields.Add(pos)
                    Case "k"c 'Kick player by god
                        Dim pl As Integer = CInt(element(1).ToString)
                        Dim fig As Integer = CInt(element(2).ToString)
                        KickedByGod(pl, fig)
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(1)
                        PostChat(msg, Color.White)
                    Case "n"c 'Next player
                        Dim who As Integer = CInt(element(1).ToString)
                        SpielerIndex = who
                        HUDBtnC.Active = Not Spielers(SpielerIndex).Angered And SpielerIndex = UserIndex
                        HUDBtnD.Active = SpielerIndex = UserIndex
                        If who = UserIndex Then
                            PrepareMove()
                        Else
                            Status = SpielStatus.Waitn
                        End If
                    Case "r"c 'Player returned and sync every player
                        Dim source As Integer = CInt(element(1).ToString)
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " is back!", Color.White)
                        HUDInstructions.Text = "Welcome back!"
                        Dim str As String = element.Substring(2)
                        Dim sp As SyncMessage = Newtonsoft.Json.JsonConvert.DeserializeObject(Of SyncMessage)(str)
                        For i As Integer = 0 To PlCount - 1
                            For j As Integer = 0 To FigCount - 1
                                Spielers(i).Spielfiguren(j) = sp.Spielers(i).Spielfiguren(j)
                            Next
                            Spielers(i).Schwierigkeit = sp.Spielers(i).Schwierigkeit
                            Spielers(i).Kicks = sp.Spielers(i).Kicks
                            Spielers(i).Angered = sp.Spielers(i).Angered
                        Next
                        If Spielers(UserIndex).Angered Then HUDBtnC.Active = False
                        SaucerFields = sp.SaucerFields
                        SendSoundFile()
                    Case "s"c 'Create transition
                        Dim playr As Integer = CInt(element(1).ToString)
                        Dim figur As Integer = CInt(element(2).ToString)
                        Dim destination As Integer = CInt(element.Substring(3).ToString)

                        Status = SpielStatus.FahreFelder
                        FigurFaderZiel = (playr, figur)
                        'Animiere wie die Figur sich nach vorne bewegt, anschließend kehre zurück zum nichts tun
                        StartMoverSub(destination)
                    Case "w"c 'Spieler hat gewonnen
                        ShowDice = False
                        HUDInstructions.Text = "Game over!"

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
                                                         Core.Schedule(i, Sub() PostChat("1st place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", Renderer3D.playcolor(ranks(ia).Item1)))
                                                     Case 1
                                                         Core.Schedule(i, Sub() PostChat("2nd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", Renderer3D.playcolor(ranks(ia).Item1)))
                                                     Case 2
                                                         Core.Schedule(i, Sub() PostChat("3rd place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", Renderer3D.playcolor(ranks(ia).Item1)))
                                                     Case Else
                                                         Core.Schedule(i, Sub() PostChat((ia + 1) & "th place: " & Spielers(ranks(ia).Item1).Name & "(" & ranks(ia).Item2 & ")", Renderer3D.playcolor(ranks(ia).Item1)))
                                                 End Select
                                             Next
                                         End Sub)
                        Status = SpielStatus.SpielZuEnde
                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0), Nothing) : Automator.Add(FigurFaderCamera)
                        Renderer.AdditionalZPos = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(5000), 0, 1000, Nothing)
                        Automator.Add(Renderer.AdditionalZPos)
                    Case "x"c 'Continue with game
                        StopUpdating = False
                    Case "y"c 'Synchronisiere Daten
                        Dim str As String = element.Substring(1)
                        Dim sp As SyncMessage = Newtonsoft.Json.JsonConvert.DeserializeObject(Of SyncMessage)(str)
                        For i As Integer = 0 To PlCount - 1
                            For j As Integer = 0 To FigCount - 1
                                Spielers(i).Spielfiguren(j) = sp.Spielers(i).Spielfiguren(j)
                            Next
                            Spielers(i).Schwierigkeit = sp.Spielers(i).Schwierigkeit
                            Spielers(i).Kicks = sp.Spielers(i).Kicks
                            Spielers(i).Angered = sp.Spielers(i).Angered
                        Next
                        If Spielers(UserIndex).Angered Then HUDBtnC.Active = False
                        SaucerFields = sp.SaucerFields
                    Case "z"c
                        Dim source As Integer = CInt(element(1).ToString)
                        Dim IdentSound As IdentType = CInt(element(2).ToString)
                        Dim dat As String = element.Substring(3).Replace("_TATA_", "")
                        If source = UserIndex Then Continue For

                        If IdentSound = IdentType.Custom Then
                            File.WriteAllBytes("Cache\server\" & Spielers(source).Name & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                            Spielers(source).CustomSound = SoundEffect.FromFile("Cache\server\" & Spielers(source).Name & ".wav")
                        Else
                            Spielers(source).CustomSound = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                        End If
                End Select
            Next
        End Sub

        'BOOTI PLS PLAE DMC 2
        'DANTE IS GUD IS DE BÄST PLAYE DMC 2 PLSSS
        Friend Sub SendArrived()
            If Rejoin Then
                LocalClient.WriteStream("r")
            Else
                LocalClient.WriteStream("a" & My.Settings.Username)
            End If
        End Sub

        Private Sub SendChatMessage(text As String)
            LocalClient.WriteStream("c" & text)
        End Sub
        Private Sub SendGameClosed()
            LocalClient.WriteStream("e")
        End Sub
        Private Sub SendGod(figur As Integer)
            LocalClient.WriteStream("j" & figur.ToString)
        End Sub
        Private Sub SendAngered()
            LocalClient.WriteStream("p")
        End Sub
        Private Sub SendSoundFile()
            Dim txt As String = ""
            If My.Settings.Sound = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache\client\sound.audio")))
            LocalClient.WriteStream("z" & CInt(My.Settings.Sound).ToString & "_TATA_" & txt)
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
            ShowDice = False
            Fahrzahl = GetNormalDiceSum() 'Setzt die Anzahl der zu fahrenden Felder im voraus(kann im Fall einer vollen Homebase überschrieben werden)

            If Is6InDiceList() And homebase > -1 And Not startfd Then 'Falls Homebase noch eine Figur enthält und 6 gewürfelt wurde, setze Figur auf Feld 0 und fahre anschließend x Felder nach vorne
                'Bereite das Homebase-verlassen vor
                Fahrzahl = GetSecondDiceAfterSix(SpielerIndex)
                HUDInstructions.Text = "Move Character out of your homebase and move him " & Fahrzahl & " spaces!"
                FigurFaderZiel = (SpielerIndex, homebase)
                'Animiere wie die Figur sich nach vorne bewegt, anschließend prüfe ob andere Spieler rausgeschmissen wurden
                If Not IsFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl) Then
                    SubmitResults(homebase, Fahrzahl)
                Else
                    StopUpdating = True
                    HUDInstructions.Text = "Field already covered! Move with the other piece!"
                    Core.Schedule(ErrorCooldown, Sub()
                                                     'Move camera
                                                     FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)
                                                     Status = SpielStatus.WähleFigur
                                                     StopUpdating = False
                                                 End Sub)
                End If
            ElseIf Is6InDiceList() And homebase > -1 And startfd Then 'Gibt an, dass das Start-Feld von einer eigenen Figur belegt ist(welche nicht gekickt werden kann) und dass selbst beim Wurf einer 6 keine weitere Figur die Homebase verlassen kann
                HUDInstructions.Text = "Start field blocked! Move pieces out of the way first!"



                If IsFutureFieldCoveredByOwnFigure(SpielerIndex, 0, -1) AndAlso Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl, -1) Then 'Spieler auf dem Start-Feld muss wenn mögl.  bewegt werden
                    homebase = GetFieldID(SpielerIndex, 0).Item2
                    FigurFaderZiel = (SpielerIndex, homebase)
                    SubmitResults(homebase, Fahrzahl)
                ElseIf IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl, -1) AndAlso Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, Fahrzahl * 2, -1) Then 'Wenn Spieler auf dem Start-Feld nicht kann, fahre stattdessen mit nächtem blockierenden Spieler
                    homebase = GetFieldID(SpielerIndex, WürfelWerte(1)).Item2
                    FigurFaderZiel = (SpielerIndex, homebase)
                    SubmitResults(homebase, Fahrzahl)
                Else 'We can't so s$*!, also schieben wir unsere Probleme einfach auf den nächst besten Deppen, der gleich dran ist

                    'Move camera
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)

                    Status = SpielStatus.WähleFigur
                    StopUpdating = True
                    Core.Schedule(ErrorCooldown, Sub() StopUpdating = False)
                End If

                Status = SpielStatus.WähleFigur
                StopUpdating = True
                Core.Schedule(ErrorCooldown, Sub() StopUpdating = False)
            ElseIf (GetHomebaseCount(SpielerIndex) = 4 And Not Is6InDiceList()) OrElse Not CanDoAMove() Then 'Falls Homebase komplett voll ist(keine Figur auf Spielfeld) und keine 6 gewürfelt wurde(oder generell kein Zug mehr möglich ist), ist kein Zug möglich und der nächste Spieler ist an der Reihe
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
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)
            End If
        End Sub

        Public Function GetStackKeystroke(keysa As Keys()) As Boolean
            If keysa.Length > ButtonStack.Count Then Return False
            For i As Integer = 0 To keysa.Length - 1
                If ButtonStack(ButtonStack.Count - i - 1) <> keysa(keysa.Length - i - 1) Then Return False
            Next
            ButtonStack.Add(Keys.BrowserBack)
            Return True
        End Function

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
                    If fieldB >= 0 And fieldB < PlCount * SpceCount And fb = fa Then
                        Spielers(playerA).Kicks += 1
                        PostChat(Spielers(playerA).Name & " kicked " & Spielers(playerB).Name & "!", Color.White)
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
                If defaultmov > -1 And defaultmov + Fahrzahl <= PlCount * SpceCount + FigCount - 1 And Not IsFutureFieldCoveredByOwnFigure(SpielerIndex, defaultmov + Fahrzahl, i) And Not IsÜberholingInSeHaus(defaultmov) Then ichmagzüge.Add(i)
            Next

            'Prüfe ob Zug möglich
            Return ichmagzüge.Count > 0
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
                    If fieldB > -1 And ((fieldA < PlCount * SpceCount AndAlso (player <> i Or figur <> j) And fb = fa) OrElse (fieldB < PlCount * SpceCount + 5 And player = i And figur <> j And fieldA = fieldB)) Then Return True
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
        Private Function GetLocalAudio(ident As IdentType) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content\prep\audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache\client\sound.audio")
            End If
        End Function

        'Prüft, ob man dreimal würfeln darf
        Private Function CanRollThrice(player As Integer) As Boolean
            Dim fieldlst As New List(Of Integer)
            For i As Integer = 0 To FigCount - 1
                Dim tm As Integer = Spielers(player).Spielfiguren(i)
                If tm >= 0 And tm < PlCount * SpceCount Then Return False 'Falls sich Spieler auf dem Spielfeld befindet, ist dreimal würfeln unmöglich
                If tm > PlCount * SpceCount - 1 Then fieldlst.Add(tm) 'Merke FIguren, die sich im Haus befinden
            Next

            'Wenn nicht alle FIguren bis an den Anschlag gefahren wurden, darf man nicht dreifach würfeln
            For i As Integer = PlCount * SpceCount + FigCount - 1 To (PlCount * SpceCount + 4 - fieldlst.Count) Step -1
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

        Private Function GetScore(pl As Integer) As Integer
            Dim ret As Single = Spielers(pl).Kicks * 2.5F + If(Spielers(pl).Angered, 0, 5)
            For Each element In Spielers(pl).Spielfiguren
                If element >= 0 Then ret += element
            Next
            Return CInt(ret * 10)
        End Function
        Private Function GetSpielfeldVector(player As Integer, figur As Integer, Optional increment As Integer = 0) As Vector2
            Return GetMapVectorPos(Map, player, figur, Spielers(player).Spielfiguren(figur) + increment)
        End Function

        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

        Private Sub PrepareMove()
            'Setze benötigte Flags
            SpielerIndex = UserIndex
            Status = SpielStatus.Würfel
            ShowDice = True
            HUDInstructions.Text = "Roll the Dice!"
            HUDBtnD.Text = If(Spielers(SpielerIndex).SacrificeCounter <= 0, "Sacrifice", "(" & Spielers(SpielerIndex).SacrificeCounter & ")")
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

        Dim chatbtnpressed As Boolean = False

        Private Sub ChatSendButton() Handles HUDChatBtn.Clicked
            If Not chatbtnpressed Then
                chatbtnpressed = True
                SFX(2).Play()
                Dim txt As String = Microsoft.VisualBasic.InputBox("Enter your message: ", "Send message", "")
                If txt <> "" Then
                    SendChatMessage(txt)
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
                Spielers(UserIndex).SacrificeCounter = SacrificeWait
                HUDBtnD.Text = "(" & SacrificeWait & ")"
                LocalClient.blastmode = False
                NetworkMode = False
                Core.StartSceneTransition(New FadeTransition(Function() New GameInstance))
            End If
        End Sub

        Private Sub AngerButton() Handles HUDBtnC.Clicked
            If Status = SpielStatus.Würfel And Not StopUpdating And Spielers(UserIndex).SacrificeCounter <= 0 Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("You get angry, because you suck at this game.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "You suck!")
                If Microsoft.VisualBasic.MsgBox("You are granted a single Joker. Do you want to utilize it now?", Microsoft.VisualBasic.MsgBoxStyle.YesNo, "You suck!") = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                    Dim res As String = Microsoft.VisualBasic.InputBox("How far do you want to move? (12 fields are the maximum and -3 the minimum)", "You suck!")
                    Try
                        Dim aim As Integer = CInt(res)
                        Do Until aim < 13 And aim > -3
                            res = Microsoft.VisualBasic.InputBox("Screw you! I said -3 <= x <= 12 FIELDS!", "You suck!")
                            aim = CInt(res)
                        Loop
                        WürfelWerte(0) = If(aim > 6, 6, aim)
                        WürfelWerte(1) = If(aim > 6, aim - 6, 0)
                        CalcMoves()
                        Spielers(UserIndex).Angered = True
                        SendAngered()
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
            If Status = SpielStatus.Würfel And Not StopUpdating Then
                StopUpdating = True
                Microsoft.VisualBasic.MsgBox("You can sacrifice one of your players to the holy BV gods. The further your player is, the higher is the chance to recieve a positive effect.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "YEET")
                If Microsoft.VisualBasic.MsgBox("You really want to sacrifice one of your precious players?", Microsoft.VisualBasic.MsgBoxStyle.YesNo, "YEET") = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                    Status = SpielStatus.WähleOpfer
                    'Move camera
                    FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)
                Else
                    Microsoft.VisualBasic.MsgBox("Dann halt nicht.", Microsoft.VisualBasic.MsgBoxStyle.OkOnly, "You suck!")
                End If
                StopUpdating = False
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

        Private ReadOnly Property IGameWindow_HUDColor As Color Implements IGameWindow.HUDColor
            Get
                Return HUDColor
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
#End Region
    End Class
End Namespace