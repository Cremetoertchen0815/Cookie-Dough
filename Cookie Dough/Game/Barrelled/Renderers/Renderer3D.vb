Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Tiled

Namespace Game.Barrelled.Renderers
    Public Class Renderer3D
        Inherits Renderer

        Sub New(baseclass As GameRoom, Optional order As Integer = 0)
            MyBase.New(order)
            Me.BaseClass = baseclass
        End Sub

        'Base shit
        Friend View As Matrix
        Private Projection As Matrix
        Friend BaseClass As GameRoom

        'Quad rendering
        Private QuadClockwise As VertexBuffer
        Private QuadCounterClockwise As VertexBuffer
        Private QuadEffect As BasicEffect
        Private DebugEffect As BasicEffect

        'Player
        Private PlayerModel As Model
        Private PlayerModelHeadless As Model
        Friend PlayerTransform As Matrix

        'Room
        Private TableModel As Model
        Private WallModel As Model
        Private WallTransforms As Matrix() = {}
        Private RoomTextures As Texture2D() = {}

        'Map
        Private MapMatrices As Matrix()
        Private CubeModel As Model

        'Debug Box Buffer
        Dim BoxVertexBuffer As VertexBuffer
        Dim BoxIndexBuffer As IndexBuffer
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
            DebugEffect.World = Matrix.Identity
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
                lst.Add(Matrix.CreateTranslation(Vector3.One) * Matrix.CreateScale(New Vector3(size.X, 10, size.Y)) * Matrix.CreateTranslation(New Vector3(pos.X, 0, pos.Y)))
            Next

            'Low
            For Each element In map.GetLayer(Of TmxLayer)("Low").GetCollisionRectangles
                Dim pos As Vector2 = element.Location.ToVector2 / 3
                Dim size As Vector2 = element.Size.ToVector2 / 6
                lst.Add(Matrix.CreateTranslation(Vector3.One) * Matrix.CreateScale(New Vector3(size.X, 5, size.Y)) * Matrix.CreateTranslation(New Vector3(pos.X, 0, pos.Y)))
            Next
            MapMatrices = lst.ToArray
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            Dev = Core.GraphicsDevice
            Projection = Matrix.CreatePerspectiveFieldOfView(0.8, CSng(Core.Instance.Window.ClientBounds.Width) / CSng(Core.Instance.Window.ClientBounds.Height), 0.01, 150)

            'Generate quads
            Dim vert As New List(Of VertexPositionNormalTexture)
            vert.Add(New VertexPositionNormalTexture(New Vector3(0, 0, 0), New Vector3(0, 0, -1), Vector2.One))
            vert.Add(New VertexPositionNormalTexture(New Vector3(1, 0, 0), New Vector3(0, 0, -1), Vector2.UnitY))
            vert.Add(New VertexPositionNormalTexture(New Vector3(1, 1, 0), New Vector3(0, 0, -1), Vector2.Zero))
            vert.Add(New VertexPositionNormalTexture(New Vector3(0, 0, 0), New Vector3(0, 0, -1), Vector2.One))
            vert.Add(New VertexPositionNormalTexture(New Vector3(1, 1, 0), New Vector3(0, 0, -1), Vector2.Zero))
            vert.Add(New VertexPositionNormalTexture(New Vector3(0, 1, 0), New Vector3(0, 0, -1), Vector2.UnitX))
            QuadClockwise = New VertexBuffer(Dev, GetType(VertexPositionNormalTexture), vert.Count, BufferUsage.WriteOnly)
            QuadClockwise.SetData(vert.ToArray)
            vert.Reverse()
            QuadCounterClockwise = New VertexBuffer(Dev, GetType(VertexPositionNormalTexture), vert.Count, BufferUsage.WriteOnly)
            QuadCounterClockwise.SetData(vert.ToArray)

            'Load quad effect
            QuadEffect = New BasicEffect(Dev) With {.TextureEnabled = True}
            ApplyDefaultFX(QuadEffect, Projection)

            'Load player
            PlayerModel = scene.Content.Load(Of Model)("mesh/piece_filled")
            PlayerModelHeadless = scene.Content.Load(Of Model)("mesh/piece_filled_headless")
            PlayerTransform = Matrix.CreateScale(0.27)
            ApplyDefaultFX(PlayerModel, Projection)
            ApplyDefaultFX(PlayerModelHeadless, Projection)

            'Load debug cube
            DebugEffect = New BasicEffect(Dev)
            DebugEffect.TextureEnabled = False
            DebugEffect.VertexColorEnabled = True
            CreateBoxBuffer()
            CubeModel = scene.Content.Load(Of Model)("mesh/Have_A_Cube")
            ApplyDefaultFX(CubeModel, Projection, Color.Red)

            'Load table 
            TableModel = scene.Content.Load(Of Model)("mesh/table")
            ApplyDefaultFX(TableModel, Projection)

            'Load room
            WallTransforms = {Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(0 * Math.PI) * Matrix.CreateTranslation(-0, 6, 20),
                              Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(0.5 * Math.PI) * Matrix.CreateTranslation(20, 6, 0),
                              Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(1 * Math.PI) * Matrix.CreateTranslation(0, 6, -20),
                              Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(1.5 * Math.PI) * Matrix.CreateTranslation(-20, 6, 0),
                              Matrix.CreateScale(40) * Matrix.CreateRotationX(-0.5 * Math.PI) * Matrix.CreateTranslation(-20, 0, 20),
                              Matrix.CreateScale(40) * Matrix.CreateRotationX(0.5 * Math.PI) * Matrix.CreateTranslation(-20, 12, -20)}
            RoomTextures = {scene.Content.LoadTexture("3Droom_floor"), DebugTexture}
            WallModel = scene.Content.Load(Of Model)("mesh\wall")
            ApplyDefaultFX(WallModel, Projection)
        End Sub

        Public Overrides Sub Render(scene As Scene)
            Dev.RasterizerState = RasterizerState.CullNone
            Dev.DepthStencilState = DepthStencilState.Default
            WallTransforms = {Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(1 * Math.PI) * Matrix.CreateTranslation(-0, 6, 20),
                              Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(1.5 * Math.PI) * Matrix.CreateTranslation(20, 6, 0),
                              Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(0 * Math.PI) * Matrix.CreateTranslation(0, 6, -20),
                              Matrix.CreateScale(20 * New Vector3(1, 0.3, 1)) * Matrix.CreateRotationY(0.5 * Math.PI) * Matrix.CreateTranslation(-20, 6, 0),
                              Matrix.CreateScale(40) * Matrix.CreateRotationX(-0.5 * Math.PI) * Matrix.CreateTranslation(-20, 0, 20),
                              Matrix.CreateScale(40) * Matrix.CreateRotationX(0.5 * Math.PI) * Matrix.CreateTranslation(-20, 12, -20)}

            'Draw walls
            For i As Integer = 0 To 3
                For Each element In WallModel.Meshes
                    ApplyFX(element, Color.White, WallTransforms(i))
                    element.Draw()
                Next
            Next
            'Draw floor/ceiling on a quad
            ApplyFX(QuadEffect, Matrix.Identity)
            For i As Integer = 4 To 5
                QuadEffect.World = WallTransforms(i)
                QuadEffect.Texture = RoomTextures(i - 4)
                For Each pass As EffectPass In QuadEffect.CurrentTechnique.Passes
                    Dev.SetVertexBuffer(QuadClockwise)
                    pass.Apply()

                    Dev.DrawPrimitives(PrimitiveType.TriangleList, 0, 2)
                Next
            Next

            'Draw Table
            For Each element In TableModel.Meshes
                ApplyFX(element, Color.White, element.ParentBone.ModelTransform * BaseClass.Table.TransformMatrix)
                element.Draw()
            Next

            'Draw debug cube
            'DrawDebug()

            'Draw players and accesories
            Dim user = BaseClass.Spielers(BaseClass.UserIndex)
            For p As Integer = 0 To BaseClass.Spielers.Count - 1
                Dim player = BaseClass.Spielers(p)
                'Draw player himself
                If player Is user Then
                    'Draw local player
                    For i As Integer = 0 To PlayerModelHeadless.Meshes.Count - 1
                        Dim element As ModelMesh = PlayerModelHeadless.Meshes(If(i = 2, 1, i))
                        ApplyFX(element, playcolor(p), If(i = 2, Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateScale(4.5) * Matrix.CreateTranslation(0, 16.12, 0), element.ParentBone.ModelTransform) * PlayerTransform * player.GetWorldMatrix)
                        element.Draw()
                    Next
                Else
                    'Draw other player
                    For i As Integer = 0 To PlayerModel.Meshes.Count - 1
                        Dim element As ModelMesh = PlayerModel.Meshes(i)
                        ApplyFX(element, If(i = 2, Color.White, playcolor(p)), element.ParentBone.ModelTransform * PlayerTransform * player.GetWorldMatrix)
                        element.Draw()
                    Next
                End If
                'Reset Rasterizer state
                Dev.RasterizerState = RasterizerState.CullNone
            Next

            'Draw map
            Dev.RasterizerState = RasterizerState.CullCounterClockwise
            Dim mesh = CubeModel.Meshes(0)
            For Each element In MapMatrices
                ApplyFX(mesh, Color.White, element)
                mesh.Draw()
            Next

        End Sub

        'Apply the current lighting data
        Private Sub ApplyFX(effect As BasicEffect, world As Matrix, Optional yflip As Integer = 1)
            effect.DirectionalLight2.Direction = BaseClass.Spielers(BaseClass.UserIndex).Direction * New Vector3(1, -1 * yflip, 1)
            effect.World = world
            effect.View = View
        End Sub
        Private Sub ApplyFX(mesh As ModelMesh, DiffuseColor As Color, world As Matrix, Optional yflip As Integer = 1)
            For Each effect As BasicEffect In mesh.Effects
                effect.DirectionalLight2.Direction = BaseClass.Spielers(BaseClass.UserIndex).Direction * New Vector3(1, -1 * yflip, 1)
                effect.DiffuseColor = DiffuseColor.ToVector3
                effect.World = world
                effect.View = View
            Next
        End Sub

        Private Sub DrawBoundingBox(ByVal buffers As VertexExtractor.BoundingBoxBuffers, ByVal effect As BasicEffect, ByVal graphicsDevice As GraphicsDevice, ByVal world As Matrix, ByVal view As Matrix, ByVal projection As Matrix)
            graphicsDevice.SetVertexBuffer(buffers.Vertices)
            graphicsDevice.Indices = buffers.Indices
            effect.World = Matrix.Identity
            effect.View = view
            effect.Projection = projection

            For Each pass As EffectPass In effect.CurrentTechnique.Passes
                pass.Apply()
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, buffers.PrimitiveCount)
            Next
        End Sub
    End Class
End Namespace