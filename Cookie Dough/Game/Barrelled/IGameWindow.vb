Imports Cookie_Dough.Game.Barrelled.Players

Namespace Game.Barrelled
    Public Interface IGameWindow
        ReadOnly Property EgoPlayer As EgoPlayer
        ReadOnly Property Spielers As CommonPlayer()
        ReadOnly Property UserIndex As Integer
    End Interface
End Namespace