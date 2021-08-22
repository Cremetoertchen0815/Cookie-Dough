﻿Imports System.Diagnostics
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.UI
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

' <summary>
' The main class.
' </summary>
Public Module Program

    Friend Property VersionString As String = "Cookie Dough V0.30"
    Friend Property Automator As TweenManager 'Old Beacon-tweening system for backwards compatibility for BV
    Friend Property ReferencePixelTrans As Texture2D
    Friend Property Dev As GraphicsDevice
    Friend Property DebugTexture As Texture2D
    Friend Property LocalClient As Client
    Friend Property SFX As SoundEffect()
    Friend Property ScaleMatrix As Matrix
    Friend Property MsgBoxer As MessageBoxer
    Friend Property FgColor As Color = Color.Lime
    Friend Property hudcolors As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Orange, New Color(255, 32, 32), New Color(48, 48, 255), Color.Teal, New Color(85, 120, 20)}
    Friend Property playcolor As Color() = {Color.Magenta, Color.Lime, Color.Cyan, Color.Yellow, Color.Maroon * 1.5F, New Color(0, 0, 200), New Color(0, 80, 80), New Color(85, 120, 20)}
    Friend Property Farben As String() = {"Telekom", "Lime", "Cyan", "Yellow", "Red", "Olive", "Teal", "Blue"}
    Friend Property CPU_MOTDs As String() = {"Erleben, was verbindet.", "I'm simply the best!", "Humans suck!", "Ching chang chong, I bims 1 Asiate!", "I'm a star!", "I SUCK!", "Alle Wege führen nach oben.", "I'm blue dabadee dabadei"}

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

    Function MsgBox(Prompt As String, Optional buttons As Microsoft.VisualBasic.MsgBoxStyle = Microsoft.VisualBasic.MsgBoxStyle.OkOnly, Optional title As String = "") As Microsoft.VisualBasic.MsgBoxResult
#If MONO Then
        Return Microsoft.VisualBasic.MsgBoxResult.Yes
#Else
        Return Microsoft.VisualBasic.MsgBox(Prompt, buttons, title)
#End If

    End Function

    Function RealInputBox(Prompt As String, Optional title As String = "", Optional def As String = "") As String
#If MONO Then
        Return ""
#Else
        Return Microsoft.VisualBasic.InputBox(Prompt, title, def)
#End If
    End Function


End Module