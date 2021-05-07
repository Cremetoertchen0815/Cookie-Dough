Imports System.Diagnostics
Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Console

Namespace Cookie_Dough
    ' <summary>
    ' The main class.
    ' </summary>
    Public Module Program
        'Old Beacon-tweening system for backwards compatibility for BV
        Friend Property VersionString As String = "Cookie Dough V0.09"
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
        Friend Property Farben As String() = {"Magenta", "Lime", "Cyan", "Yellow", "Red", "Olive", "Teal", "Blue"}

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
            End Try
#End If
            StopServer()
            Process.GetCurrentProcess.Kill()
        End Sub
    End Module
End Namespace