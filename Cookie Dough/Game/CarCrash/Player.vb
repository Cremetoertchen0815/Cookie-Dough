Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.CarCrash
    Public Class Player
        Implements IPlayer

        Public Property Connection As Connection Implements IPlayer.Connection
        Public Property Bereit As Boolean Implements IPlayer.Bereit
        Public Property Typ As SpielerTyp Implements IPlayer.Typ
        Public Property Name As String Implements IPlayer.Name
        Public Property MOTD As String Implements IPlayer.MOTD
        Public Property ID As String Implements IPlayer.ID
        Public Property CustomSound As SoundEffect() Implements IPlayer.CustomSound
        Public Property Thumbnail As Texture2D Implements IPlayer.Thumbnail
        Public Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub
    End Class
End Namespace