Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor.Figures
    Public Class TestFigur
        Inherits Spielfigur

        'Property Overrides
        Private Shared CommonModel As Model = Core.Content.Load(Of Model)("mesh/piece_std")
        Public Overrides ReadOnly Property Model3D As Model
            Get
                Return CommonModel
            End Get
        End Property
        Public Overrides ReadOnly Property Type As SpielfigurType = SpielfigurType.Debug

        Public Overrides Function GetAllPossibleMoves() As Move()
            Return {}
        End Function
    End Class
End Namespace