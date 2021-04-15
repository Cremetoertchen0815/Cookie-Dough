Imports Microsoft.Xna.Framework

Namespace Game.Megäa.Renderers
    Public Class CrosshairRenderable
        Inherits RenderableComponent
        Public Overrides Sub Render(batcher As Batcher, camera As Camera)
            Dim center As New Vector2(1920 / 2, 1080 / 2)
            batcher.DrawRect(New Rectangle(center.X - 27.0F, center.Y - 3.0F, 54, 6), Color.Black)
            batcher.DrawRect(New Rectangle(center.X - 3.0F, center.Y - 26.0F, 6, 52), Color.Black)

            batcher.DrawRect(New Rectangle(center.X - 25.0F, center.Y - 2.0F, 50, 4), Color.LightGray * 0.9)
            batcher.DrawRect(New Rectangle(center.X - 2.0F, center.Y - 25.0F, 4, 50), Color.LightGray * 0.9)
        End Sub

        Private _bnd As New RectangleF(1920 / 2 - 25, 1080 / 2 - 25, 50, 50)
        Public Overrides ReadOnly Property Bounds As RectangleF
            Get
                Return _bnd
            End Get
        End Property
    End Class
End Namespace