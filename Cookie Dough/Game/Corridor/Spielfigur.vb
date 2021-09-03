Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Corridor
    Public MustInherit Class Spielfigur
        'Properties
        Public MustOverride Property Model3D As Model
        Public MustOverride ReadOnly Property Type As SpielfigurType
        Public Property Position As Vector2 = Vector2.Zero

        Public Sub Draw(drawindex As Integer, view As Matrix, projection As Matrix)
            Dim clor As Color = If(drawindex < 1, Color.Black, Color.White)
            For Each mesh In Model3D.Meshes
                Dim fx = CType(mesh.Effects(0), BasicEffect)
                fx.World = Matrix.Identity
                fx.View = view
                fx.Projection = projection
                fx.DiffuseColor = Color.White.ToVector3
                fx.LightingEnabled = True '// turn on the lighting subsystem.
                fx.PreferPerPixelLighting = True
                fx.AmbientLightColor = Vector3.Zero
                fx.EmissiveColor = clor.ToVector3 * 0.12
                fx.DirectionalLight0.Direction = New Vector3(0, 0.8, 1.5)
                fx.DirectionalLight0.DiffuseColor = clor.ToVector3 * 0.6 '// a gray light
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