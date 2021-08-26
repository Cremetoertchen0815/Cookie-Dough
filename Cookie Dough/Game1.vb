﻿Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Microsoft.Xna.Framework.Media

' <summary>
' This is the main type for your game.
' </summary>
Public Class Game1
    Inherits Core

    Public Sub New()
        MyBase.New(1920, 1080, False, "Cookie Dough - Just another games collection", "Content")
        Window.Title = " "
    End Sub
    Protected Overrides Sub Initialize()
        MyBase.Initialize()

        'Prepare program
        IO.Directory.CreateDirectory("Cache/server/")
        IO.Directory.CreateDirectory("Log/")
        IO.Directory.CreateDirectory("Save/")
        PauseOnFocusLost = False
        Screen.SetSize(1280, 720)
        Scene.SetDefaultDesignResolution(1920, 1080, Scene.SceneResolutionPolicy.BestFit)
        Window.AllowUserResizing = True
        ExitOnEscapeKeypress = False

        'Upgrade settings if necessairy
        If My.Settings.MissingNo Then
            My.Settings.Upgrade()
            My.Settings.MissingNo = False
            My.Settings.Save()
            My.Settings.Reload()
        End If

        'Create MessageBoxer
        MsgBoxer = New MessageBoxer
        FinalRenderable = MsgBoxer
        RegisterGlobalManager(MsgBoxer)

        'Load settings
        If My.Settings.Servers Is Nothing Then My.Settings.Servers = New Collections.Specialized.StringCollection From {"weihnachtsaktion.ddns.net"}
        If My.Settings.Username = "" Then My.Settings.Username = Environment.UserName
        If My.Settings.SoundA = IdentType.Custom AndAlso Not IO.File.Exists("Cache/client/soundA.audio") Then My.Settings.SoundA = 0
        If My.Settings.SoundB = IdentType.Custom AndAlso Not IO.File.Exists("Cache/client/soundB.audio") Then My.Settings.SoundB = 0
        My.Settings.Save()

        'Create client(for debug: create and/or connect to server)
        LocalClient = New Client
#If DEBUG Then
        Dim arg As String() = Environment.GetCommandLineArgs()
        If (arg.Length > 1 AndAlso arg(1) = "-launchserver") Then
            StartServer()
            LocalClient.Connect("127.0.0.1", My.Settings.Username)
        Else
            LocalClient.SecondaryClient = True
            LocalClient.Connect("127.0.0.1", My.Settings.Username & "a")
        End If
#End If

        'Load common assets
        ReferencePixelTrans = New Texture2D(GraphicsDevice, 1, 1)
        ReferencePixelTrans.SetData({Color.Transparent})
        SFX = {Content.Load(Of SoundEffect)("sfx/access_denied"),
              Content.Load(Of SoundEffect)("sfx/checkpoint"),
              Content.Load(Of SoundEffect)("sfx/item_collect"),
              Content.Load(Of SoundEffect)("sfx/jump"),
              Content.Load(Of SoundEffect)("sfx/land"),
              Content.Load(Of SoundEffect)("sfx/sucess"),
              Content.Load(Of SoundEffect)("sfx/switch"),
              Content.Load(Of SoundEffect)("sfx/text_skip"),
              Content.Load(Of SoundEffect)("sfx/saucer"),
              Content.Load(Of SoundEffect)("sfx/tuba")}
        Lalala = Content.Load(Of Song)("games/BV/lalalala")
        triumph = Content.Load(Of Song)("sfx/triumph")
        DebugTexture = Content.LoadTexture("dbg1")
        Dev = GraphicsDevice

        'Create Emmond Tween-Manager(for BV backwards compat.)
        Automator = New TweenManager
        RegisterGlobalManager(Automator)

        'Update transformation matrix if event is fired
        Emitter.AddObserver(CoreEvents.GraphicsDeviceReset, Sub() ScaleMatrix = Scene.ScreenTransformMatrix)

        'Load intro screen
#If DEBUG Then
        Scene = New Menu.MainMenu.MainMenuScene
#Else
        If My.Settings.PlayedIntro Then
            Scene = New Menu.MainMenu.SplashScreen
        Else
            My.Settings.PlayedIntro = True
            Scene = New Intros.LFIntro
        End If
#End If

    End Sub

    Private keysa As New List(Of Keys)
    Private ButtonStack As New List(Of Keys)
    Private oldpress As New List(Of Keys)

    Protected Lalala As Song
    Protected triumph As Song
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

        If GetStackKeystroke({Keys.Y, Keys.O, Keys.S, Keys.H, Keys.A}) Then
            Client.NetworkLog = Not Client.NetworkLog
        End If

        If GetStackKeystroke({Keys.R, Keys.E, Keys.C, Keys.T, Keys.A, Keys.N, Keys.G, Keys.L, Keys.E}) Then
            MediaPlayer.Play(triumph)
            MediaPlayer.Volume = 0.4
            MediaPlayer.IsRepeating = True
        End If

        If MediaPlayer.State = MediaState.Stopped And Not MediaPlayer.IsRepeating Then
            MediaPlayer.Play(Content.Load(Of Song)("bgm/acc_" & (MusicCounter + 1).ToString))
            '#If DEBUG Then
            '            MediaPlayer.Volume = 0
            '#Else
            If MediaPlayer.Volume > 0 Then MediaPlayer.Volume = 0.1
            '#End If
            MediaPlayer.IsRepeating = False
            MusicCounter = (MusicCounter + 1) Mod 4
        End If
    End Sub
End Class