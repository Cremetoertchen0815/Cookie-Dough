Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Tiled

Namespace Game.Barrelled.Renderers
    Public Class Renderer3D
        Inherits Renderer

        Public Sub New(baseclass As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.BaseClass = baseclass
        End Sub

        'Base shit
        Friend View As Matrix
        Friend Projection As Matrix
        Friend BaseClass As IGameWindow

        'Quad rendering
        Private QuadClockwise As VertexBuffer
        Private DebugEffect As BasicEffect

        'Player
        Private PlayerModel As Model
        Private PlayerModelHeadless As Model
        Private PlayerModelBoundingBox As VertexExtractor.BoundingBoxBuffers
        Friend Shared PlayerModelExtremePoints As Vector3()
        Friend PlayerTransform As Matrix

        'Room
        Private FloorEffect As BasicEffect
        Private BarrierModel As Model
        Public Floorsize As Vector2

        'Map
        Private HiMapMatrices As Matrix()
        Private LoMapMatrices As Matrix()
        Private CeilingMapMatrices As Matrix()
        Private CubeModel As Model
        Private GrassMatrices As Matrix()
        Private GrassModel As Model

        'Debug Box Buffer
        Private BoxVertexBuffer As VertexBuffer
        Private BoxIndexBuffer As IndexBuffer
        Private Sub CreateBoxBuffer()

            '---VERTICES---
            Dim genboxvert = New VertexPositionColor(23) {}
            'Left|Top|Front
            genboxvert(0) = New VertexPositionColor(New Vector3(0, 0, 0), Color.Lime)
            genboxvert(1) = New VertexPositionColor(New Vector3(20, 20, 0), Color.Lime)
            genboxvert(2) = New VertexPositionColor(New Vector3(0, 20, 0), Color.Lime)
            'Right|Top|Front
            genboxvert(3) = New VertexPositionColor(New Vector3(1920, 0, 0), Color.Lime)
            genboxvert(4) = New VertexPositionColor(New Vector3(1920, 20, 0), Color.Lime)
            genboxvert(5) = New VertexPositionColor(New Vector3(1900, 20, 0), Color.Lime)
            'Left|Bottom|Front
            genboxvert(6) = New VertexPositionColor(New Vector3(1900, 1060, 0), Color.Lime)
            genboxvert(7) = New VertexPositionColor(New Vector3(1920, 1060, 0), Color.Lime)
            genboxvert(8) = New VertexPositionColor(New Vector3(1920, 1080, 0), Color.Lime)
            'Right|Bottom|Front
            genboxvert(9) = New VertexPositionColor(New Vector3(0, 1080, 0), Color.Lime)
            genboxvert(10) = New VertexPositionColor(New Vector3(0, 1060, 0), Color.Lime)
            genboxvert(11) = New VertexPositionColor(New Vector3(20, 1060, 0), Color.Lime)
            'Left|Top|Back
            genboxvert(12) = New VertexPositionColor(New Vector3(0, 0, 1000), Color.Magenta)
            genboxvert(13) = New VertexPositionColor(New Vector3(20, 20, 1000), Color.Magenta)
            genboxvert(14) = New VertexPositionColor(New Vector3(0, 20, 1000), Color.Magenta)
            'Right|Top|Back
            genboxvert(15) = New VertexPositionColor(New Vector3(1920, 0, 1000), Color.Magenta)
            genboxvert(16) = New VertexPositionColor(New Vector3(1920, 20, 1000), Color.Magenta)
            genboxvert(17) = New VertexPositionColor(New Vector3(1900, 20, 1000), Color.Magenta)
            'Left|Bottom|Back
            genboxvert(18) = New VertexPositionColor(New Vector3(1900, 1060, 1000), Color.Magenta)
            genboxvert(19) = New VertexPositionColor(New Vector3(1920, 1060, 1000), Color.Magenta)
            genboxvert(20) = New VertexPositionColor(New Vector3(1920, 1080, 1000), Color.Magenta)
            'Right|Bottom|Back
            genboxvert(21) = New VertexPositionColor(New Vector3(0, 1080, 1000), Color.Magenta)
            genboxvert(22) = New VertexPositionColor(New Vector3(0, 1060, 1000), Color.Magenta)
            genboxvert(23) = New VertexPositionColor(New Vector3(20, 1060, 1000), Color.Magenta)


            '---INDICES---
            Dim genboxind As Short() = {0, 3, 4,
                                          0, 4, 2,
                                          5, 4, 7,
                                          5, 7, 6,
                                          10, 7, 8,
                                          10, 8, 9,
                                          2, 1, 11,
                                          2, 11, 10,
                                          12, 15, 16,
                                          12, 16, 14,
                                          17, 16, 19,
                                          17, 19, 18,
                                          22, 19, 20,
                                          22, 20, 21,
                                          14, 13, 23,
                                          14, 23, 22,
                                          12, 0, 2, 'Side Left
                                          12, 2, 14,
                                          22, 10, 9,
                                          22, 9, 21,
                                          3, 15, 16,
                                          3, 16, 4,
                                          7, 19, 20,
                                          7, 20, 8}

            BoxVertexBuffer = New VertexBuffer(Dev, GetType(VertexPositionColor), genboxvert.Length, BufferUsage.WriteOnly)
            BoxVertexBuffer.SetData(genboxvert)

            BoxIndexBuffer = New IndexBuffer(Dev, GetType(Short), genboxind.Length, BufferUsage.WriteOnly)
            BoxIndexBuffer.SetData(genboxind)
        End Sub

        Public Sub DrawDebug()
            DebugEffect.Alpha = 1.0F
            DebugEffect.World = Matrix.CreateScale(0.03) * Matrix.CreateTranslation(30, 15, 30)
            DebugEffect.View = View
            DebugEffect.Projection = Projection

            For Each t In DebugEffect.CurrentTechnique.Passes
                t.Apply()
                Dev.SetVertexBuffer(BoxVertexBuffer)
                Dev.Indices = BoxIndexBuffer

                Dev.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, BoxIndexBuffer.IndexCount / 3)
            Next
        End Sub

        Friend Sub GenerateMapMatrices(map As TmxMap)
            Dim lst As New List(Of Matrix)

            'High
            For Each element In map.GetLayer(Of TmxLayer)("High").GetCollisionRectangles
                Dim pos As Vector2 = element.Location.ToVector2 / 3
                Dim size As Vector2 = element.Size.ToVector2 / 6
                lst.Add(Matrix.CreateTranslation(Vector3.One) * Matrix.CreateScale(New Vector3(size.X, 15, size.Y)) * Matrix.CreateTranslation(New Vector3(pos.X, 0, pos.Y)))
            Next
            HiMapMatrices = lst.ToArray
            lst.Clear()

            'Low
            For Each element In map.GetLayer(Of TmxLayer)("Low").GetCollisionRectangles
                Dim pos As Vector2 = element.Location.ToVector2 / 3
                Dim size As Vector2 = element.Size.ToVector2 / 6
                lst.Add(Matrix.CreateTranslation(Vector3.One) * Matrix.CreateScale(New Vector3(size.X, 5, size.Y)) * Matrix.CreateTranslation(New Vector3(pos.X, 0, pos.Y)))
            Next
            LoMapMatrices = lst.ToArray
            lst.Clear()

            'Low
            For Each element In map.GetLayer(Of TmxLayer)("Ceiling").GetCollisionRectangles
                Dim pos As Vector2 = element.Location.ToVector2 / 3
                Dim size As Vector2 = element.Size.ToVector2 / 6
                lst.Add(Matrix.CreateTranslation(Vector3.One) * Matrix.CreateScale(New Vector3(size.X, 1, size.Y)) * Matrix.CreateTranslation(New Vector3(pos.X, 30, pos.Y)))
            Next
            CeilingMapMatrices = lst.ToArray
            lst.Clear()

            'Grass
            For Each element In map.GetLayer(Of TmxLayer)("Floor").Tiles
                If element Is Nothing OrElse element.Gid <> 4 Then Continue For
                Dim pos As Vector2 = New Vector2(element.X, element.Y) * 16 / 3
                lst.Add(Matrix.CreateTranslation(New Vector3(1, 1.02, 1)) * Matrix.CreateScale(New Vector3(2.67, 4, 2.67)) * Matrix.CreateTranslation(New Vector3(pos.X, 0, pos.Y)))
            Next
            GrassMatrices = lst.ToArray
            lst.Clear()
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            'Generate Projection matrix
            Dev = Core.GraphicsDevice
            Projection = Matrix.CreatePerspectiveFieldOfView(1.15, 1920 / 1080, 0.01, 500)

            'Generate quads
            Dim vert As New List(Of VertexPositionNormalTexture) From {
                New VertexPositionNormalTexture(New Vector3(0, 0, 0), New Vector3(0, 0, -1), Vector2.One),
                New VertexPositionNormalTexture(New Vector3(1, 0, 0), New Vector3(0, 0, -1), Vector2.UnitY),
                New VertexPositionNormalTexture(New Vector3(1, 1, 0), New Vector3(0, 0, -1), Vector2.Zero),
                New VertexPositionNormalTexture(New Vector3(0, 0, 0), New Vector3(0, 0, -1), Vector2.One),
                New VertexPositionNormalTexture(New Vector3(1, 1, 0), New Vector3(0, 0, -1), Vector2.Zero),
                New VertexPositionNormalTexture(New Vector3(0, 1, 0), New Vector3(0, 0, -1), Vector2.UnitX)
            }
            QuadClockwise = New VertexBuffer(Dev, GetType(VertexPositionNormalTexture), vert.Count, BufferUsage.WriteOnly)
            QuadClockwise.SetData(vert.ToArray)

            'Load room stuff
            BarrierModel = scene.Content.Load(Of Model)("mesh/Have_A_Cube")
            GrassModel = scene.Content.Load(Of Model)("mesh/grass")
            ApplyDefaultAlpha(GrassModel, Color.White)
            FloorEffect = New BasicEffect(Dev) With {.TextureEnabled = True, .Texture = scene.Content.LoadTexture("3Droom_floor"), .View = View, .Projection = Projection, .EmissiveColor = Vector3.One * 0.8, .FogStart = 15, .FogEnd = 80, .FogColor = Vector3.Zero, .FogEnabled = True}

            'Load player
            PlayerModel = scene.Content.Load(Of Model)("mesh/piece_filled")
            PlayerModelHeadless = scene.Content.Load(Of Model)("mesh/piece_filled_headless")
            PlayerModelExtremePoints = VertexExtractor.ExtractExtremeVerticesFromModel(PlayerModel)
            PlayerModelBoundingBox = VertexExtractor.BoundingBoxBuffers.CreateBoundingBoxBuffers(BoundingBox.CreateFromPoints(PlayerModelExtremePoints), Dev)
            PlayerTransform = Matrix.CreateScale(0.27)
            ApplyDefaultFX(PlayerModel, Color.White)
            ApplyDefaultFX(PlayerModelHeadless, Color.White)

            'Load debug cube
            DebugEffect = New BasicEffect(Dev) With {
                .TextureEnabled = False,
                .VertexColorEnabled = True
            }
            CreateBoxBuffer()
            CubeModel = scene.Content.Load(Of Model)("mesh/Have_A_Cube")
            ApplyDefaultFX(CubeModel, Color.Red)
        End Sub

        Public Overrides Sub Render(scene As Scene)
            Dev.RasterizerState = RasterizerState.CullNone
            Dev.DepthStencilState = DepthStencilState.Default

            'Draw floor/ceiling on a quad
            For x As Integer = 0 To Floorsize.X
                For y As Integer = 0 To Floorsize.Y
                    FloorEffect.DirectionalLight2.Direction = BaseClass.EgoPlayer.Direction * New Vector3(1, -1, 1)
                    FloorEffect.World = Matrix.CreateTranslation(New Vector3(x, y, 0)) * Matrix.CreateScale(40) * Matrix.CreateRotationX(0.5 * Math.PI) * Matrix.CreateTranslation(-20, 0, -20)
                    FloorEffect.View = View
                    FloorEffect.Projection = Projection
                    FloorEffect.DiffuseColor = Vector3.Zero
                    For Each pass As EffectPass In FloorEffect.CurrentTechnique.Passes
                        Dev.SetVertexBuffer(QuadClockwise)
                        pass.Apply()

                        Dev.DrawPrimitives(PrimitiveType.TriangleList, 0, 2)
                    Next
                Next
            Next

#If DEBUG Then
            'Draw debug cube
            DrawDebug()
#End If

            'Draw local player
            For i As Integer = 0 To PlayerModelHeadless.Meshes.Count - 1
                Dim element As ModelMesh = PlayerModelHeadless.Meshes(If(i = 2, 1, i))
                ApplyFX(element, PlayerColors(BaseClass.EgoPlayer.Mode), If(i = 2, Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateScale(4.5) * Matrix.CreateTranslation(0, 16.12, 0), element.ParentBone.ModelTransform) * PlayerTransform * BaseClass.EgoPlayer.GetWorldMatrix)
                element.Draw()
            Next

            'Draw other players
            For p As Integer = 0 To BaseClass.Spielers.Length - 1
                If p = BaseClass.UserIndex Then Continue For
                Dim player = BaseClass.Spielers(p)
                'Draw other player
                For i As Integer = 0 To PlayerModel.Meshes.Count - 1
                    Dim element As ModelMesh = PlayerModel.Meshes(i)
                    For Each fx As BasicEffect In PlayerModel.Meshes(i).Effects
                        fx.World = element.ParentBone.ModelTransform * PlayerTransform * player.GetWorldMatrix
                        fx.View = View
                        fx.Projection = Projection
                        fx.DiffuseColor = If(i = 2, Color.White, PlayerColors(BaseClass.Spielers(p).Mode)).ToVector3
                        fx.Texture = player.Thumbnail
                    Next
                    element.Draw()
                Next
                'Reset Rasterizer state

                'Draw bounding box
#If DEBUG Then
                DrawBoundingBox(PlayerModelBoundingBox, DebugEffect, Dev, Matrix.CreateScale(0.003) * player.GetWorldMatrix(0), View, Projection)
#End If
            Next

            'Draw map
            Dev.RasterizerState = RasterizerState.CullCounterClockwise
            Dim mesh = CubeModel.Meshes(0)
            For Each element In HiMapMatrices
                ApplyFX(mesh, Color.Magenta, element)
                mesh.Draw()
            Next
            For Each element In LoMapMatrices
                ApplyFX(mesh, Color.Lime, element)
                mesh.Draw()
            Next
            For Each element In CeilingMapMatrices
                ApplyFX(mesh, Color.Cyan, element)
                mesh.Draw()
            Next
            Dev.RasterizerState = RasterizerState.CullNone
            For Each element In GrassMatrices
                For Each mess As ModelMesh In GrassModel.Meshes
                    ApplyFXAlpha(mess, Color.White, mess.ParentBone.ModelTransform * element)
                    mess.Draw()
                Next
            Next

            Dev.DepthStencilState = DepthStencilState.Default
            'Draw barrier
            If BaseClass.EgoPlayer.PrisonEnabled Then
                For Each element In BarrierModel.Meshes
                    For Each fx As BasicEffect In element.Effects
                        fx.World = Matrix.CreateTranslation(Vector3.One) * Matrix.CreateScale(New Vector3(BaseClass.BarrierRectangle.Width, 25, BaseClass.BarrierRectangle.Height)) * Matrix.CreateTranslation(New Vector3(BaseClass.BarrierRectangle.X, -2, BaseClass.BarrierRectangle.Y))
                        fx.View = View
                        fx.Projection = Projection
                        fx.FogEnabled = False
                        fx.Alpha = 0.8F
                        fx.DiffuseColor = New Vector3(0, 0, 0)
                        fx.EmissiveColor = New Vector3(0.8, 0.1, 0)
                    Next
                    element.Draw()
                Next

            End If


        End Sub

        Private Sub ApplyFX(mesh As ModelMesh, DiffuseColor As Color, world As Matrix, Optional yflip As Integer = 1)
            For Each effect As BasicEffect In mesh.Effects
                effect.EmissiveColor = Vector3.Zero
                effect.Alpha = 1.0F
                effect.DirectionalLight2.Direction = BaseClass.EgoPlayer.Direction * New Vector3(1, -1 * yflip, 1)
                effect.DiffuseColor = DiffuseColor.ToVector3
                effect.World = world
                effect.FogEnabled = True
                effect.View = View
                effect.Projection = Projection
            Next
        End Sub
        Private Sub ApplyFXAlpha(mesh As ModelMesh, DiffuseColor As Color, world As Matrix, Optional yflip As Integer = 1)
            For Each effect As AlphaTestEffect In mesh.Effects
                effect.AlphaFunction = CompareFunction.GreaterEqual
                effect.ReferenceAlpha = 254
                effect.Alpha = 1.0F
                effect.DiffuseColor = DiffuseColor.ToVector3
                effect.World = world
                effect.FogEnabled = True
                effect.View = View
                effect.Projection = Projection
            Next
        End Sub
        Friend Sub ApplyDefaultFX(effect As BasicEffect, Optional yflip As Integer = 1)
            effect.LightingEnabled = True
            effect.AmbientLightColor = Color.White.ToVector3 * 0.06
            effect.DirectionalLight0.Enabled = True
            effect.DirectionalLight0.DiffuseColor = Color.White.ToVector3 * 0.35
            effect.DirectionalLight0.Direction = New Vector3(0.7, yflip, 0.7)
            effect.DirectionalLight0.SpecularColor = Color.SkyBlue.ToVector3 * 0.5
            effect.DirectionalLight1.Enabled = True
            effect.DirectionalLight1.DiffuseColor = Color.White.ToVector3 * 0.35
            effect.DirectionalLight1.Direction = New Vector3(-0.7, yflip, -0.7)
            effect.DirectionalLight1.SpecularColor = Color.SkyBlue.ToVector3 * 0.5
            effect.DirectionalLight2.Enabled = True
            effect.DirectionalLight2.DiffuseColor = Color.White.ToVector3 * 0.25
            effect.DirectionalLight2.SpecularColor = Color.SkyBlue.ToVector3 * 0.08
            effect.FogEnabled = True
            effect.FogStart = 15
            effect.FogEnd = 80
            effect.FogColor = Vector3.Zero
            effect.SpecularPower = 15
            effect.Alpha = 1
        End Sub
        Friend Sub ApplyDefaultFX(effect As BasicEffect, color As Color)
            effect.LightingEnabled = True
            effect.AmbientLightColor = Color.White.ToVector3 * 0.06
            effect.DirectionalLight0.Enabled = True
            effect.DirectionalLight0.DiffuseColor = Color.White.ToVector3 * 0.35
            effect.DirectionalLight0.Direction = New Vector3(0.7, 1, 0.7)
            effect.DirectionalLight0.SpecularColor = Color.SkyBlue.ToVector3 * 0.5
            effect.DirectionalLight1.Enabled = True
            effect.DirectionalLight1.DiffuseColor = Color.White.ToVector3 * 0.35
            effect.DirectionalLight1.Direction = New Vector3(-0.7, 1, -0.7)
            effect.DirectionalLight1.SpecularColor = Color.SkyBlue.ToVector3 * 0.5
            effect.DirectionalLight2.Enabled = True
            effect.DirectionalLight2.DiffuseColor = Color.White.ToVector3 * 0.25
            effect.DirectionalLight2.SpecularColor = Color.SkyBlue.ToVector3 * 0.08
            effect.DiffuseColor = color.ToVector3
            effect.FogEnabled = True
            effect.FogStart = 15
            effect.FogEnd = 80
            effect.FogColor = Vector3.Zero
            effect.SpecularPower = 15
            effect.Alpha = 1
        End Sub
        Friend Sub ApplyDefaultFX(model As Model, color As Color)
            For Each element In model.Meshes
                For Each fx As BasicEffect In element.Effects
                    ApplyDefaultFX(fx, color)
                Next
            Next
        End Sub
        Friend Sub ApplyDefaultAlpha(model As Model, color As Color)
            For Each element In model.Meshes
                For Each effect As AlphaTestEffect In element.Effects
                    effect.FogEnabled = True
                    effect.FogStart = 15
                    effect.FogEnd = 80
                    effect.FogColor = Vector3.Zero
                    effect.Alpha = 1
                Next
            Next
        End Sub
        Friend Sub ApplyDefaultFX(model As Model, Projection As Matrix, Optional yflip As Integer = 1)
            For Each element In model.Meshes
                For Each fx As BasicEffect In element.Effects
                    ApplyDefaultFX(fx, yflip)
                Next
            Next
        End Sub

        Private Sub DrawBoundingBox(ByVal buffers As VertexExtractor.BoundingBoxBuffers, ByVal effect As BasicEffect, ByVal graphicsDevice As GraphicsDevice, ByVal world As Matrix, ByVal view As Matrix, ByVal projection As Matrix)
            graphicsDevice.SetVertexBuffer(buffers.Vertices)
            graphicsDevice.Indices = buffers.Indices
            effect.Alpha = 1.0F
            effect.World = world
            effect.View = view
            effect.Projection = projection

            For Each pass As EffectPass In effect.CurrentTechnique.Passes
                pass.Apply()
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, buffers.PrimitiveCount)
            Next
        End Sub
    End Class
End Namespace