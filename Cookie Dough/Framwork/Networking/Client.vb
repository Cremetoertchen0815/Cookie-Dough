Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Sockets
Imports System.Threading
Imports Nez.Console

Namespace Framework.Networking
    Public Class Client
        Public Property Connected As Boolean = False
        Public Property Hostname As String
        Public Property IsHost As Boolean
        Public Property SecondaryClient As Boolean = False
        Public Property LeaveFlag As Boolean
            Get
                Return _LeaveFlag
            End Get
            Set
                _LeaveFlag = Value
            End Set
        End Property

        Public Property AutomaticRefresh As Boolean = True
        Public Shared Property OutputDelegate As Action(Of String) = Sub(x) Return
        Public Shared Property NetworkLog As Boolean = False
        Public Shared Blocker As New Object


        Private stream As NetworkStream
        Private streamw As StreamWriter
        Private streamr As StreamReader
        Private client As TcpClient
        Friend blastmode As Boolean
        Private listener As Thread
        Private data As New List(Of String)
        Private naem As String
        Private _LeaveFlag As Boolean = False

        Public Sub New()
            Directory.CreateDirectory("Cache/client/")
            If Not File.Exists("Cache/client/pp.png") Then File.Copy("Content/prep/plc.png", "Cache/client/pp.png", True)
        End Sub

        Public Sub Connect(hostname As String, nickname As String)

            Try
                My.Settings.IP = hostname
                My.Settings.Save()
                client = New TcpClient
                client.Connect(hostname, 187) ' hier die ip des servers eintragen. 
                naem = nickname

                If Not client.Connected Then Throw New NotImplementedException()
                stream = client.GetStream
                streamw = New StreamWriter(stream) With {.AutoFlush = True}
                streamr = New StreamReader(stream)
                Dim vv As String = ReadString()
                If Not vv = VersionString Then Microsoft.VisualBasic.MsgBox("Your client's version (" & VersionString & ") differs from the server's (" & vv & ")!") : Throw New NotImplementedException()
                If Not ReadString() = "Hello there!" Then Throw New NotImplementedException()
                WriteString("Wassup?")
                If Not ReadString() = "What's your name?" Then Throw New NotImplementedException()
                WriteString(nickname)
                WriteString(If(SecondaryClient, "b", UniqueIdentifier))
                If Not SecondaryClient AndAlso UniqueIdentifier = "" Then UniqueIdentifier = ReadString()
                Select Case ReadString()
                    Case "Sorry m8! Username already taken"
                        Microsoft.VisualBasic.MsgBox("Username already taken on this server! Please change username!")
                        Exit Sub
                    Case "Sorry m8! Invalid username"
                        Microsoft.VisualBasic.MsgBox("Username invalid! Please change username!")
                        Exit Sub
                    Case "Sorry m8! ID already used"
                        Microsoft.VisualBasic.MsgBox("Client already logged on on this PC! Please close other client before continuing!")
                        Exit Sub
                End Select
                Connected = True
                blastmode = False
                Me.Hostname = hostname
            Catch ex As Exception
                NoteError(ex, False)
                Microsoft.VisualBasic.MsgBox("Verbindung zum Server nicht möglich!")
            End Try
        End Sub

        Private IDFilePath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "/Cookie Dough/ID.dat"
        Private IDFileFolder As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & "/Cookie Dough"
        Friend Property UniqueIdentifier As String
            Get
                If My.Settings.UniqueIdentifier = "" And File.Exists(IDFilePath) Then My.Settings.UniqueIdentifier = File.ReadAllText(IDFilePath)
                My.Settings.Save()
                Return My.Settings.UniqueIdentifier
            End Get
            Set(value As String)
                My.Settings.UniqueIdentifier = value
                If Not Directory.Exists(IDFileFolder) Then Directory.CreateDirectory(IDFileFolder)
                File.WriteAllText(IDFilePath, value)
                My.Settings.Save()
            End Set
        End Property

        'Gibt an, ob ein Server bereits läuft
        Friend Function TryConnect() As Boolean
            Using cl As New TcpClient
                Try
                    cl.Connect("127.0.0.1", 187)
                    Return True
                Catch ex As Exception
                    NoteError(ex, False)
                    Return False
                End Try
            End Using
        End Function

        Public Sub Disconnect()
            If client.Connected Then
                WriteString("I'm outta here!")
                streamw.Close()
                streamr.Close()
                stream.Close()
                client.Close()
            End If

            Connected = False
            blastmode = False
        End Sub

        Private Function ReadString() As String
            Try
                Dim tmp As String = streamr.ReadLine
                If Not Server.ServerActive And Not tmp.Contains("_TATA_") Then Console.WriteLine("[Client/I]" & tmp)
                If NetworkLog And Not tmp.Contains("_TATA_") Then OutputDelegate.Invoke("[Client/I]" & tmp)
                Return tmp
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
                Return ""
            End Try
        End Function

        Friend Sub WriteString(str As String)
            Try
                If Not Server.ServerActive And Not str.Contains("_TATA_") Then Console.WriteLine("[Client/O]" & str)
                If NetworkLog And Not str.Contains("_TATA_") Then OutputDelegate.Invoke("[Client/O]" & str)
                streamw.WriteLine(str)
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
            End Try
        End Sub

        Friend Function ReadStream() As String()
            SyncLock data
                Dim dataS As String() = data.ToArray
                data.Clear()
                Return dataS
            End SyncLock
        End Function

        Friend Sub WriteStream(msg As String)
            SyncLock Blocker
                If (msg(0) = "l"c And IsHost) Or (msg(0) = "e"c And Not IsHost) Then blastmode = False
                If Connected Then WriteString(msg)
            End SyncLock
        End Sub

        Public Function GetGamesList() As OnlineGameInstance()
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return {}

            Try
                Dim lst As New List(Of OnlineGameInstance)
                WriteString("list")
                Do
                    Dim firstline As String = ReadString()
                    If firstline <> "That's it!" Then
                        Dim gaem As New OnlineGameInstance With {.Key = firstline,
                                                                 .Name = ReadString(),
                                                                 .Type = [Enum].Parse(GetType(GameType), ReadString()),
                                                                 .PlayerCount = ReadString()}
                        lst.Add(gaem)
                    Else
                        Exit Do
                    End If
                Loop
                Return lst.ToArray
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
                Return {}
            End Try
        End Function

        Public Function GetOnlineMemberCount() As Integer
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return 0

            Try
                WriteString("membercount")
                Return ReadString()
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
                Return 0
            End Try
        End Function

        Public Function GetAllUsers() As (String, String)()
            WriteString("users")
            Dim ret As (String, String)() = New(String, String)(ReadString()) {}
            For i As Integer = 0 To ret.Length - 1
                ret(i) = (ReadString(), ReadString())
            Next
            Return ret
        End Function

        Public Function CreateGameFinal() As Boolean
            WriteString("Okidoki!")
            If ReadString() <> "LET'S HAVE A BLAST!" Then Return False
            blastmode = True
            LeaveFlag = False
            data.Clear()
            listener = New Thread(AddressOf MainClientListenerSub)
            listener.Start()
            Return True
        End Function

        Public Function JoinGame(info As OnlineGameInstance, loadaction As Action(Of Func(Of String))) As Boolean
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return False
            blastmode = True

            WriteString("join")
            WriteString(info.Key.ToString)

            loadaction(AddressOf ReadString)

            WriteString("Okidoki!")
            If ReadString() <> "LET'S HAVE A BLAST!" Then blastmode = False : Return False
            blastmode = True
            LeaveFlag = False
            data.Clear()
            listener = New Thread(AddressOf MainClientListenerSub)
            listener.Start()
            Return True
        End Function

        Private Sub MainClientListenerSub()
            Try
                While blastmode And Not LeaveFlag
                    Dim tmp As String = ReadString()
                    If tmp.StartsWith("Understandable") Then LeaveFlag = True : Exit While
                    If tmp.StartsWith("Sorry m8!") Then Throw New Exception() Else data.Add(tmp)
                End While
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
            End Try
            blastmode = False
            AutomaticRefresh = True
            WriteString("e")
            WriteString("Ich putz hier mal durch.")
            For i As Integer = 1 To 3
                WriteString("Tach" & i)
            Next
        End Sub

        <Command("network-log", "Enables the logging of any network traffic to the in-game chat")>
        Public Shared Sub SetNetworkLog(Optional enable As Boolean = Nothing)
            Select Case enable
                Case Nothing
                    NetworkLog = Not NetworkLog
                Case True
                    NetworkLog = True
                Case False
                    NetworkLog = False
            End Select
        End Sub

    End Class

End Namespace