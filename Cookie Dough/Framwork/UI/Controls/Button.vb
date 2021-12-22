Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Framework.UI.Controls
    Public Class Button
        Inherits GuiControl

        Private _txt As String = ""
        Public Property Text As String
            Get
                Return _txt
            End Get
            Set(value As String)
                _txt = value
            End Set
        End Property
        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return rect
            End Get
        End Property

        Public Event Clicked(ByVal sender As Object, ByVal e As EventArgs)
        Private rect As Rectangle
        Private par As IParent


        Public Sub New(text As String, location As Vector2, size As Vector2)
            Me.Text = text
            Me.Location = location
            Me.Size = size
            Color = Color.White
            Border = New ControlBorder(Color.White, 2)
            BackgroundColor = New Color(40, 40, 40, 255)
            GamepadInteractable = True
        End Sub

        Public Overrides Sub Init(system As IParent)
            If Font Is Nothing Then Font = system.Font
            par = system
        End Sub

        Public Overrides Sub Render(batcher As Batcher, color As Color)
            If RedrawBackground AndAlso BackgroundImage IsNot Nothing Then batcher.Draw(BackgroundImage, rect, New Rectangle(rect.X + rect.Width / 10, rect.Y + rect.Height / 10, rect.Width - rect.Width / 5, rect.Height - rect.Height / 5), Color.White)
            batcher.DrawRect(rect, BackgroundColor)
            batcher.DrawHollowRect(rect, color, Border.Width)
            batcher.DrawString(Font, Text, rect.Center.ToVector2 - Font.MeasureString(Text) / 2, If(Me.Color = Color.Transparent, color, Me.Color))
        End Sub

        Public Overrides Sub Update(mstate As GuiInput, offset As Vector2)
            rect = New Rectangle(Location.X + offset.X, Location.Y + offset.Y, Size.X, Size.Y)
            If mstate.LeftClickOneshot And rect.Contains(mstate.MousePosition) Then Activate()
        End Sub

        Public Overrides Sub Activate()
            RaiseEvent Clicked(Me, New EventArgs())
        End Sub
    End Class
End Namespace