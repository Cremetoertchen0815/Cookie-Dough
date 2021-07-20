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
        Private rects As Dictionary(Of Vector2, Rectangle)
        Private Spielfeldsize As Vector2

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


            rects = New Dictionary(Of Vector2, Rectangle)
            Spielfeldsize = New Vector2(8, 8)

            Dim sizz As New Vector2(950 * ResolutionMultiplier / Spielfeldsize.X, 950 * ResolutionMultiplier / Spielfeldsize.Y)
            For x As Integer = 0 To Spielfeldsize.X
                For y As Integer = 0 To Spielfeldsize.X
                    rects.Add(New Vector2(x, y), New Rectangle(sizz.X * x, sizz.Y * y, sizz.X, sizz.Y))
                Next
            Next
        End Sub

#Region "Rendering"



        Public Overrides Sub Render(scene As Scene)

            Dim cam = Game.GetCamPos
            CamMatrix = Matrix.CreateFromYawPitchRoll(cam.Yaw, cam.Pitch, cam.Roll) * Matrix.CreateTranslation(cam.Location)
            'If Game.Status = SpielStatus.SaucerFlight Then CamMatrix = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(0), MathHelper.ToRadians(70), MathHelper.ToRadians(Nez.Time.TotalTime / 40 * 360)) * Matrix.CreateTranslation(New Vector3(0, 0, -300))
            View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(dev.Viewport.Width, dev.Viewport.Height, 1, 100000)

            dev.SetRenderTarget(SpielfeldTextur)
            dev.Clear(Color.Black)

            batchlor.Begin()


            'Zeichne Spielfeld
            For x As Integer = 0 To Spielfeldsize.X - 1
                For y As Integer = 0 To Spielfeldsize.X - 1
                    If (x + y) Mod 2 = 0 Then Continue For
                    batchlor.DrawRect(rects(New Vector2(x, y)), Color.White * 0.35)
                Next
            Next

            'Zeichne Verbindungen
            batchlor.DrawHollowRect(New Rectangle(0, 0, 950, 950), Color.White, 5)

            batchlor.End()


            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)


            'Zeichne figuren
            dev.RasterizerState = RasterizerState.CullNone
            dev.DepthStencilState = DepthStencilState.Default



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