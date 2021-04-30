Imports System.Collections.Generic
Imports System.Linq
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Game.DropTrop.Renderers
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Nez.Tweens

Namespace Game.DropTrop
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class GameRoom
        Inherits Scene
        Implements IGameWindow

        'Spiele-Flags und Variables
        Friend Spielers As Player() 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend PlCount As Integer
        Friend NetworkMode As Boolean = False 'Gibt an, ob das Spiel über das Netzwerk kommunuziert
        Friend SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
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
        Friend Spielfeld As New Dictionary(Of Vector2, Integer)
        Friend SpielfeldSize As Vector2
        Friend FigurFaderCamera As New Transition(Of Keyframe3D) With {.Value = New Keyframe3D(-30, -20, -50, 0, 0.75, 0)} 'Bewegt die Kamera 
        Friend CPUTimer As Single 'Timer-Flag um der CPU etwas "Überlegzeit" zu geben
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0) 'Gibt die Standard-Position der Kamera an
        Private rects As Dictionary(Of Vector2, Rectangle)

        'Konstanten
        Private CPUThinkingTime As Single = 0.6
        Private CPUMoveTime As Integer = 100
        Private CamSpeed As Integer = 1300
        Sub New(map As GaemMap)
            'Bereite Flags und Variablen vor
            Status = SpielStatus.WarteAufOnlineSpieler
            SpielfeldSize = GameInstance.GetMapSize(map)
            LocalClient.LeaveFlag = False
            LocalClient.IsHost = True
            Chat = New List(Of (String, Color))
            Status = SpielStatus.WarteAufOnlineSpieler
            SpielerIndex = -1
            MoveActive = False
            CoreInstance = CType(Core.Instance, Cookie_Dough.Game1)
            Timer = New TimeSpan(0, 0, 22, 22, 22)
            LastTimer = Timer

            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)
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



            Dim sf As SoundEffect = GetLocalAudio(My.Settings.Sound)
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                If pl.Typ <> SpielerTyp.Online Then Spielers(i).CustomSound = sf
            Next

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
            CalcScore
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
                        If GetPlayerInput() AndAlso CalcMove() Then
                            StopUpdating = True
                            Core.Schedule(CPUThinkingTime, AddressOf SwitchPlayer)
                        End If


                        'Check if game is over
                        CheckWin()

                        CalcScore()
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
                                               SwitchPlayer()
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
            If MoveActive Then Return

            Dim data As String() = LocalClient.ReadStream()
            For Each element In data
                Dim source As Integer = CInt(element(0).ToString)
                Dim command As Char = element(1)
                Select Case command
                    Case "a"c 'Player arrived
                        Spielers(source).Name = element.Substring(2)
                        Spielers(source).Bereit = True
                        PostChat(Spielers(source).Name & " arrived!", Color.White)
                        SendPlayerArrived(source, Spielers(source).Name)
                    Case "c"c 'Sent chat message
                        Dim text As String = element.Substring(2)
                        PostChat("[" & Spielers(source).Name & "]: " & text, playcolor(source))
                        SendChatMessage(source, text)
                    Case "e"c 'Suspend gaem
                        If Spielers(source).Typ = SpielerTyp.None Then Continue For
                        Spielers(source).Bereit = False
                        PostChat(Spielers(source).Name & " left!", Color.White)
                        If Not StopUpdating And Status <> SpielStatus.SpielZuEnde And Status <> SpielStatus.WarteAufOnlineSpieler Then PostChat("The game is being suspended!", Color.White)
                        If Status <> SpielStatus.WarteAufOnlineSpieler Then StopUpdating = True
                        SendPlayerLeft(source)
                    Case "n"c 'Switch player
                        SwitchPlayer()
                    Case "r"c 'Player is back
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
                    Case "z"c
                        Dim IdentSound As IdentType = CInt(element(2).ToString)
                        Dim dat As String = element.Substring(3).Replace("_TATA_", "")

                        If IdentSound = IdentType.Custom Then
                            IO.File.WriteAllBytes("Cache\server\" & Spielers(source).Name & ".wav", Compress.Decompress(Convert.FromBase64String(dat)))
                            Spielers(source).CustomSound = SoundEffect.FromFile("Cache\server\" & Spielers(source).Name & ".wav")
                        Else
                            Spielers(source).CustomSound = SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
                        End If
                        SendNetworkMessageToAll("z" & source.ToString & CInt(IdentSound).ToString & "_TATA_" & dat)
                End Select
            Next
        End Sub

        ' ---Methoden um Daten via den Server an die Clients zu senden---
        Private Sub SendPlayerArrived(index As Integer, name As String)
            SendNetworkMessageToAll("a" & index.ToString & name)
        End Sub
        Private Sub SendBeginGaem()
            SendNetworkMessageToAll("b")
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
            'Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers, SaucerFields))
            'SendNetworkMessageToAll("r" & index.ToString & str)
            'SendSoundFile()
        End Sub

        Private Sub SendFigureTransition(who As Integer, figur As Integer, destination As Integer)
            SendNetworkMessageToAll("s" & who.ToString & figur.ToString & destination.ToString)
        End Sub
        Private Sub SendWinFlag()
            SendNetworkMessageToAll("w")
        End Sub
        Private Sub SendGameActive()
            SendNetworkMessageToAll("x")
        End Sub

        Private Sub SendSync()
            'Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers, SaucerFields))
            'SendNetworkMessageToAll("y" & str)
        End Sub
        Private Sub SendSoundFile()
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                If pl.Typ = SpielerTyp.Local Then
                    Dim txt As String = ""
                    If My.Settings.Sound = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache\client\sound.audio")))
                    SendNetworkMessageToAll("z" & i.ToString & CInt(My.Settings.Sound).ToString & "_TATA_" & txt) 'Suffix "_TATA_" is to not print out in console
                End If
            Next
        End Sub

        Private Sub SendNetworkMessageToAll(message As String)
            If NetworkMode Then LocalClient.WriteStream(message)
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

                If counter > 0 And Spielfeld.ContainsKey(CurrentCursor + (counter + 1) * vec) Then
                    For j As Integer = 0 To counter + 1
                        Spielfeld(CurrentCursor + j * vec) = SpielerIndex
                    Next
                    movesuccessful = True
                End If
            Next

            If Not movesuccessful Then NonViableCoord = CurrentCursor
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

            Select Case Spielers(SpielerIndex).Typ
                Case SpielerTyp.Local

                    Dim sizz As New Vector2(Math.Floor(950 / SpielfeldSize.X), Math.Floor(950 / SpielfeldSize.Y))
                    'Return true when left clicked and cursor valid(store cursor coords in CurrentCursor)
                    CurrentCursor = Vector2.One * -1
                    For Each element In rects
                        If element.Value.Contains(mpos - Feld.Location) Then CurrentCursor = element.Key : Exit For
                    Next

                    Return mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released And rects.ContainsKey(CurrentCursor)
                Case SpielerTyp.CPU

                    'Calculate CPU moves
                    If ElapsedMoveTime < 0 Then

                        'Get possible moves
                        Dim selectIndex As Integer
                        If Not IsMovePossible() Then StopUpdating = True : SwitchPlayer() : Return False

                        'Sort moves by amount of fields covered
                        MovesPossible.Sort(Function(x, y) x.Item2.CompareTo(y.Item2))
                        'Get Index by Difficulty
                        Dim cnt As Integer = 0
                        Do While (cnt < 1 OrElse NonViableCoord = MovesPossible(selectIndex).Item1) And cnt < 10
                            Select Case Spielers(SpielerIndex).Schwierigkeit
                                Case Difficulty.Brainless
                                    selectIndex = Nez.Random.Range(0, CInt(Math.Floor(0.7 * MovesPossible.Count)))
                                Case Difficulty.Smart
                                    selectIndex = Nez.Random.Range(CInt(Math.Floor(0.5 * MovesPossible.Count)), MovesPossible.Count)
                            End Select
                            cnt += 1
                        Loop
                        If cnt >= 10 Then StopUpdating = True : SwitchPlayer() : Return False
                        'Set aim position
                        AimCursor = MovesPossible(selectIndex).Item1
                        'Return back
                        ElapsedMoveTime = 0
                        Return False
                    End If

                    'Implement time buffer
                    ElapsedMoveTime += Time.DeltaTime * 1000
                    If ElapsedMoveTime > CPUMoveTime Then
                        If CurrentCursor = AimCursor Then
                            ElapsedMoveTime = -1
                            Return True
                        Else
                            Dim dif As Vector2 = AimCursor - CurrentCursor
                            If dif.X <> 0 Then CurrentCursor.X += Math.Sign(dif.X) Else CurrentCursor.Y += Math.Sign(dif.Y)
                            ElapsedMoveTime = 0
                        End If
                    End If
                    Return False

                Case Else
                    Return False
            End Select
        End Function

        Private Function CheckField(pos As Vector2, ByRef fieldscovered As Integer) As Boolean
            Dim movesuccessful As Boolean = False

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
                SendWinFlag()
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(5000), GetCamPos, New Keyframe3D(-90, -240, 0, Math.PI / 4 * 5, Math.PI / 2, 0), Nothing) : Automator.Add(FigurFaderCamera)
                Renderer.AdditionalZPos = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(5000), 0, 1234, Nothing)
                Automator.Add(Renderer.AdditionalZPos)
            End If
        End Sub

        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

        Private Function GetLocalAudio(ident As IdentType) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content\prep\audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache\client\sound.audio")
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

        Private Sub SwitchPlayer()
            Dim lastind As Integer = SpielerIndex
            Console.WriteLine("--")
            'Get new viable player
            Dim LoopCount As Integer = 0
            SpielerIndex = (SpielerIndex + 1) Mod PlCount
            Do While (Spielers(SpielerIndex).AdditionalPoints = 0 OrElse Not IsMovePossible()) And LoopCount < PlCount
                SpielerIndex = (SpielerIndex + 1) Mod PlCount
                LoopCount += 1
            Loop
            If LoopCount >= PlCount Then CheckWin(True) 'If no player can be switched to, force the game to end

            SendNewPlayerActive(SpielerIndex)
            If Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex
            HUD.Color = hudcolors(UserIndex)
            StopUpdating = False
            SendGameActive()
            HUDInstructions.Text = "Move!"
            If Status <> SpielStatus.SpielZuEnde Then Status = SpielStatus.SpielAktiv
            NonViableCoord = Vector2.One * -1

            If (lastind < 0 OrElse Spielers(lastind).Typ = SpielerTyp.Online) And Spielers(SpielerIndex).Typ <> SpielerTyp.Online Then
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(0, 0, 0, 0, 0, 0), Nothing) : Automator.Add(FigurFaderCamera)
            ElseIf (lastind < 0 OrElse Spielers(lastind).Typ <> SpielerTyp.Online) And Spielers(SpielerIndex).Typ = SpielerTyp.Online Then
                FigurFaderCamera = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(CamSpeed), GetCamPos, New Keyframe3D(-30, -20, -50, 0, 0.75, 0), Nothing) : Automator.Add(FigurFaderCamera)
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
                    SendChatMessage(UserIndex, txt)
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

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function
#End Region
    End Class
End Namespace