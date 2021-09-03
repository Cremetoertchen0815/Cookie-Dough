Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor
    Public MustInherit Class Spielfigur
        'Properties
        Public MustOverride Property Model3D As Model
        Public MustOverride ReadOnly Property Type As SpielfigurType
        Public Property Position As Vector2 = Vector2.Zero

        Public Sub Draw()

        End Sub
        Public MustOverride Function GetAllPossibleMoves() As Move()
        Public Function IsMoveOK(move As Move) As Boolean
            If move.Figur IsNot Me Then Return False
            For Each älämänt In GetAllPossibleMoves()
                If älämänt.Zielposition = move.Zielposition Then Return True
            Next
            Return False
        End Function
    End Class
End Namespace