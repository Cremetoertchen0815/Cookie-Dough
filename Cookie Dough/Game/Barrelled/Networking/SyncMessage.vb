Namespace Game.Barrelled.Networking
    Public Class SyncMessage
        Public Name As String
        Public MOTD As String
        Public ID As String
        Public Typ As SpielerTyp
        Public Mode As PlayerMode

        Public Sub New(Name As String, MOTD As String, ID As String, Typ As SpielerTyp, Mode As PlayerMode)
            Me.Name = Name
            Me.MOTD = MOTD
            Me.Typ = Typ
            Me.Mode = Mode
        End Sub
    End Class
End Namespace