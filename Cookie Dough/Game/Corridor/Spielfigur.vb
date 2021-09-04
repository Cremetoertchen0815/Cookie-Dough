Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor
    Public MustInherit Class Spielfigur
        'Properties
        Public MustOverride ReadOnly Property Model3D As Model
        Public MustOverride ReadOnly Property Type As SpielfigurType
        Public Overridable Property TransformMatrix As Matrix = Matrix.CreateScale(New Vector3(1, 1, 1) * 5) * Matrix.CreateRotationY(Math.PI) * Matrix.CreateTranslation(New Vector3(475, 475, 0) - New Vector3(55, 55, 0))
        Public Property Position As Vector2 = Vector2.Zero

        ''' <summary>
        ''' Draws the figure to its designated spot on the playing field
        ''' </summary>
        ''' <param name="drawindex">Represents the index of the player in order to determine the color of the playing pieces(0 is white, 1 is black)</param>
        ''' <param name="view">Requests the view matrix of the 3D renderer</param>
        ''' <param name="projection">Requests the projection matrix of the 3D renderer</param>
        ''' <param name="selectionblinker">Determines through a span of 0 through 1 how much the pieces shall be colored in red(for showing the selection of pieces)</param>
        Public Sub Draw(drawindex As Integer, view As Matrix, projection As Matrix, Optional selectionblinker As Single = 1.0F)
            Dim clor As Color = Color.Lerp(If(drawindex < 1, Color.White, Color.Black), Color.Red, selectionblinker)
            Dim stepp = 950 / 8   'Distance of one filed to its neighbor
            For Each mesh In Model3D.Meshes
                Dim fx = CType(mesh.Effects(0), BasicEffect)
                fx.World = TransformMatrix * Matrix.CreateTranslation(New Vector3(-Position.X * stepp, -Position.Y * stepp, 0))
                fx.View = view
                fx.Projection = projection
                fx.DiffuseColor = Color.White.ToVector3
                fx.LightingEnabled = True '// turn on the lighting subsystem.
                fx.PreferPerPixelLighting = True
                fx.AmbientLightColor = Vector3.Zero
                fx.EmissiveColor = clor.ToVector3 * 0.12
                fx.DirectionalLight0.Direction = New Vector3(0, 0.8, 1.5)
                fx.DirectionalLight0.DiffuseColor = clor.ToVector3 * 0.6
                fx.DirectionalLight0.SpecularColor = New Vector3(1, 1, 1) '// with white highlights
                mesh.Draw()
            Next
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