Imports Cookie_Dough.Framework.Networking
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
        Private Mover As TiledMapMover
        Private MovementBtn As VirtualJoystick
        Private JumpBtn As VirtualButton
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

        'Constants
        Private Const MouseSensivity As Single = 232
        Private Const Speed As Single = 120
        Private Const JumpHeight As Single = 50
        Private Const Gravity As Single = 85

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
            Mover = Entity.AddComponent(New TiledMapMover(CollisionLayers(0)))
            Collider = Entity.AddComponent(New BoxCollider(12, 12))
            Entity.AddComponent(New PrototypeSpriteRenderer(12, 12)).SetRenderLayer(5)

            'Assign virtual buttons
            MovementBtn = New VirtualJoystick(False, New VirtualJoystick.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D, Keys.W, Keys.S))
            JumpBtn = New VirtualButton(New VirtualButton.KeyboardKey(Keys.Space))
        End Sub

        Friend Function GetWorldMatrix() As Matrix
            Dim rotation As Single = Mathf.AngleBetweenVectors(New Vector2(Direction.X, -Direction.Z), Vector2.UnitY) * -2
            Return Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(GetLocation)
        End Function

        Friend Function GetLocation() As Vector3
            Return New Vector3(Entity.LocalPosition.X / 3, LocationY, Entity.LocalPosition.Y / 3)
        End Function

        Public Sub Update() Implements IUpdatable.Update
            Dim mstate As MouseState = Mouse.GetState
            Dim SPEEEN As New Vector3
            Dim Location As Vector3 = GetLocation()

            'Apply gravity
            VelocityY += Gravity * Time.DeltaTime

            'Grab jump
            If JumpBtn.IsPressed And LocationY - VelocityY * Time.DeltaTime <= 0 Then VelocityY = -JumpHeight

            'Get horizontal movement vector
            Dim movDir As New Vector3(Direction.X, 0, Direction.Z) : movDir.Normalize()
            Dim lastJoystickPos = MovementBtn.Value * New Vector2(0.5, 1)

            'Move player
            SPEEEN += lastJoystickPos.Y * movDir * Speed
            SPEEEN += lastJoystickPos.X * Vector3.Cross(Vector3.Up, movDir) * New Vector3(Speed, 0, Speed)
            If Location.Y <= 0 And Not JumpBtn.IsPressed Then Location = New Vector3(Location.X, 0, Location.Z) : VelocityY = 0
            SPEEEN += New Vector3(0, VelocityY, 0)

            ''Collision check with Colliders
            'Dim checkX As New BoundingBox(.Location + New Vector3(-2 - SPEEEN.X, 0, -2), .Location + New Vector3(2 - SPEEEN.X, 5, 2))
            'Dim checkY As New BoundingBox(.Location + New Vector3(-2, 0 - VerticalVel * delta, -2), .Location + New Vector3(2, 5 - VerticalVel * delta, 2))
            'Dim checkZ As New BoundingBox(.Location + New Vector3(-2, 0, -2 - SPEEEN.Z), .Location + New Vector3(2, 5, 2 - SPEEEN.Z))
            'For Each cl In Colliders
            '    If checkX.Intersects(cl) Then SPEEEN.X = 0
            '    If checkY.Intersects(cl) Then SPEEEN.Y = 0 : VerticalVel = 0
            '    If checkZ.Intersects(cl) Then SPEEEN.Z = 0
            'Next

            'Clamp and move Y-Pos
            LocationY = Mathf.Clamp(Location.Y - SPEEEN.Y, 0, 15)

            'Enable/Disable collision when jumping

            'Collision
            Dim state As New TiledMapMover.CollisionState
            Dim velocity2D As Vector2 = New Vector2(SPEEEN.X, SPEEEN.Z) * -Time.DeltaTime
            'Console.WriteLine(SPEEEN.ToString.ToString & velocity2D.ToString)
            Mover.CollisionLayer = CollisionLayers(If(LocationY > 10, 1, 0)) 'Adapt collision layer for jump
            Mover.Move(velocity2D, Collider, state)
            Location = GetLocation()

            'Generate view matrix and ray
            Dim camShift As Vector3 = Direction : camShift.Y = 0 : camShift.Normalize() : camShift *= 0.5
            CameraPosition = Location + camShift + New Vector3(0, 6, 0)

            'Smooth out mouse movement
            lastMousePos = Vector2.Lerp(mstate.Position.ToVector2, lastMousePos, 0.2)

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