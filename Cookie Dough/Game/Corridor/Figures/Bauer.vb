Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor.Figures
    Public Class Bauer
        Inherits Spielfigur

        'Property Overrides
        Public Overrides Property Model3D As Model = Core.Content.Load(Of Model)("")
        Public Overrides ReadOnly Property Type As SpielfigurType = SpielfigurType.Bauer

        Public Overrides Function GetAllPossibleMoves() As Move()
            Return {}
        End Function
    End Class
End Namespace