Namespace Game.Megäa
    Public Class Player
        Implements IPlayer

        Public Property Connection As Connection Implements IPlayer.Connection
        Public Property Bereit As Boolean = True Implements IPlayer.Bereit
        Public Property Typ As SpielerTyp = SpielerTyp.Local Implements IPlayer.Typ
        Public Property Name As String = "Soos" Implements IPlayer.Name
        Public Property PositionLocked As Boolean = False
        'Game data
        Public Property Deck As New List(Of Card)
        Public Property HandCard As Card = Card.NoCard
        Public Property TableCard As Card = Card.NoCard

        Public Property ID As String Implements IPlayer.ID
        Public Property MOTD As String Implements IPlayer.MOTD

        Public Sub New(type As SpielerTyp)
            Typ = type
        End Sub

    End Class
End Namespace