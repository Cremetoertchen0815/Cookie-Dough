Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Game.DropTrop.Networking
Imports Cookie_Dough.Game.DropTrop.Renderers
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Newtonsoft.Json
Imports Nez.Tweens

Namespace Game.DropTrop
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class SlaveWindow
        Inherits Scene
        Implements IGameWindow

        'Spiele-Flags und Variables
        Friend Spielers As Player() 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend PlCount As Integer
        Friend Rejoin As Boolean = False
        Friend NetworkMode As Boolean = False 'Gibt an, ob das Spiel über das Netzwerk kommunuziert
        Friend SpielerIndex As Integer = 0 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
        Friend Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private MoveActive As Boolean = False 'Gibt an, ob eine Figuranimation in Gange ist
        Private Timer As TimeSpan 'Misst die Zeit seit dem Anfang des Spiels
        Private LastTimer As TimeSpan 'Gibt den Timer des vergangenen Frames an
        Private TimeOver As Boolean = False 'Gibt an, ob die registrierte Zeit abgelaufen ist
        Private CurrentCursor As Vector2
        Private NonViableCoord As Vector2
        Private CoreInstance As Cookie_Dough.Game1
        Private MovesPossible As New List(Of (Vector2, Integer))
        Private NetworkLocation As Vector2 = Vector2.One * -1

        'Assets
        Private Fanfare As Song
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont

        'Renderer
        Friend Renderer As Renderer3D
        Friend Psyground As PsygroundRenderer

        'HUD
        Private WithEvents HUD As GuiSystem
        Private WithEvents HUDBtn As Controls.Button
        Private WithEvents HUDChat As Controls.TextscrollBox
        Private WithEvents HUDChatBtn As Controls.Button
        Private WithEvents HUDInstructions As Controls.Label
        Private WithEvents HUDNameBtn As Controls.Button
        Private WithEvents HUDFullscrBtn As Controls.Button
        Private WithEvents HUDMusicBtn As Controls.Button
        Private WithEvents HUDScores As Controls.CustomControl
        Private WithEvents HUDDiceBtn As GameRenderable
        Private InstructionFader As ITween(Of Color)
        Private HUDColor As Color
        Private Chat As List(Of (String, Color))

        'Keystack & other debug shit
        Private keysa As New List(Of Keys)
        Private ButtonStack As New List(Of Keys)
        Private oldpress As New List(Of Keys)

        'Spielfeld
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        Private Center As Vector2 'Gibt den Mittelpunkt des Screen-Viewports des Spielfelds an
        Private Feld As Rectangle
        Private rects As Dictionary(Of Vector2, Rectangle)
        Private Map As GaemMap
        Friend Spielfeld As New Dictionary(Of Vector2, Integer)
        Friend SpielfeldSize As Vector2
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(-30, -20, -50, 0, 0.75, 0, True)} 'Bewegt die Kamera 
        Friend CPUTimer As Single 'Timer-Flag um der CPU etwas "Überlegzeit" zu geben
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0, True) 'Gibt die Standard-Position der Kamera an

        'Konstanten
        Private CPUThinkingTime As Single = 0.6
        Private CPUMoveTime As Integer = 100
        Private CamSpeed As Integer = 1300
        Sub New(ins As OnlineGameInstance)
            LocalClient.AutomaticRefresh = False
            NetworkMode = False

            If Not LocalClient.JoinGame(ins, Sub(x)
                                                 Map = CInt(x())
                                                 SpielfeldSize = GameInstance.GetMapSize(Map)
                                                 Select Case Map
                                                     Case GaemMap.Smol
                                                         PlCount = 2
                                                     Case GaemMap.Big
                                                         PlCount = 4
                                                     Case GaemMap.Huuuge
                                                         PlCount = 6
                                                     Case GaemMap.TREMENNNDOUS
                                                         PlCount = 8
                                                 End Select
                                                 ReDim Spielers(PlCount - 1)
                                                 UserIndex = CInt(x())
                                                 For i As Integer = 0 To PlCount - 1
                                                     Dim type As SpielerTyp = CInt(x())
                                                     Dim name As String = x()
                                                     Spielers(i) = New Player(If(type = SpielerTyp.None, type, SpielerTyp.Online)) With {.Name = If(i = UserIndex, My.Settings.Username, name)}
                                                 Next

                                                 Rejoin = x() = "Rejoin"
                                             End Sub) Then LocalClient.AutomaticRefresh = True : Return

            'Bereite Flags und Variablen vor
            Status = SpielStatus.WarteAufOnlineSpieler
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            NetworkMode = True
            StopUpdating = True
            Chat = New List(Of (String, Color))
            Status = SpielStatus.WarteAufOnlineSpieler
            MoveActive = False
            Me.Map = Map
            CoreInstance = CType(Core.Instance, Cookie_Dough.Game1)
            Timer = New TimeSpan(0, 0, 22, 22, 22)
            LastTimer = Timer

            LoadContent()

            Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ChatText"))
            Fanfare = Content.Load(Of Song)("bgm\fanfare")

            'Lade HUD
            HUD = New GuiSystem
            HUDDiceBtn = New GameRenderable(Me) : HUD.Controls.Add(HUDDiceBtn)
            HUDBtn = New Controls.Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtn)
            HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Controls.Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Controls.Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Controls.Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Controls.Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
            HUDScores = New Controls.CustomControl(AddressOf RenderScore, Sub() Return, New Vector2(1500, 700), New Vector2(370, 300)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow} : HUD.Controls.Add(HUDScores)
            CreateEntity("HUD").AddComponent(HUD)
            HUD.Color = hudcolors(0)

            Renderer = AddRenderer(New Renderer3D(Me, -1))
            Psyground = AddRenderer(New PsygroundRenderer(0, 0.3))
            AddRenderer(New DefaultRenderer(1))

            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black
            Material.DefaultMaterial.SamplerState = SamplerState.AnisotropicClamp

            Center = New Rectangle(500, 70, 950, 950).Center.ToVector2
            Feld = New Rectangle(500, 70, 950, 950)
            SelectFader = 0 : Tween("SelectFader", 1.0F, 0.4F).SetLoops(LoopType.PingPong, -1).Start()

            Spielers(UserIndex).CustomSound = {GetLocalAudio(My.Settings.SoundA), GetLocalAudio(My.Settings.SoundB, True)}

            'Generate Spielfeld
            For x As Integer = 0 To SpielfeldSize.X - 1
                For y As Integer = 0 To SpielfeldSize.Y - 1
                    Spielfeld.Add(New Vector2(x, y), -1)
                Next
            Next

            rects = New Dictionary(Of Vector2, Rectangle)
            Dim sizz As New Vector2(950 / SpielfeldSize.X, 950 / SpielfeldSize.Y)
            For x As Integer = 0 To SpielfeldSize.X - 1
                For y As Integer = 0 To SpielfeldSize.Y - 1
                    rects.Add(New Vector2(x, y), New Rectangle(sizz.X * x, sizz.Y * y, sizz.X, sizz.Y))
                Next
            Next

            'Set Player positions
            CurrentCursor = New Vector2(Math.Floor((SpielfeldSize.X - PlCount) / 2), Math.Floor((SpielfeldSize.Y - PlCount) / 2))
            For x As Integer = 0 To PlCount - 1
                For y As Integer = 0 To PlCount - 1
                    Spielfeld(CurrentCursor + New Vector2(x, y)) = (x + y) Mod PlCount
                Next
            Next
            CalcScore()
        End Sub

        Public Overrides Sub Unload()
            Client.OutputDelegate = Sub(x) Return
        End Sub

        ''' <summary>
        ''' Berechnet die Spielelogik.
        ''' </summary>
        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState()
            Dim kstate As KeyboardState = Keyboard.GetState()
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint

            If Not StopUpdating Then

                'Debug speed hack
                If CoreInstance.GetStackKeystroke({Keys.S, Keys.P, Keys.E, Keys.E, Keys.D}) Then
                    CPUMoveTime = 5
                    CPUThinkingTime = 0.01
                End If


                'Setze den lokalen Spieler
                If SpielerIndex > -1 AndAlso Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex

                'Update die Spielelogik
                Select Case Status
                    Case SpielStatus.SpielAktiv

                        'Check if action was being performed and calculate moves
                        If UserIndex = SpielerIndex AndAlso GetPlayerInput() AndAlso CalcMove() Then
                            StopUpdating = True
                            SendMoves()
                            Core.Schedule(CPUThinkingTime, AddressOf SendSwitch)
                        End If


                        'Check if game is over
                        CheckWin()

                        CalcScore()
                    Case SpielStatus.WarteAufOnlineSpieler
                        HUDInstructions.Text = "Waiting for all players to connect..."

                    Case SpielStatus.SpielZuEnde
                        StopUpdating = True
                End Select

                'Set HUD color
                HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name, "")
                HUDNameBtn.Color = hudcolors(If(SpielerIndex > -1, SpielerIndex, 0))
                HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (Spielers(SpielerIndex).Typ = SpielerTyp.Local)
            End If

            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New GameInstance))
                If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Disconnected! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New GameInstance))
            End If

            If NetworkMode Then ReadAndProcessInputData()

            'Misc things
            If kstate.IsKeyDown(Keys.Escape) And lastkstate.IsKeyUp(Keys.Escape) Then MenuButton()
            lastmstate = mstate
            lastkstate = kstate
            MyBase.Update()
        End Sub

        Private Sub RenderScore(batcher As Batcher, InnerBounds As Rectangle, color As Color)
            batcher.DrawRect(InnerBounds, HUDScores.BackgroundColor)
            batcher.DrawHollowRect(InnerBounds, color, HUDScores.Border.Width)
            For i As Integer = 0 To Spielers.Length - 1
                batcher.DrawString(HUDScores.Font, Spielers(i).Name & ": " & Spielers(i).AdditionalPoints, InnerBounds.Location.ToVector2 + New Vector2(30, 30 + i * 30), hudcolors(i))
            Next
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
                        PostChat("[" & Spielers(source).Name & "]: " & element.Substring(2), playcolor(source))
                    Case "e"c 'Suspend gaem
                        Dim who As Integer = CInt(element(1).ToString)
                        StopUpdating = True
                        Spielers(who).Bereit = False
                        PostChat(Spielers(who).Name & " left!", Color.White)
                        PostChat("The game is being suspended!", Color.White)
                    Case "m"c 'Sent chat message
                        Dim msg As String = element.Substring(1)
                        PostChat(msg, Color.White)
                    Case "n"c 'Next player
                        Dim who As Integer = CInt(element(1).ToString)
                        SpielerIndex = who
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
                        Dim sp As SyncMessage = JsonConvert.DeserializeObject(Of SyncMessage)(str)
                        For i As Integer = 0 To PlCount - 1
                            Spielers(i).AdditionalPoints = sp.Spielers(i).AdditionalPoints
                        Next
                        Spielfeld.Clear()
                        For x As Integer = 0 To SpielfeldSize.X - 1
                            For y As Integer = 0 To SpielfeldSize.Y - 1
                                Spielfeld.Add(New Vector2(x, y), -1)
                            Next
                        Next

                        For Each el In sp.Fields
                            Spielfeld(el.Key) = el.Value
                        Next
                        SendSoundFile()
                    Case "s"c 'Create transition
                        Dim dat As String = element.Substring(1)
                        Dim movs As List(Of (Vector2, Integer)) = JsonConvert.DeserializeObject(Of List(Of (Vector2, Integer)))(dat)
                        For Each el In movs
                            Spielfeld(el.Item1) = el.Item2
                        Next
                    Case "w"c 'Spieler hat gewonnen
                        HUDInstructions.Text = "Game over!"
                        MediaPlayer.Play(Fanfare)
                        MediaPlayer.Volume = 0.3

                        'Berechne Rankings
                        Core.Schedule(1, Sub()
                                             Dim ranks As New List(Of (Integer, Integer)) '(Spieler ID, Score)
                                             For i As Integer = 0 To PlCount - 1
                                                 ranks.Add((i, Spielers(i).AdditionalPoints))
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
                                         End Sub)
                        Status = SpielStatus.SpielZuEnde
                        FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0, True), Nothing) : Automator.Add(FigurFaderCamera)
                        Renderer.AdditionalZPos = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(5000), 0, 1234, Nothing)
                        Automator.Add(Renderer.AdditionalZPos)
                    Case "x"c 'Continue with game
                        StopUpdating = False
                    Case "y"c 'Synchronisiere Daten
                        Dim str As String = element.Substring(1)
                        Dim sp As SyncMessage = JsonConvert.DeserializeObject(Of SyncMessage)(str)
                        For i As Integer = 0 To PlCount - 1
                            Spielers(i).AdditionalPoints = sp.Spielers(i).AdditionalPoints
                        Next
                        Spielfeld.Clear()
                        For x As Integer = 0 To SpielfeldSize.X - 1
                            For y As Integer = 0 To SpielfeldSize.Y - 1
                                Spielfeld.Add(New Vector2(x, y), -1)
                            Next
                        Next

                        For Each el In sp.Fields
                            Spielfeld(el.Key) = el.Value
                        Next
                    Case "z"c
                        Dim source As Integer = CInt(element(1).ToString)
                        Dim IdentSound As IdentType = CInt(element(2).ToString)
                        Dim dat As String = element.Substring(3).Replace("_TATA_", "")
                        If source = UserIndex Then Continue For
                        Dim sound As SoundEffect

                        If IdentSound = IdentType.Custom Then
                            File.WriteAllBytes("Cache\server\" & Spielers(source).Name & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                            sound = SoundEffect.FromFile("Cache\server\" & Spielers(source).Name & ".wav")
                        Else
                            sound = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                        End If

                        If Spielers(source).Typ = SpielerTyp.Local Then
                            'Set sound for every local player
                            For Each pl In Spielers
                                If pl.Typ <> SpielerTyp.Online Then pl.CustomSound(0) = sound
                            Next
                        Else
                            'Set sound for player
                            Spielers(source).CustomSound(0) = sound
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
        Private Sub SendMoves()
            LocalClient.WriteStream("p" & JsonConvert.SerializeObject(CurrentCursor))
        End Sub
        Private Sub SendSoundFile()
            Dim txt As String = ""
            If My.Settings.SoundA = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(File.ReadAllBytes("Cache\client\sound.audio")))
            LocalClient.WriteStream("z" & CInt(My.Settings.SoundA).ToString & "_TATA_" & txt)
        End Sub

        Private Sub SendSwitch()
            LocalClient.WriteStream("n")
        End Sub
#End Region

#Region "Hilfsfunktionen"

        Private Function CalcMove() As Boolean
            Dim movesuccessful As Boolean = False

            If Spielfeld(CurrentCursor) > -1 Then Return False

            For dir As Integer = 0 To 7
                'Check lines 
                Dim counter As Integer = 0
                Dim vec As Vector2 = GetJumpVector(dir)
                For i As Integer = 1 To 20
                    If CheckRow(i, CurrentCursor + i * vec, counter) Then Exit For
                Next

                If counter > 0 And Spielfeld.ContainsKey(CurrentCursor + (counter + 1) * vec) Then movesuccessful = True
            Next

            Return movesuccessful
        End Function

        Private Sub CalcScore()
            'Calculate scores
            For i As Integer = 0 To PlCount - 1
                Spielers(i).AdditionalPoints = 0
            Next

            For Each element In Spielfeld
                If element.Value > -1 Then Spielers(element.Value).AdditionalPoints += 10
            Next
        End Sub


        Private Counters As Integer() = {0, 0, 0, 0} '{Up, Down, Left, Right}
        Private Skipper As Integer() = {0, 0}
        Private ElapsedMoveTime As Integer = -1
        Private AimCursor As Vector2

        Private Function GetPlayerInput() As Boolean
            Dim mstate As MouseState = Mouse.GetState
            Dim mpos As Point = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix)).ToPoint


            If SpielerIndex = UserIndex Then

                Dim sizz As New Vector2(Math.Floor(950 / SpielfeldSize.X), Math.Floor(950 / SpielfeldSize.Y))
                'Return true when left clicked and cursor valid(store cursor coords in CurrentCursor)
                CurrentCursor = Vector2.One * -1
                For Each element In rects
                    If element.Value.Contains(mpos - Feld.Location) Then CurrentCursor = element.Key : Exit For
                Next

                Return mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And rects.ContainsKey(CurrentCursor)
            End If
            Return False
        End Function

        Private Function CheckField(pos As Vector2, ByRef fieldscovered As Integer) As Boolean
            Dim movesuccessful As Boolean = False

            If pos = New Vector2(0, 5) Then Console.WriteLine()
            If Spielfeld(pos) <> -1 Then Return False

            For dir As Integer = 0 To 7
                'Check lines 
                Dim counter As Integer = 0
                Dim vec As Vector2 = GetJumpVector(dir)
                For i As Integer = 1 To 20
                    If CheckRow(i, pos + i * vec, counter) Then Exit For
                Next

                If counter > 0 Then fieldscovered += counter : movesuccessful = True
            Next
            If fieldscovered > 10 Then Console.WriteLine()
            Return movesuccessful
        End Function

        Private Function CheckRow(i As Integer, checkpos As Vector2, ByRef counter As Integer) As Boolean 'Return type: If true, stop counting in this direction
            If Spielfeld.ContainsKey(checkpos) Then
                Select Case Spielfeld(checkpos)
                    Case SpielerIndex 'Landing on a field of your own
                        'If we have at least jumped over 1 enemy, the row is complete
                        Console.WriteLine()
                    Case -1    'Landing on an empty field
                        counter = -1 'Theres a gap
                    Case Else 'Landing on an enemy field
                        counter += 1
                        Return False
                End Select
            End If

            Return True 'Stop counting, as we've either successfully jumped over enemies, we've hit a gap or we're OOB
        End Function

        Private Function GetJumpVector(index As Integer) As Vector2
            Select Case index
                Case 0 'Above
                    Return -Vector2.UnitY
                Case 1 'Below
                    Return Vector2.UnitY
                Case 2 'To the left
                    Return -Vector2.UnitX
                Case 3 'To the right
                    Return Vector2.UnitX
                Case 4 'To the top-left
                    Return -Vector2.One
                Case 5 'To the top-right
                    Return -Vector2.UnitY + Vector2.UnitX
                Case 6 'To the bottom-left
                    Return Vector2.UnitY - Vector2.UnitX
                Case 7 'To the bottom-right
                    Return Vector2.One
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Private Sub CheckWin(Optional force As Boolean = False)
            If Status = SpielStatus.SpielZuEnde Then Return

            Dim notover As Boolean
            For Each element In Spielfeld
                If element.Value < 0 Then notover = True : Exit For
            Next
            If Not notover Or force Or TimeOver Then
                Status = SpielStatus.SpielZuEnde
                MediaPlayer.Play(Fanfare)
                MediaPlayer.Volume = 0.3
                StopUpdating = True
                HUDInstructions.Text = "Game over!"
                'Berechne Rankings
                Dim ranks As New List(Of (Integer, Integer)) '(Spieler ID, Score)
                For i As Integer = 0 To PlCount - 1
                    ranks.Add((i, Spielers(i).AdditionalPoints))
                Next
                ranks = ranks.OrderBy(Function(x) x.Item2).ToList()
                ranks.Reverse()

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
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0, True), Nothing) : Automator.Add(FigurFaderCamera)
                Renderer.AdditionalZPos = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(5000), 0, 1234, Nothing)
                Automator.Add(Renderer.AdditionalZPos)
            End If
        End Sub

        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

        Private Function GetLocalAudio(ident As IdentType, Optional IsSoundB As Boolean = False) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content\prep\audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache\client\sound" & If(IsSoundB, "B", "A") & ".audio")
            End If
        End Function

        Private Function GetActualPlayerNbr() As Integer
            Dim cnt As Integer = 0
            For Each element In Spielers
                If element.Typ <> SpielerTyp.None Then cnt += 1
            Next
            Return cnt
        End Function

        Private Function IsMovePossible() As Boolean
            MovesPossible.Clear()

            'Get field boundaries
            Dim xa As Single = SpielfeldSize.X
            Dim xb As Single = 0
            Dim ya As Single = SpielfeldSize.Y
            Dim yb As Single = 0
            For Each element In Spielfeld
                If element.Key.X > xb And element.Value > -1 Then xb = element.Key.X
                If element.Key.Y > yb And element.Value > -1 Then yb = element.Key.Y
                If element.Key.X < xa And element.Value > -1 Then xa = element.Key.X
                If element.Key.Y < ya And element.Value > -1 Then ya = element.Key.Y
            Next
            'Inflate field boundary borders
            Dim bounds As New Rectangle(xa - 1, ya - 1, xb - xa + 2, yb - ya + 2)
            'Add all possible positions + the number of fields they cover to a list
            For x As Integer = bounds.Left To bounds.Right
                For y As Integer = bounds.Top To bounds.Bottom
                    Dim pos As New Vector2(x, y)
                    Dim fields As Integer = 0
                    'Definetely add to random selection, if move length > 0
                    If Spielfeld.ContainsKey(pos) AndAlso Spielfeld(pos) = -1 AndAlso CheckField(pos, fields) Then MovesPossible.Add((pos, fields))
                Next
            Next
            Return MovesPossible.Count > 0
        End Function

        Private Sub PrepareMove()
            Dim lastind As Integer = SpielerIndex
            'Get new viable player
            Dim LoopCount As Integer = 0

            HUD.Color = hudcolors(UserIndex)
            HUDInstructions.Text = "Move!"
            If Status <> SpielStatus.SpielZuEnde Then Status = SpielStatus.SpielAktiv
            NonViableCoord = Vector2.One * -1

            If SpielerIndex = UserIndex Then
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0, True), Nothing) : Automator.Add(FigurFaderCamera)
            Else
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(-30, -20, -50, 0, 0.75, 0, True), Nothing) : Automator.Add(FigurFaderCamera)
            End If
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
                    PostChat("[" & Spielers(UserIndex).Name & "]: " & txt, HUDColor)
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
        Private Sub MenuButton() Handles HUDBtn.Clicked
            If Microsoft.VisualBasic.MsgBox("Do you really want to leave?", Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                SFX(2).Play()
                SendGameClosed()
                NetworkMode = False
                Core.StartSceneTransition(New FadeTransition(Function() New GameInstance))
            End If
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

        Public ReadOnly Property SpielfeldSizeS As Vector2 Implements IGameWindow.SpielfeldSize
            Get
                Return SpielfeldSize
            End Get
        End Property

        Private ReadOnly Property IGameWindow_HUDColor As Color Implements IGameWindow.HUDColor
            Get
                Return HUDColor
            End Get
        End Property

        Public ReadOnly Property BGTexture As Texture2D Implements IGameWindow.BGTexture
            Get
                Return Renderer.RenderTexture
            End Get
        End Property

        Private ReadOnly Property IGameWindow_CurrentCursor As Vector2 Implements IGameWindow.CurrentCursor
            Get
                Return CurrentCursor
            End Get
        End Property

        Private ReadOnly Property IGameWindow_Spielfeld As Dictionary(Of Vector2, Integer) Implements IGameWindow.Spielfeld
            Get
                Return Spielfeld
            End Get
        End Property

        Private ReadOnly Property IGameWindow_Map As GaemMap Implements IGameWindow.Map
            Get
                Return Map
            End Get
        End Property

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function
#End Region
    End Class
End Namespace