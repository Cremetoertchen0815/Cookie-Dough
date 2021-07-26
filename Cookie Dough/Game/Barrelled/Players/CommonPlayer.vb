Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.Physics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Tiled

Namespace Game.Barrelled.Players
    Public MustInherit Class CommonPlayer
        Inherits Component
        Implements IUpdatable, IPlayer

        'Common properties & implemenation of interfaces
        Public Property Bereit As Boolean Implements IPlayer.Bereit
        Public Property Connection As Connection Implements IPlayer.Connection
        Public Property Typ As SpielerTyp Implements IPlayer.Typ
        Public Property Name As String Implements IPlayer.Name
        Public Property MOTD As String Implements IPlayer.MOTD
        Public Property ID As String Implements IPlayer.ID
        Public Property CustomSound As SoundEffect() Implements IPlayer.CustomSound
        Public Property Thumbnail As Texture2D Implements IPlayer.Thumbnail
        Public MustOverride Property Location As Vector3
        Public MustOverride Property Direction As Vector3
        Public MustOverride Sub Update() Implements IUpdatable.Update
        Friend MustOverride Function GetWorldMatrix() As Matrix
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


        'Collision
        Friend Shared CollisionLayers As TmxLayer()
        Protected Mover As TiledMapCollisionResolver
    End Class
End Namespace