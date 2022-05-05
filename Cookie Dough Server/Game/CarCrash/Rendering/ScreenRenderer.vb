Imports Nez.Textures

Namespace Game.CarCrash.Rendering
    Public Class ScreenRenderer
        Inherits RenderableComponent

        Private txt As RenderTexture

        Public Sub New(txt As RenderTexture)
            Me.txt = txt
        End Sub

        Public Overrides Sub Render(batcher As Batcher, camera As Camera)
            batcher.Draw(txt.RenderTarget, New RectangleF(0, 0, 1920, 1080))
        End Sub

        Public Overrides ReadOnly Property Bounds As RectangleF
            Get
                Return New RectangleF(0, 0, 1920, 1080)
            End Get
        End Property
    End Class
End Namespace