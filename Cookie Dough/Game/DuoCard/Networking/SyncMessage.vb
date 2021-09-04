Imports Cookie_Dough.Game.Common

Namespace Game.DuoCard.Networking
    Public Structure SyncMessage
        Public Spielers As BaseCardPlayer()
        Public TableCard As Common.Card

        Public Sub New(Spielers As BaseCardPlayer())
            Me.Spielers = Spielers
        End Sub
    End Structure
End Namespace