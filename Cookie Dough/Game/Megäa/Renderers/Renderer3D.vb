Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Megäa.Renderers
    Public Class Renderer3D
        Inherits Renderer

        Public Sub New(baseclass As GameRoom, Optional order As Integer = 0)
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

        'Player
        Private PlayerModel As Model
        Private PlayerModelHeadless As Model
        Friend PlayerTransform As Matrix

        'Cards
        Private CardBaseTransform As Matrix
        Private CardFrontTextures As Texture2D()
        Private CardBackTexture As Texture2D

        'Room
        Private TableModel As Model
        Private TotemModel As Model
        Private WallModel As Model
        Private WallTransforms As Matrix() = {}
        Private RoomTextures As Texture2D() = {}

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            Dev = Core.GraphicsDevice
            Projection = Matrix.CreatePerspectiveFieldOfView(1, Core.Instance.Window.ClientBounds.Width / CSng(Core.Instance.Window.ClientBounds.Height), 0.01, 10000000)

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

            'Load table 
            TableModel = scene.Content.Load(Of Model)("mesh/table")
            ApplyDefaultFX(TableModel, Projection)

            'Load totem
            TotemModel = scene.Content.Load(Of Model)("mesh\totem")
            ApplyDefaultFX(TotemModel, Projection, -1)

            'Load cards
            CardBaseTransform = Matrix.CreateRotationX(MathHelper.PiOver2 * 3) * Matrix.CreateScale(1.4) * Matrix.CreateTranslation(New Vector3(-0.5, 0, 0.5))
            CardFrontTextures = {scene.Content.LoadTexture("games/MG/cards/A1"), scene.Content.LoadTexture("games/MG/cards/A2"), scene.Content.LoadTexture("games/MG/cards/A3"), scene.Content.LoadTexture("games/MG/cards/A4"),
                                 scene.Content.LoadTexture("games/MG/cards/B1"), scene.Content.LoadTexture("games/MG/cards/B2"), scene.Content.LoadTexture("games/MG/cards/B3"), scene.Content.LoadTexture("games/MG/cards/B4"),
                                 scene.Content.LoadTexture("games/MG/cards/C1"), scene.Content.LoadTexture("games/MG/cards/C2"), scene.Content.LoadTexture("games/MG/cards/C3"), scene.Content.LoadTexture("games/MG/cards/C4"),
                                 scene.Content.LoadTexture("games/MG/cards/D1"), scene.Content.LoadTexture("games/MG/cards/D2"), scene.Content.LoadTexture("games/MG/cards/D3"), scene.Content.LoadTexture("games/MG/cards/D4"),
                                 scene.Content.LoadTexture("games/MG/cards/E1"), scene.Content.LoadTexture("games/MG/cards/E2"), scene.Content.LoadTexture("games/MG/cards/E3"), scene.Content.LoadTexture("games/MG/cards/E4"),
                                 scene.Content.LoadTexture("games/MG/cards/F1"), scene.Content.LoadTexture("games/MG/cards/F2"), scene.Content.LoadTexture("games/MG/cards/F3"), scene.Content.LoadTexture("games/MG/cards/F4"),
                                 scene.Content.LoadTexture("games/MG/cards/G1"), scene.Content.LoadTexture("games/MG/cards/G2"), scene.Content.LoadTexture("games/MG/cards/G3"), scene.Content.LoadTexture("games/MG/cards/G4"),
                                 scene.Content.LoadTexture("games/MG/cards/H1"), scene.Content.LoadTexture("games/MG/cards/H2"), scene.Content.LoadTexture("games/MG/cards/H3"), scene.Content.LoadTexture("games/MG/cards/H4"),
                                 scene.Content.LoadTexture("games/MG/cards/I1"), scene.Content.LoadTexture("games/MG/cards/I2"), scene.Content.LoadTexture("games/MG/cards/I3"), scene.Content.LoadTexture("games/MG/cards/I4"),
                                 scene.Content.LoadTexture("games/MG/cards/J1"), scene.Content.LoadTexture("games/MG/cards/J2"), scene.Content.LoadTexture("games/MG/cards/J3"), scene.Content.LoadTexture("games/MG/cards/J4"),
                                 scene.Content.LoadTexture("games/MG/cards/K1"), scene.Content.LoadTexture("games/MG/cards/K2"), scene.Content.LoadTexture("games/MG/cards/K3"), scene.Content.LoadTexture("games/MG/cards/K4"),
                                 scene.Content.LoadTexture("games/MG/cards/L1"), scene.Content.LoadTexture("games/MG/cards/L2"), scene.Content.LoadTexture("games/MG/cards/L3"), scene.Content.LoadTexture("games/MG/cards/L4"),
                                 scene.Content.LoadTexture("games/MG/cards/M1"), scene.Content.LoadTexture("games/MG/cards/M2"), scene.Content.LoadTexture("games/MG/cards/M3"), scene.Content.LoadTexture("games/MG/cards/M4"),
                                 scene.Content.LoadTexture("games/MG/cards/N1"), scene.Content.LoadTexture("games/MG/cards/N2"), scene.Content.LoadTexture("games/MG/cards/N3"), scene.Content.LoadTexture("games/MG/cards/N4"),
                                 scene.Content.LoadTexture("games/MG/cards/O1"), scene.Content.LoadTexture("games/MG/cards/O2"), scene.Content.LoadTexture("games/MG/cards/O3"), scene.Content.LoadTexture("games/MG/cards/O4"),
                                 scene.Content.LoadTexture("games/MG/cards/P1"), scene.Content.LoadTexture("games/MG/cards/P2"), scene.Content.LoadTexture("games/MG/cards/P3"), scene.Content.LoadTexture("games/MG/cards/P4"),
                                 scene.Content.LoadTexture("games/MG/cards/Q1"), scene.Content.LoadTexture("games/MG/cards/Q2"), scene.Content.LoadTexture("games/MG/cards/Q3"), scene.Content.LoadTexture("games/MG/cards/Q4"),
                                 scene.Content.LoadTexture("games/MG/cards/R1"), scene.Content.LoadTexture("games/MG/cards/R2"), scene.Content.LoadTexture("games/MG/cards/R3"), scene.Content.LoadTexture("games/MG/cards/R4"),
                                 scene.Content.LoadTexture("games/MG/cards/special_1"), scene.Content.LoadTexture("games/MG/cards/special_2"), scene.Content.LoadTexture("games/MG/cards/special_3")}
            CardBackTexture = scene.Content.LoadTexture("games/MG/cards/back")

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

            'Draw Totem
            For Each element In TotemModel.Meshes
                ApplyFX(element, Color.White, BaseClass.Totem.TransformMatrix, -1)
                element.Draw()
            Next

            'Current player
            Dim user = BaseClass.Spielers(BaseClass.UserIndex)

            'Draw players and accesories
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

                'Draw table card
                If player.PositionLocked And player.TableCard > -1 Then DrawCard(player.TableCardTransform.GetMatrix, CardFrontTextures(player.TableCard))
                'Draw hand card if animating
                If player.HandCardTransform.State = TransitionState.InProgress Then DrawCard(player.HandCardTransform.Value.GetMatrix * Matrix.CreateTranslation(0, 0.001, 0), CardFrontTextures(player.HandCard))

                'Reset Rasterizer state
                Dev.RasterizerState = RasterizerState.CullNone
            Next

        End Sub

        'Draw card
        Private Sub DrawCard(world As Matrix, front As Texture2D)
            ''Draw front
            Dev.RasterizerState = RasterizerState.CullClockwise
            QuadEffect.World = CardBaseTransform * world
            QuadEffect.Texture = front
            For Each pass As EffectPass In QuadEffect.CurrentTechnique.Passes
                Dev.SetVertexBuffer(QuadClockwise)
                pass.Apply()

                Dev.DrawPrimitives(PrimitiveType.TriangleList, 0, 2)
            Next

            'Draw back
            QuadEffect.Texture = CardBackTexture
            For Each pass As EffectPass In QuadEffect.CurrentTechnique.Passes
                Dev.SetVertexBuffer(QuadCounterClockwise)
                pass.Apply()

                Dev.DrawPrimitives(PrimitiveType.TriangleList, 0, 2)
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