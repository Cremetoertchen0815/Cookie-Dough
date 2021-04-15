﻿Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Framework.Graphics
    Public Class PsygroundRenderer
        Inherits Renderer

        'Vertex & index buffers
        Dim vertexlist As VertexPositionColorTexture()
        Dim indexlist As Integer()
        Dim vertexbuffer As DynamicVertexBuffer
        Dim indexbuffer As IndexBuffer
        Dim Effect As BasicEffect

        'Color faders
        Dim faderA As Transition(Of Color)
        Dim faderB As Transition(Of Color)
        Dim faderC As Transition(Of Color)
        Dim faderD As Transition(Of Color)

        'Other junk
        Dim loops As Boolean = True
        Dim rand As New System.Random

        'render stuff
        Protected Alpha As Single
        Protected Dev As GraphicsDevice
        Protected World As Matrix
        Protected View As Matrix
        Protected Projection As Matrix
        Public StencilTexture As Texture2D = ReferencePixel

        'Properties
        Public Property Size As New Vector2(1920.0F, 1080)
        Public Property Colors As Color() = {Color.Blue, Color.DarkCyan, Color.Purple, Color.Black, Color.Green}
        Public Property Speed As Integer = 10000

        Sub New(Optional order As Integer = 0, Optional Alpha As Single = 0.3F)
            MyBase.New(order)
            Me.Alpha = Alpha
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            World = Matrix.Identity

            dev = Core.GraphicsDevice

            View = Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), New Vector3(0, -10, 0))
            Projection = Matrix.CreateScale(1, -1, 1) * Matrix.CreatePerspectiveOffCenter(0, Size.X, Size.Y, 0, 1, 999)

            vertexlist = {New VertexPositionColorTexture(New Vector3(0, 0, 0), Color.White, New Vector2(0, 0)),
                          New VertexPositionColorTexture(New Vector3(Size.X, 0, 0), Color.White, New Vector2(1, 0)),
                          New VertexPositionColorTexture(New Vector3(Size.X, Size.Y, 0), Color.White, New Vector2(1, 1)),
                          New VertexPositionColorTexture(New Vector3(0, Size.Y, 0), Color.White, New Vector2(0, 1))}
            indexlist = {0, 1, 2, 0, 2, 3}

            vertexbuffer = New DynamicVertexBuffer(dev, GetType(VertexPositionColorTexture), vertexlist.Length, BufferUsage.WriteOnly)
            vertexbuffer.SetData(vertexlist)
            indexbuffer = New IndexBuffer(dev, IndexElementSize.ThirtyTwoBits, indexlist.Length, BufferUsage.WriteOnly)
            indexbuffer.SetData(indexlist)

            Effect = New BasicEffect(dev)

            faderA = New Transition(Of Color) With {.EndValue = RndColor(), .Value = .EndValue}
            faderB = New Transition(Of Color) With {.EndValue = RndColor(), .Value = .EndValue}
            faderC = New Transition(Of Color) With {.EndValue = RndColor(), .Value = .EndValue}
            faderD = New Transition(Of Color) With {.EndValue = RndColor(), .Value = .EndValue}
            loops = True
            LaunchColorFaders()
        End Sub

        Public Overrides Sub Render(scene As Scene)

            Effect.World = World
            Effect.View = View
            Effect.Projection = Projection
            Effect.Alpha = Alpha
            Effect.VertexColorEnabled = True

            vertexlist = {New VertexPositionColorTexture(New Vector3(0, 0, 0), faderA.Value, New Vector2(0, 0)),
                          New VertexPositionColorTexture(New Vector3(Size.X, 0, 0), faderB.Value, New Vector2(1, 0)),
                          New VertexPositionColorTexture(New Vector3(Size.X, Size.Y, 0), faderC.Value, New Vector2(1, 1)),
                          New VertexPositionColorTexture(New Vector3(0, Size.Y, 0), faderD.Value, New Vector2(0, 1))}

            vertexbuffer.SetData(vertexlist)

            dev.BlendState = BlendState.AlphaBlend

            For Each element In Effect.CurrentTechnique.Passes
                dev.SetVertexBuffer(vertexbuffer)
                dev.Indices = indexbuffer

                element.Apply()

                dev.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4)
            Next
        End Sub

        Public Sub DirectDraw(gameTime As GameTime)
        End Sub

        Public Sub [End]()
            loops = False
        End Sub


        Private Sub LaunchColorFaders()
            If loops Then
                faderA = New Transition(Of Color)(New TransitionTypes.TransitionType_Linear(Speed), faderA.EndValue, RndColor, AddressOf LaunchColorFaders)
                faderB = New Transition(Of Color)(New TransitionTypes.TransitionType_Linear(Speed), faderB.EndValue, RndColor, Nothing)
                faderC = New Transition(Of Color)(New TransitionTypes.TransitionType_Linear(Speed), faderC.EndValue, RndColor, Nothing)
                faderD = New Transition(Of Color)(New TransitionTypes.TransitionType_Linear(Speed), faderD.EndValue, RndColor, Nothing)
                Automator.Add(faderA)
                Automator.Add(faderB)
                Automator.Add(faderC)
                Automator.Add(faderD)
            End If
        End Sub

        Private Function RndColor() As Color
            Return Colors(rand.Next(0, Colors.Length))
        End Function
    End Class
End Namespace