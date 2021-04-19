﻿Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading
Imports Microsoft.Xna.Framework.Audio
Imports Nez.Console

Namespace Framework.Networking
    Public Class Client
        Public Property Connected As Boolean = False
        Public Property Hostname As String
        Public Property IsHost As Boolean
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


        Private stream As NetworkStream
        Private streamw As StreamWriter
        Private streamr As StreamReader
        Private client As TcpClient
        Friend blastmode As Boolean
        Private listener As Thread
        Private data As New List(Of String)
        Private naem As String
        Private _LeaveFlag As Boolean = False

        Sub New()
            Directory.CreateDirectory("Cache\client\")
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
                Select Case ReadString()
                    Case "Sorry m8! Username already taken"
                        Microsoft.VisualBasic.MsgBox("Username already taken on this server! Please change username!")
                        Exit Sub
                    Case "Sorry m8! Invalid username"
                        Microsoft.VisualBasic.MsgBox("Username invalid! Please change username!")
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
                If Not Server.ServerActive Then Console.WriteLine("[Client/I]" & tmp)
                If NetworkLog Then OutputDelegate.Invoke("[Client/I]" & tmp)
                Return tmp
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
                Return ""
            End Try
        End Function

        Friend Sub WriteString(str As String)
            Try
                If Not Server.ServerActive Then Console.WriteLine("[Client/O]" & str)
                If NetworkLog Then OutputDelegate.Invoke("[Client/O]" & str)
                If str = "Ich putz hier mal durch." Then Console.WriteLine()
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
            If (msg(0) = "l"c And IsHost) Or (msg(0) = "e"c And Not IsHost) Then blastmode = False
            If Connected Then WriteString(msg)
        End Sub

        'Friend Sub WriteSound(ident As IdentType)
        '    WriteString(CInt(ident).ToString)
        '    If ident = IdentType.Custom Then
        '        Dim data As Byte() = File.ReadAllBytes("Cache\client\sound.audio")
        '        streamw.WriteLine(Convert.ToBase64String(data))
        '    End If
        'End Sub
        'Friend Sub WriteSoundRaw(ident As IdentType, data As String)
        '    WriteString(CInt(ident).ToString)
        '    If ident = IdentType.Custom Then
        '        streamw.WriteLine(data)
        '    End If
        'End Sub

        'Friend Function ReadSound(nick As String, ByRef IdentSound As IdentType, ByRef data As String) As SoundEffect
        '    IdentSound = CType(ReadString(), IdentType)
        '    If IdentSound = IdentType.Custom Then
        '        data = streamr.ReadLine
        '        File.WriteAllBytes("Cache\server\" & nick & ".wav", Convert.FromBase64String(data))
        '        Return SoundEffect.FromFile("Cache\server\" & nick & ".wav")
        '    Else
        '        Return SoundEffect.FromFile("Content\prep\audio_" & CInt(IdentSound).ToString & ".wav")
        '    End If
        'End Function

        Public Function GetGamesList() As OnlineGameInstance()
            'Kein Zugriff auf diese Daten wenn in Blastmodus oder Verbindung getrennt
            If blastmode Or Not Connected Then Return {}

            Try
                Dim lst As New List(Of OnlineGameInstance)
                WriteString("list")
                Do
                    Dim firstline As String = ReadString()
                    If firstline <> "That's it!" Then
                        Dim gaem As New OnlineGameInstance With {.Key = CInt(firstline),
                                                                 .Name = ReadString(),
                                                                 .Type = CType([Enum].Parse(GetType(GameType), ReadString()), GameType),
                                                                 .PlayerCount = CInt(ReadString())}
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
                Return CInt(ReadString())
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
                Return 0
            End Try
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
                    If tmp.StartsWith("Sorry m8!") Then Throw New Exception() Else data.Add(tmp)
                    If tmp.StartsWith("Understandable, have a nice day!") Then LeaveFlag = True : Exit While
                End While
            Catch ex As Exception
                NoteError(ex, False)
                Disconnect()
            End Try
            blastmode = False
            AutomaticRefresh = True
            WriteString("Ich putz hier mal durch.")
            WriteString("Damit keine ReadLine-Commands offen bleiben.")
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