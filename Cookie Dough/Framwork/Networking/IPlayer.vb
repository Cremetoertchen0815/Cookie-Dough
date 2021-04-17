Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

Namespace Framework.Networking
    Public Interface IPlayer
        Property Connection As Connection
        Property Bereit As Boolean
        Property Typ As SpielerTyp
        Property Name As String
        Property CustomSound As SoundEffect
        Property Thumbnail As Texture2D
    End Interface
End Namespace