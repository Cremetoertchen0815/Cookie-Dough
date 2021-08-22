Imports Microsoft.Xna.Framework

Namespace Framework.UI
    Public Class MessageBoxer
        Inherits GlobalManager
        Implements IDrawable

        'IDrabable implementation
        Public ReadOnly Property DrawOrder As Integer = 0 Implements IDrawable.DrawOrder
        Public Property Visible As Boolean = True Implements IDrawable.Visible
        Public Event DrawOrderChanged As EventHandler(Of EventArgs) Implements IDrawable.DrawOrderChanged
        Public Event VisibleChanged As EventHandler(Of EventArgs) Implements IDrawable.VisibleChanged

        'Fields
        Private batcher As Batcher

        Sub New()
            batcher = New Batcher(Core.GraphicsDevice)
        End Sub

        Public Sub Draw(gameTime As GameTime) Implements IDrawable.Draw
            batcher.Begin()
            batcher.DrawRect(New Rectangle(50, 50, 100, 200), Color.Cyan)
            batcher.End()
        End Sub

        Public Overrides Sub Update()

        End Sub
    End Class
End Namespace