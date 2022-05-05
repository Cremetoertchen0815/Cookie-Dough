Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Megäa
    Public Class Totem
        Inherits Component
        Implements IObject3D, IUpdatable

        Public Property Distance As Single Implements IObject3D.Distance
        Public Property BoundingBox As BoundingBox Implements IObject3D.BoundingBox
        Private ReadOnly Property IUpdatable_Enabled As Boolean = True Implements IUpdatable.Enabled
        Private ReadOnly Property IUpdatable_UpdateOrder As Integer = 0 Implements IUpdatable.UpdateOrder


        Private TotemModel As Model
        Private Location As Vector3
        Friend TransformMatrix As Matrix = Matrix.Identity

        'Animations
        Private TransY As New Transition(Of Single)
        Private TransRotation As New Transition(Of Vector3)

        Public Overrides Sub Initialize()
            MyBase.Initialize()

            TotemModel = Entity.Scene.Content.Load(Of Model)("mesh/totem")
            BoundingBox = VertexExtractor.CreateBoundingBox(TotemModel, Matrix.CreateScale(0.0028, 0.0028, 0.008) * Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateTranslation(0, 3.62F, 0))

            Dim handler = Entity.Scene.GetSceneComponent(Of Object3DHandler)
            If handler IsNot Nothing Then handler.Objects.Add(Me)
        End Sub

        Public Sub ClickedFunction(sender As GameRoom) Implements IObject3D.ClickedFunction
            sender.TotemClicked(sender.UserIndex)
        End Sub

        Public Sub Update() Implements IUpdatable.Update
            Dim Rotation As Vector3 = TransRotation.Value
            TransformMatrix = Matrix.CreateScale(0.28, 0.28, 0.8) * Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateFromYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z) * Matrix.CreateTranslation(Location.X, Location.Y + 3.62F + TransY.Value, Location.Z)
        End Sub

    End Class
End Namespace