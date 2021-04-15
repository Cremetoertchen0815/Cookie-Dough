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
        Friend Property Automator As TweenManager
        Friend Property ReferencePixel As Texture2D
        Friend Property Dev As GraphicsDevice
        Friend Property DefaultFont As SpriteFont
        Friend Property DebugTexture As Texture2D
        Friend Property LocalClient As Client
        Friend Property SFX As SoundEffect()
        Friend Property ScaleMatrix As Matrix
        Friend Property FgColor As Color = Color.Lime

        ' <summary>
        ' The main entry point for the application.
        ' </summary>
        <STAThread>
        Friend Sub Main()
            'Using-Block gibt nach Beendigung des Spiels Resourcen frei und ruft game.Dispose() auf.
            'Try
            Using game As New Game1
                game.Run()
            End Using
            'Catch ex As Exception
            '    NoteError(ex, True)
            'End Try
            StopServer()
            Process.GetCurrentProcess.Kill()
        End Sub
    End Module
End Namespace