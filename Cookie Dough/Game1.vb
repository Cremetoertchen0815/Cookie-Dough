Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media

Namespace Cookie_Dough
    ' <summary>
    ' This is the main type for your game.
    ' </summary>
    Public Class Game1
        Inherits Core
        Sub New()
            MyBase.New(1920, 1080, False, "Cookie Dough - Just another games collection", "Content")
            Window.Title = " "
        End Sub
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            'Prepare program
            PauseOnFocusLost = False
            Screen.SetSize(1280, 720)
            Scene.SetDefaultDesignResolution(1920, 1080, Scene.SceneResolutionPolicy.BestFit)
            Window.AllowUserResizing = True
            Core.ExitOnEscapeKeypress = False

            Dim arg As String() = Environment.GetCommandLineArgs()
            LocalClient = New Client
            If (arg.Length > 1 AndAlso arg(1) = "-launchserver") Then
                StartServer()
                LocalClient.Connect("127.0.0.1", My.Settings.Username)
            Else
                LocalClient.Connect("127.0.0.1", My.Settings.Username & "a")
            End If

            ReferencePixel = New Texture2D(GraphicsDevice, 1, 1)
            ReferencePixel.SetData({Color.White})
            SFX = {Content.Load(Of SoundEffect)("sfx/access_denied"),
              Content.Load(Of SoundEffect)("sfx/checkpoint"),
              Content.Load(Of SoundEffect)("sfx/item_collect"),
              Content.Load(Of SoundEffect)("sfx/jump"),
              Content.Load(Of SoundEffect)("sfx/land"),
              Content.Load(Of SoundEffect)("sfx/sucess"),
              Content.Load(Of SoundEffect)("sfx/switch"),
              Content.Load(Of SoundEffect)("sfx/text_skip")}
            Lalala = Content.Load(Of Song)("games\BV\lalalala")
            DebugTexture = Content.LoadTexture("dbg1")

            'Create Emmond Tween-Manager(for BV backwards compat.)
            Automator = New TweenManager
            RegisterGlobalManager(Automator)

            'Update transformation matrix if event is fired
            Emitter.AddObserver(CoreEvents.GraphicsDeviceReset, Sub() ScaleMatrix = Scene.ScreenTransformMatrix)

            'Load settings
            If My.Settings.Servers Is Nothing Then My.Settings.Servers = New Collections.Specialized.StringCollection
            If My.Settings.Username = "" Then My.Settings.Username = Environment.UserName
            My.Settings.Save()

            'Load intro screen
            Scene = New Menu.MainMenu.SplashScreen
        End Sub

        Private keysa As New List(Of Keys)
        Private ButtonStack As New List(Of Keys)
        Private oldpress As New List(Of Keys)

        Protected Lalala As Song
        Private MusicCounter As Integer = 0

        Protected Overrides Sub Update(gameTime As GameTime)


            'Record key-strokes(ignore keystrokes if window is not focused, in order to protect privacy[in case game window is minimized and user types in sth confidential]
            keysa.Clear()
            If IsActive Then
                For Each A In Keyboard.GetState.GetPressedKeys
                    keysa.Add(A)
                Next
            End If


            For Each element In keysa
                If Not oldpress.Contains(element) Then ButtonStack.Add(element)
            Next

            oldpress.Clear()
            For Each A In keysa
                oldpress.Add(A)
            Next

            If ButtonStack.Count > 20 Then ButtonStack.RemoveAt(0)

            'Update music
            UpdateMusic()

            MyBase.Update(gameTime)
        End Sub

        Public Function GetStackKeystroke(keysa As Keys()) As Boolean
            If keysa.Length > ButtonStack.Count Then Return False
            For i As Integer = 0 To keysa.Length - 1
                If ButtonStack(ButtonStack.Count - i - 1) <> keysa(keysa.Length - i - 1) Then Return False
            Next
            ButtonStack.Add(Keys.BrowserBack)
            Return True
        End Function

        Private Sub UpdateMusic()
            If GetStackKeystroke({Keys.L, Keys.A, Keys.L, Keys.A, Keys.L, Keys.A, Keys.L, Keys.A}) Then
                MediaPlayer.Play(Lalala)
                If MediaPlayer.Volume > 0 Then MediaPlayer.Volume = 0.6
            End If

            If MediaPlayer.State = MediaState.Stopped Then
                MediaPlayer.Play(Content.Load(Of Song)("bgm/acc_" & (MusicCounter + 1).ToString))
                If MediaPlayer.Volume > 0 Then MediaPlayer.Volume = 0.1
                MediaPlayer.IsRepeating = False
                MusicCounter = (MusicCounter + 1) Mod 4
            End If
        End Sub
    End Class
End Namespace