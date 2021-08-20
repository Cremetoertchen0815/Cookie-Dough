Imports Cookie_Dough.Framework.Physics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input
Imports Nez.Sprites
Imports Nez.Tiled

Namespace Game.Barrelled.Players
    Public Class OtherPlayer
        Inherits CommonPlayer

        Public Overrides Property Direction As Vector3 = Vector3.Backward
        Public Overrides Property Location As Vector3
            Get
                Return GetLocation()
            End Get
            Set(value As Vector3)
                If Entity Is Nothing Then Return
                Entity.SetPosition(value.X * 3, value.Z * 3)
            End Set
        End Property

        'Misc
        Private MinimapSprite As SpriteRenderer
        Private Index As Integer

        'Movement
        Public Velocity As Vector2
        Private LocationY As Single = 4
        Private VelocityY As Single = 0
        Private Collider As BoxCollider
        Private stickPos As Vector2
        Private TrueDirection As Vector3

        Private Const SprintSpeed As Single = 150
        Private Const SneakSpeed As Single = 8
        Private Const Speed As Single = 60 '40
        Private Const Gravity As Single = 85
        Private Const Acc As Single = 180
        Private Const Dec As Single = 220

        Public Overrides Property ThreeDeeVelocity As Vector3
            Get
                Return New Vector3(stickPos.X, VelocityY, stickPos.Y)
            End Get
            Set(value As Vector3)
                stickPos = New Vector2(value.X, value.Z)
                VelocityY = value.Y
            End Set
        End Property

        Public Sub New(typ As SpielerTyp, index As Integer)
            Me.Typ = typ
            Me.Index = index
            MinimapSprite = New SpriteRenderer(Core.Content.LoadTexture("games/BR/minimap_player")).SetColor(PlayerColors(Mode)).SetRenderLayer(5)
        End Sub

        Public Sub New(typ As SpielerTyp, mode As PlayerMode, index As Integer)
            Me.New(typ, index)
            Me.Mode = mode
        End Sub


        Public Overrides Sub OnAddedToEntity()

            Mover = Entity.AddComponent(New TiledMapCollisionResolver(CollisionLayers(0), 16))
            Collider = Entity.AddComponent(New BoxCollider(12, 12))
            Entity.AddComponent(MinimapSprite)
            Entity.SetPosition(PlayerSpawn)
        End Sub

        Friend Overrides Function GetWorldMatrix(Optional rotatone As Single = 1) As Matrix
            Dim rotato As New Vector2(TrueDirection.X, -TrueDirection.Z) : rotato.Normalize()
            Dim rotation As Single = Mathf.AngleBetweenVectors(rotato, Vector2.UnitY) * -2 * rotatone
            Return Matrix.CreateScale(New Vector3(1, If(RunningMode = PlayerStatus.Sneaky, 0.6, 1), 1) * 1.3) * Matrix.CreateRotationY(-rotation + Math.PI) * Matrix.CreateTranslation(GetLocation)
        End Function

        Friend Function GetLocation() As Vector3
            If Entity Is Nothing Then Return New Vector3
            Return New Vector3(Entity.LocalPosition.X / 3, LocationY, Entity.LocalPosition.Y / 3)
        End Function

        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState
            Dim Location As Vector3 = GetLocation()
            Dim maxSpeed As Vector2

            'Interpolate direction
            TrueDirection = Vector3.Lerp(TrueDirection, Direction, 0.3)

            'Manage two-dimensional movement
            Dim movDir = New Vector3(Direction.X, 0, Direction.Z) : movDir.Normalize()
            maxSpeed = Vector2.One * Speed
            If RunningMode = PlayerStatus.Sneaky Then maxSpeed = Vector2.One * SneakSpeed
            If RunningMode = PlayerStatus.Sprinty Then maxSpeed = Vector2.One * SprintSpeed

            'Calculate player velocity for X-Axis
            If stickPos.X < 0 And Velocity.X >= maxSpeed.X * stickPos.X Then 'Move the character to the left
                Velocity.X += Acc * stickPos.X * Time.DeltaTime
            ElseIf stickPos.X > 0 And Velocity.X <= maxSpeed.X * stickPos.X Then 'Move the character to the right
                Velocity.X += Acc * stickPos.X * Time.DeltaTime
            Else  'Deccelerate & stop(if a certain minimal threshold is reached) the charcter
                If Velocity.X > 0 Then
                    If Velocity.X > Dec * Time.DeltaTime And Velocity.X - Dec * Time.DeltaTime > 0 Then Velocity.X -= Dec * Time.DeltaTime Else Velocity.X = 0
                ElseIf Velocity.X < 0 Then
                    If Velocity.X < -Dec * Time.DeltaTime And Velocity.X + Dec * Time.DeltaTime < 0 Then Velocity.X += Dec * Time.DeltaTime Else Velocity.X = 0
                End If
            End If

            'Calculate player velocity for Y-Axis
            If stickPos.Y < 0 And Velocity.Y >= maxSpeed.Y * stickPos.Y Then 'Move the character to the left
                Velocity.Y += Acc * stickPos.Y * Time.DeltaTime
            ElseIf stickPos.Y > 0 And Velocity.Y <= maxSpeed.Y * stickPos.Y Then 'Move the character to the right
                Velocity.Y += Acc * stickPos.Y * Time.DeltaTime
            Else  'Deccelerate & stop(if a certain minimal threshold is reached) the charcter
                If Velocity.Y > 0 Then
                    If Velocity.Y > Dec * Time.DeltaTime And Velocity.Y - Dec * Time.DeltaTime > 0 Then Velocity.Y -= Dec * Time.DeltaTime Else Velocity.Y = 0
                ElseIf Velocity.Y < 0 Then
                    If Velocity.Y < -Dec * Time.DeltaTime And Velocity.Y + Dec * Time.DeltaTime < 0 Then Velocity.Y += Dec * Time.DeltaTime Else Velocity.Y = 0
                End If
            End If

            'Apply gravity
            VelocityY += Gravity * Time.DeltaTime

            'Move player in 3D space
            Dim Velocity3D As Vector3 = Vector3.Zero
            Velocity3D += Velocity.Y * movDir
            Velocity3D += Velocity.X * Vector3.Cross(Vector3.Up, movDir) * New Vector3(1, 0, 1)
            If Location.Y <= 0 Then Location = New Vector3(Location.X, 0, Location.Z)
            Velocity3D += New Vector3(0, VelocityY, 0)

            'Collision
            Dim state As New TiledMapMover.CollisionState
            Dim velocity2D As Vector2 = New Vector2(Velocity3D.X, Velocity3D.Z) * -Time.DeltaTime * 2
            Mover.CollisionLayer = CollisionLayers(If(LocationY > 10, 1, 0)) 'Adapt collision layer for jump
            Mover.Move(velocity2D, Collider)
            Location = GetLocation()
            Entity.SetLocalRotation(Mathf.AngleBetweenVectors(New Vector2(movDir.X, -movDir.Z), Vector2.UnitY) * -2 + Math.PI / 2)

            'Clamp position and move Y-Pos
            LocationY = Mathf.Clamp(Location.Y - Velocity3D.Y * Time.DeltaTime, 0, 30)
        End Sub

        Friend Overrides Sub SetColor(color As Color)
            MinimapSprite.SetColor(color)
        End Sub

        Public Overrides Sub ClickedFunction(sender As IGameWindow)
            sender.PlayerPressed(Index)
        End Sub
    End Class
End Namespace