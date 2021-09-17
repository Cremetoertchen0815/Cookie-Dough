Imports Microsoft.Xna.Framework

Namespace Game.BetretenVerboten
    Friend Module Maps
        Friend Function RollDice(Optional hard As Boolean = False) As Integer
            If hard Then Return (Nez.Random.Range(0, 17) Mod 6) + 1
            Return Nez.Random.Range(1, 7)
        End Function

        Public Function GetMapName(map As GaemMap) As String
            Select Case map
                Case GaemMap.Plus
                    Return "Plus"
                Case GaemMap.Star
                    Return "Star"
                Case GaemMap.Octagon
                    Return "Octagon"
                Case GaemMap.Snakes
                    Return "Le snek
"
                Case Else
                    Return "Invalid Map"
            End Select
        End Function

        Public Function GetMapSize(map As GaemMap) As Integer
            Select Case map
                Case GaemMap.Plus
                    Return 4
                Case GaemMap.Star
                    Return 6
                Case GaemMap.Octagon
                    Return 8
                Case GaemMap.Snakes
                    Return 4
                Case Else
                    Return 0
            End Select
        End Function


        Friend Function GetIntroKeyframes(map As GaemMap, player As Integer, ending As Boolean) As Keyframe3D
            Select Case map
                Case GaemMap.Plus
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
                Case GaemMap.Star
                    Select Case player
                        Case 0
                            If Not ending Then Return New Keyframe3D(-70, 57, -546, 5.31, 1.39, 0.17, False) Else Return New Keyframe3D(145, 57, -546, 5.31, 1.39, 0.17, False)
                        Case 1
                            If Not ending Then Return New Keyframe3D(225, -162, -1206, 1.91, 1.19, 0.17, False) Else Return New Keyframe3D(111, -165, -1278, 1.44, 1.19, 0.17, False)
                        Case 2
                            If Not ending Then Return New Keyframe3D(245, -110, -1326, 0.78, 1.31, 0.17, False) Else Return New Keyframe3D(-50, -145, -1372, -0.07, 1.31, 0.17, False)
                        Case 3
                            If Not ending Then Return New Keyframe3D(140, 50, -460, 2.21, 1.31, 0.17, False) Else Return New Keyframe3D(18, 95, -550, 2.21, 1.31, 0.17, False)
                        Case 4
                            If Not ending Then Return New Keyframe3D(-354, 241, -1078, 2.21, -0.09, 0.17, False) Else Return New Keyframe3D(-36, 434, -890, 1.36, -0.06, 0.13, False)
                        Case 5
                            If Not ending Then Return New Keyframe3D(280, 75, -680, 5.67, 1.2, 0.17, False) Else Return New Keyframe3D(420, 75, -680, 5.67, 1.2, 0.17, False)
                    End Select
                Case GaemMap.Octagon
                    Select Case player
                        Case 0
                            If Not ending Then Return New Keyframe3D(-325, -196, -990, 2.08, 0.77, 0, False) Else Return New Keyframe3D(245, -280, -1117, 3.55, 0.77, 0, False)
                        Case 1
                            If Not ending Then Return New Keyframe3D(-329, 50, -601, 0, 1.25, 0, False) Else Return New Keyframe3D(-140, 100, -567, -0.64, 1.25, 0, False)
                        Case 2
                            If Not ending Then Return New Keyframe3D(-100, -43, -560, 4.65, 1.6, 0.17, False) Else Return New Keyframe3D(-150, -43, -560, 4.65, 1.6, 0.17, False)
                        Case 3
                            If Not ending Then Return New Keyframe3D(-11, 400, -828, 3.59, 0.48, 0.17, False) Else Return New Keyframe3D(-11, 73, -560, 3.59, 1.37, 0.17, False)
                        Case 4
                            If Not ending Then Return New Keyframe3D(-246, 335, -973, 3.59, -0.23, 0.17, False) Else Return New Keyframe3D(-360, 250, -876, 3.59, -0.34, 0.17, False)
                        Case 5
                            If Not ending Then Return New Keyframe3D(-212, -146, -1375, 4.66, 1.22, 0.17, False) Else Return New Keyframe3D(-212, -100, -1262, 4.66, 1.22, 0.17, False)
                        Case 6
                            If Not ending Then Return New Keyframe3D(217, -382, -1042, 4.82, -0.0699, 0.17, False) Else Return New Keyframe3D(217, -382, -900, 4.82, -0.0699, 0.17, False)
                        Case 7
                            If Not ending Then Return New Keyframe3D(424, -37, -802, 5.1, 1.54, 0.17, False) Else Return New Keyframe3D(424, -37, -802, 5.5, 1.54, 0.17, False)
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
                Case GaemMap.Plus
                    Select Case pos
                        Case -1 'Zeichne Figur in Homebase
                            Return Vector2.Transform(Map0GetLocalPos(figur), transmatrices0(player))
                        Case 40, 41, 42, 43 'Zeichne Figur in Haus
                            Return Vector2.Transform(Map0GetLocalPos(pos - 26), transmatrices0(player))
                        Case Else 'Zeichne Figur auf Feld
                            Dim matrx As Matrix = transmatrices0((player + Math.Floor(pos / 10)) Mod 4)
                            Return Vector2.Transform(Map0GetLocalPos((pos Mod 10) + 4), matrx)
                    End Select
                Case GaemMap.Star
                    Select Case pos
                        Case -1 'Zeichne Figur in Homebase
                            Return Vector2.Transform(Map1GetLocalPos(figur), transmatrices1(player))
                        Case 48, 49 'Zeichne Figur in Haus
                            Return Vector2.Transform(Map1GetLocalPos(pos - 34), transmatrices1(player))
                        Case Else 'Zeichne Figur auf Feld
                            Dim matrx As Matrix = transmatrices1((player + Math.Floor(pos / 8)) Mod 6)
                            Return Vector2.Transform(Map1GetLocalPos((pos Mod 8) + 4), matrx)
                    End Select
                Case GaemMap.Octagon
                    Select Case pos
                        Case -1 'Zeichne Figur in Homebase
                            Return Vector2.Transform(Map2GetLocalPos(figur), transmatrices2(player))
                        Case 56, 57 'Zeichne Figur in Haus
                            Return Vector2.Transform(Map2GetLocalPos(pos - 42), transmatrices2(player))
                        Case Else 'Zeichne Figur auf Feld
                            Dim matrx As Matrix = transmatrices2((player + Math.Floor(pos / 7)) Mod 8)
                            Return Vector2.Transform(Map2GetLocalPos((pos Mod 7) + 4), matrx)
                    End Select
                Case GaemMap.Snakes
                    Select Case pos
                        Case -1 'Zeichne Figur in Homebase
                            Return Map3GetLocalPos(PlayFieldPos.Home1 + player, player)
                        Case 100 'Zeichne Figur in Haus
                            Return Map3GetLocalPos(PlayFieldPos.Haus1 + player, player)
                        Case Else 'Zeichne Figur auf Feld
                            Dim row As Integer = Math.Floor(pos / 10)
                            Return Map3GetLocalPos(PlayFieldPos.Feld1 + If(Mathf.IsEven(row) Or pos = 0, (pos Mod 10), 9 - (pos Mod 10)), row)
                    End Select
            End Select
        End Function

        Friend Function GetMapVectorPos(map As GaemMap, ps As PlayFieldPos, pl As Integer) As Vector2
            Select Case map
                Case GaemMap.Plus
                    Return Vector2.Transform(Map0GetLocalPos(ps), transmatrices0(pl))
                Case GaemMap.Star
                    Return Vector2.Transform(Map1GetLocalPos(ps), transmatrices1(pl))
                Case GaemMap.Octagon
                    Return Vector2.Transform(Map2GetLocalPos(ps), transmatrices2(pl))
                Case GaemMap.Snakes
                    If ps > PlayFieldPos.Home4 And ps < PlayFieldPos.Haus1 And Mathf.IsOdd(pl) Then Return Map3GetLocalPos(4 + (9 - ((ps - 4) Mod 10)), pl)
                    Return Map3GetLocalPos(ps, pl)
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Friend Function GetMapVectorPos(map As GaemMap, pos As Integer) As Vector2
            Select Case map
                Case GaemMap.Plus
                    Dim matrx As Matrix = transmatrices0(Math.Floor(pos / 10) Mod 4)
                    Return Vector2.Transform(Map0GetLocalPos((pos Mod 10) + 4), matrx)
                Case GaemMap.Star
                    Dim matrx As Matrix = transmatrices1(Math.Floor(pos / 8) Mod 6)
                    Return Vector2.Transform(Map1GetLocalPos((pos Mod 8) + 4), matrx)
                Case GaemMap.Octagon
                    Dim matrx As Matrix = transmatrices2(Math.Floor(pos / 7) Mod 8)
                    Return Vector2.Transform(Map2GetLocalPos((pos Mod 7) + 4), matrx)
                Case GaemMap.Snakes
                    Dim row As Integer = Math.Floor(pos / 10)
                    Dim collumn As Integer = (pos Mod 10)
                    If Mathf.IsOdd(row) Then Return Map3GetLocalPos(PlayFieldPos.Feld1 + (9 - collumn), row)
                    Return Map3GetLocalPos(PlayFieldPos.Feld1 + collumn, row)
                Case Else
                    Return Vector2.Zero
            End Select
        End Function

        Friend Function GetFieldSizes(map As GaemMap) As (Integer, Integer, Single, Integer)
            Select Case map
                Case GaemMap.Plus
                    Return (28, 20, 3.5, 35)
                Case GaemMap.Star
                    Return (22, 15, 2.3, 30)
                Case GaemMap.Octagon
                    Return (14, 12, 1.75, 18)
                Case GaemMap.Snakes
                    Return (14, 12, 1.75, 18)
                Case Else
                    Return (5, 5, 10, 35)
            End Select
        End Function

        Friend transmatrices0 As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
        Private rotato0 As Single() = {0, MathHelper.PiOver2, MathHelper.Pi, MathHelper.PiOver2 * 3} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
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
        Friend rotato1 As Single() = {MathHelper.TwoPi * 0 / 6, MathHelper.TwoPi * 1 / 6, MathHelper.TwoPi * 2 / 6, MathHelper.TwoPi * 3 / 6, MathHelper.TwoPi * 4 / 6, MathHelper.TwoPi * 5 / 6, MathHelper.TwoPi * 6 / 6} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
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
        Friend rotato2 As Single() = {0, MathHelper.TwoPi * 1 / 8, MathHelper.TwoPi * 2 / 8, MathHelper.TwoPi * 3 / 8, MathHelper.TwoPi * 4 / 8, MathHelper.TwoPi * 5 / 8, MathHelper.TwoPi * 6 / 8, MathHelper.TwoPi * 7 / 8, MathHelper.TwoPi * 8 / 8} 'Enthält Transform-Matritzen, welche die SPielfeld-Hitboxen um den Spielfeld-Mittelpunkt rotieren.
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
        Private Function Map3GetLocalPos(ps As PlayFieldPos, field_multiplier As Integer) As Vector2
            Select Case ps
                Case PlayFieldPos.Home1
                    Return New Vector2(50, 50)
                Case PlayFieldPos.Home2
                    Return New Vector2(100, 50)
                Case PlayFieldPos.Home3
                    Return New Vector2(50, 100)
                Case PlayFieldPos.Home4
                    Return New Vector2(100, 100)
                Case PlayFieldPos.Haus1
                    Return New Vector2(50, 850)
                Case PlayFieldPos.Haus2
                    Return New Vector2(100, 850)
                Case PlayFieldPos.Haus3
                    Return New Vector2(50, 900)
                Case PlayFieldPos.Haus4
                    Return New Vector2(100, 900)
                Case Else
                    Return New Vector2(150 + (ps - 4) * 70, 150 + field_multiplier * 70)
                    'Return New Vector2(800 - (ps - 4) * 30, 150 + field_multiplier * 30)
            End Select
        End Function

        Public Function GetFigureRectangle(map As GaemMap, pl As Integer, figure As Integer, Spielers As Player(), Center As Vector2) As Rectangle
            Select Case map
                Case GaemMap.Plus

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
                Case GaemMap.Star

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
                Case GaemMap.Octagon

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

        Friend Function GetPProtation(pl As Integer, map As GaemMap) As Single
            Select Case map
                Case GaemMap.Plus, GaemMap.Snakes
                    Return rotato0(pl)
                Case GaemMap.Star
                    Return rotato1(pl)
                Case Else
                    Return rotato2(pl) + 0.38
            End Select
        End Function
        Friend Function GetPPsize(map As GaemMap) As Integer
            Select Case map
                Case GaemMap.Plus
                    Return 140
                Case GaemMap.Star
                    Return 80
                Case Else
                    Return 60
            End Select
        End Function

        Friend Function GetSnakeFields(map As GaemMap) As (Integer, Integer)()
            Select Case map
                Case GaemMap.Snakes
                    Return {(10, 8), (20, 23)}
                Case Else
                    Return {}
            End Select
        End Function
    End Module
End Namespace