Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking

Namespace Game.DuoCard.Networking
    Public Class ExtGame
        Implements IGame

        Public Property Key As Integer Implements IGame.Key
        Public Property Name As String Implements IGame.Name
        Public Property Ended As Boolean = False Implements IGame.Ended
        Public Property Active As Boolean = False Implements IGame.Active
        Public Property HostConnection As Connection Implements IGame.HostConnection
        Public ReadOnly Property Type As GameType = GameType.BetretenVerboten Implements IGame.Type
        Private ReadOnly Property IGame_Players As IPlayer() Implements IGame.Players
            Get
                Return Players.ToArray
            End Get
        End Property

        'Houses all the players internally
        Private Players As New List(Of Player)


        '-----SERVER-----


        'Read data from client to initiate the creation of a new game
        Public Shared Function ServerSendCreateData(ReadString As Func(Of Connection, String), con As Connection, gamename As String, Key As Integer) As IGame
            Dim tmp As New ExtGame With {.HostConnection = con, .Name = gamename, .Key = Key}
            tmp.Players.Add(New Player(SpielerTyp.Local) With {.Bereit = True, .Connection = con})
            Return tmp
        End Function

        'Send joining initiation data to client
        Public Sub ServerSendJoinGlobalData(con As Connection, writer As Action(Of Connection, String)) Implements IGame.ServerSendJoinGlobalData

        End Sub

        'If the client left or lost connection, send him some data to keep him up to date
        Public Sub ServerSendJoinRejoinData(index As Integer, con As Connection, writer As Action(Of Connection, String)) Implements IGame.ServerSendJoinRejoinData
            For i As Integer = 0 To Players.Count - 1
                writer(con, CInt(Players(i).Typ))
                writer(con, Players(i).Name)
            Next
            Players(index).Connection = con
        End Sub

        'If the player is new to the round, register him and send him data about the other players
        Public Sub ServerSendJoinNujoinData(index As Integer, con As Connection, writer As Action(Of Connection, String)) Implements IGame.ServerSendJoinNujoinData
            Players.Add(New Player(SpielerTyp.Online) With {.Bereit = False, .Connection = con, .Name = con.Nick})
            For i As Integer = 0 To Players.Count - 1
                writer(con, CInt(Players(i).Typ))
                writer(con, Players(i).Name)
            Next
        End Sub

        'Get the amount of players who are ready
        Public Function GetReadyPlayerCount() As Integer Implements IGame.GetReadyPlayerCount
            Dim cnt As Integer = 0
            For Each element In Players
                If element.Bereit Then cnt += 1
            Next
            Return cnt
        End Function
        'Get the amount of players that are registered
        Public Function GetRegisteredPlayerCount() As Integer Implements IGame.GetRegisteredPlayerCount
            Return Players.Count
        End Function


        '-----CLIENT-----

        'Create new game and transmit to server(in this case, no extra data in being sent, only the default header)
        Public Shared Function CreateGame(client As Client, name As String) As Boolean
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If client.blastmode Or Not client.Connected Then Return False

            client.WriteString("create")
            client.WriteString(name)
            client.WriteString(GameType.DuoCard.ToString)
            Return client.CreateGameFinal()
        End Function

        Public Function GetLobbySize() As Integer Implements IGame.GetLobbySize
            Return 10
        End Function
    End Class
End Namespace