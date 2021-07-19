Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking

Namespace Game.DuoCard.Networking
    Public Class ExtGame
        Implements IGame

        Public Property Key As Integer Implements IGame.Key
        Public Property Name As String Implements IGame.Name
        Public Property Size As Integer
        Public Property Casual As Boolean
        Public Property Players As Player() = {Nothing, Nothing, Nothing, Nothing}
        Public Property Ended As EndingMode = EndingMode.Running Implements IGame.Ended
        Public Property Active As Boolean = False Implements IGame.Active
        Public Property HostConnection As Connection Implements IGame.HostConnection
        Public ReadOnly Property Type As GameType = GameType.DuoCard Implements IGame.Type
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
            If index > -1 Then Players(index) = New Player(SpielerTyp.Online) With {.Bereit = False, .Connection = con, .Name = con.Nick, .ID = con.Identifier}
            For i As Integer = 0 To Players.Length - 1
                If Players(i) IsNot Nothing Then
                    writer(con, CInt(Players(i).Typ))
                    writer(con, Players(i).Name)
                Else
                    writer(con, CInt(SpielerTyp.Online))
                    writer(con, "")
                End If
            Next
        End Sub

        Public Sub ServerSendJoinGlobalData(con As Connection, writer As Action(Of Connection, String)) Implements IGame.ServerSendJoinGlobalData
            writer(con, CInt(Size))
            writer(con, Casual.ToString)
        End Sub

        Public Shared Function ServerSendCreateData(ReadString As Func(Of Connection, String), con As Connection, gamename As String, Key As Integer) As IGame
            'Read map from stream and resize arrays accordingly
            Dim size As Integer = CInt(ReadString(con))
            Dim casual As Boolean = CBool(ReadString(con))
            Dim nugaem As New ExtGame With {.HostConnection = con, .Name = gamename, .Key = Key, .Size = size, .Casual = casual}
            ReDim nugaem.Players(size - 1)
            ReDim nugaem.WhiteList(size - 1)
            Dim types As SpielerTyp() = New SpielerTyp(size - 1) {}
            'Receive player data
            For i As Integer = 0 To types.Length - 1
                types(i) = CInt(ReadString(con))
                Select Case types(i)
                    Case SpielerTyp.Local
                        Dim name As String = ReadString(con)
                        nugaem.Players(i) = New Player(types(i)) With {.Name = name, .Bereit = True, .Connection = con, .ID = con.Identifier}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.CPU
                        Dim name As String = ReadString(con)
                        nugaem.Players(i) = New Player(types(i)) With {.Name = name, .Bereit = True, .ID = con.Identifier}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.None
                        nugaem.Players(i) = New Player(types(i)) With {.Bereit = True}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.Online
                        nugaem.WhiteList(i) = ReadString(con)
                End Select
            Next
            Return nugaem
        End Function

        Public Shared Function CreateGame(client As Client, name As String, size As Integer, types As Player(), whitelist As String(), casual As Boolean) As Boolean
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If client.blastmode Or Not client.Connected Then Return False

            client.WriteString("create")
            client.WriteString(name)
            client.WriteString(GameType.DuoCard.ToString)
            client.WriteString(size.ToString)
            client.WriteString(casual.ToString)
            For i As Integer = 0 To size - 1
                client.WriteString(CInt(types(i).Typ).ToString) 'Send player type
                If types(i).Typ = SpielerTyp.Online Then client.WriteString(whitelist(i)) 'Send whitelist slot
                If types(i).Typ <> SpielerTyp.Online And types(i).Typ <> SpielerTyp.None Then client.WriteString(types(i).Name) : types(i).ID = client.UniqueIdentifier 'Send name
            Next
            Return client.CreateGameFinal()
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