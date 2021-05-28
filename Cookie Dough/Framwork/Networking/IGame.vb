Imports System.Collections.Generic

Namespace Framework.Networking
    Public Interface IGame
        Property Key As Integer
        Property Name As String
        Property Ended As EndingMode
        Property Active As Boolean
        ReadOnly Property Players As IPlayer()
        Property Viewers As List(Of Connection)
        ReadOnly Property Type As GameType
        Property HostConnection As Connection
        Property WhiteList As String()

        Function GetReadyPlayerCount() As Integer
        Function GetRegisteredPlayerCount() As Integer
        Function GetLobbySize() As Integer

        'Server functions, see specific class for more information
        Sub ServerSendJoinRejoinData(index As Integer, con As Connection, writer As Action(Of Connection, String))
        Sub ServerSendJoinNujoinData(index As Integer, con As Connection, writer As Action(Of Connection, String))
        Sub ServerSendJoinGlobalData(con As Connection, writer As Action(Of Connection, String))

    End Interface
End Namespace