Imports System.Collections.Generic

Namespace Game.CarCrash.Networking
    Public Structure SyncMessage
        Public Spielers As Player()
        Public SaucerFields As List(Of Integer)

        Public Sub New(Spielers As Player(), SaucerFields As List(Of Integer))
            Me.Spielers = Spielers
            Me.SaucerFields = SaucerFields
        End Sub
    End Structure
End Namespace