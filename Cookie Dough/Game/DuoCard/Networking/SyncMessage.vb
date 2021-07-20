Imports System.Collections.Generic

Namespace Game.DuoCard.Networking
    Public Structure SyncMessage
        Public Spielers As Player()
        Public TableCard As Common.Card

        Public Sub New(Spielers As Player())
            Me.Spielers = Spielers
        End Sub
    End Structure
End Namespace