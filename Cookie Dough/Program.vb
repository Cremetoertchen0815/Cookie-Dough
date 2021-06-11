Imports System.Diagnostics
Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

Namespace Cookie_Dough
    ' <summary>
    ' The main class.
    ' </summary>
    Public Module Program
        'Old Beacon-tweening system for backwards compatibility for BV
        Friend Property VersionString As String = "Cookie Dough V0.27"
        Friend Property Automator As TweenManager
        Friend Property ReferencePixel As Texture2D
        Friend Property Dev As GraphicsDevice
        Friend Property DefaultFont As SpriteFont
        Friend Property DebugTexture As Texture2D
        Friend Property LocalClient As Client
        Friend Property SFX As SoundEffect()
        Friend Property ScaleMatrix As Matrix
        Friend Property FgColor As Color = Color.Lime
        Friend Property hudcolors As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Orange, New Color(255, 32, 32), Color.Olive, Color.Teal, New Color(48, 48, 255)}
        Friend Property playcolor As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Yellow, Color.Maroon * 1.5F, New Color(85, 120, 20), New Color(0, 80, 80), Color.Blue}
        Friend Property Farben As String() = {"Telekom", "Lime", "Cyan", "Yellow", "Red", "Olive", "Teal", "Blue"}
        Friend Property CPU_MOTDs As String() = {"Erleben, was verbindet.", "I'm simply the best!", "Humans suck!", "Ching chang chong, I bims 1 Asiate!", "I'm a star!", "I SACK!", "Alle Wege führen nach oben.", "I'm blue dabadee dabadei"}

        ' <summary>
        ' The main entry point for the application.
        ' </summary>
        <STAThread>
        Friend Sub Main()
            'Using-Block gibt nach Beendigung des Spiels Resourcen frei und ruft game.Dispose() auf.
#If DEBUG Then
            Using game As New Game1
                game.Run()
            End Using
#Else
            Try
                Using game As New Game1
                    game.Run()
                End Using
            Catch ex As Exception
                NoteError(ex, True)
                System.Windows.Forms.MessageBox.Show("There was an cwwitical ewwor while pwwaying the game! P... Pweease tell the deweloper!" & Environment.NewLine & Environment.NewLine & "Message: " & ex.Message & Environment.NewLine & "Stack Trace: " & ex.StackTrace, "OwO, what's this? *notices error*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
                System.Windows.Forms.MessageBox.Show("No, seriously! It would be a huge help if you sent a message to the Luminous Friends. They'll give you further assistance in fixing the issue ^w^.", "*notices another message box*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
                System.Windows.Forms.MessageBox.Show("If that sounds like too much work to you, it would already be really helpful if you sent us a photo of the error message box via our social media. ", "*notices another message box*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
                System.Windows.Forms.MessageBox.Show("What? Oh yeah, you already closed it... " & Environment.NewLine & Environment.NewLine & "You know what? I'll reopen it. Just for you ^w^", "*notices another message box*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
                System.Windows.Forms.MessageBox.Show("There was an cwwitical ewwor while pwwaying the game! P... Pweease tell the deweloper!" & Environment.NewLine & Environment.NewLine & "Message: " & ex.Message & Environment.NewLine & "Stack Trace: " & ex.StackTrace, "OwO, what's this? *notices error*", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
                System.Windows.Forms.MessageBox.Show("Here we go! Have a good day!" & Environment.NewLine & Environment.NewLine & "Yours truly, " & Environment.NewLine & "Creme, your next door programmer", "I seriously don't know what to put here...", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1, System.Windows.Forms.MessageBoxOptions.DefaultDesktopOnly)
            End Try
#End If
            If Server.streamw IsNot Nothing Then streamw.Close()
            StopServer()
            Process.GetCurrentProcess.Kill()
        End Sub
    End Module
End Namespace