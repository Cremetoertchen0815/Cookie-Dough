Imports Cookie_Dough.Framework.Physics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input
Imports Nez.Tiled

Namespace Game.Barrelled.Players
    Public Class OtherPlayer
        Inherits CommonPlayer
        Public Overrides Property Location As Vector3 = New Vector3(0, 4, 0)
        Public Overrides Property Direction As Vector3 = Vector3.Backward

        'Movement
        Private LocationY As Single = 4
        Private Collider As BoxCollider
        Public Velocity3D As Vector3
        Public RunningMode As PlayerStatus


        Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub

        Public Overrides Sub OnAddedToEntity()

            Mover = Entity.AddComponent(New TiledMapCollisionResolver(CollisionLayers(0), 16))
            Collider = Entity.AddComponent(New BoxCollider(12, 12))
            Entity.AddComponent(New PrototypeSpriteRenderer(15, 15) With {.Color = Color.Red}).SetRenderLayer(5)
            Entity.SetPosition(PlayerSpawn)
        End Sub

        Friend Overrides Function GetWorldMatrix() As Matrix
            Dim rotation As Single = Mathf.AngleBetweenVectors(New Vector2(Direction.X, -Direction.Z), Vector2.UnitY) * -2
            Return Matrix.CreateScale(1, If(RunningMode = PlayerStatus.Sneaky, 0.6, 1), 1) * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(GetLocation)
        End Function

        Friend Function GetLocation() As Vector3
            If Entity Is Nothing Then Return New Vector3
            Return New Vector3(Entity.LocalPosition.X / 3, LocationY, Entity.LocalPosition.Y / 3)
        End Function

        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState
            Dim Location As Vector3 = GetLocation()

            'Clamp position and move Y-Pos
            LocationY = Mathf.Clamp(Location.Y - Velocity3D.Y * Time.DeltaTime, 0, 15)

            'Collision
            Dim state As New TiledMapMover.CollisionState
            Dim velocity2D As Vector2 = New Vector2(Velocity3D.X, Velocity3D.Z) * -Time.DeltaTime * 2
            Mover.CollisionLayer = CollisionLayers(If(LocationY > 10, 1, 0)) 'Adapt collision layer for jump
            Mover.Move(velocity2D, Collider)
            Location = GetLocation()
        End Sub
    End Class
End Namespace