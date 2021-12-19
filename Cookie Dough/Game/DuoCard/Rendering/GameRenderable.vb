Imports Cookie_Dough.Game.Common
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.DuoCard.Rendering
    Public Class GameRenderable
        Inherits Framework.UI.GuiControl

        Private window As ICardRendererWindow
        Private WürfelRahmen As Texture2D

        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return Rectangle.Empty
            End Get
        End Property

        Public Sub New(window As ICardRendererWindow)
            MyBase.New()
            Me.window = window
        End Sub

        Public Overrides Sub Init(system As Framework.UI.IParent)
            WürfelRahmen = Core.Content.Load(Of Texture2D)("games/BV/würfel_rahmen")
        End Sub

        Public Overrides Sub Update(cstate As Framework.UI.GuiInput, offset As Vector2)

        End Sub

        Public Overrides Sub Render(batcher As Batcher, color As Color)
            Core.GraphicsDevice.DepthStencilState = DepthStencilState.None

            'Draw BG
            batcher.Draw(window.GameTexture, New Rectangle(0, 0, 1920, 1080), Nothing, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0)
        End Sub

        Private Function GetWürfelSourceRectangle(augenzahl As Integer) As Rectangle
            Select Case augenzahl
                Case 1
                    Return New Rectangle(0, 0, 260, 260)
                Case 2
                    Return New Rectangle(260, 0, 260, 260)
                Case 3
                    Return New Rectangle(520, 0, 260, 260)
                Case 4
                    Return New Rectangle(0, 260, 260, 260)
                Case 5
                    Return New Rectangle(260, 260, 260, 260)
                Case 6
                    Return New Rectangle(520, 260, 260, 260)
                Case Else
                    Return New Rectangle(0, 0, 0, 0)
            End Select
        End Function

    End Class
End Namespace