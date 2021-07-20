Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Tiled

Namespace Game.Barrelled
    Public Class Player
        Inherits Component
        Implements IPlayer, IUpdatable

        'Properties
        Public Property Connection As Connection Implements IPlayer.Connection
        Public Property Bereit As Boolean = True Implements IPlayer.Bereit
        Public Property Typ As SpielerTyp = SpielerTyp.Local Implements IPlayer.Typ
        Public Property Mode As PlayerMode = PlayerMode.Chased
        Public Property Name As String = "Soos" Implements IPlayer.Name
        Public Property Location As Vector3 = New Vector3(0, 4, 0)
        Public Property Direction As Vector3 = Vector3.Forward
        Public Property CustomSound As SoundEffect() = {SFX(3), SFX(4)} Implements IPlayer.CustomSound
        Public Property Thumbnail As Texture2D Implements IPlayer.Thumbnail
        Public Property ID As String Implements IPlayer.ID
        Public Property MOTD As String Implements IPlayer.MOTD

        'Fields
        Friend Shared Map As TmxMap
        Private Mover As TiledMapMover
        Private AxisMover As VirtualJoystick

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

        Sub New(type As SpielerTyp)
            Me.Typ = type
            If type = SpielerTyp.Local Then
                Mover = New TiledMapMover(Map.TileLayers(0))
                AxisMover = New VirtualJoystick(False, New VirtualJoystick.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Input.Keys.A, Input.Keys.D, Input.Keys.W, Input.Keys.S))
            End If
        End Sub

        Friend Function GetRelativeAngle(Optional preangle As Single = 0) As Single
            Dim cardPosUnit As New Vector2(Location.X, Location.Z)
            cardPosUnit.Normalize()
            Dim cardRotation As Single = Mathf.AngleBetweenVectors(cardPosUnit, Vector2.UnitY) - preangle
            If cardRotation < 0 Then cardRotation += CSng(Math.Floor(-cardRotation / MathHelper.TwoPi) + 1) * MathHelper.TwoPi
            If cardRotation > 0 Then cardRotation = cardRotation Mod MathHelper.TwoPi
            Return cardRotation
        End Function

        Friend Function GetWorldMatrix() As Matrix
            Dim rotation As Single = Mathf.AngleBetweenVectors(New Vector2(Direction.X, -Direction.Z), Vector2.UnitY) * -2
            Return Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(Location)
        End Function

        Public Sub Update() Implements IUpdatable.Update

        End Sub
    End Class
End Namespace