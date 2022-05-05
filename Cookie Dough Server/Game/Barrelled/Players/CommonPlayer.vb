
Namespace Game.Barrelled.Players

    ''' <summary>
    ''' Provides an inheritable structure as base for different player connectivity methods.
    ''' </summary>
    Public Class CommonPlayer
        Implements IPlayer

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
        ''' Identifies the role of the player in Barrelled.
        ''' </summary>
        Public Property Mode As PlayerMode = PlayerMode.Ghost
        ''' <summary>
        ''' Indicates whether the player running normally, sneaking or sprinting.
        ''' </summary>
        Public Property RunningMode As PlayerStatus



        Public Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub
    End Class
End Namespace