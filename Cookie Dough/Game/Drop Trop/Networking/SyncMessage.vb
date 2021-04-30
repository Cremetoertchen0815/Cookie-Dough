Imports System.Collections.Generic

Namespace Game.DropTrop.Networking
    Public Structure SyncMessage
        Public Spielers As Player()

        Public Sub New(Spielers As Player())
            Me.Spielers = Spielers
        End Sub
    End Structure
End Namespace