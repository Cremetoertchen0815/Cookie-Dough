Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Megäa
    Public Class Table
        Inherits Component
        Implements IObject3D

        Public Property Distance As Single Implements IObject3D.Distance
        Public Property BoundingBox As BoundingBox Implements IObject3D.BoundingBox

        Private TableModel As Model
        Friend TransformMatrix As Matrix = Matrix.Identity

        Public Overrides Sub Initialize()
            MyBase.Initialize()

            TableModel = Entity.Scene.Content.Load(Of Model)("mesh/table")
            TransformMatrix = Matrix.CreateScale(New Vector3(3.2, 3.2, 2)) * Matrix.CreateRotationX(MathHelper.PiOver2)
            BoundingBox = VertexExtractor.CreateBoundingBox(TableModel, Matrix.CreateScale(0.01) * TransformMatrix)

            Dim handler = Entity.Scene.GetSceneComponent(Of Object3DHandler)
            If handler IsNot Nothing Then handler.Objects.Add(Me)
        End Sub

        Public Sub ClickedFunction(sender As GameRoom) Implements IObject3D.ClickedFunction
            Dim user As Player = sender.Spielers(sender.UserIndex)

            If sender.Status = GameStatus.GameActive And user.Deck.Count > 0 And Not Player.CardBeingPlaced And sender.UserIndex = sender.SpielerIndex Then
                user.LayCard(AddressOf sender.SwitchPlayer)
                sender.TotemPressedForCurrentCardSet = False
                sender.SendCardPlaced(sender.UserIndex, user.HandCard)
            End If
        End Sub
    End Class
End Namespace