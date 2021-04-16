﻿Imports System.Collections.Generic
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Namespace Framework.Networking
    Module Server
        Public Property ServerActive As Boolean

        Public Const Port As Integer = 187
        Private MainThread As Thread
        Private server As TcpListener
        Private client As New TcpClient
        Private endpoint As IPEndPoint = New IPEndPoint(IPAddress.Any, Port)
        Private list As New List(Of Connection)
        Private games As New Dictionary(Of Integer, IGame)
        Private RNG As New System.Random

        Public Sub StartServer()
            MainThread = New Thread(AddressOf ServerMainSub)
            MainThread.Start()
            ServerActive = True
            Directory.CreateDirectory("Cache\server\")
        End Sub

        Private Sub SendToAllClients(ByVal s As String)
            For Each c As Connection In list ' an alle clients weitersenden.
                Try
                    c.StreamW.WriteLine(s)
                Catch
                End Try
            Next
        End Sub

        Private Sub ServerMainSub()
            'Starte Server
            Try
                server = New TcpListener(endpoint)
                server.Start()


                Do ' wir warten auf eine neue verbindung...
                    Try
                        client = server.AcceptTcpClient
                        client.LingerState.Enabled = True
                        client.LingerState.LingerTime = 1000
                        Dim c As New Connection ' und erstellen für die neue verbindung eine neue connection...
                        c.Stream = client.GetStream
                        c.StreamR = New StreamReader(c.Stream)
                        c.StreamW = New StreamWriter(c.Stream) With {.AutoFlush = True}
                        list.Add(c) ' und fügen sie der liste der clients hinzu.
                        ' falls alle anderen das auch lesen sollen können, an alle clients weiterleiten. siehe SendToAllClients
                        Dim t As New Thread(AddressOf ListenToConnection)
                        t.Start(c)
                    Catch
                    End Try
                Loop

            Catch ex As Exception
                Microsoft.VisualBasic.MsgBox("Other server already active!")
            End Try

        End Sub

        Friend Sub StopServer()
            Try
                If ServerActive Then
                    ServerActive = False
                    server.Stop()
                End If
            Catch
            End Try
        End Sub

        Friend Sub FillListWithConnectedUsers(ByRef lst As List(Of String))
            For Each element In list
                lst.Add(element.Nick)
            Next
        End Sub

        Private Sub ListenToConnection(con As Connection)
            Try
                WriteString(con, "Hello there!")
                If Not ReadString(con) = "Wassup?" Then Exit Try
                WriteString(con, "What's your name?")
                Dim tmpusr As String = ReadString(con)
                If AlreadyContainsNickname(tmpusr) Then WriteString(con, "Sorry m8! Username already taken") : Exit Try
                If Not FilenameIsOK(tmpusr) Then WriteString(con, "Sorry m8! Invalid username") : Exit Try
                con.Nick = tmpusr
                WriteString(con, "Okeydokey!")
                'Grab thumbnail
                WriteString(con, "What's your thumbnail?")
                con.IdentThumbnail = CType(ReadString(con), IdentType)
                If con.IdentThumbnail = IdentType.Custom Then
                    Dim data = ReadString(con)
                    File.WriteAllBytes("Cache\server\" & con.Nick & ".png", Convert.FromBase64String(data))
                End If
                'Grab ident sound
                WriteString(con, "What's your sound?")
                con.IdentSound = CType(ReadString(con), IdentType)
                If con.IdentSound = IdentType.Custom Then
                    Dim len = CInt(ReadString(con))
                    Dim data = ReadString(con)
                    File.WriteAllBytes("Cache\server\" & con.Nick & ".wav", Convert.FromBase64String(data))
                End If
                WriteString(con, "Alrighty!")

                Do
                    Select Case ReadString(con)
                        Case "list"
                            'Implement that a lobby is viewable for the player who left only!!!
                            For Each element In games
                                If element.Value.GetReadyPlayerCount < element.Value.GetLobbySize Then
                                    WriteString(con, element.Key.ToString)
                                    WriteString(con, element.Value.Name.ToString)
                                    WriteString(con, element.Value.Type.ToString)
                                    WriteString(con, element.Value.GetRegisteredPlayerCount.ToString)
                                End If
                            Next
                            WriteString(con, "That's it!")
                        Case "join"
                            Try
                                Dim id As Integer = CInt(ReadString(con))
                                Dim gaem As IGame = games(id)
                                Dim index As Integer = -1
                                gaem.ServerSendJoinGlobalData(con, AddressOf WriteString)
                                If gaem.GetRegisteredPlayerCount >= gaem.GetLobbySize Then
                                    If IsRejoining(gaem, con.Nick, index) Then
                                        'Is Rejoining
                                        If index = -1 Then Throw New NotImplementedException
                                        WriteString(con, index)
                                        gaem.ServerSendJoinRejoinData(index, con, AddressOf WriteString)
                                        WriteString(con, "Rejoin")
                                    Else
                                        Throw New NotImplementedException
                                    End If
                                Else
                                    'Is joining from scratch
                                    For i As Integer = 0 To gaem.Players.Length - 1
                                        If gaem.Players(i) Is Nothing Then index = i : Exit For
                                    Next
                                    If index = -1 Then Throw New NotImplementedException
                                    WriteString(con, index.ToString)
                                    gaem.ServerSendJoinNujoinData(index, con, AddressOf WriteString)
                                    WriteString(con, "Nujoin")
                                End If

                                'Check if rejoining
                                If ReadString(con) <> "Okidoki!" Then Throw New NotImplementedException
                                WriteString(con, "LET'S HAVE A BLAST!")
                                gaem.Players(index).Bereit = True
                                EnterJoinMode(con, gaem, index)
                            Catch ex As Exception
                                WriteString(con, "Sorry m8!")
                            End Try
                        Case "create"
                            Try
                                Dim gamename As String = ReadString(con)
                                Dim key As Integer = RNG.Next()
                                Dim type As GameType
                                If Not [Enum].TryParse(ReadString(con), type) Then Throw New NotImplementedException
                                Dim nugaem As IGame = CallFittingMethod(type, con, gamename, key)
                                If ReadString(con) <> "Okidoki!" Then Throw New NotImplementedException
                                games.Add(key, nugaem)
                                WriteString(con, "LET'S HAVE A BLAST!")
                                EnterCreateMode(con, nugaem)
                            Catch ex As Exception
                                WriteString(con, "Sorry m8!")
                            End Try
                        Case "membercount"
                            WriteString(con, list.Count)
                        Case Else
                            Console.WriteLine("sos")
                    End Select
                Loop

                If con.Stream.CanWrite Then
                    WriteString(con, "Bye!")
                    con.Stream.Close()
                End If
            Catch ' die aktuelle überwachte verbindung hat sich wohl verabschiedet.
            End Try

            list.Remove(con)

        End Sub
        Private Function RandomString(len As Integer) As String
            Dim s As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
            Dim sb As New StringBuilder
            For i As Integer = 1 To len
                Dim idx As Integer = Nez.Random.Range(0, s.Length)
                sb.Append(s.Substring(idx, 1))
            Next
            Return sb.ToString()
        End Function


        Private Function CallFittingMethod(type As GameType, con As Connection, gamename As String, Key As Integer) As IGame
            Select Case type
                Case GameType.BetretenVerboten
                    Return Game.BetretenVerboten.Networking.ExtGame.ServerSendCreateData(AddressOf ReadString, con, gamename, Key)
                Case GameType.Megäa
                    Return Game.Megäa.Networking.ExtGame.ServerSendCreateData(AddressOf ReadString, con, gamename, Key)
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function IsRejoining(gaem As IGame, nick As String, ByRef index As Integer) As Boolean
            If gaem Is Nothing Then Return False
            For i As Integer = 0 To gaem.Players.Length - 1
                If gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Name = nick Then index = i : Return True
            Next
            Return False
        End Function


        Private Function ReadString(con As Connection) As String
            Dim tmp As String = con.StreamR.ReadLine
            Console.WriteLine("[I]" & tmp)
            If tmp = "I'm outta here!" Then Throw New Exception("Client disconnected!")
            Return tmp
        End Function

        Private Sub WriteString(con As Connection, str As String)
            Console.WriteLine("[O]" & str)
            con.StreamW.WriteLine(str)
        End Sub

        Private Sub EnterJoinMode(con As Connection, gaem As IGame, index As Integer)
            Try
                Dim break As Boolean = False
                Do Until gaem.Ended Or break
                    Dim txt As String = ReadString(con)
                    If gaem.HostConnection IsNot Nothing Then WriteString(gaem.HostConnection, index.ToString & txt)
                    If txt = "e" Then break = True
                Loop
            Catch ex As Exception
                gaem.Players(index).Bereit = False
                gaem.Players(index).Connection = Nothing
                If gaem.HostConnection IsNot Nothing Then WriteString(gaem.HostConnection, index.ToString & "e") 'If connection was interrupted, send to host that connection was interrupted and halt game
            End Try

            If Not gaem.Active Then gaem.Players(index) = Nothing : WriteString(con, "LOL")
        End Sub

        Private Sub EnterCreateMode(con As Connection, gaem As IGame)
            Try
                Do Until gaem.Ended
                    Dim nl As String = ReadString(con)
                    Select Case nl(0)
                        Case "b"c
                            'If host sends that the game shall begin, unlist round
                            If games.ContainsKey(gaem.Key) Then games.Remove(gaem.Key)
                            gaem.Active = True
                        Case "l"c, "I"c
                            'If host left, end game for everyone
                            SendToAllGameClients(gaem)
                            gaem.HostConnection = Nothing
                            Exit Try
                        Case "e"c
                            'If remote client lost connection, send to all other remotes and add relist game
                            Dim who As Integer = CInt(nl(1).ToString)
                            If gaem.Players(who) IsNot Nothing Then gaem.Players(who).Bereit = False
                            If Not games.ContainsKey(gaem.Key) Then games.Add(gaem.Key, gaem)

                            For i As Integer = 1 To gaem.Players.Length - 1
                                If gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Typ = SpielerTyp.Online AndAlso gaem.Players(i).Connection IsNot Nothing Then WriteString(gaem.Players(i).Connection, nl)
                            Next
                        Case Else
                            For i As Integer = 1 To gaem.Players.Length - 1
                                If gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Typ = SpielerTyp.Online AndAlso gaem.Players(i).Connection IsNot Nothing Then WriteString(gaem.Players(i).Connection, nl)
                            Next
                    End Select
                Loop
            Catch ex As Exception
                SendToAllGameClients(gaem)
            End Try
            If games.ContainsKey(gaem.Key) Then games.Remove(gaem.Key)
        End Sub

        Private Sub SendToAllGameClients(gaem As IGame)
            If Not gaem.Ended Then
                gaem.Ended = True
                Dim takenconnections As New List(Of Connection)
                For i As Integer = 0 To gaem.Players.Length - 1
                    Try
                        If gaem IsNot Nothing AndAlso gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Connection IsNot Nothing AndAlso Not takenconnections.Contains(gaem.Players(i).Connection) Then
                            WriteString(gaem.Players(i).Connection, "Understandable, have a nice day!")
                            takenconnections.Add(gaem.Players(i).Connection)
                        End If
                    Catch
                    End Try
                Next
            End If
        End Sub

        Private Function AlreadyContainsNickname(nick As String) As Boolean
            For Each element In list
                If element.Nick = nick Then Return True
            Next
            Return False
        End Function
    End Module
End Namespace