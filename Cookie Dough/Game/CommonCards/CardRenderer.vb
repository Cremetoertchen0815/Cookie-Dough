Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.CommonCards
    Public Class CardRenderer
        Inherits Renderer

        'Models and textures
        Private figur_model As Model
        Private card_model As Model
        Private card_Matrix As Matrix
        Private card_deck_model As Model
        Private card_deck_Matrix As Matrix
        Private TableModel As Model
        Private TableMatrix As Matrix
        Private CardTextures As List(Of Texture2D)

        'Graphics
        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private MapBuffer As VertexBuffer

        Private View As Matrix
        Private Projection As Matrix

        Private Game As ICardRendererWindow
        Private Feld As Rectangle
        Private Center As Vector2

        Sub New(window As ICardRendererWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Game = window
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            'Load models
            figur_model = scene.Content.Load(Of Model)("mesh/piece_std")
            card_model = scene.Content.Load(Of Model)("games/Cards/card")
            card_Matrix = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateScale(150) * Matrix.CreateTranslation(-150, 0, 0)
            ApplyDefaultFX(card_model, Projection)

            card_deck_model = scene.Content.Load(Of Model)("games/Cards/card_deck")
            card_deck_Matrix = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateScale(150) * Matrix.CreateTranslation(150, 0, -50)
            ApplyDefaultFX(card_deck_model, Projection)

            TableModel = scene.Content.Load(Of Model)("mesh/table")
            TableMatrix = Matrix.CreateScale(New Vector3(3.2, 3.2, 3) * 150) * Matrix.CreateTranslation(New Vector3(0, 0, 590))
            ApplyDefaultFX(TableModel, Projection)

            'Load cards
            CardTextures = New List(Of Texture2D)
            For Each element In Card.GetAllCards
                CardTextures.Add(scene.Content.LoadTexture("games/Cards/" & element.GetTextureName))
            Next

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

            RenderTexture = New Textures.RenderTexture()

            EffectA = New BasicEffect(dev) With {.Alpha = 1.0F,
            .VertexColorEnabled = True,
            .LightingEnabled = False,
            .TextureEnabled = True
        }

            batchlor = New Batcher(dev)

        End Sub

#Region "Rendering"

        Public Overrides Sub Render(scene As Scene)

            SetViewMatrix(Game.GetCamPos)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(dev.Viewport.Width, dev.Viewport.Height, 1, 100000)

            '---RENDERER 3D---

            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)

            dev.RasterizerState = RasterizerState.CullNone
            dev.DepthStencilState = DepthStencilState.Default

            'Draw card
            For Each element In card_model.Meshes
                ApplyFX(element, Color.White, element.ParentBone.ModelTransform * card_Matrix)
                element.Draw()
            Next

            'Draw deck model
            For Each element In card_deck_model.Meshes
                ApplyFX(element, Color.White, element.ParentBone.ModelTransform * card_deck_Matrix)
                element.Draw()
            Next

            'Draw Table
            For Each element In TableModel.Meshes
                ApplyFX(element, Color.White, element.ParentBone.ModelTransform * TableMatrix)
                element.Draw()
            Next

            'Draw Hand Deck
            SetViewMatrix(New Keyframe3D)
            For i As Integer = 0 To Game.HandDeck.Count - 1
                Dim transform As Matrix = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(4.3 - i * 1.4, -3, -1069.2)
                CType(card_model.Meshes(1).Effects(0), BasicEffect).Texture = CardTextures(Game.HandDeck(i).ID)
                For Each element In card_model.Meshes
                    ApplyFX(element, Color.White, element.ParentBone.ModelTransform * transform)
                    element.Draw()
                Next
            Next


        End Sub

        Private Sub ApplyFX(mesh As ModelMesh, DiffuseColor As Color, world As Matrix, Optional yflip As Integer = 1)
            For Each effect As BasicEffect In mesh.Effects
                effect.DirectionalLight2.Direction = New Vector3(1, -1 * yflip, 1)
                effect.DiffuseColor = DiffuseColor.ToVector3
                effect.World = world
                effect.View = View
                effect.Projection = Projection
            Next
        End Sub

        Private Sub SetViewMatrix(cam As Keyframe3D)
            View = cam.GetMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
        End Sub

#End Region

    End Class
End Namespace