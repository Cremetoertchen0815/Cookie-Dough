Imports Microsoft.Xna.Framework

Namespace Framework.UI.Controls
    Public Class CustomControl
        Inherits GuiControl

        Public UpdateSubroutine As ExternalUpdate
        Public DrawSubroutine As ExternalDraw

        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return rect
            End Get
        End Property

        Public Delegate Sub ExternalDraw(batcher As Batcher, InnerBounds As Rectangle)
        Public Delegate Sub ExternalUpdate(mstate As GuiInput, InnerBounds As Rectangle)

        Dim rect As Rectangle
        Dim par As IParent
        Sub New(draw As ExternalDraw, update As ExternalUpdate, location As Vector2, size As Vector2)
            Me.UpdateSubroutine = update
            Me.DrawSubroutine = draw
            Me.Location = location
            Me.Size = size
            Me.Color = Color.White
            Me.Border = New ControlBorder(Color.White, 2)
            Me.BackgroundColor = New Color(40, 40, 40, 255)
        End Sub

        Public Overrides Sub Init(system As IParent)
            If Font Is Nothing Then Font = system.Font
            par = system
        End Sub

        Public Overrides Sub Render(batcher As Batcher)
            DrawSubroutine(batcher, InnerBounds)
        End Sub

        Public Overrides Sub Update(mstate As GuiInput, offset As Vector2)
            rect = New Rectangle(Location.X + offset.X, Location.Y + offset.Y, Size.X, Size.Y)
            UpdateSubroutine(mstate, rect)
        End Sub
    End Class
End Namespace