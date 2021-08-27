Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Common
    Public Class CardRenderer
        Inherits Renderer

        'Models and textures
        Private figur_model As Model
        Private card_model As Model
        Private card_fx As BasicEffect
        Private card_Matrix As Matrix
        Private card_deck_model As Model
        Private card_deck_Matrix As Matrix
        Public card_deck_top_pos As Transition(Of Vector3)
        Private TableModel As Model
        Private TableMatrix As Matrix
        Private CardTextures As List(Of Texture2D)
        Private CardWhite As Color

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

        Public Sub New(window As ICardRendererWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Game = window
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            'Load models
            figur_model = scene.Content.Load(Of Model)("mesh/piece_std")
            card_model = scene.Content.Load(Of Model)("games/Cards/card")
            card_fx = CType(card_model.Meshes(1).Effects(0), BasicEffect)
            card_Matrix = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateScale(150) * Matrix.CreateTranslation(-150, 0, 0)
            CardWhite = New Color(228, 228, 228)
            ApplyCardFX(card_model, Projection)

            card_deck_model = scene.Content.Load(Of Model)("games/Cards/card_deck")
            card_deck_Matrix = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateScale(150) * Matrix.CreateTranslation(150, 0, -50)
            card_deck_top_pos = New Transition(Of Vector3) With {.Value = New Vector3(0, 0, -0.1)}
            ApplyDefaultFX(card_deck_model, Projection, Color.White)

            TableModel = scene.Content.Load(Of Model)("mesh/table")
            TableMatrix = Matrix.CreateScale(New Vector3(3.2, 3.2, 3) * 150) * Matrix.CreateTranslation(New Vector3(0, 0, 590))
            ApplyDefaultFX(TableModel, Projection, Color.White)

            'Load cards
            CardTextures = New List(Of Texture2D)
            For Each element In Card.GetAllCards
                CardTextures.Add(scene.Content.LoadTexture("games/Cards/" & element.GetTextureName))
            Next
            CardTextures.Add(scene.Content.LoadTexture("games/Cards/back"))

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
            Dim rotatoA As Matrix = Matrix.CreateRotationZ(If(Game.SpielerIndex > -1, Game.UserIndex * -MathHelper.PiOver2, 0))
            Dim rotatoB As Matrix = Matrix.CreateRotationZ(If(Game.SpielerIndex > -1, Game.SpielerIndex * MathHelper.PiOver2, 0))
            SetViewMatrix(Game.GetCamPos, rotatoA * rotatoB)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(1920, 1080, 1, 100000)

            '---RENDERER 3D---

            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)

            dev.RasterizerState = RasterizerState.CullNone
            dev.DepthStencilState = DepthStencilState.Default

            'Draw card
            If Game.TableCard.Visible Then
                card_fx.Texture = CardTextures(Game.TableCard.ID)
                For Each element In card_model.Meshes
                    ApplyFX(element, element.ParentBone.ModelTransform * card_Matrix)
                    element.Draw()
                Next
            End If

            'Draw deck model
            For Each element In card_deck_model.Meshes
                ApplyFX(element, element.ParentBone.ModelTransform * card_deck_Matrix)
                element.Draw()
            Next

            'Draw deck top card
            If card_deck_top_pos.State = TransitionState.InProgress Then
                card_fx.Texture = CardTextures(CardTextures.Count - 1)
                For Each element In card_model.Meshes
                    ApplyFX(element, element.ParentBone.ModelTransform * card_deck_Matrix * Matrix.CreateTranslation(card_deck_top_pos.Value))
                    element.Draw()
                Next
            End If

            SetViewMatrix(Game.GetCamPos, Matrix.CreateRotationZ(If(Game.SpielerIndex > -1, Game.UserIndex * -MathHelper.PiOver2, 0)))

            'Draw other player's hand decks
            For i As Integer = 0 To Game.Spielers.Length - 1
                If (i = Game.UserIndex And Game.UserIndex > -1) Or (i = 0 And Game.UserIndex < 0) Then Continue For
                For j As Integer = 0 To Game.Spielers(i).HandDeck.Count - 1
                    'If Game.DeckScroll <> Math.Floor(i / 7) OrElse Not Game.HandDeck(i).Visible Then Continue For
                    Dim transform As Matrix = Matrix.CreateRotationZ(MathHelper.PiOver2) * Matrix.CreateTranslation(0, 0, Math.Floor(j / 7) * 2.1) * Matrix.CreateRotationY(0.4) * Matrix.CreateScale(80) * Matrix.CreateTranslation(550, 300 - (j Mod 7) * 100, -150) * Matrix.CreateRotationZ((i - 1) * MathHelper.PiOver2)
                    card_fx.Texture = CardTextures(CardTextures.Count - 1)
                    For Each element In card_model.Meshes
                        ApplyFX(element, element.ParentBone.ModelTransform * transform)
                        element.Draw()
                    Next
                Next
            Next

            'Draw Table
            For Each element In TableModel.Meshes
                ApplyFX(element, element.ParentBone.ModelTransform * TableMatrix)
                element.Draw()
            Next

            'Draw Hand Deck
            If Game.State <> CardGameState.SelectAction Then Return
            dev.DepthStencilState = DepthStencilState.None
            SetViewMatrix(New Keyframe3D, Matrix.Identity)
            For i As Integer = 0 To Game.HandDeck.Count - 1
                If Game.DeckScroll <> Math.Floor(i / 7) OrElse Not Game.HandDeck(i).Visible Then Continue For
                Dim transform As Matrix = GetHandCardWorldMatrix(i)
                card_fx.Texture = CardTextures(Game.HandDeck(i).ID)
                For Each element In card_model.Meshes
                    ApplyFX(element, element.ParentBone.ModelTransform * transform)
                    element.Draw()
                Next
            Next
        End Sub

        Private Function GetHandCardWorldMatrix(i As Integer) As Matrix
            Return Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(4.3 - (i Mod 7) * 1.4, -3, -1069.2)
        End Function

        Private Sub ApplyFX(mesh As ModelMesh, world As Matrix, Optional yflip As Integer = 1)
            For Each effect As BasicEffect In mesh.Effects
                effect.DirectionalLight2.Direction = New Vector3(1, -1 * yflip, 1)
                effect.DirectionalLight0.Enabled = True
                effect.DirectionalLight1.Enabled = True
                effect.DirectionalLight2.Enabled = True
                effect.World = world
                effect.View = View
                effect.Projection = Projection
            Next
        End Sub

        Private Sub SetViewMatrix(cam As Keyframe3D, FinalMatrix As Matrix)
            View = FinalMatrix * cam.GetMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
        End Sub

        Friend Sub ApplyCardFX(model As Model, Projection As Matrix)
            For i As Integer = 0 To model.Meshes.Count - 1
                For Each fx As BasicEffect In model.Meshes(i).Effects
                    ApplyDefaultFX(fx, Projection, If(i > 0, Color.White, CardWhite))
                Next
            Next
        End Sub

#End Region

#Region "Animation"
        Friend Sub TriggerDeckPullAnimation(final As Transition(Of Vector3).FinishedDelegate)

            card_deck_top_pos = New Transition(Of Vector3)(New TransitionTypes.TransitionType_Acceleration(500), New Vector3(0, 0, -0.1), New Vector3(0, -700, -0.1), final)
            Automator.Add(card_deck_top_pos)
        End Sub
#End Region

    End Class
End Namespace