Imports Microsoft.Xna.Framework

Namespace Game.Barrelled.Renderers
    Public Class TargetRendererable
        Inherits RenderableComponent

        Dim r As Renderer

        Sub New(r As Renderer)
            Me.r = r
        End Sub

        Public Overrides Sub Render(batcher As Batcher, camera As Camera)
            batcher.End()
            batcher.Begin(Entity.Transform.LocalToWorldTransform)
            batcher.Draw(r.RenderTexture.RenderTarget, LocalOffset)
        End Sub

        Public Overrides ReadOnly Property Bounds As RectangleF
            Get
                Return New RectangleF(LocalOffset, r.RenderTexture.RenderTarget.Bounds.Size.ToVector2)
            End Get
        End Property
    End Class
End Namespace