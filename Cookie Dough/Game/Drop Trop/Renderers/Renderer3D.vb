Imports System.Collections.Generic
Imports System.IO
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.DropTrop.Renderers
    Public Class Renderer3D
        Inherits Renderer

        Private figur_model As Model
        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private SpielfeldTextur As RenderTarget2D
        Private Pfeil As Texture2D
        Private MapBuffer As VertexBuffer

        Private View As Matrix
        Private Projection As Matrix
        Private CamMatrix As Matrix

        Private Game As IGameWindow
        Private Feld As Rectangle
        Private transmatrices As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)}
        Private rects As Dictionary(Of Vector2, Rectangle)
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

            SpielfeldTextur = New RenderTarget2D(
            dev,
            950,
            950,
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
            Dim sizz As New Vector2(950 / Game.SpielfeldSize.X, 950 / Game.SpielfeldSize.Y)
            For x As Integer = 0 To Game.SpielfeldSize.X
                For y As Integer = 0 To Game.SpielfeldSize.X
                    rects.Add(New Vector2(x, y), New Rectangle(sizz.X * x, sizz.Y * y, sizz.X, sizz.Y))
                Next
            Next

        End Sub

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
            For x As Integer = 0 To Game.SpielfeldSize.X - 1
                For y As Integer = 0 To Game.SpielfeldSize.X - 1
                    If Game.CurrentCursor = New Vector2(x, y) Then batchlor.DrawHollowRect(rects(New Vector2(x, y)), hudcolors(Game.UserIndex), 5)
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

            For Each element In Game.Spielfeld
                If element.Value < 0 Then Continue For
                DrawChr(rects(element.Key).Center.ToVector2 - Feld.Size.ToVector2 / 2, playcolor(element.Value), 3)
            Next

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

        Private Sub DrawChr(pos As Vector2, color As Color, basescale As Single, Optional zpos As Integer = 0, Optional scale As Single = 1)
            For Each mesh In figur_model.Meshes
                For Each element As BasicEffect In mesh.Effects
                    element.VertexColorEnabled = False
                    element.TextureEnabled = False
                    element.DiffuseColor = Color.White.ToVector3
                    element.LightingEnabled = True '// turn on the lighting subsystem.
                    element.PreferPerPixelLighting = True
                    element.AmbientLightColor = Vector3.Zero
                    element.EmissiveColor = color.ToVector3 * 0.12
                    element.DirectionalLight0.Direction = New Vector3(0, 0.8, 1.5)
                    element.DirectionalLight0.DiffuseColor = color.ToVector3 * 0.6 '// a gray light
                    element.DirectionalLight0.SpecularColor = New Vector3(1, 1, 1) '// with white highlights
                    element.World = Matrix.CreateScale(basescale * scale * New Vector3(1, 1, 1)) * Matrix.CreateRotationY(Math.PI) * Matrix.CreateTranslation(-pos.X, -pos.Y, -zpos - AdditionalZPos.Value)
                    element.View = View
                    element.Projection = Projection
                Next
                mesh.Draw()
            Next
        End Sub

        Private Sub DrawArrow(vc As Vector2, color As Color, iteration As Integer)
            batchlor.Draw(Pfeil, New Rectangle(vc.X, vc.Y, 35, 35), Nothing, color, MathHelper.PiOver2 * ((iteration / Game.Spielers.Length) * 4 + 3), New Vector2(35, 35) / 2, SpriteEffects.None, 0)
        End Sub

        Private Function GetBufferedTime(afteraction As Action(Of ITimer)) As Transition(Of Single).FinishedDelegate
            Return Sub()
                       Core.Schedule(0.3, afteraction)
                   End Sub
        End Function
    End Class
End Namespace