Imports System.Collections.Generic
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
        Private MapBuffer As VertexBuffer
        Private TableModel As Model
        Private TableMatrix As Matrix
        Private ResolutionMultiplier As Single = 3
        Private rects As Dictionary(Of Vector2, Rectangle)
        Private Spielfeldsize As Vector2

        Private View As Matrix
        Private Projection As Matrix
        Private CamMatrix As Matrix

        Private Game As IGameWindow
        Private Feld As Rectangle
        Private Center As Vector2

        Public Sub New(game As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.Game = game
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            Dim vertices As New List(Of VertexPositionColorTexture) From {
                New VertexPositionColorTexture(New Vector3(-475, 475, 0), Color.White, Vector2.UnitX),
                New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero),
                New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One),
                New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero),
                New VertexPositionColorTexture(New Vector3(475, -475, 0), Color.White, Vector2.UnitY),
                New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One)
            }
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
            debug_batcher = New Batcher(dev)


            rects = New Dictionary(Of Vector2, Rectangle)
            Spielfeldsize = New Vector2(8, 8)

            Dim sizz As New Vector2(950 * ResolutionMultiplier / Spielfeldsize.X, 950 * ResolutionMultiplier / Spielfeldsize.Y)
            For x As Integer = 0 To Spielfeldsize.X
                For y As Integer = 0 To Spielfeldsize.X
                    rects.Add(New Vector2(x, y), New Rectangle(sizz.X * x, sizz.Y * y, sizz.X, sizz.Y))
                Next
            Next
        End Sub

        Dim debug_batcher As Batcher

#Region "Rendering"


        Public Overrides Sub Render(scene As Scene)

            'Update matrices
            Dim cam = Game.GetCamPos
            CamMatrix = Matrix.CreateFromYawPitchRoll(cam.Yaw, cam.Pitch, cam.Roll) * Matrix.CreateTranslation(cam.Location)
            View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(1920, 1080, 1, 100000)

            'Set render target to be the play field texture
            dev.SetRenderTarget(SpielfeldTextur)
            dev.Clear(New Color(15, 15, 15))

            'Render playfield texture
            batchlor.Begin()

            'Draw playing field squares
            For x As Integer = 0 To Spielfeldsize.X - 1
                For y As Integer = 0 To Spielfeldsize.X - 1
                    If (x + y) Mod 2 = 0 Then Continue For
                    batchlor.DrawRect(rects(New Vector2(x, y)), Color.White * 0.35)
                Next
            Next

            'Draw playing field white border
            batchlor.DrawHollowRect(New Rectangle(0, 0, 950 * ResolutionMultiplier, 950 * ResolutionMultiplier), Color.White, 5)

            batchlor.End()


            'Set render target to be the pseudo backbuffer
            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)

            dev.RasterizerState = RasterizerState.CullNone 'Don't cull shit out
            dev.DepthStencilState = DepthStencilState.Default 'Z-Buffer shall be used for sorting


            'Draw playing figures
            For i As Integer = 0 To Game.Spielers.Length - 1
                For Each figur In Game.Spielers(i).Figuren
                    figur.Draw(i, View, Projection, If(Game.Status = SpielStatus.WähleFigur And Game.GetSelectedFigure Is figur, Game.SelectFader, 0))
                Next
            Next


            'Draw playing field
            EffectA.World = Matrix.Identity
            EffectA.View = View
            EffectA.Projection = Projection
            EffectA.TextureEnabled = True
            EffectA.Texture = SpielfeldTextur

            For Each pass As EffectPass In EffectA.CurrentTechnique.Passes
                dev.SetVertexBuffer(MapBuffer)
                pass.Apply()

                dev.DrawPrimitives(PrimitiveType.TriangleList, 0, MapBuffer.VertexCount)
            Next
        End Sub



#End Region

#Region "Animation"

#End Region
    End Class
End Namespace