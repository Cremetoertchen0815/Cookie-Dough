Imports System.Collections.Generic

Namespace Game.DuoCard.Networking
    Public Structure SyncMessage
        Public Spielers As List(Of Player)

        Public Sub New(Spielers As List(Of Player))
            Me.Spielers = Spielers
        End Sub
    End Structure
End Namespace