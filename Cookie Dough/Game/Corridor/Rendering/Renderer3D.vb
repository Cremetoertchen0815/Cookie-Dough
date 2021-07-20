Imports System.Collections.Generic
Imports System.IO
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor.Rendering
    Public Class Renderer3D
        Inherits Renderer

        Private figur_model As Model
        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private SpielfeldTextur As RenderTarget2D
        Private SpielfeldVerbindungen As Texture2D
        Private Pfeil As Texture2D
        Private MapBuffer As VertexBuffer
        Private TableModel As Model
        Private TableMatrix As Matrix
        Private ResolutionMultiplier As Single = 3

        Private SaucerModel As Model
        Private SaucerLift As Transition(Of Single)
        Private SaucerTarget As (Integer, Integer) = (-1, -1)
        Private SaucerMover As Transition(Of Vector2)
        Private SaucerPickedUp As Boolean = False
        Private SaucerDefaultPosition As New Vector3(0, 0, 1000)

        Private BeginCurrentPlayer As Integer
        Friend BeginTriggered As Boolean
        Private BeginCam As Transition(Of Keyframe3D)

        Private View As Matrix
        Private Projection As Matrix
        Private CamMatrix As Matrix

        Private Game As IGameWindow
        Private FigCount As Integer
        Private SpceCount As Integer
        Private Feld As Rectangle
        Private Center As Vector2
        Private transmatrices As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)}
        Friend AdditionalZPos As New Transition(Of Single)
        Sub New(game As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.Game = game
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            figur_model = scene.Content.Load(Of Model)("mesh\piece_std")
            Pfeil = scene.Content.Load(Of Texture2D)("games\BV\arrow_right")

            'Load table 
            TableModel = scene.Content.Load(Of Model)("mesh/table")
            ApplyDefaultFX(TableModel, Projection)

            Dim vertices As New List(Of VertexPositionColorTexture)
            vertices.Add(New VertexPositionColorTexture(New Vector3(-475, 475, 0), Color.White, Vector2.UnitX))
            vertices.Add(New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero))
            vertices.Add(New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One))
            vertices.Add(New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero))
            vertices.Add(New VertexPositionColorTexture(New Vector3(475, -475, 0), Color.White, Vector2.UnitY))
            vertices.Add(New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One))
            MapBuffer = New VertexBuffer(dev, GetType(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly)
            MapBuffer.SetData(vertices.ToArray)

            Feld = New Rectangle(500, 70, 950, 950)
            Center = Feld.Center.ToVector2

            SpielfeldTextur = New RenderTarget2D(
            dev,
            950 * ResolutionMultiplier,
            950 * ResolutionMultiplier,
            False,
            dev.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents) With {.Name = "TmpA"}

            RenderTexture = New Textures.RenderTexture()

            EffectA = New BasicEffect(dev) With {.Alpha = 1.0F,
            .VertexColorEnabled = True,
            .LightingEnabled = False,
            .TextureEnabled = True
        }

            batchlor = New Batcher(dev)

            SaucerModel = scene.Content.Load(Of Model)("mesh/saucer")
            SaucerMover = New Transition(Of Vector2) With {.Value = New Vector2(SaucerDefaultPosition.X, SaucerDefaultPosition.Y)}
            SaucerLift = New Transition(Of Single) With {.Value = SaucerDefaultPosition.Z}

        End Sub

#Region "Rendering"

        Public Overrides Sub Render(scene As Scene)

            CamMatrix = If(BeginTriggered, BeginCam.Value, Game.GetCamPos).GetMatrix
            View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(dev.Viewport.Width, dev.Viewport.Height, 1, 100000)


            dev.SetRenderTarget(SpielfeldTextur)
            dev.Clear(Color.Black)

            Material.SamplerState = SamplerState.AnisotropicWrap
            Material.BlendState = BlendState.NonPremultiplied
            batchlor.Begin(Material, Matrix.CreateScale(ResolutionMultiplier))

            batchlor.End()

            '---RENDERER 3D---

            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)

            dev.RasterizerState = RasterizerState.CullNone
            dev.DepthStencilState = DepthStencilState.Default

        End Sub

#End Region

#Region "Animation"

#End Region
    End Class
End Namespace