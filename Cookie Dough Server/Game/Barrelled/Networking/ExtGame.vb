Imports Cookie_Dough_Server.Game.Barrelled.Players

Namespace Game.Barrelled.Networking
    Public Class ExtGame
        Implements IGame


        Public Property Key As Integer Implements IGame.Key
        Public Property Name As String Implements IGame.Name
        Public Property Casual As Boolean
        Public Property GaemMap As Map
        Public Property Players As CommonPlayer() = {Nothing, Nothing, Nothing, Nothing}
        Public Property Ended As EndingMode = EndingMode.Running Implements IGame.Ended
        Public Property Active As Boolean = False Implements IGame.Active
        Public Property HostConnection As Connection Implements IGame.HostConnection
        Public ReadOnly Property Type As GameType = GameType.Barrelled Implements IGame.Type
        Public Property WhiteList As String() Implements IGame.WhiteList
        Public Property Viewers As New List(Of Connection) Implements IGame.Viewers

        Private ReadOnly Property IGame_Players As IPlayer() Implements IGame.Players
            Get
                Return Players
            End Get
        End Property

        Public Sub ServerSendJoinRejoinData(index As Integer, con As Connection, writer As Action(Of Connection, String)) Implements IGame.ServerSendJoinRejoinData
            If index < 0 Then Return
            For i As Integer = 0 To Players.Length - 1
                writer(con, CInt(Players(i).Typ))
                writer(con, Players(i).Name)
            Next
            Players(index).Connection = con
            Players(index).ID = con.Identifier
        End Sub

        Public Sub ServerSendJoinNujoinData(index As Integer, con As Connection, writer As Action(Of Connection, String)) Implements IGame.ServerSendJoinNujoinData
            If index > -1 Then Players(index) = New CommonPlayer(SpielerTyp.Online) With {.Bereit = False, .Connection = con, .Name = con.Nick, .ID = con.Identifier}
            For i As Integer = 0 To Players.Length - 1
                If Players(i) IsNot Nothing Then
                    writer(con, 1.ToString)
                    writer(con, CInt(Players(i).Typ))
                    writer(con, Players(i).Name)
                Else
                    writer(con, 0.ToString)
                    writer(con, CInt(SpielerTyp.Online))
                    writer(con, "")
                End If
            Next
        End Sub

        Public Sub ServerSendJoinGlobalData(con As Connection, writer As Action(Of Connection, String)) Implements IGame.ServerSendJoinGlobalData
            writer(con, CInt(GaemMap))
            writer(con, Casual.ToString)
        End Sub

        Public Shared Function ServerSendCreateData(ReadString As Func(Of Connection, String), con As Connection, gamename As String, Key As Integer) As IGame
            'Read map from stream and resize arrays accordingly
            Dim map As Map = CInt(ReadString(con))
            Dim casual As Boolean = ReadString(con)
            Dim nugaem As New ExtGame With {.HostConnection = con, .Name = gamename, .Key = Key, .GaemMap = map, .Casual = casual}
            ReDim nugaem.Players(GetMapSize(map) - 1)
            ReDim nugaem.WhiteList(GetMapSize(map) - 1)
            Dim types As SpielerTyp() = New SpielerTyp(GetMapSize(map) - 1) {}
            'Receive player data
            For i As Integer = 0 To types.Length - 1
                types(i) = CInt(ReadString(con))
                Select Case types(i)
                    Case SpielerTyp.Local
                        Dim name As String = ReadString(con)
                        nugaem.Players(i) = New CommonPlayer(types(i)) With {.Name = name, .Bereit = True, .Connection = con, .ID = con.Identifier}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.CPU
                        Dim name As String = ReadString(con)
                        nugaem.Players(i) = New CommonPlayer(types(i)) With {.Name = name, .Bereit = True, .ID = con.Identifier}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.None
                        nugaem.Players(i) = New CommonPlayer(types(i)) With {.Bereit = True}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.Online
                        nugaem.WhiteList(i) = ReadString(con)
                End Select
            Next
            Return nugaem
        End Function

        Public Function GetReadyPlayerCount() As Integer Implements IGame.GetReadyPlayerCount
            Dim cnt As Integer = 0
            For Each element In Players
                If element IsNot Nothing AndAlso element.Bereit Then cnt += 1
            Next
            Return cnt
        End Function
        Public Function GetRegisteredPlayerCount() As Integer Implements IGame.GetRegisteredPlayerCount
            Dim cnt As Integer = 0
            For Each element In Players
                If element IsNot Nothing Then cnt += 1
            Next
            Return cnt
        End Function

        Public Function GetLobbySize() As Integer Implements IGame.GetLobbySize
            Return Players.Length
        End Function
    End Class
End Namespace