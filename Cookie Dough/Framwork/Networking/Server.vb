Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports Nez.Console

Namespace Framework.Networking
    Module Server
        Public Property ServerActive As Boolean

        Public Const Port As Integer = 187
        Private MainThread As Thread
        Private server As TcpListener
        Private client As New TcpClient
        Private endpoint As IPEndPoint = New IPEndPoint(IPAddress.Any, Port)
        Private list As New List(Of Connection)
        Private registered As Dictionary(Of String, String) '(ID, Username)
        Private games As New Dictionary(Of Integer, IGame)
        Private RNG As New System.Random
        Private LogPath As String = "Log\" & Date.Now.ToShortDateString & ".log"
        Friend streamw As StreamWriter

        Public Sub StartServer()
            MainThread = New Thread(AddressOf ServerMainSub)
            MainThread.Start()
            ServerActive = True

            Try
                'Create log file
                If Not File.Exists(LogPath) Then
                    streamw = File.CreateText(LogPath)
                Else
                    Dim oldtxt As String = File.ReadAllText(LogPath)
                    streamw = New StreamWriter(LogPath) With {.AutoFlush = True}
                    streamw.Write(oldtxt)
                End If
                'Load register
                If File.Exists("Save\register.dat") Then registered = Newtonsoft.Json.JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(File.ReadAllText("Save\register.dat")) Else registered = New Dictionary(Of String, String)
            Catch x As Exception
                Console.WriteLine("Logging file blocked!")
            End Try
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
                        c.Client = client
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
                    streamw.WriteLine()
                    streamw.Close()
                    streamw.Dispose()
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
                WriteString(con, VersionString)
                WriteString(con, "Hello there!")
                If Not ReadString(con) = "Wassup?" Then Exit Try
                WriteString(con, "What's your name?")
                Dim usrname As String = ReadString(con)
                Dim IDs As String = ReadString(con)

                'If player is first time logging on, generate random key that isn't yet taken
                If IDs = "" Then
                    Dim rnd As String = RandomString(6)
                    Do While registered.ContainsKey(rnd)
                        rnd = RandomString(6)
                    Loop
                    IDs = rnd
                    WriteString(con, IDs)
                End If

                If AlreadyContainsID(IDs) Then WriteString(con, "Sorry m8! ID already used") : Exit Try
                If AlreadyContainsNickname(usrname) Then WriteString(con, "Sorry m8! Username already taken") : Exit Try
                If Not FilenameIsOK(usrname) Then WriteString(con, "Sorry m8! Invalid username") : Exit Try
                con.Nick = usrname
                con.Identifier = IDs
                RegisterPlayer(con)
                WriteString(con, "Alrighty!")

                Do
                    Select Case ReadString(con)
                        Case "list"
                            For Each element In games
                                WriteString(con, element.Key.ToString)
                                WriteString(con, element.Value.Name.ToString)
                                WriteString(con, element.Value.Type.ToString)
                                WriteString(con, element.Value.GetRegisteredPlayerCount.ToString)
                            Next
                            WriteString(con, "That's it!")
                        Case "users"
                            WriteString(con, (registered.Count - 1).ToString)
                            For Each element In registered
                                WriteString(con, element.Key.ToString)
                                WriteString(con, element.Value.ToString)
                            Next
                        Case "join"
                            Try
                                Dim id As Integer = CInt(ReadString(con))
                                Dim gaem As IGame = games(id)
                                Dim index As Integer = -1
                                gaem.ServerSendJoinGlobalData(con, AddressOf WriteString)
                                If IsRejoining(gaem, con.Identifier, index) Then
                                    'Is Rejoining
                                    If index = -1 Then Throw New NotImplementedException
                                    WriteString(con, index)
                                    gaem.ServerSendJoinRejoinData(index, con, AddressOf WriteString)
                                    WriteString(con, "Rejoin")
                                Else
                                    WriteString(con, index.ToString)
                                    gaem.ServerSendJoinNujoinData(index, con, AddressOf WriteString)
                                    WriteString(con, "Nujoin")
                                End If

                                'Check if rejoining
                                If ReadString(con) <> "Okidoki!" Then Throw New NotImplementedException
                                WriteString(con, "LET'S HAVE A BLAST!")

                                If index > -1 Then
                                    gaem.Players(index).ID = con.Identifier
                                    gaem.Players(index).Bereit = True
                                    EnterJoinMode(con, gaem, index)
                                Else
                                    EnterViewerJoinMode(con, gaem)
                                End If
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
        Friend Function RandomString(len As Integer) As String
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
                Case GameType.DuoCard
                    Return Game.DuoCard.Networking.ExtGame.ServerSendCreateData(AddressOf ReadString, con, gamename, Key)
                Case GameType.DropTrop
                    Return Game.DropTrop.Networking.ExtGame.ServerSendCreateData(AddressOf ReadString, con, gamename, Key)
                Case Else
                    Return Nothing
            End Select
        End Function

        Private Function IsRejoining(gaem As IGame, ID As String, ByRef index As Integer) As Boolean
            If gaem Is Nothing Then Return False
            'If player is rejoining, return yes and hand over old ID
            For i As Integer = 0 To gaem.Players.Length - 1
                If gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).ID = ID Then index = i : Return True
            Next
            'If not rejoining, check whitelist if welcome
            For i As Integer = 0 To gaem.WhiteList.Length - 1
                If gaem.WhiteList(i) = ID Or gaem.WhiteList(i) = "" Then
                    gaem.WhiteList(i) = ID
                    index = i
                    Return False
                End If
            Next
            'Else, join as guest
            index = -1
            Return False
        End Function


        Private Function ReadString(con As Connection) As String
            Dim tmp As String = con.StreamR.ReadLine
            If Not tmp.Contains("_TATA_") Then Console.WriteLine("[I/" & con.Nick & "]" & tmp) : streamw.WriteLine("[" & con.Nick & "]: " & tmp)
            If tmp = "I'm outta here!" Then Throw New Exception("Client disconnected!")
            Return tmp
        End Function

        Private Sub WriteString(con As Connection, str As String)
            If Not str.Contains("_TATA_") Then Console.WriteLine("[O/" & con.Nick & "]" & str)
            con.StreamW.WriteLine(str)
        End Sub

        Private Sub EnterJoinMode(con As Connection, gaem As IGame, index As Integer)
            Try
                Dim break As Boolean = False
                Do Until gaem.Ended = EndingMode.Abruptly Or break
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

        Private Sub EnterViewerJoinMode(con As Connection, gaem As IGame)
            gaem.Viewers.Add(con)
            Try
                Dim break As Boolean = False
                Do Until gaem.Ended = EndingMode.Abruptly Or break
                    Dim txt As String = ReadString(con)
                    If gaem.HostConnection IsNot Nothing And (txt(0) = "y"c Or txt(0) = "c"c) Then WriteString(gaem.HostConnection, "9" & txt)
                    If txt = "e" Then break = True
                Loop
            Catch ex As Exception
            End Try

            'Send terminator/flusher
            Try
                con.StreamW.WriteLine("0")
            Catch
            End Try
            gaem.Viewers.Remove(con)
        End Sub

        Private Sub EnterCreateMode(con As Connection, gaem As IGame)
            Try
                Do Until gaem.Ended = EndingMode.Abruptly
                    Dim nl As String = ReadString(con)
                    Select Case nl(0)
                        Case "b"c
                            'If host sends that the game shall begin, unlist round
                            gaem.Active = True
                            'Transmit begin message
                            SendToAllGameClients(gaem, nl)
                        Case "h"c
                            'Receive player scores
                            Dim game As Integer = CInt(nl(1).ToString)
                            Dim map As Integer = CInt(nl(2).ToString)
                            Dim path As String = "Save\highsc" & game.ToString & map.ToString & ".dat"
                            Dim highscore As List(Of (String, Integer))
                            Dim data As List(Of (String, Integer)) = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of (String, Integer)))(nl.Substring(3))
                            Dim updated As Boolean() = New Boolean(2) {}
                            'Load and update highscores
                            If File.Exists(path) Then highscore = Newtonsoft.Json.JsonConvert.DeserializeObject(Of List(Of (String, Integer)))(File.ReadAllText(path)) Else highscore = New List(Of (String, Integer))
                            highscore.AddRange(data.ToArray)
                            'Remove double ganger
                            highscore = highscore.OrderBy(Function(x) x.Item2).ToList()
                            highscore.Reverse()
                            'Delete access
                            Do While highscore.Count > 10
                                highscore.RemoveAt(highscore.Count - 1)
                            Loop
                            'Update "updated" list
                            For i As Integer = 0 To 2
                                updated(i) = data.Contains(highscore(i))
                            Next
                            'Save to file
                            File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(highscore))

                            SendToAllGameClients(gaem, "mHighscores:", False)

                            For i As Integer = 0 To 2
                                Dim ii As Integer = i
                                Core.Schedule(i + 1, Sub() SendToAllGameClients(gaem, "m" & (ii + 1).ToString & ": " & highscore(ii).Item1 & "(" & highscore(ii).Item2.ToString & If(updated(ii), ", GG!)", ")"), False))
                            Next
                        Case "l"c, "I"c
                            'If host left, end game for everyone
                            SendEndToAllGameClients(gaem)
                            gaem.HostConnection = Nothing
                            Exit Try
                        Case "e"c
                            'If remote client lost connection, send to all other remotes and add relist game
                            Dim who As Integer = CInt(nl(1).ToString)
                            If Not games.ContainsKey(gaem.Key) And Not gaem.Ended = EndingMode.Properly Then games.Add(gaem.Key, gaem)

                            SendToAllGameClients(gaem, nl)

                            If gaem.Players(who) IsNot Nothing Then gaem.Players(who).Bereit = False : gaem.Players(who).Connection = Nothing
                        Case "w"c
                            'Win flag was sent, remove game from server list
                            gaem.Ended = EndingMode.Properly
                            If games.ContainsKey(gaem.Key) Then games.Remove(gaem.Key)
                            SendToAllGameClients(gaem, nl)
                        Case Else
                            SendToAllGameClients(gaem, nl)
                    End Select
                Loop
            Catch ex As Exception
                SendEndToAllGameClients(gaem)
            End Try
            If games.ContainsKey(gaem.Key) Then games.Remove(gaem.Key)
        End Sub

        Private Sub SendEndToAllGameClients(gaem As IGame)
            If gaem.Ended <> EndingMode.Abruptly Then
                gaem.Ended = EndingMode.Abruptly
                Dim takenconnections As New List(Of Connection)

                'Send to players
                For i As Integer = 0 To gaem.Players.Length - 1
                    Try
                        If gaem IsNot Nothing AndAlso gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Connection IsNot Nothing AndAlso Not takenconnections.Contains(gaem.Players(i).Connection) Then
                            WriteString(gaem.Players(i).Connection, "Understandable, have a nice day!")
                            takenconnections.Add(gaem.Players(i).Connection)
                        End If
                    Catch
                    End Try
                Next

                'Send to viewers
                For i As Integer = 0 To gaem.Viewers.Count - 1
                    Try
                        If gaem IsNot Nothing AndAlso gaem.Viewers(i) IsNot Nothing AndAlso Not takenconnections.Contains(gaem.Viewers(i)) Then
                            WriteString(gaem.Viewers(i), "Understandable, have a nice day!")
                            takenconnections.Add(gaem.Viewers(i))
                        End If
                    Catch
                    End Try
                Next
            End If
        End Sub

        Private Sub SendToAllGameClients(gaem As IGame, msg As String, Optional IgnoreLocals As Boolean = True)
            'Send to online players
            For i As Integer = 1 To gaem.Players.Length - 1
                If gaem.Players(i) IsNot Nothing AndAlso gaem.Players(i).Typ = SpielerTyp.Online AndAlso gaem.Players(i).Connection IsNot Nothing Then WriteString(gaem.Players(i).Connection, msg)
            Next

            'Send to host
            If Not IgnoreLocals Then gaem.HostConnection.StreamW.WriteLine("9" & msg)

            'Send to viewers
            For i As Integer = 0 To gaem.Viewers.Count - 1
                Try
                    If gaem.Viewers(i) IsNot Nothing Then WriteString(gaem.Viewers(i), msg)
                Catch
                End Try
            Next
        End Sub

        Private Function AlreadyContainsNickname(nick As String) As Boolean
            For Each element In list
                If element.Nick = nick Then Return True
            Next
            Return False
        End Function

        Private Function AlreadyContainsID(id As String) As Boolean
            For Each element In list
                If element.Identifier = id Then Return True
            Next
            Return False
        End Function
        Private Sub RegisterPlayer(con As Connection)
            If registered.ContainsKey(con.Identifier) Then
                registered(con.Identifier) = con.Nick
            Else
                registered.Add(con.Identifier, con.Nick)
            End If
            SaveRegister()
        End Sub

        Private Sub SaveRegister()
            File.WriteAllText("Save\register.dat", Newtonsoft.Json.JsonConvert.SerializeObject(registered))
        End Sub

        <Command("network-kick", "Kicks a specific user from the server.")>
        Public Sub KickUser(nick As String)
            For Each element In list
                If element.Nick = nick Then
                    element.Client.Close()
                End If
            Next
        End Sub

        <Command("network-list", "Lists all online users.")>
        Public Sub ListUsers(identifier As String, Optional ShowRegistered As Boolean = False)
            If ShowRegistered Then
                For Each element In registered
                    DebugConsole.Instance.Log(element.Value & ": " & element.Key)
                Next
            Else
                For Each element In list
                    DebugConsole.Instance.Log(element.Nick & ": " & element.Identifier)
                Next
            End If
        End Sub

        <Command("network-deregister", "Deregisters a certain user + ID.")>
        Public Sub Deregister(id As String, Optional clear_all As Boolean = False)
            If clear_all Then
                registered.Clear()
                For Each element In list
                    element.Client.Close()
                Next
            Else
                If registered.ContainsKey(id) Then
                    registered.Remove(id)
                    For Each element In list
                        If element.Identifier = id Then element.Client.Close()
                    Next
                End If
            End If
            SaveRegister()
        End Sub
    End Module
End Namespace
