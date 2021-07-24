Imports System.Collections.Generic
Imports System.IO
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor.Rendering
    Public Class Renderer3D
        Inherits Renderer


        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private SpielfeldTextur As RenderTarget2D
        Private Cubus As Model
        Private MapBuffer As VertexBuffer

        Private ResolutionMultiplier As Single = 3
        Private rects As Dictionary(Of Vector2, Rectangle)
        Private thingcountlibre As Dictionary(Of Vector2, Rectangle)
        Private Spielfeldsize As Vector2


        Private View As Matrix
        Private Projection As Matrix
        Private CamMatrix As Matrix

        Private Game As IGameWindow

        Private Feld As Rectangle
        Private Center As Vector2

        Private thingycounter As Integer = 0

        Sub New(game As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.Game = game

        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene) 'INITIALIZE
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

            Cubus = scene.Content.Load(Of Model)("mesh/Cowboy_Checkers_V1") 'load cube


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

            Dim sizz As New Vector2(950 / Spielfeldsize.X, 950 / Spielfeldsize.Y)
            For x As Integer = 0 To Spielfeldsize.X - 1
                For y As Integer = 0 To Spielfeldsize.X - 1
                    rects.Add(New Vector2(x, y), New Rectangle(sizz.X * x, sizz.Y * y, sizz.X, sizz.Y))
                Next
            Next
        End Sub

#Region "Rendering"



        Public Overrides Sub Render(scene As Scene)

            Dim cam = New Keyframe3D(0, 0, 0, 0, 0.75, 0, False) '0.75, 0, False)
            CamMatrix = Matrix.CreateFromYawPitchRoll(cam.Yaw, cam.Pitch, cam.Roll) * Matrix.CreateTranslation(cam.Location)
            'If Game.Status = SpielStatus.SaucerFlight Then CamMatrix = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(0), MathHelper.ToRadians(70), MathHelper.ToRadians(Nez.Time.TotalTime / 40 * 360)) * Matrix.CreateTranslation(New Vector3(0, 0, -300))
            View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(dev.Viewport.Width, dev.Viewport.Height, 1, 100000)

            dev.SetRenderTarget(SpielfeldTextur)
            dev.Clear(Color.Black)

            batchlor.Begin(Material, Matrix.CreateScale(ResolutionMultiplier))



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

            'zeichne Figuren
            'thingcountlibre = New Dictionary(Of Vector2, Rectangle)
            'For Each IDC In rects
            '    thingcountlibre.Add(New Vector2(IDC.Value.X, IDC.Value.Y), New Rectangle())
            '    thingycounter += 1
            '    If thingycounter = 16 Then Exit For
            'Next









            For Each IDK In Game.Spielers(0).Figuren 'thingcountlibre
                Dim Zwischenspeicher As Rectangle
                Zwischenspeicher = rects(IDK.Location)
                For Each thingy In Cubus.Meshes


                    For Each element As BasicEffect In thingy.Effects
                    element.TextureEnabled = False
                        element.World = Matrix.CreateScale(90.01 / 2) * Matrix.CreateTranslation(New Vector3(Zwischenspeicher.Center.X - 475, Zwischenspeicher.Center.Y - 475, +25)) 'Matrix.CreateRotationZ(0.75) * Matrix.CreateRotationX(-0.175) * 
                        element.View = View
                    element.Projection = Projection
                    element.LightingEnabled = False
                    element.EnableDefaultLighting()

                Next
                thingy.Draw()

            Next
            Next
        End Sub



#End Region

#Region "Animation"

#End Region
    End Class
End Namespace