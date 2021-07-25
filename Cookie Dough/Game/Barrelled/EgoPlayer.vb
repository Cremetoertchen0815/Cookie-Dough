Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.Physics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Nez.Tiled

Namespace Game.Barrelled
    Public Class EgoPlayer
        Inherits Component
        Implements IUpdatable

        'Properties
        Public Property Bereit As Boolean = True
        Public Property Mode As PlayerMode = PlayerMode.Chased
        Public Property CustomSound As SoundEffect() = {SFX(3), SFX(4)}
        Public Property Thumbnail As Texture2D
        Public Property MOTD As String

        'Misc
        Friend CameraPosition As Vector3
        Friend Map As TmxMap
        Private CollisionLayers As TmxLayer()
        Private Mover As TiledMapCollisionResolver
        Private MovementBtn As VirtualJoystick
        Private JumpBtn As VirtualButton
        Friend SneakBtn As VirtualButton
        Private lastMousePos As Vector2 = Mouse.GetState.Position.ToVector2
        Private lastSpeen As Vector2
        Private lastmstate As MouseState = Mouse.GetState

        'Friend GameFocused As Boolean = True

        'Movement
        Private LocationY As Single = 4
        Private VelocityY As Single = 0
        Private Collider As BoxCollider
        Public Direction As Vector3 = Vector3.Backward
        Public DisableCollision As Boolean = True
        Public Velocity As Vector2

        'Constants
        Private Const MouseSensivity As Single = 232
        Private Const SprintSpeed As Single = 130
        Private Const SneakSpeed As Single = 8
        Private Const Speed As Single = 50 '40
        Private Const JumpHeight As Single = 50
        Private Const Gravity As Single = 85
        Private Const Acc As Single = 180
        Private Const Dec As Single = 210

        Private ReadOnly Property IUpdatable_Enabled As Boolean Implements IUpdatable.Enabled
            Get
                Return Enabled
            End Get
        End Property

        Private ReadOnly Property IUpdatable_UpdateOrder As Integer Implements IUpdatable.UpdateOrder
            Get
                Return 0
            End Get
        End Property

        Sub New(map As TmxMap)
            Me.Map = map
        End Sub

        Public Overrides Sub OnAddedToEntity()
            CollisionLayers = {Map.GetLayer(Of TmxLayer)("Collision"), Map.GetLayer(Of TmxLayer)("High")}
            Mover = Entity.AddComponent(New TiledMapCollisionResolver(Map, "Collision"))
            Collider = Entity.AddComponent(New BoxCollider(12, 12))
            Entity.AddComponent(New PrototypeSpriteRenderer(12, 12)).SetRenderLayer(5)

            'Assign virtual buttons
            MovementBtn = New VirtualJoystick(False, New VirtualJoystick.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D, Keys.W, Keys.S))
            JumpBtn = New VirtualButton(New VirtualButton.KeyboardKey(Keys.Space))
            SneakBtn = New VirtualButton(New VirtualButton.KeyboardKey(Keys.LeftShift))
        End Sub

        Friend Function GetWorldMatrix() As Matrix
            Dim rotation As Single = Mathf.AngleBetweenVectors(New Vector2(Direction.X, -Direction.Z), Vector2.UnitY) * -2
            Return Matrix.CreateScale(1, If(SneakBtn IsNot Nothing AndAlso SneakBtn.IsDown, 0.6, 1), 1) * Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(GetLocation)
        End Function

        Friend Function GetLocation() As Vector3
            Return New Vector3(Entity.LocalPosition.X / 3, LocationY, Entity.LocalPosition.Y / 3)
        End Function

        Public Sub Update() Implements IUpdatable.Update
            Dim mstate As MouseState = Mouse.GetState
            Dim Location As Vector3 = GetLocation()

            'Manage two-dimensional movement
            Dim movDir As New Vector3(Direction.X, 0, Direction.Z) : movDir.Normalize()
            Dim stickPos As Vector2 = MovementBtn.Value * New Vector2(0.75, 1)
            Dim maxSpeed As Vector2 = Vector2.One * Speed
            If SneakBtn.IsDown Then maxSpeed = Vector2.One * SneakSpeed

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
            Velocity.X = Mathf.Clamp(Velocity.X, -maxSpeed.X, maxSpeed.X) 'Clamp speed

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
            Velocity.Y = Mathf.Clamp(Velocity.Y, -maxSpeed.Y, maxSpeed.Y) 'Clamp speed

            'Apply gravity
            VelocityY += Gravity * Time.DeltaTime

            'Apply jump
            If JumpBtn.IsPressed And LocationY - VelocityY * Time.DeltaTime <= 0 Then VelocityY = -JumpHeight

            'Move player in 3D space
            Dim Velocity3D As New Vector3
            Velocity3D += Velocity.Y * movDir
            Velocity3D += Velocity.X * Vector3.Cross(Vector3.Up, movDir) * New Vector3(1, 0, 1)
            If Location.Y <= 0 And Not JumpBtn.IsPressed Then Location = New Vector3(Location.X, 0, Location.Z) : VelocityY = 0
            Velocity3D += New Vector3(0, VelocityY, 0)

            'Clamp position and move Y-Pos
            LocationY = Mathf.Clamp(Location.Y - Velocity3D.Y * Time.DeltaTime, 0, 15)

            'Collision
            Dim state As New TiledMapMover.CollisionState
            Dim velocity2D As Vector2 = New Vector2(Velocity3D.X, Velocity3D.Z) * -Time.DeltaTime * 2
            Mover.CollisionLayer = CollisionLayers(If(LocationY > 10, 1, 0)) 'Adapt collision layer for jump
            Mover.Move(velocity2D, Collider)
            Location = GetLocation()

            'Generate camera position
            Dim camShift As Vector3 = Direction : camShift.Y = 0 : camShift.Normalize() : camShift *= 0.5
            CameraPosition = Location + camShift + New Vector3(0, If(SneakBtn.IsDown, 3, 6), 0)

            'Smooth out mouse movement
            lastMousePos = Vector2.Lerp(mstate.Position.ToVector2, lastMousePos, 0.25)

            'Calculate direction from mouse
            If Core.Instance.IsActive Then
                Dim nudirection As Vector3 = Direction
                nudirection = Vector3.Transform(nudirection, Matrix.CreateFromAxisAngle(Vector3.Up, (-MathHelper.PiOver4 / MouseSensivity) * (lastMousePos.X - lastmstate.X)))
                nudirection = Vector3.Transform(nudirection, Matrix.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, nudirection), (MathHelper.PiOver4 / MouseSensivity) * (lastMousePos.Y - lastmstate.Y)))
                nudirection.Normalize()
                Direction = nudirection

                Dim pos = Core.Instance.Window.ClientBounds.Size
                Mouse.SetPosition(CInt(pos.X / 2), CInt(pos.Y / 2))
            End If

            lastmstate = Mouse.GetState
        End Sub
    End Class
End Namespace