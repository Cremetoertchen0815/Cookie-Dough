Imports System.IO
Imports System.Net.Sockets

Namespace Framework.Networking
    Public Class Connection
        Public Property Stream As NetworkStream
        Public Property StreamW As StreamWriter
        Public Property StreamR As StreamReader
        Public Property Nick As String
        Public Property IdentThumbnail As IdentType
        Public Property IdentSound As IdentType
        Public Property IdentThumbnailName As String
        Public Property IdentSoundName As String
    End Class
End Namespace
