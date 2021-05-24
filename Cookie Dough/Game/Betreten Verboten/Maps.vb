Imports Microsoft.Xna.Framework

Namespace Game.BetretenVerboten
    Module Maps
        Friend Function RollDice(Optional hard As Boolean = False) As Integer
            If hard Then Return (Nez.Random.Range(0, 17) Mod 6) + 1
            Return Nez.Random.Range(1, 7)
        End Function

        Public Function GetMapName(map As GaemMap) As String
            Select Case map
                Case GaemMap.Default4Players
                    Return "Plus"
                Case GaemMap.Default6Players
                    Return "Star"
                Case GaemMap.Default8Players
                    Return "Octagon"
                Case Else
                    Return "Invalid Map"
            End Select
        End Function

        Public Function GetMapSize(map As GaemMap) As Integer
            Select Case map
                Case GaemMap.Default4Players
                    Return 4
                Case GaemMap.Default6Players
                    Return 6
                Case GaemMap.Default8Players
                    Return 8
                Case Else
                    Return 0
            End Select
        End Function


        Friend Function GetIntroKeyframes(map As GaemMap, player As Integer, ending As Boolean) As Keyframe3D
            Select Case map
                Case GaemMap.Default4Players
                    Select Case player
                        Case 0
                            If Not ending Then Return New Keyframe3D(-215, -250, -850, -0.37, 1, 2.3, False) Else Return New Keyframe3D(250, -235, -800, 0.65, 1, 2.3, False)
                        Case 1
                            If Not ending Then Return New Keyframe3D(-315, -142, -925, 0, 1.1, 0, False) Else Return New Keyframe3D(255, -250, -1044, 1.23, 1, 0, False)
                        Case 2
                            If Not ending Then Return New Keyframe3D(340, -410, -710, -0.12, 0.64, 0, False) Else Return New Keyframe3D(-100, -230, -1110, -0.2, 1.1, -1, False)
                        Case 3
                            If Not ending Then Return New Keyframe3D(-120, 75, -530, -0.2, 1.3, -4.55, False) Else Return New Keyframe3D(-390, 70, -440, -0.2, 1.3, -4.64, False)
                    End Select
                Case Else
                    Return New Keyframe3D
            End Select
        End Function

        Private FDist0 As Integer = 85
        Private FDist1 As Integer = 65
        Private FDist2 As Integer = 40
        Private Opos1 As Integer = 420
        Private Opos2 As Integer = 420

        Friend Function GetMapVectorPos(map As GaemMap, player As Integer, figur As Integer, pos As Integer) As Vector2

            Select Case map
                Case GaemMap.Default4Players
                    Select Case pos
                        Case -1 'Zeichne Figur in Homebase
                            Return Vector2.Transform(Map0GetLocalPos(figur), transmatrices0(player))
                        Case 40, 41, 42, 43 'Zeichne Figur in Haus
                            Return Vector2.Transform(Map0GetLocalPos(pos - 26), transmatrices0(player))
                        Case Else 'Zeichne Figur auf Feld
                            Dim matrx As Matrix = transmatrices0((player + Math.Floor(pos / 10)) Mod 4)
                            Return Vector2.Transform(Map0GetLocalPos((pos Mod 10) + 4), matrx)
                    End Select
                Case GaemMap.Default6Players
                    Select Case pos
                        Case -1 'Zeichne Figur in Homebase
                            Return Vector2.Transform(Map1GetLocalPos(figur), transmatrices1(player))
                        Case 48, 49 'Zeichne Figur in Haus
                            Return Vector2.Transform(Map1GetLocalPos(pos - 34), transmatrices1(player))
                        Case Else 'Zeichne Figur auf Feld
                            Dim matrx As Matrix = transmatrices1((player + Math.Floor(pos / 8)) Mod 6)
                            Return Vector2.Transform(Map1GetLocalPos((pos Mod 8) + 4), matrx)
                    End Select
                Case GaemMap.Default8Players
                    Select Case pos
                        Case -1 'Zeichne Figur in Homebase
                            Return Vector2.Transform(Map2GetLocalPos(figur), transmatrices2(player))
                        Case 56, 57 'Zeichne Figur in Haus
                            Return Vector2.Transform(Map2GetLocalPos(pos - 42), transmatrices2(player))
                        Case Else 'Zeichne Figur auf Feld
                            Dim matrx As Matrix = transmatrices2((player + Math.Floor(pos / 7)) Mod 8)
                            Return Vector2.Transform(Map2GetLocalPos((pos Mod 7) + 4), matrx)
                    End Select
            End Select
        End Function

        Friend Function GetMapVectorPos(map As GaemMap, ps As PlayFieldPos, pl As Integer) As Vector2
            Select Case map
                Case GaemMap.Default4Players
                    Return Vector2.Transform(Map0GetLocalPos(ps), transmatrices0(pl))
                Case GaemMap.Default6Players
                    Return Vector2.Transform(Map1GetLocalPos(ps), transmatrices1(pl))
                Case GaemMap.Default8Players
                    Return Vector2.Transform(Map2GetLocalPos(ps), transmatrices2(pl))
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Friend Function GetMapVectorPos(map As GaemMap, pos As Integer) As Vector2
            Select Case map
                Case GaemMap.Default4Players
                    Dim matrx As Matrix = transmatrices0(Math.Floor(pos / 10) Mod 4)
                    Return Vector2.Transform(Map0GetLocalPos((pos Mod 10) + 4), matrx)
                Case GaemMap.Default6Players
                    Dim matrx As Matrix = transmatrices1(Math.Floor(pos / 8) Mod 6)
                    Return Vector2.Transform(Map1GetLocalPos((pos Mod 8) + 4), matrx)
                Case GaemMap.Default8Players
                    Dim matrx As Matrix = transmatrices2(Math.Floor(pos / 7) Mod 8)
                    Return Vector2.Transform(Map2GetLocalPos((pos Mod 7) + 4), matrx)
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Friend Function GetFieldSizes(map As GaemMap) As (Integer, Integer, Single, Integer)
            Select Case map
                Case GaemMap.Default4Players
                    Return (28, 20, 3.5, 35)
                Case GaemMap.Default6Players
                    Return (22, 15, 2.3, 30)
                Case GaemMap.Default8Players
                    Return (14, 12, 1.75, 18)
                Case Else
                    Return (5, 5, 10, 35)
            End Select
        End Function

        Friend transmatrices0 As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
        Private Function Map0GetLocalPos(ps As PlayFieldPos) As Vector2
            Select Case ps
                Case PlayFieldPos.Home1
                    Return New Vector2(-420, -420)
                Case PlayFieldPos.Home2
                    Return New Vector2(-350, -420)
                Case PlayFieldPos.Home3
                    Return New Vector2(-420, -350)
                Case PlayFieldPos.Home4
                    Return New Vector2(-350, -350)
                Case PlayFieldPos.Haus1
                    Return New Vector2(-FDist0 * 4, 0)
                Case PlayFieldPos.Haus2
                    Return New Vector2(-FDist0 * 3, 0)
                Case PlayFieldPos.Haus3
                    Return New Vector2(-FDist0 * 2, 0)
                Case PlayFieldPos.Haus4
                    Return New Vector2(-FDist0, 0)
                Case PlayFieldPos.Feld1
                    Return New Vector2(-FDist0 * 5, -FDist0)
                Case PlayFieldPos.Feld2
                    Return New Vector2(-FDist0 * 4, -FDist0)
                Case PlayFieldPos.Feld3
                    Return New Vector2(-FDist0 * 3, -FDist0)
                Case PlayFieldPos.Feld4
                    Return New Vector2(-FDist0 * 2, -FDist0)
                Case PlayFieldPos.Feld5
                    Return New Vector2(-FDist0, -FDist0)
                Case PlayFieldPos.Feld6
                    Return New Vector2(-FDist0, -FDist0 * 2)
                Case PlayFieldPos.Feld7
                    Return New Vector2(-FDist0, -FDist0 * 3)
                Case PlayFieldPos.Feld8
                    Return New Vector2(-FDist0, -FDist0 * 4)
                Case PlayFieldPos.Feld9
                    Return New Vector2(-FDist0, -FDist0 * 5)
                Case PlayFieldPos.Feld10
                    Return New Vector2(0, -FDist0 * 5)
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Friend transmatrices1 As Matrix() = {Matrix.CreateRotationZ(MathHelper.TwoPi * 2 / 6), Matrix.CreateRotationZ(MathHelper.TwoPi * 3 / 6), Matrix.CreateRotationZ(MathHelper.TwoPi * 4 / 6), Matrix.CreateRotationZ(MathHelper.TwoPi * 5 / 6), Matrix.CreateRotationZ(MathHelper.TwoPi * 6 / 6), Matrix.CreateRotationZ(MathHelper.TwoPi * 7 / 6), Matrix.CreateRotationZ(MathHelper.TwoPi * 1 / 6)} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
        Private Function Map1GetLocalPos(ps As PlayFieldPos) As Vector2
            Dim defvec As New Vector2(Opos1, 0)
            Dim angvecA As Vector2 = RotateVector(Vector2.UnitX * (FDist1), MathHelper.TwoPi / 5)
            Dim angvecB As Vector2 = RotateVector(Vector2.UnitY * FDist1, MathHelper.TwoPi / 5.8)
            Dim angvecC As Vector2 = RotateVector(Vector2.UnitY * FDist1, MathHelper.TwoPi / 6)
            Dim angvecD As Vector2 = New Vector2(-25, -16)
            Dim angvecE As Vector2 = RotateVector(-Vector2.UnitX * FDist1, MathHelper.TwoPi)
            Dim angvecF As Vector2 = RotateVector(Vector2.UnitX * (FDist1), MathHelper.TwoPi / 6)
            Select Case ps
                Case PlayFieldPos.Home1
                    Return defvec + angvecF
                Case PlayFieldPos.Home2
                    Return defvec + angvecC + angvecF
                Case PlayFieldPos.Haus1
                    Return defvec + angvecD + angvecE
                Case PlayFieldPos.Haus2
                    Return defvec + angvecD + angvecE * 2
                Case PlayFieldPos.Feld1
                    Return defvec
                Case PlayFieldPos.Feld2
                    Return defvec + angvecC
                Case PlayFieldPos.Feld3
                    Return defvec + angvecC * 2
                Case PlayFieldPos.Feld4
                    Return defvec + angvecC * 3
                Case PlayFieldPos.Feld5
                    Return defvec + angvecC * 4
                Case PlayFieldPos.Feld6
                    Return defvec + angvecC * 4 + angvecA
                Case PlayFieldPos.Feld7
                    Return defvec + angvecC * 4 + angvecA * 2
                Case PlayFieldPos.Feld8
                    Return defvec + angvecC * 4 + angvecA * 3
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Friend transmatrices2 As Matrix() = {Matrix.CreateRotationZ(MathHelper.TwoPi * 3 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 4 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 5 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 6 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 7 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 8 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 1 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 2 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 3 / 8)} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
        Private Function Map2GetLocalPos(ps As PlayFieldPos) As Vector2
            FDist2 = 46
            transmatrices2 = {Matrix.CreateRotationZ(MathHelper.TwoPi * 2 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 3 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 4 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 5 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 6 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 7 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 8 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 1 / 8), Matrix.CreateRotationZ(MathHelper.TwoPi * 2 / 8)} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
            Dim defvec As New Vector2(Opos2, 0)
            Dim angvecC As Vector2 = RotateVector(Vector2.UnitY * FDist2, MathHelper.TwoPi / 16)
            Dim angvecD As Vector2 = New Vector2(-10, 0)
            Dim angvecE As Vector2 = RotateVector(-Vector2.UnitX * FDist2, MathHelper.TwoPi)
            Dim angvecF As Vector2 = New Vector2(46, 18)
            Select Case ps
                Case PlayFieldPos.Home1
                    Return defvec + angvecF + angvecC
                Case PlayFieldPos.Home2
                    Return defvec + angvecF + angvecC * 2
                Case PlayFieldPos.Haus1
                    Return defvec + angvecD + angvecE
                Case PlayFieldPos.Haus2
                    Return defvec + angvecD + angvecE * 2
                Case PlayFieldPos.Feld1
                    Return defvec
                Case PlayFieldPos.Feld2
                    Return defvec + angvecC
                Case PlayFieldPos.Feld3
                    Return defvec + angvecC * 2
                Case PlayFieldPos.Feld4
                    Return defvec + angvecC * 3
                Case PlayFieldPos.Feld5
                    Return defvec + angvecC * 4
                Case PlayFieldPos.Feld6
                    Return defvec + angvecC * 5
                Case PlayFieldPos.Feld7
                    Return defvec + angvecC * 6
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Public Function GetFigureRectangle(map As GaemMap, pl As Integer, figure As Integer, Spielers As Player(), Center As Vector2) As Rectangle
            Select Case map
                Case GaemMap.Default4Players

                    Dim chr As Integer = Spielers(pl).Spielfiguren(figure)
                    Dim vec As Vector2 = Vector2.Zero
                    Dim matrx As Matrix = transmatrices0(pl)
                    Select Case chr
                        Case -1
                        Case 40, 41, 42, 43 'Figur in Haus
                            vec = Map0GetLocalPos(chr - 26)
                        Case Else 'Figur auf Feld
                            vec = Map0GetLocalPos((chr Mod 10) + 4)
                            matrx = transmatrices0((pl + Math.Floor(chr / 10)) Mod 4)
                    End Select

                    Return GetChrRect(Center + Vector2.Transform(vec, matrx))
                Case GaemMap.Default6Players

                    Dim chr As Integer = Spielers(pl).Spielfiguren(figure)
                    Dim vec As Vector2 = Vector2.Zero
                    Dim matrx As Matrix = transmatrices1(pl)
                    Select Case chr
                        Case -1
                        Case 48, 49 'Figur in Haus
                            vec = Map1GetLocalPos(chr - 34)
                        Case Else 'Figur auf Feld
                            vec = Map1GetLocalPos((chr Mod 8) + 4)
                            matrx = transmatrices1((pl + Math.Floor(chr / 8)) Mod 6)
                    End Select

                    Return GetChrRect(Center + Vector2.Transform(vec, matrx))
                Case GaemMap.Default8Players

                    Dim chr As Integer = Spielers(pl).Spielfiguren(figure)
                    Dim vec As Vector2 = Vector2.Zero
                    Dim matrx As Matrix = transmatrices2(pl)
                    Select Case chr
                        Case -1
                        Case 56, 57 'Figur in Haus
                            vec = Map2GetLocalPos(chr - 42)
                        Case Else 'Figur auf Feld
                            vec = Map2GetLocalPos((chr Mod 7) + 4)
                            matrx = transmatrices2((pl + Math.Floor(chr / 7)) Mod 8)
                    End Select

                    Return GetChrRect(Center + Vector2.Transform(vec, matrx))
            End Select
        End Function

        Private Function GetChrRect(vc As Vector2) As Rectangle
            Return New Rectangle(vc.X - 20, vc.Y - 20, 40, 40)
        End Function
    End Module
End Namespace