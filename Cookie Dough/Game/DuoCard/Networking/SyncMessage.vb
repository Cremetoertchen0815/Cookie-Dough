Imports System.Collections.Generic

Namespace Game.DuoCard.Networking
    Public Structure SyncMessage
        Public Spielers As Player()
        Public SaucerFields As List(Of Integer)

        Public Sub New(Spielers As Player())
            Me.Spielers = Spielers
        End Sub
    End Structure
End Namespace