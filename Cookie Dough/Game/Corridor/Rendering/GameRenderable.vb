Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor.Rendering
    Public Class GameRenderable
        Inherits Framework.UI.GuiControl

        Private window As IGameWindow
        Private WürfelAugen As Texture2D
        Private WürfelRahmen As Texture2D

        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return Rectangle.Empty
            End Get
        End Property

        Public Sub New(window As IGameWindow)
            MyBase.New()
            Me.window = window
        End Sub

        Public Overrides Sub Init(system As Framework.UI.IParent)
            WürfelAugen = Core.Content.Load(Of Texture2D)("games/BV/würfel_augen")
            WürfelRahmen = Core.Content.Load(Of Texture2D)("games/BV/würfel_rahmen")
        End Sub

        Public Overrides Sub Update(cstate As Framework.UI.GuiInput, offset As Vector2)

        End Sub

        Public Overrides Sub Render(batcher As Batcher, color As Color)
            Core.GraphicsDevice.DepthStencilState = DepthStencilState.None
            batcher.Draw(window.BGTexture, New Rectangle(0, 0, 1920, 1080), Nothing, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0)

        End Sub

        Public Overrides Sub Activate()
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace