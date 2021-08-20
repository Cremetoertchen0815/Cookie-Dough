Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.Physics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Tiled

Namespace Game.Barrelled.Players

    ''' <summary>
    ''' Provides an inheritable structure as base for different player connectivity methods.
    ''' </summary>
    Public MustInherit Class CommonPlayer
        Inherits Component
        Implements IUpdatable, IPlayer, IObject3D

        'Common properties & implemenation of interfaces

        ''' <summary>
        ''' Indicates whether the player has a stable connection and has joined the game.
        ''' </summary>
        Public Property Bereit As Boolean Implements IPlayer.Bereit
        ''' <summary>
        ''' Represents the IO-connection of the player to the server.
        ''' </summary>
        Public Property Connection As Connection Implements IPlayer.Connection
        ''' <summary>
        ''' Declares whether the player is controlled locally, remotely by the server, by an AI or not at all(as a placeholder).
        ''' </summary>
        Public Property Typ As SpielerTyp Implements IPlayer.Typ
        ''' <summary>
        ''' Identifies the player for the other players.
        ''' </summary>
        Public Property Name As String Implements IPlayer.Name
        ''' <summary>
        ''' Haha funny
        ''' </summary>
        Public Property MOTD As String Implements IPlayer.MOTD
        ''' <summary>
        ''' Identifies the player for the server and host.
        ''' </summary>
        Public Property ID As String Implements IPlayer.ID
        ''' <summary>
        ''' A custom defined sound that can be player under different circumstances.
        ''' </summary>
        Public Property CustomSound As SoundEffect() = {SFX(3), SFX(4)} Implements IPlayer.CustomSound
        ''' <summary>
        ''' A custom defined thumbnail that can be player under different circumstances.
        ''' </summary>
        Public Property Thumbnail As Texture2D = PlaceholderFace Implements IPlayer.Thumbnail
        ''' <summary>
        ''' Identifies the role of the player in Barrelled.
        ''' </summary>
        Public Property Mode As PlayerMode = PlayerMode.Ghost
        ''' <summary>
        ''' Indicates whether the player running normally, sneaking or sprinting.
        ''' </summary>
        Public Property RunningMode As PlayerStatus

        'Implementing properties
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
        Public Property Distance As Single Implements IObject3D.Distance
        Public ReadOnly Property BoundingBox As BoundingBox Implements IObject3D.BoundingBox
            Get
                Dim pts As Vector3() = New Vector3(1) {}
                Dim trans As Matrix = GetWorldMatrix(0)
                Vector3.Transform(Renderers.Renderer3D.PlayerModelExtremePoints, Matrix.CreateScale(0.003) * trans, pts)
                Return BoundingBox.CreateFromPoints(pts)
            End Get
        End Property
        Public Property UserAlternateTrigger As Boolean = False Implements IObject3D.UserAlternateTrigger

        'MustOverride Properties
        Public MustOverride Property Location As Vector3
        Public MustOverride Property Direction As Vector3
        Public Overridable Property ThreeDeeVelocity As Vector3


        Public Overridable Sub ClickedFunction(sender As IGameWindow) Implements IObject3D.ClickedFunction
            Throw New NotImplementedException
        End Sub

        'MustOverride functions
        Public MustOverride Sub Update() Implements IUpdatable.Update
        Friend MustOverride Function GetWorldMatrix(Optional rotato As Single = 1) As Matrix
        Friend MustOverride Sub SetColor(color As Color)


        'Collision and Shared fields
        Friend Shared CollisionLayers As TmxLayer()
        Friend Shared PlayerSpawn As Vector2
        Friend Shared PrisonPosition As Rectangle
        Private Shared PlaceholderFace As Texture2D = Core.Content.LoadTexture("games/BR/face_placeholder")
        Protected Mover As TiledMapCollisionResolver

    End Class
End Namespace