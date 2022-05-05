﻿Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Game.Common
Imports Cookie_Dough_Server.Game.Common

Namespace Game.DuoCard.Networking
    Public Class ExtGame
        Implements IGame

        Public Property Key As Integer Implements IGame.Key
        Public Property Name As String Implements IGame.Name
        Public Property Size As Integer
        Public Property Casual As Boolean
        Public Property Players As BaseCardPlayer() = {Nothing, Nothing, Nothing, Nothing}
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
            If index > -1 Then Players(index) = New BaseCardPlayer(SpielerTyp.Online) With {.Bereit = False, .Connection = con, .Name = con.Nick, .ID = con.Identifier}
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
            writer(con, Size)
            writer(con, Casual.ToString)
        End Sub

        Public Shared Function ServerSendCreateData(ReadString As Func(Of Connection, String), con As Connection, gamename As String, Key As Integer) As IGame
            'Read map from stream and resize arrays accordingly
            Dim size As Integer = ReadString(con)
            Dim casual As Boolean = ReadString(con)
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
                        nugaem.Players(i) = New BaseCardPlayer(types(i)) With {.Name = name, .Bereit = True, .Connection = con, .ID = con.Identifier}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.CPU
                        Dim name As String = ReadString(con)
                        nugaem.Players(i) = New BaseCardPlayer(types(i)) With {.Name = name, .Bereit = True, .ID = con.Identifier}
                        nugaem.WhiteList(i) = con.Identifier
                    Case SpielerTyp.None
                        nugaem.Players(i) = New BaseCardPlayer(types(i)) With {.Bereit = True}
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