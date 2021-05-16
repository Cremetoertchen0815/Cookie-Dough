Imports System.Collections.Generic
Imports Cookie_Dough.Framework.UI
Imports Cookie_Dough.Game.DuoCard.Renderers
Imports Cookie_Dough.Menu.MainMenu
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media
Imports Nez.Tweens

Namespace Game.DuoCard
    ''' <summary>
    ''' Enthällt den eigentlichen Code für das Basis-Spiel
    ''' </summary>
    Public Class GameRoom
        Inherits Scene
        Implements IGameWindow

        'Spiele-Flags und Variables
        Friend Spielers As Player() 'Enthält sämtliche Spieler, die an dieser Runde teilnehmen
        Friend PlCount As Integer 'Gibt an wieviele Spieler das Spiel enthält
        Friend NetworkMode As Boolean = False 'Gibt an, ob das Spiel über das Netzwerk kommunuziert
        Friend SpielerIndex As Integer = -1 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        Friend UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan durch diese Spielinstanz repräsentiert wird
        Friend Status As SpielStatus 'Speichert den aktuellen Status des Spiels
        Private StopUpdating As Boolean 'Deaktiviert die Spielelogik
        Private MoveActive As Boolean
        Private lastmstate As MouseState 'Enthält den Status der Maus aus dem letzten Frame
        Private lastkstate As KeyboardState 'Enthält den Status der Tastatur aus dem letzten Frame
        Private Timer As TimeSpan 'Misst die Zeit seit dem Anfang des Spiels
        Private LastTimer As TimeSpan 'Gibt den Timer des vergangenen Frames an
        Private TimeOver As Boolean = False 'Gibt an, ob die registrierte Zeit abgelaufen ist

        'Assets
        Private Fanfare As Song
        Private ButtonFont As NezSpriteFont
        Private ChatFont As NezSpriteFont

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
        Private WithEvents HUDDiceBtn As GameRenderable
        Private InstructionFader As ITween(Of Color)
        Private HUDColor As Color
        Private InputBoxFlag As Boolean = False
        Private Chat As List(Of (String, Color))

        'Keystack & other debug shit
        Private keysa As New List(Of Keys)
        Private ButtonStack As New List(Of Keys)
        Private oldpress As New List(Of Keys)
        Private Shared dbgKickuser As Integer = -1

        'Spielfeld
        Friend Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        Private Center As Vector2 'Gibt den Mittelpunkt des Screen-Viewports des Spielfelds an
        Friend StdCam As New Keyframe3D(-30, -20, -50, 0, 0.75, 0) 'Gibt die Standard-Position der Kamera an

        'Konstanten
        Private Const WürfelDauer As Integer = 320
        Private Const WürfelAnimationCooldown As Integer = 4
        Private Const FigurSpeed As Integer = 450
        Private Const ErrorCooldown As Integer = 1
        Private Const RollDiceCooldown As Single = 0.5
        Private Const CPUThinkingTime As Single = 0.6
        Private Const DopsHöhe As Integer = 150
        Private Const CamSpeed As Integer = 1300
        Private Const SaucerChance As Integer = 18
        Private Const SacrificeWait As Integer = 5
        Sub New()
            'Bereite Flags und Variablen vor
            Chat = New List(Of (String, Color))
            SpielerIndex = -1
            SwitchPlayer()
            Status = SpielStatus.WarteAufOnlineSpieler
            Framework.Networking.Client.OutputDelegate = Sub(x) PostChat(x, Color.DarkGray)

            If Spielers Is Nothing Then Spielers = {New Player(SpielerTyp.Local), New Player(SpielerTyp.CPU)}

            LoadContent()
        End Sub

        Public Sub LoadContent()

            'Lade Assets
            ButtonFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ButtonText"))
            ChatFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font\ChatText"))
            Fanfare = Content.Load(Of Song)("bgm\fanfare")

            'Lade HUD
            HUD = New GuiSystem
            HUDDiceBtn = New GameRenderable(Me) : HUD.Controls.Add(HUDDiceBtn)
            HUDBtnB = New Controls.Button("Main Menu", New Vector2(1500, 50), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnB)
            HUDBtnC = New Controls.Button("Anger", New Vector2(1500, 200), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnC)
            HUDBtnD = New Controls.Button("Sacrifice", New Vector2(1500, 350), New Vector2(370, 120)) With {.Font = ButtonFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDBtnD)
            HUDChat = New Controls.TextscrollBox(Function() Chat.ToArray, New Vector2(50, 50), New Vector2(400, 800)) With {.Font = ChatFont, .BackgroundColor = New Color(0, 0, 0, 100), .Border = New ControlBorder(Color.Transparent, 3), .Color = Color.Yellow, .LenLimit = 35} : HUD.Controls.Add(HUDChat)
            HUDChatBtn = New Controls.Button("Send Message", New Vector2(50, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDChatBtn)
            HUDInstructions = New Controls.Label("Wait for all Players to arrive...", New Vector2(50, 1005)) With {.Font = New NezSpriteFont(Content.Load(Of SpriteFont)("font/InstructionText")), .Color = Color.BlanchedAlmond} : HUD.Controls.Add(HUDInstructions)
            InstructionFader = HUDInstructions.Tween("Color", Color.Lerp(Color.BlanchedAlmond, Color.Black, 0.5), 0.7).SetLoops(LoopType.PingPong, -1).SetEaseType(EaseType.QuadInOut) : InstructionFader.Start()
            HUDNameBtn = New Controls.Button("", New Vector2(500, 20), New Vector2(950, 30)) With {.Font = ButtonFont, .BackgroundColor = Color.Transparent, .Border = New ControlBorder(Color.Black, 0), .Color = Color.Transparent} : HUD.Controls.Add(HUDNameBtn)
            HUDFullscrBtn = New Controls.Button("Fullscreen", New Vector2(220, 870), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDFullscrBtn)
            HUDMusicBtn = New Controls.Button("Toggle Music", New Vector2(50, 920), New Vector2(150, 30)) With {.Font = ChatFont, .BackgroundColor = Color.Black, .Border = New ControlBorder(Color.Yellow, 3), .Color = Color.Transparent} : HUD.Controls.Add(HUDMusicBtn)
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

            Dim sf As SoundEffect = GetLocalAudio(My.Settings.SoundA)
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                If pl.Typ <> SpielerTyp.Online Then Spielers(i).CustomSound(0) = sf
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

            If Not StopUpdating Then


                'Setze den lokalen Spieler
                If SpielerIndex > -1 AndAlso Spielers(SpielerIndex).Typ = SpielerTyp.Local Then UserIndex = SpielerIndex

                'Update die Spielelogik
                Select Case Status

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
                'HUDNameBtn.Text = If(SpielerIndex > -1, Spielers(SpielerIndex).Name & "(" & GetScore(SpielerIndex) & ")", "")
                HUDNameBtn.Color = hudcolors(If(SpielerIndex > -1, SpielerIndex, 0))
                HUDInstructions.Active = (Status = SpielStatus.WarteAufOnlineSpieler) OrElse (Spielers(SpielerIndex).Typ = SpielerTyp.Local)
            End If

            'Network stuff
            If NetworkMode Then
                If Not LocalClient.Connected And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Connection lost!") : Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
                If LocalClient.LeaveFlag And Status <> SpielStatus.SpielZuEnde Then StopUpdating = True : NetworkMode = False : Microsoft.VisualBasic.MsgBox("Disconnected! Game was ended!") : Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
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
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers))
            SendNetworkMessageToAll("r" & index.ToString & str)
            SendSoundFile()
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
            Dim str As String = Newtonsoft.Json.JsonConvert.SerializeObject(New Networking.SyncMessage(Spielers))
            SendNetworkMessageToAll("y" & str)
        End Sub
        Private Sub SendSoundFile()
            For i As Integer = 0 To Spielers.Length - 1
                Dim pl = Spielers(i)
                If pl.Typ = SpielerTyp.Local Then
                    Dim txt As String = ""
                    If My.Settings.SoundA = IdentType.Custom Then txt = Convert.ToBase64String(Compress.Compress(IO.File.ReadAllBytes("Cache\client\sound.audio")))
                    SendNetworkMessageToAll("z" & i.ToString & CInt(My.Settings.SoundA).ToString & "_TATA_" & txt) 'Suffix "_TATA_" is to not print out in console
                End If
            Next
        End Sub

        Private Sub SendNetworkMessageToAll(message As String)
            If NetworkMode Then LocalClient.WriteStream(message)
        End Sub
#End Region
#Region "Zusatzscheiß"
        Private Sub SwitchPlayer()

        End Sub
        Private Function GetLocalAudio(ident As IdentType) As SoundEffect
            If ident <> IdentType.Custom Then
                Return SoundEffect.FromFile("Content\prep\audio_" & CInt(ident).ToString & ".wav")
            Else
                Return SoundEffect.FromFile("Cache\client\sound.audio")
            End If
        End Function
        Private Sub OpenInputbox(message As String, title As String, finalaction As Action(Of String), Optional defaultvalue As String = "")
            If Not InputBoxFlag Then
                InputBoxFlag = True
                Dim txt As String = Microsoft.VisualBasic.InputBox(message, title, defaultvalue)
                If txt <> "" Then
                    finalaction.Invoke(txt)
                End If
                InputBoxFlag = False
            End If
        End Sub
#End Region
#Region "Knopfgedrücke"
        Private Sub PostChat(txt As String, color As Color)
            Chat.Add((txt, color))
            HUDChat.ScrollDown = True
        End Sub

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
        Private Sub MenuButton() Handles HUDBtnB.Clicked
            If Microsoft.VisualBasic.MsgBox("Do you really want to leave?", Microsoft.VisualBasic.MsgBoxStyle.YesNo) = Microsoft.VisualBasic.MsgBoxResult.Yes Then
                SFX(2).Play()
                SendGameClosed()
                NetworkMode = False
                Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
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

        Public Function GetCamPos() As Keyframe3D Implements IGameWindow.GetCamPos
            'If FigurFaderCamera IsNot Nothing Then Return FigurFaderCamera.Value
            Return New Keyframe3D
        End Function
#End Region
    End Class
End Namespace