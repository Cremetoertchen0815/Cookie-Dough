Imports System.Diagnostics
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

''' <summary>
''' The launch module
''' </summary>
Public Module Launcher

    ''' <summary>
    ''' Determines the version of the server/client(to check for compatibility)
    ''' </summary>
    Friend Property VersionString As String = "Cookie Dough V0.37"
    ''' <summary>
    ''' Old Beacon Tweening system for backwards compatibility with BV
    ''' </summary>
    Friend Property Automator As TweenManager
    ''' <summary>
    ''' Provides a 1x1px texture that is completely transparent
    ''' </summary>
    Friend Property ReferencePixelTrans As Texture2D
    ''' <summary>
    ''' The current graphics device
    ''' </summary>
    Friend Property Dev As GraphicsDevice
    ''' <summary>
    ''' A texture that can be used for prototyping
    ''' </summary>
    Friend Property DebugTexture As Texture2D
    ''' <summary>
    ''' The client currently used for network communication
    ''' </summary>
    Friend Property LocalClient As Client
    ''' <summary>
    ''' An array containing commonly used sound effects
    ''' </summary>
    Friend Property SFX As SoundEffect()
    ''' <summary>
    ''' The matrix used for scaling the screen contents to the used window size
    ''' </summary>
    Friend Property ScaleMatrix As Matrix
    ''' <summary>
    ''' Global system being used for displaying message boxes and input boxes(carbonUI independent for 64-Bit MacOS applications)
    ''' </summary>
    Friend Property MsgBoxer As MessageBoxer
    ''' <summary>
    ''' The menu color
    ''' </summary>
    Friend Property FgColor As Color = New Color(My.Settings.colorR, My.Settings.colorG, My.Settings.colorB)
    ''' <summary>
    ''' The colors used for the HUD by users
    ''' </summary>
    Friend Property hudcolors As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Orange, New Color(255, 32, 32), New Color(48, 48, 255), Color.Teal, New Color(85, 120, 20)}
    ''' <summary>
    ''' The colors used for the player characters by users
    ''' </summary>
    Friend Property playcolor As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Yellow, Color.Maroon * 1.5F, New Color(0, 0, 200), New Color(0, 80, 80), New Color(85, 120, 20)}
    ''' <summary>
    ''' The name of the colors by users
    ''' </summary>
    Friend Property Farben As String() = {"Telekom", "Lime", "Cyan", "Yellow", "Red", "Blue", "Teal", "Olive"}
    ''' <summary>
    ''' The "message of the days" of the colors by users
    ''' </summary>
    Friend Property CPU_MOTDs As String() = {"Erleben, was verbindet.", "Karsten.", "Humans suck!", "Ching chang chong, I bims 1 Asiate!", "I SUCK!", "I'm blue dabadee dabadei", "Alle Wege führen nach oben.", "I'm a star!"}

    ''' <summary>
    ''' The main entry point for the application.
    ''' </summary>
    <STAThread>
    Friend Sub Main()
        'Using-Block gibt nach Beendigung des Spiels Resourcen frei und ruft game.Dispose() auf.
#If DEBUG Then
        Using game As New GameCore
            game.Run()
        End Using
#Else
        Try
            Using game As New GameCore
                game.Run()
            End Using
        Catch ex As Exception
            NoteError(ex, True)
#If Not MONO Then
            System.Windows.Forms.MessageBox.Show("There was an cwwitical ewwor while pwwaying the game! P... Pweease tell the deweloper!" & Environment.NewLine & Environment.NewLine & "Message: " & ex.Message & Environment.NewLine & "Stack Trace: " & ex.StackTrace, "OwO, what's this? *notices error*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
            System.Windows.Forms.MessageBox.Show("No, seriously! It would be a huge help if you sent a message to the Luminous Friends. They'll give you further assistance in fixing the issue ^w^.", "*notices another message box*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
            System.Windows.Forms.MessageBox.Show("If that sounds like too much work to you, it would already be really helpful if you sent us a photo of the error message box via our social media. ", "*notices another message box*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
            System.Windows.Forms.MessageBox.Show("What? Oh yeah, you already closed it... " & Environment.NewLine & Environment.NewLine & "You know what? I'll reopen it. Just for you ^w^", "*notices another message box*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
            System.Windows.Forms.MessageBox.Show("There was an cwwitical ewwor while pwwaying the game! P... Pweease tell the deweloper!" & Environment.NewLine & Environment.NewLine & "Message: " & ex.Message & Environment.NewLine & "Stack Trace: " & ex.StackTrace, "OwO, what's this? *notices error*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
            System.Windows.Forms.MessageBox.Show("Here we go! Have a good day!" & Environment.NewLine & Environment.NewLine & "Yours truly, " & Environment.NewLine & "Creme, your next door programmer", "I seriously don't know what to put here...", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
#End If
        End Try
#End If
        Try
            If Server.streamw IsNot Nothing Then streamw.Close()
            StopServer()
            Process.GetCurrentProcess.Kill()
        Catch ex As Exception

        End Try
    End Sub


End Module