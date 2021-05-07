Imports System.Collections.Generic
Imports System.IO
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.BetretenVerboten.Renderers
    Public Class Renderer3D
        Inherits Renderer

        Private figur_model As Model
        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private SpielfeldTextur As RenderTarget2D
        Private SpielfeldVerbindungen As Texture2D
        Private Pfeil As Texture2D
        Private MapBuffer As VertexBuffer

        Private SaucerModel As Model
        Private SaucerLift As Transition(Of Single)
        Private SaucerTarget As (Integer, Integer) = (-1, -1)
        Private SaucerMover As Transition(Of Vector2)
        Private SaucerPickedUp As Boolean = False
        Private SaucerDefaultPosition As New Vector3(0, 0, 1000)

        Private View As Matrix
        Private Projection As Matrix
        Private CamMatrix As Matrix

        Private Game As IGameWindow
        Private FigCount As Integer
        Private SpceCount As Integer
        Private Feld As Rectangle
        Private Center As Vector2
        Private transmatrices As Matrix() = {Matrix.CreateRotationZ(MathHelper.PiOver2 * 3), Matrix.Identity, Matrix.CreateRotationZ(MathHelper.PiOver2), Matrix.CreateRotationZ(MathHelper.Pi)}
        Friend AdditionalZPos As New Transition(Of Single)
        Sub New(game As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.Game = game
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            figur_model = scene.Content.Load(Of Model)("mesh\piece_std")
            SpielfeldVerbindungen = scene.Content.Load(Of Texture2D)("games\BV\playfield_connections_" & CInt(Game.Map))
            Pfeil = scene.Content.Load(Of Texture2D)("games\BV\arrow_right")

            Dim vertices As New List(Of VertexPositionColorTexture)
            vertices.Add(New VertexPositionColorTexture(New Vector3(-475, 475, 0), Color.White, Vector2.UnitX))
            vertices.Add(New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero))
            vertices.Add(New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One))
            vertices.Add(New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero))
            vertices.Add(New VertexPositionColorTexture(New Vector3(475, -475, 0), Color.White, Vector2.UnitY))
            vertices.Add(New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One))
            MapBuffer = New VertexBuffer(dev, GetType(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly)
            MapBuffer.SetData(vertices.ToArray)

            Select Case Game.Map
                Case GaemMap.Default4Players
                    SpceCount = 10
                    FigCount = 4
                Case GaemMap.Default6Players
                    SpceCount = 8
                    FigCount = 2
                Case GaemMap.Default8Players
                    SpceCount = 7
                    FigCount = 2
            End Select
            Feld = New Rectangle(500, 70, 950, 950)
            Center = Feld.Center.ToVector2

            SpielfeldTextur = New RenderTarget2D(
            dev,
            950,
            950,
            False,
            dev.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents) With {.Name = "TmpA"}

            RenderTexture = New Textures.RenderTexture()

            EffectA = New BasicEffect(dev) With {.Alpha = 1.0F,
            .VertexColorEnabled = True,
            .LightingEnabled = False,
            .TextureEnabled = True
        }

            batchlor = New Batcher(dev)

            SaucerModel = scene.Content.Load(Of Model)("mesh/saucer")
            SaucerMover = New Transition(Of Vector2) With {.Value = New Vector2(SaucerDefaultPosition.X, SaucerDefaultPosition.Y)}
            SaucerLift = New Transition(Of Single) With {.Value = SaucerDefaultPosition.Z}

        End Sub

        Public Overrides Sub Render(scene As Scene)

            Dim cam = Game.GetCamPos
            CamMatrix = Matrix.CreateFromYawPitchRoll(0, 0, cam.Yaw) * Matrix.CreateFromYawPitchRoll(0, cam.Pitch, cam.Roll) * Matrix.CreateTranslation(cam.Location)
            If Game.Status = SpielStatus.SaucerFlight Then CamMatrix = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(0), MathHelper.ToRadians(70), MathHelper.ToRadians(Nez.Time.TotalTime / 40 * 360)) * Matrix.CreateTranslation(New Vector3(0, 0, -300))
            View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(dev.Viewport.Width, dev.Viewport.Height, 1, 100000)


            dev.SetRenderTarget(SpielfeldTextur)
            dev.Clear(Color.Black)

            '(Normal field diameter, small field diameter, figure scale, arrow size)
            Dim sizes As (Integer, Integer, Single, Integer) = GetFieldSizes(Game.Map)

            batchlor.Begin()

            'Zeichne Verbindungen
            batchlor.Draw(SpielfeldVerbindungen, New Rectangle(0, 0, 950, 950), Color.White)
            batchlor.DrawHollowRect(New Rectangle(0, 0, 950, 950), Color.White, 5)

            'Draw fields
            For j = 0 To Game.Spielers.Length - 1
                'Zeichne Spielfeld
                For i = 0 To 17
                    Dim loc As Vector2 = New Vector2(475) + GetMapVectorPos(Game.Map, i, j)
                    Select Case i
                        Case PlayFieldPos.Haus1, PlayFieldPos.Haus2, PlayFieldPos.Haus3, PlayFieldPos.Haus4, PlayFieldPos.Home1, PlayFieldPos.Home2, PlayFieldPos.Home3, PlayFieldPos.Home4
                            If FigCount = 2 AndAlso (i = PlayFieldPos.Haus4 Or i = PlayFieldPos.Haus3 Or i = PlayFieldPos.Home3 Or i = PlayFieldPos.Home4) Then Continue For 'Skip houses and homes 3 + 4 is 6 player map is selected
                            batchlor.DrawCircle(loc, sizes.Item2, playcolor(j), 2, 25)
                        Case PlayFieldPos.Feld1
                            batchlor.DrawCircle(loc, sizes.Item1, playcolor(j), 3, 30)
                            DrawArrow(loc, playcolor(j), j, sizes.Item4)
                        Case Else
                            If i - 4 >= SpceCount Then Continue For
                            batchlor.DrawCircle(loc, sizes.Item1, Color.White, 3, 30)
                    End Select
                Next
            Next

            'Zeichne UFO-Felder
            For Each element In Game.SaucerFields
                batchlor.DrawCircle(New Vector2(475) + GetMapVectorPos(Game.Map, element), sizes.Item1, Color.SandyBrown, 5)
            Next


            batchlor.End()

            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)

            dev.RasterizerState = RasterizerState.CullNone
            dev.DepthStencilState = DepthStencilState.Default

            EffectA.World = Matrix.Identity
            EffectA.View = View
            EffectA.Projection = Projection
            EffectA.TextureEnabled = True
            EffectA.Texture = SpielfeldTextur

            For Each pass As EffectPass In EffectA.CurrentTechnique.Passes
                dev.SetVertexBuffer(MapBuffer)
                pass.Apply()

                dev.DrawPrimitives(PrimitiveType.TriangleList, 0, MapBuffer.VertexCount)
            Next

            'Zeichne Spielfiguren

            For j = 0 To Game.Spielers.Length - 1
                Dim pl As Player = Game.Spielers(j)
                If pl.Typ = SpielerTyp.None Then Continue For
                Dim color As Color = playcolor(j) * If((Game.Status = SpielStatus.WähleFigur Or Game.Status = SpielStatus.WähleOpfer) And j = Game.SpielerIndex And (pl.Typ = SpielerTyp.Local Or pl.Typ = SpielerTyp.Online), Game.SelectFader, 1.0F)
                For k As Integer = 0 To FigCount - 1
                    Dim scale As Single = If(Game.FigurFaderScales.ContainsKey((j, k)), Game.FigurFaderScales((j, k)).Value, 1)

                    If SaucerPickedUp And SaucerTarget.Item1 = j And SaucerTarget.Item2 = k Then 'UFO hat Spielfigur aufgenommen
                        DrawChr(SaucerMover.Value, color, sizes.Item3, SaucerLift.Value)
                    ElseIf Game.Status = SpielStatus.FahreFelder And Game.FigurFaderZiel.Item1 = j And Game.FigurFaderZiel.Item2 = k Then 'Spielfigur fährt nach vorne
                        DrawChr(Game.FigurFaderXY.Value, color, sizes.Item3, Game.FigurFaderZ.Value)
                    ElseIf pl.Spielfiguren(k) = -1 Then 'Zeichne Figur in Homebase
                        DrawChr(GetSpielfeldVector(j, k), playcolor(j), sizes.Item3, 0, scale)
                    Else 'Zeichne Figur in Haus
                        DrawChr(GetSpielfeldVector(j, k), color, sizes.Item3, 0, scale)
                    End If
                Next
            Next


            dev.RasterizerState = RasterizerState.CullCounterClockwise
            dev.DepthStencilState = DepthStencilState.Default
            For Each mesh As ModelMesh In SaucerModel.Meshes

                For Each effect As BasicEffect In mesh.Effects
                    Dim rotato As Matrix = Matrix.CreateRotationZ(MathHelper.ToRadians(Time.TotalTime / 15 * 360))
                    effect.World = rotato * Matrix.CreateTranslation(New Vector3(-SaucerMover.Value.X, -SaucerMover.Value.Y, -182 - SaucerLift.Value))
                    effect.View = View
                    effect.Projection = Projection
                    effect.Alpha = 1
                Next

                mesh.Draw()
            Next
        End Sub

        Private Sub DrawChr(pos As Vector2, color As Color, basescale As Single, Optional zpos As Integer = 0, Optional scale As Single = 1)
            For Each mesh In figur_model.Meshes
                For Each element As BasicEffect In mesh.Effects
                    element.VertexColorEnabled = False
                    element.TextureEnabled = False
                    element.DiffuseColor = Color.White.ToVector3
                    element.LightingEnabled = True '// turn on the lighting subsystem.
                    element.PreferPerPixelLighting = True
                    element.AmbientLightColor = Vector3.Zero
                    element.EmissiveColor = color.ToVector3 * 0.12
                    element.DirectionalLight0.Direction = New Vector3(0, 0.8, 1.5)
                    element.DirectionalLight0.DiffuseColor = color.ToVector3 * 0.6 '// a gray light
                    element.DirectionalLight0.SpecularColor = New Vector3(1, 1, 1) '// with white highlights
                    element.World = Matrix.CreateScale(basescale * scale * New Vector3(1, 1, 1)) * Matrix.CreateRotationY(Math.PI) * Matrix.CreateTranslation(-pos.X, -pos.Y, -zpos - AdditionalZPos.Value)
                    element.View = View
                    element.Projection = Projection
                Next
                mesh.Draw()
            Next
        End Sub

        Private Sub DrawArrow(vc As Vector2, color As Color, iteration As Integer, size As Integer)
            batchlor.Draw(Pfeil, New Rectangle(vc.X, vc.Y, size, size), Nothing, color, MathHelper.PiOver2 * ((iteration / Game.Spielers.Length) * 4 + 3), New Vector2(35) / 2, SpriteEffects.None, 0)
        End Sub

        Friend Sub TriggerSaucerAnimation(target As (Integer, Integer), ChangeFigure As Action, FinalAction As Action)
            Dim aimpos As Vector2 = GetSpielfeldVector(target.Item1, target.Item2)
            Dim startpos As Vector2 = New Vector2(SaucerDefaultPosition.X, SaucerDefaultPosition.Y)
            SaucerTarget = target

            SaucerMover = New Transition(Of Vector2)(New TransitionTypes.TransitionType_CriticalDamping(1000), startpos, aimpos, Nothing)
            Automator.Add(SaucerMover)

            SaucerLift = New Transition(Of Single)(New TransitionTypes.TransitionType_CriticalDamping(1000), SaucerDefaultPosition.Z, 0, GetBufferedTime(Sub()
                                                                                                                                                             SaucerPickedUp = True
                                                                                                                                                             ChangeFigure()
                                                                                                                                                             Dim newpos As Vector2 = GetSpielfeldVector(target.Item1, target.Item2)

                                                                                                                                                             SaucerMover = New Transition(Of Vector2)(New TransitionTypes.TransitionType_EaseInEaseOut(1500), aimpos, newpos, Nothing)
                                                                                                                                                             Automator.Add(SaucerMover)

                                                                                                                                                             SaucerLift = New Transition(Of Single)(New TransitionTypes.TransitionType_Parabole(1200), 0, 200, GetBufferedTime(Sub()
                                                                                                                                                                                                                                                                                   SaucerPickedUp = False
                                                                                                                                                                                                                                                                                   SaucerMover = New Transition(Of Vector2)(New TransitionTypes.TransitionType_Acceleration(1000), newpos, startpos, Sub() SaucerPickedUp = False)
                                                                                                                                                                                                                                                                                   Automator.Add(SaucerMover)


                                                                                                                                                                                                                                                                                   SaucerLift = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(1000), 0, SaucerDefaultPosition.Z, Sub() If FinalAction IsNot Nothing Then FinalAction())
                                                                                                                                                                                                                                                                                   Automator.Add(SaucerLift)
                                                                                                                                                                                                                                                                               End Sub))
                                                                                                                                                             Automator.Add(SaucerLift)
                                                                                                                                                         End Sub))
            Automator.Add(SaucerLift)
        End Sub

        Private Function GetBufferedTime(afteraction As Action(Of ITimer)) As Transition(Of Single).FinishedDelegate
            Return Sub()
                       Core.Schedule(0.3, afteraction)
                   End Sub
        End Function

        Private Function GetSpielfeldVector(player As Integer, figur As Integer, Optional increment As Integer = 0) As Vector2
            Return GetMapVectorPos(Game.Map, player, figur, Game.Spielers(player).Spielfiguren(figur) + increment)
        End Function
    End Class
End Namespace