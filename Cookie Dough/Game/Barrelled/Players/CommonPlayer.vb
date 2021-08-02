Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Framework.Physics
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Tiled

Namespace Game.Barrelled.Players

    Public MustInherit Class CommonPlayer
        Inherits Component
        Implements IUpdatable, IPlayer, IObject3D

        'Common properties & implemenation of interfaces
        Public Property Bereit As Boolean Implements IPlayer.Bereit
        Public Property Connection As Connection Implements IPlayer.Connection
        Public Property Typ As SpielerTyp Implements IPlayer.Typ
        Public Property Name As String Implements IPlayer.Name
        Public Property MOTD As String Implements IPlayer.MOTD
        Public Property ID As String Implements IPlayer.ID
        Public Property CustomSound As SoundEffect() Implements IPlayer.CustomSound
        Public Property Thumbnail As Texture2D = PlaceholderFace Implements IPlayer.Thumbnail
        Public Property Mode As PlayerMode = PlayerMode.Ghost
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
        Public MustOverride Property Location As Vector3
        Public MustOverride Property Direction As Vector3
        Public Overridable Property ThreeDeeVelocity As Vector3
        Public Overridable Sub ClickedFunction(sender As IGameWindow) Implements IObject3D.ClickedFunction
            Throw New NotImplementedException
        End Sub
        Public Property Distance As Single Implements IObject3D.Distance
        Public ReadOnly Property BoundingBox As BoundingBox Implements IObject3D.BoundingBox
            Get
                Dim pts As Vector3() = New Vector3(1) {}
                Dim trans As Matrix = GetWorldMatrix(0)
                Vector3.Transform(Renderers.Renderer3D.PlayerModelExtremePoints, Matrix.CreateScale(0.003) * trans, pts)
                Return BoundingBox.CreateFromPoints(pts)
            End Get
        End Property

        Public MustOverride Sub Update() Implements IUpdatable.Update
        Friend MustOverride Function GetWorldMatrix(Optional rotato As Single = 1) As Matrix
        Friend MustOverride Sub SetColor(color As Color)


        'Collision and Misc
        <Newtonsoft.Json.JsonIgnore>
        Friend Shared CollisionLayers As TmxLayer()
        Friend Shared PlayerSpawn As Vector2
        Private Shared PlaceholderFace As Texture2D = Core.Content.LoadTexture("games/BR/face_placeholder")
        Friend RunningMode As PlayerStatus
        'Friend MatchedColor As Color
        Protected Mover As TiledMapCollisionResolver

    End Class
End Namespace