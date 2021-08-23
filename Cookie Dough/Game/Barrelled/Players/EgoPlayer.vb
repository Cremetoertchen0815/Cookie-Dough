Imports Cookie_Dough.Framework.Physics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Input
Imports Nez.Sprites
Imports Nez.Tiled

Namespace Game.Barrelled.Players
    Public Class EgoPlayer
        Inherits CommonPlayer


        'Misc
        Friend PrisonEnabled As Boolean
        Friend CameraPosition As Vector3
        Private lastMousePos As Vector2 = Mouse.GetState.Position.ToVector2
        Private lastmstate As MouseState = Mouse.GetState
        Private movDir As Vector3
        Private MinimapSprite As SpriteRenderer

        'Virtual game pad
        Private MovementBtn As VirtualJoystick
        Private JumpBtn As VirtualButton
        Private SneakBtn As VirtualButton
        Private SprintBtn As VirtualButton

        'Movement
        Private LocationY As Single = 4
        Private VelocityY As Single = 0
        Private Collider As BoxCollider
        Private stickPos As Vector2
        Public Velocity As Vector2
        Public SprintLeft As Single = 1
        Public Focused As Boolean = True
        Public CanMove As Boolean = True

        'Audio shit
        Private Sounds As SoundEffect()
        Friend SoundRunCounter As Single

        'Constants
        Private Const MouseSensivity As Single = 232
        Private Const SprintSpeed As Single = 150
        Private Const SneakSpeed As Single = 8
        Private Const Speed As Single = 60 '40
        Private Const JumpHeight As Single = 50
        Private Const Gravity As Single = 85
        Private Const Acc As Single = 180
        Private Const Dec As Single = 220
        Private Const SprintMeterDrain As Single = 0.1

        Public Overrides Property Location As Vector3
            Get
                Return New Vector3(Entity.LocalPosition.X / 3, LocationY, Entity.LocalPosition.Y / 3)
            End Get
            Set(value As Vector3)
                Throw New NotImplementedException()
            End Set
        End Property


        Public Overrides Property ThreeDeeVelocity As Vector3
            Get
                Return New Vector3(stickPos.X, VelocityY, stickPos.Y)
            End Get
            Set(value As Vector3)
                Throw New NotImplementedException()
            End Set
        End Property
        Public Overrides Property Direction As Vector3 = Vector3.Backward

        Public Sub New(typ As SpielerTyp)
            Me.Typ = typ
            Bereit = True
            PrisonEnabled = True
            MinimapSprite = New SpriteRenderer(Core.Content.LoadTexture("games/BR/minimap_player")).SetRenderLayer(5)
        End Sub

        Public Sub New(typ As SpielerTyp, mode As PlayerMode)
            Me.New(typ)
            Me.Mode = mode
            If mode = PlayerMode.Ghost Then PrisonEnabled = False
        End Sub

        Public Overrides Sub OnAddedToEntity()
            Mover = Entity.AddComponent(New TiledMapCollisionResolver(CollisionLayers(0)))
            Collider = Entity.AddComponent(New BoxCollider(12, 12))
            Entity.AddComponent(MinimapSprite)
            Entity.SetPosition(PlayerSpawn)

            'Load audio
            Sounds = {Entity.Scene.Content.LoadSoundEffect("sfx/step_1"),
                      Entity.Scene.Content.LoadSoundEffect("sfx/step_2"),
                      Entity.Scene.Content.LoadSoundEffect("sfx/step_3"),
                      Entity.Scene.Content.LoadSoundEffect("sfx/step_4"),
                      Entity.Scene.Content.LoadSoundEffect("sfx/jump")}

            'Assign virtual buttons
            MovementBtn = New VirtualJoystick(False, New VirtualJoystick.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D, Keys.W, Keys.S))
            JumpBtn = New VirtualButton(New VirtualButton.KeyboardKey(Keys.Space))
            SneakBtn = New VirtualButton(New VirtualButton.KeyboardKey(Keys.LeftShift))
            SprintBtn = New VirtualButton(New VirtualButton.KeyboardKey(Keys.LeftControl))
        End Sub

        Friend Overrides Function GetWorldMatrix(Optional rotato As Single = 1) As Matrix
            Return Matrix.CreateScale(1, If(RunningMode = PlayerStatus.Sneaky, 0.6, 1), 1) * Matrix.CreateTranslation(Location)
        End Function

        Public Overrides Sub Update()
            Dim mstate As MouseState = Mouse.GetState
            Dim Location As Vector3 = Me.Location
            Dim maxSpeed As Vector2

            If Focused Then
                'Sneaking and sprinting
                RunningMode = PlayerStatus.Normal
                If SneakBtn.IsDown And Location.Y <= 0 Then RunningMode = PlayerStatus.Sneaky
                If SprintBtn.IsDown And SprintLeft > 0 Then RunningMode = PlayerStatus.Sprinty
                If RunningMode = PlayerStatus.Sprinty Then SprintLeft = Math.Max(SprintLeft - SprintMeterDrain * Time.DeltaTime, 0)

                'Manage two-dimensional movement
                movDir = New Vector3(Direction.X, 0, Direction.Z) : movDir.Normalize()
                stickPos = MovementBtn.Value * New Vector2(0.75, 1)
                maxSpeed = Vector2.One * Speed
                If RunningMode = PlayerStatus.Sneaky Then maxSpeed = Vector2.One * SneakSpeed
                If RunningMode = PlayerStatus.Sprinty Then maxSpeed = Vector2.One * SprintSpeed

                'Apply jump
                If JumpBtn.IsPressed And LocationY - VelocityY * Time.DeltaTime <= 0 Then VelocityY = -JumpHeight : Sounds(4).Play()
            Else
                stickPos = Vector2.Zero
            End If

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
            If Location.Y <= 0 And Not JumpBtn.IsPressed Then Location = New Vector3(Location.X, 0, Location.Z) : VelocityY = 0
            Velocity3D += New Vector3(0, VelocityY, 0)

            'Clamp position and move on Y-Axis
            If CanMove Then LocationY = Mathf.Clamp(Location.Y - Velocity3D.Y * Time.DeltaTime, 0, 30)

            'Collision
            Dim state As New TiledMapMover.CollisionState
            Dim velocity2D As Vector2 = New Vector2(Velocity3D.X, Velocity3D.Z) * -Time.DeltaTime * 2
            Mover.CollisionLayer = CollisionLayers(If(LocationY > 10, 1, 0)) 'Adapt collision layer for jump
            If CanMove Then Mover.Move(velocity2D, Collider)

            'Clamp 2D coords
            If Mode <> PlayerMode.Ghost AndAlso PrisonEnabled Then Entity.Position = New Vector2(Mathf.Clamp(Entity.Position.X, PrisonPosition.Left + 5, PrisonPosition.Right - 5), Mathf.Clamp(Entity.Position.Y, PrisonPosition.Top + 5, PrisonPosition.Bottom - 5))
            Location = Me.Location

            'Generate camera position
            Dim camShift As Vector3 = Direction : camShift.Y = 0 : camShift.Normalize() : camShift *= 0.5
            CameraPosition = Location + camShift + New Vector3(0, If(RunningMode = PlayerStatus.Sneaky, 3, 6), 0)

            'Update Audio Listener
            AudioListener.Forward = movDir
            AudioListener.Position = Location
            AudioListener.Up = Vector3.Up
            AudioListener.Velocity = Velocity3D

            'Play running sounds
            Dim speeeeeed As Single = Velocity.Length
            If speeeeeed > 15 And Location.Y = 0 Then
                SoundRunCounter += Time.DeltaTime
                If SoundRunCounter > SoundRunFactor / Math.Sqrt(speeeeeed) Then
                    Sounds(Nez.Random.Range(0, 4)).Play()
                    SoundRunCounter = 0
                End If
            Else
                SoundRunCounter = 5
            End If


                If Not Core.Instance.IsActive Or Not Focused Then Return

            'Smooth out mouse movement
            lastMousePos = Vector2.Lerp(mstate.Position.ToVector2, lastMousePos, 0.25)

            'Calculate direction from mouse
            Dim nudirection As Vector3 = Direction
            nudirection = Vector3.Transform(nudirection, Matrix.CreateFromAxisAngle(Vector3.Up, (-MathHelper.PiOver4 / MouseSensivity) * (lastMousePos.X - lastmstate.X)))
            nudirection = Vector3.Transform(nudirection, Matrix.CreateFromAxisAngle(Vector3.Cross(Vector3.Up, nudirection), (MathHelper.PiOver4 / MouseSensivity) * (lastMousePos.Y - lastmstate.Y)))
            nudirection.Normalize()
            Direction = nudirection
            Entity.SetLocalRotation(Mathf.AngleBetweenVectors(New Vector2(movDir.X, -movDir.Z), Vector2.UnitY) * -2 + Math.PI / 2)

            Dim pos = Core.Instance.Window.ClientBounds.Size
            Mouse.SetPosition(pos.X / 2, pos.Y / 2)

            lastmstate = Mouse.GetState
        End Sub

        Friend Overrides Sub SetColor(color As Color)
            MinimapSprite.SetColor(color)
        End Sub
    End Class
End Namespace