Imports Microsoft.Xna.Framework.Graphics

Namespace Game.DuoCard.Renderers
    Public Class Renderer3D
        Inherits Renderer

        Private Game As IGameWindow
        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private SpielfeldTextur As RenderTarget2D

        Sub New(game As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.Game = game
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            batchlor = New Batcher(dev)

            RenderTexture = New Textures.RenderTexture()
        End Sub

        Public Overrides Sub Render(scene As Scene)

        End Sub
    End Class
End Namespace