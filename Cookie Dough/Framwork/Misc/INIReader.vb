Imports System.Runtime.InteropServices
Imports System.Text

Namespace Framework.Misc
    Public Class INIReader

        <DllImport("kernel32", EntryPoint:="GetPrivateProfileString")>
        Public Shared Function Lesen(Sektion As String, Key As String, StandartVal As String, Result As String, Size As Int32, Dateiname As String) As Int32
        End Function

        Private Declare Ansi Function WritePrivateProfileString Lib "kernel32" Alias "WritePrivateProfileStringA" (
 ByVal lpApplicationName As String,
 ByVal lpKeyName As String,
 ByVal lpString As String,
 ByVal lpFileName As String) _
As Integer
    End Class
End Namespace