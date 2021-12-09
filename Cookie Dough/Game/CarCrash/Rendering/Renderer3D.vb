Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Textures

Namespace Game.CarCrash.Rendering
    Public Class Renderer3D
        Inherits Renderer


        'Assets
        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private MapBuffer As VertexBuffer
        Private ResolutionMultiplier As Single = 2

        'Intro
        Private BeginCurrentPlayer As Integer
        Friend BeginTriggered As Boolean
        Private BeginCam As Transition(Of Keyframe3D)

        'Camera and projection matrices
        Private View As Matrix
        Private Projection As Matrix
        Private CamMatrix As Matrix

        'Common fields
        Public BlurredContents As RenderTexture
        Private BlurEffect As GaussianBlurPostProcessor
        Private Game As IGameWindow
        Friend AdditionalZPos As New Transition(Of Single)

        Public Sub New(game As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.Game = game

            RenderTexture = New RenderTexture()
            BlurredContents = New RenderTexture(1920, 1080)
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            'Generate quad for map to be projected onto
            Dim vertices As New List(Of VertexPositionColorTexture) From {
                New VertexPositionColorTexture(New Vector3(-600, 338, 0), Color.White, Vector2.UnitX),
                New VertexPositionColorTexture(New Vector3(600, 338, 0), Color.White, Vector2.Zero),
                New VertexPositionColorTexture(New Vector3(-600, -338, 0), Color.White, Vector2.One),
                New VertexPositionColorTexture(New Vector3(600, 338, 0), Color.White, Vector2.Zero),
                New VertexPositionColorTexture(New Vector3(600, -338, 0), Color.White, Vector2.UnitY),
                New VertexPositionColorTexture(New Vector3(-600, -338, 0), Color.White, Vector2.One)
            }
            MapBuffer = New VertexBuffer(dev, GetType(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly)
            MapBuffer.SetData(vertices.ToArray)

            BlurEffect = scene.AddPostProcessor(New GaussianBlurPostProcessor(0) With {.Enabled = False})
            BlurEffect.Effect.BlurAmount = 4


            EffectA = New BasicEffect(dev) With {.Alpha = 1.0F,
            .VertexColorEnabled = True,
            .LightingEnabled = False,
            .TextureEnabled = True,
            .FogEnabled = True,
            .FogColor = Vector3.Zero,
            .FogStart = 0.5F,
            .FogEnd = 2.5F
        }

            batchlor = New Batcher(dev)

        End Sub

#Region "Rendering"

        Public Overrides Sub Render(scene As Scene)

            CamMatrix = If(BeginTriggered, BeginCam.Value, Game.GetCamPos).GetMatrix
            View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(1920, 1080, 1, 100000)

            '---RENDERER 3D---

            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)

            batchlor.Begin()
            batchlor.Draw(Game.BGTexture, New Rectangle(0, 0, 1920, 1080))
            batchlor.End()


            dev.RasterizerState = RasterizerState.CullNone
            dev.DepthStencilState = DepthStencilState.Default
            dev.SamplerStates(0) = SamplerState.AnisotropicClamp

            'Render map
            EffectA.World = Matrix.Identity
            EffectA.View = View
            EffectA.Projection = Projection
            EffectA.TextureEnabled = True
            EffectA.Texture = Game.EmuTexture

            For Each pass As EffectPass In EffectA.CurrentTechnique.Passes
                dev.SetVertexBuffer(MapBuffer)
                pass.Apply()

                dev.DrawPrimitives(PrimitiveType.TriangleList, 0, MapBuffer.VertexCount)
            Next

            '---END OF RENDERER3D---

            'Apply blur to the contents
            BlurEffect.Process(RenderTexture, BlurredContents)
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

#End Region

#Region "Animation"
        Friend Sub TriggerStartAnimation(FinalAction As Action)
            'Move camera down
            BeginTriggered = True
            BeginCam = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_Acceleration(2500), New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False), New Keyframe3D(-79, -90, -169, 4.36, 1.39, 0.17, False), Sub()
                                                                                                                                                                                                                                FinalAction()
                                                                                                                                                                                                                                BeginTriggered = False
                                                                                                                                                                                                                            End Sub)
            Automator.Add(BeginCam)
        End Sub



        Private Function GetBufferedTime(afteraction As Action(Of ITimer)) As Transition(Of Single).FinishedDelegate
            Return Sub()
                       Core.Schedule(0.3, afteraction)
                   End Sub
        End Function

#End Region
    End Class
End Namespace