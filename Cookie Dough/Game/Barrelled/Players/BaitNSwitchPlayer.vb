Imports Microsoft.Xna.Framework

Namespace Game.Barrelled.Players
    Public Class BaitNSwitchPlayer
        Inherits CommonPlayer


        Public Overrides Property Location As Vector3
        Public Overrides Property Direction As Vector3

        Friend Overrides Function GetWorldMatrix() As Matrix
            Return Matrix.Identity
        End Function

        Public Overrides Sub Update()
        End Sub

        Public Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub

        Friend Overrides Sub SetColor(color As Color)
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace