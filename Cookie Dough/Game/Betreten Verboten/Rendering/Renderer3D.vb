Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Nez.Textures

Namespace Game.BetretenVerboten.Rendering
    Public Class Renderer3D
        Inherits Renderer

        'Assets
        Private figur_model As Model
        Private batchlor As Batcher
        Private dev As GraphicsDevice
        Private EffectA As BasicEffect
        Private SpielfeldTextur As RenderTarget2D
        Private SpielfeldVerbindungen As Texture2D
        Private Pfeil As Texture2D
        Private MapBuffer As VertexBuffer
        Private TableModel As Model
        Private TableMatrix As Matrix
        Private ResolutionMultiplier As Single = 2

        'Sliding stuff
        Private SlideMover As Transition(Of Vector2)
        Private SlideActive As Boolean = False
        Private SlideTarget As (Integer, Integer)
        Private SlideSFX As SoundEffect()
        Private SlideFields As (Integer, Integer)()


        'Saucer stuff
        Private SaucerModel As Model
        Private SaucerLift As Transition(Of Single)
        Private SaucerTarget As (Integer, Integer) = (-1, -1)
        Private SaucerMover As Transition(Of Vector2)
        Private SaucerPickedUp As Boolean = False
        Private SaucerDefaultPosition As New Vector3(0, 0, 1000)
        Private SaucerCam As Boolean = False

        'Intro
        Private BeginCurrentPlayer As Integer
        Friend BeginTriggered As Boolean
        Private BeginCam As Transition(Of Keyframe3D)

        'Camera and projection matrices
        Private View As Matrix
        Private Projection As Matrix
        Private CamMatrix As Matrix

        'Common fields
        Public BlurredContents As RenderTexture
        Private BlurEffect As GaussianBlurPostProcessor
        Private Game As IGameWindow
        Private FigCount As Integer
        Private SpceCount As Integer
        Private Feld As Rectangle
        Private FieldOffset As Vector2
        Friend AdditionalZPos As New Transition(Of Single)

        Public Sub New(game As IGameWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Me.Game = game

            RenderTexture = New RenderTexture()
            BlurredContents = New RenderTexture(1920, 1080)
        End Sub

        Public Overrides Sub OnAddedToScene(scene As Scene)
            MyBase.OnAddedToScene(scene)

            dev = Core.GraphicsDevice

            'Load models
            figur_model = scene.Content.Load(Of Model)("mesh/piece_std")
            SpielfeldVerbindungen = scene.Content.Load(Of Texture2D)("games/BV/playfield_connections_" & CInt(Game.Map))
            Pfeil = scene.Content.Load(Of Texture2D)("games/BV/arrow_right")

            'Load table 
            TableModel = scene.Content.Load(Of Model)("mesh/table")
            ApplyDefaultFX(TableModel, Projection)

            'Generate quad for map to be projected onto
            Dim vertices As New List(Of VertexPositionColorTexture) From {
                New VertexPositionColorTexture(New Vector3(-475, 475, 0), Color.White, Vector2.UnitX),
                New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero),
                New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One),
                New VertexPositionColorTexture(New Vector3(475, 475, 0), Color.White, Vector2.Zero),
                New VertexPositionColorTexture(New Vector3(475, -475, 0), Color.White, Vector2.UnitY),
                New VertexPositionColorTexture(New Vector3(-475, -475, 0), Color.White, Vector2.One)
            }
            MapBuffer = New VertexBuffer(dev, GetType(VertexPositionColorTexture), vertices.Count, BufferUsage.WriteOnly)
            MapBuffer.SetData(vertices.ToArray)

            'Load SFX for sliding
            SlideSFX = {scene.Content.LoadSoundEffect("sfx/slide_up_A"), scene.Content.LoadSoundEffect("sfx/slide_down_A")}

            BlurEffect = scene.AddPostProcessor(New GaussianBlurPostProcessor(0) With {.Enabled = False})
            BlurEffect.Effect.BlurAmount = 4

            'Load map related data
            Select Case Game.Map
                Case GaemMap.Plus
                    SpceCount = 10
                    FigCount = 4
                    FieldOffset = New Vector2(475)
                Case GaemMap.Star
                    SpceCount = 8
                    FigCount = 2
                    FieldOffset = New Vector2(475)
                Case GaemMap.Octagon
                    SpceCount = 7
                    FigCount = 2
                    FieldOffset = New Vector2(475)
                Case GaemMap.Snakes
                    SpceCount = 100
                    FigCount = 1
                    FieldOffset = New Vector2(0)
            End Select
            SlideFields = GetSnakeFields(Game.Map)
            Feld = New Rectangle(500, 70, 950, 950)

            SpielfeldTextur = New RenderTarget2D(
            dev,
            950 * ResolutionMultiplier,
            950 * ResolutionMultiplier,
            False,
            dev.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24, 0, RenderTargetUsage.DiscardContents) With {.Name = "TmpA"}


            EffectA = New BasicEffect(dev) With {.Alpha = 1.0F,
            .VertexColorEnabled = True,
            .LightingEnabled = False,
            .TextureEnabled = True,
            .FogEnabled = True,
            .FogColor = Vector3.Zero,
            .FogStart = 0.5F,
            .FogEnd = 2.5F
        }

            batchlor = New Batcher(dev)

            SaucerModel = scene.Content.Load(Of Model)("mesh/saucer")
            SaucerMover = New Transition(Of Vector2) With {.Value = New Vector2(SaucerDefaultPosition.X, SaucerDefaultPosition.Y)}
            SaucerLift = New Transition(Of Single) With {.Value = SaucerDefaultPosition.Z}

        End Sub

#Region "Rendering"

        Public Overrides Sub Render(scene As Scene)

            CamMatrix = If(BeginTriggered, BeginCam.Value, Game.GetCamPos).GetMatrix
            If Game.Status = SpielStatus.SaucerFlight Or SaucerCam Then CamMatrix = Matrix.CreateFromYawPitchRoll(MathHelper.ToRadians(0), MathHelper.ToRadians(70), MathHelper.ToRadians(Nez.Time.TotalTime / 40 * 360)) * Matrix.CreateTranslation(New Vector3(0, 0, -300))
            View = CamMatrix * Matrix.CreateScale(1, 1, 1 / 1080) * Matrix.CreateLookAt(New Vector3(0, 0, -1), New Vector3(0, 0, 0), Vector3.Up)
            Projection = Matrix.CreateScale(100) * Matrix.CreatePerspective(1920, 1080, 1, 100000)


            dev.SetRenderTarget(SpielfeldTextur)
            dev.Clear(Color.Black)

            '(Normal field diameter, small field diameter, figure scale, arrow size)
            Dim sizes As (Integer, Integer, Single, Integer) = GetFieldSizes(Game.Map)

            Material.SamplerState = SamplerState.AnisotropicWrap
            Material.BlendState = BlendState.NonPremultiplied
            batchlor.Begin(Material, Matrix.CreateScale(ResolutionMultiplier))

            'Draw fields
            For j = 0 To Game.Spielers.Length - 1
                'Draw player thumbnail
                Dim ptA As Vector2 = FieldOffset + GetMapVectorPos(Game.Map, PlayFieldPos.Home1, j)
                Dim ptB As Vector2 = FieldOffset + GetMapVectorPos(Game.Map, If(Game.Map = GaemMap.Plus, PlayFieldPos.Home4, PlayFieldPos.Home2), j)
                batchlor.Draw(Game.Spielers(j).Thumbnail, New Rectangle((ptA + (ptB - ptA) * 0.5).ToPoint, New Point(GetPPsize(Game.Map))), Nothing, Color.White * 0.85, GetPProtation(j, Game.Map), Game.Spielers(j).Thumbnail.Bounds.Size.ToVector2 * 0.5, SpriteEffects.None, 0)
            Next

            batchlor.End()
            Material.BlendState = BlendState.AlphaBlend
            batchlor.Begin(Material, Matrix.CreateScale(ResolutionMultiplier))

            'Draw field connections + border
            batchlor.Draw(SpielfeldVerbindungen, New Rectangle(0, 0, 950, 950), Color.White)
            batchlor.DrawHollowRect(New Rectangle(0, 0, 950, 950), Color.White, 5)

            'Draw slide fields
            For Each element In SlideFields
                Dim source = FieldOffset + GetMapVectorPos(Game.Map, element.Item1)
                Dim destination = FieldOffset + GetMapVectorPos(Game.Map, element.Item2)
                Dim dir_vec = Vector2.Normalize(destination - source)
                Dim clor = If(element.Item1 < element.Item2, New Color(50, 120, 50), New Color(180, 30, 30))
                Dim radius = sizes.Item1 + 2
                batchlor.DrawLine(source + dir_vec * radius, destination, clor, 5)
                batchlor.DrawCircle(destination, 3, clor, 6)
                batchlor.DrawCircle(source, radius, clor, 6)
            Next

            'Draw playfield
            If Game.Map = GaemMap.Snakes Then
                'Draw snek field
                For j = 0 To Game.Spielers.Length - 1
                    batchlor.DrawCircle(GetMapVectorPos(Game.Map, PlayFieldPos.Home1 + j, j), sizes.Item2, playcolor(j), 2, 25)
                    batchlor.DrawCircle(GetMapVectorPos(Game.Map, PlayFieldPos.Haus1 + j, j), sizes.Item2, playcolor(j), 2, 25)
                Next
                For i = 0 To SpceCount - 1
                    batchlor.DrawCircle(GetMapVectorPos(Game.Map, PlayFieldPos.Feld1 + i Mod 10, Math.Floor(i / 10)), sizes.Item1, Color.White, 3, 30)
                Next
            Else
                For j = 0 To Game.Spielers.Length - 1
                    'Draw regular playfield
                    For i = 0 To 17
                        Dim loc As Vector2 = FieldOffset + GetMapVectorPos(Game.Map, i, j)
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
            End If

            'Draw UFO fields
            For Each element In Game.SaucerFields
                batchlor.DrawCircle(FieldOffset + GetMapVectorPos(Game.Map, element), sizes.Item1, Color.SandyBrown, 5)
            Next

            'Draw suicide fields
            For i As Integer = 0 To Game.Spielers.Length - 1
                If Game.Spielers(i).SuicideField < 0 Then Continue For
                Dim center As Vector2 = FieldOffset + GetMapVectorPos(Game.Map, Game.Spielers(i).SuicideField)
                Dim line_coords As Vector2() = {center - Vector2.One * 20, center + Vector2.One * 20, center - New Vector2(-20, 20), center - New Vector2(20, -20)}
                batchlor.DrawLine(line_coords(0), line_coords(1), playcolor(i), 3)
                batchlor.DrawLine(line_coords(2), line_coords(3), playcolor(i), 3)
            Next


            batchlor.End()

            '---RENDERER 3D---

            dev.SetRenderTarget(RenderTexture)
            dev.Clear(Color.Transparent)

            batchlor.Begin()
            batchlor.Draw(Game.BGTexture, New Rectangle(0, 0, 1920, 1080))
            batchlor.End()


            dev.RasterizerState = RasterizerState.CullNone
            dev.DepthStencilState = DepthStencilState.Default
            dev.SamplerStates(0) = SamplerState.AnisotropicClamp

            'Render map
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

            'Draw Table
            TableMatrix = Matrix.CreateScale(New Vector3(3.2, 3.2, 3) * 150) * Matrix.CreateTranslation(New Vector3(0, 0, 590))
            For Each element In TableModel.Meshes
                ApplyFX(element, Color.White, element.ParentBone.ModelTransform * TableMatrix)
                element.Draw()
            Next

            'Zeichne Spielfiguren

            For j = 0 To Game.Spielers.Length - 1
                Dim pl As Player = Game.Spielers(j)
                If pl.Typ = SpielerTyp.None Then Continue For
                Dim color As Color = playcolor(j) * If((Game.Status = SpielStatus.WähleFigur Or Game.Status = SpielStatus.WähleOpfer) And j = Game.SpielerIndex And (pl.Typ = SpielerTyp.Local Or pl.Typ = SpielerTyp.Online), Game.SelectFader, 1.0F)
                For k As Integer = 0 To FigCount - 1
                    Dim scale As Single = If(Game.FigurFaderScales.ContainsKey((j, k)), Game.FigurFaderScales((j, k)).Value, 1)
                    If SlideActive And SlideTarget.Item1 = j And SlideTarget.Item2 = k Then 'Figure slidet
                        DrawChr(SlideMover.Value, color, sizes.Item3, 0)
                    ElseIf SaucerPickedUp And SaucerTarget.Item1 = j And SaucerTarget.Item2 = k Then 'UFO hat Spielfigur aufgenommen
                        DrawChr(SaucerMover.Value, color, sizes.Item3, SaucerLift.Value)
                    ElseIf Game.Status = SpielStatus.FahreFelder And Game.FigurFaderZiel.Item1 = j And Game.FigurFaderZiel.Item2 = k Then 'Spielfigur fährt nach vorne
                        DrawChr(Game.FigurFaderXY.Value, color, sizes.Item3, Game.FigurFaderZ.Value)
                    ElseIf pl.Spielfiguren(k) = -1 Then 'Zeichne Figur in Homebase
                        DrawChr(GetSpielfeldVector(j, k), playcolor(j), sizes.Item3, 0, scale)
                    Else 'Zeichne sonstige Figuren
                        DrawChr(GetSpielfeldVector(j, k), color, sizes.Item3, 0, scale)
                    End If
                Next
            Next


            dev.RasterizerState = RasterizerState.CullCounterClockwise
            dev.DepthStencilState = DepthStencilState.Default
            For Each mesh As ModelMesh In SaucerModel.Meshes

                For Each effect As BasicEffect In mesh.Effects
                    Dim rotato As Matrix = Matrix.CreateRotationZ(MathHelper.ToRadians(Time.TotalTime / 15 * 360))
                    effect.World = rotato * Matrix.CreateTranslation(New Vector3(-SaucerMover.Value.X, -SaucerMover.Value.Y, -182 - SaucerLift.Value) + If(Game.Map > 2, New Vector3(475, 475, 0), Vector3.Zero))
                    effect.View = View
                    effect.Projection = Projection
                    effect.Alpha = 1
                    effect.FogEnabled = True
                    effect.FogColor = Vector3.Zero
                    effect.FogStart = 0.5F
                    effect.FogEnd = 2.5F
                Next

                mesh.Draw()
            Next

            '---END OF RENDERER3D---

            'Apply blur to the contents
            BlurEffect.Process(RenderTexture, BlurredContents)
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
                    element.World = Matrix.CreateScale(basescale * scale * New Vector3(1, 1, 1)) * Matrix.CreateRotationY(Math.PI) * Matrix.CreateTranslation(New Vector3(-pos.X, -pos.Y, -zpos - AdditionalZPos.Value) + If(Game.Map > 2, New Vector3(475, 475, 0), Vector3.Zero))
                    element.View = View
                    element.Projection = Projection
                    element.FogEnabled = True
                    element.FogColor = Vector3.Zero
                    element.FogStart = 0.5F
                    element.FogEnd = 2.5F
                Next
                mesh.Draw()
            Next
        End Sub

        Private Sub DrawArrow(vc As Vector2, color As Color, iteration As Integer, size As Integer)
            batchlor.Draw(Pfeil, New Rectangle(vc.X, vc.Y, size, size), Nothing, color, MathHelper.PiOver2 * (iteration / Game.Spielers.Length * 4 + 3), New Vector2(35) / 2, SpriteEffects.None, 0)
        End Sub
        Private Sub ApplyFX(mesh As ModelMesh, DiffuseColor As Color, world As Matrix, Optional yflip As Integer = 1)
            For Each effect As BasicEffect In mesh.Effects
                effect.DirectionalLight2.Direction = New Vector3(1, -1 * yflip, 1)
                effect.DiffuseColor = DiffuseColor.ToVector3
                effect.World = world
                effect.View = View
                effect.Projection = Projection
            Next
        End Sub

#End Region

#Region "Animation"
        Friend Sub TriggerSaucerAnimation(target As (Integer, Integer), ChangeFigure As Action, FinalAction As Action)
            Dim aimpos As Vector2 = GetSpielfeldVector(target.Item1, target.Item2)
            Dim startpos As Vector2 = New Vector2(SaucerDefaultPosition.X, SaucerDefaultPosition.Y)
            SaucerTarget = target

            'Play sound
            SFX(8).Play()

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

        Friend Sub TriggerSlideAnimation(target As (Integer, Integer), aim As Integer, FinalAction As Action)
            Dim src As Integer = Game.Spielers(target.Item1).Spielfiguren(target.Item2)
            Dim aimpos As Vector2 = GetMapVectorPos(Game.Map, aim)
            Dim startpos As Vector2 = GetMapVectorPos(Game.Map, src)
            SlideTarget = target

            Core.Schedule(0.5, Sub()
                                   SlideActive = True
                                   SlideMover = New Transition(Of Vector2)(New TransitionTypes.TransitionType_Acceleration(800), startpos, aimpos, Sub()
                                                                                                                                                       Game.Spielers(target.Item1).Spielfiguren(target.Item2) = aim
                                                                                                                                                       SlideActive = False
                                                                                                                                                       FinalAction()
                                                                                                                                                   End Sub)
                                   Automator.Add(SlideMover)

                                   'Play sound
                                   SlideSFX(If(aim > src, 0, 1)).Play()
                               End Sub)
        End Sub

        Friend Sub TriggerStartAnimation(TeamMode As Boolean, FinalAction As Action)
            Dim plcount As Integer = 0
            BeginCurrentPlayer = -1
            BeginTriggered = True
            Game.HUDmotdLabel.Active = True
            Game.HUDNameBtn.Active = True
            Game.HUDNameBtn.Location = New Vector2(500, 650)
            Game.HUDNameBtn.Font = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuTitle"))

            'Get actual player count
            For Each element In Game.Spielers
                If element.Typ <> SpielerTyp.None Then plcount += 1
            Next

            'Prepare team shit
            Dim act As Action = AddressOf PlayerAnimation
            If TeamMode Then act = Sub()
                                       BeginCurrentPlayer = -2
                                       TeamPlayerAnimationCheck()
                                   End Sub
            CurrentlyTeamA = True

            'Move camera down
            BeginCam = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_Acceleration(2500), New Keyframe3D(79, -80, 560, 4.24, 1.39, 0.17, False), New Keyframe3D(-79, -90, -169, 4.36, 1.39, 0.17, False), Sub() act())
            Automator.Add(BeginCam)

            'Continue with game
            Core.Schedule(3 * plcount + If(TeamMode, 8.8, 4.8), Sub() FinalAction())
        End Sub

        Private CurrentlyTeamA As Boolean
        Private Sub TeamPlayerAnimation()

            'Find next player, if available
            BeginCurrentPlayer += 2
            For i As Integer = BeginCurrentPlayer To Game.Spielers.Length
                'Let the counter overflow if no valid player can be found
                If i >= Game.Spielers.Length OrElse Game.Spielers(i).OriginalType <> SpielerTyp.None Then BeginCurrentPlayer = i : Exit For
            Next

            'End loop if end reached
            If BeginCurrentPlayer >= Game.Spielers.Length Then

                If CurrentlyTeamA Then
                    CurrentlyTeamA = False
                    BeginCurrentPlayer = -1
                    TeamPlayerAnimationCheck()
                Else
                    'Prepare HUD
                    Game.HUDmotdLabel.Active = False
                    Game.HUDNameBtn.Active = False
                    Game.HUDNameBtn.Location = New Vector2(500, 20)
                    Game.HUDNameBtn.Font = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText"))

                    'Move camera down and disable camera overtake
                    BeginCam = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_CriticalDamping(1500), Game.StartCamPoses(0), Game.StartCamPoses(1), Sub() BeginTriggered = False)
                    Automator.Add(BeginCam)
                End If

                Return 'End this looping hell
            End If

            'Play sound
            Game.Spielers(BeginCurrentPlayer).CustomSound(0).Play()

            'Set presentation stuff
            Game.HUDmotdLabel.Active = True
            Game.HUDNameBtn.Color = hudcolors(BeginCurrentPlayer)
            Game.HUDNameBtn.Text = Game.Spielers(BeginCurrentPlayer).Name
            Game.HUDmotdLabel.Text = Game.Spielers(BeginCurrentPlayer).MOTD
            Game.HUDmotdLabel.Location = New Vector2(1920 / 2 - Game.HUDmotdLabel.Font.MeasureString(Game.HUDmotdLabel.Text).X / 2, 730)

            'Move camera down
            BeginCam = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_Linear(3000), GetIntroKeyframes(Game.Map, BeginCurrentPlayer, False), GetIntroKeyframes(Game.Map, BeginCurrentPlayer, True), AddressOf TeamPlayerAnimation)
            Automator.Add(BeginCam)
        End Sub

        Private Sub TeamPlayerAnimationCheck()
            If CurrentlyTeamA Then
                Game.HUDNameBtn.Color = Color.Red
                Game.HUDNameBtn.Text = "Team " & Game.TeamNames(0)
            Else
                Game.HUDNameBtn.Color = Color.Blue
                Game.HUDNameBtn.Text = "Team " & Game.TeamNames(1)
            End If

            Game.HUDNameBtn.Active = True
            Game.HUDmotdLabel.Active = False
            SaucerCam = True
            Core.Schedule(2, Sub()
                                 SaucerCam = False
                                 TeamPlayerAnimation()
                             End Sub)
        End Sub

        Private Sub PlayerAnimation()

            'Find next player, if available
            BeginCurrentPlayer += 1
            For i As Integer = BeginCurrentPlayer To Game.Spielers.Length
                'Let the counter overflow if no valid player can be found
                If i >= Game.Spielers.Length OrElse Game.Spielers(i).OriginalType <> SpielerTyp.None Then BeginCurrentPlayer = i : Exit For
            Next

            'End loop if end reached
            If BeginCurrentPlayer >= Game.Spielers.Length Then

                'Prepare HUD
                Game.HUDmotdLabel.Active = False
                Game.HUDNameBtn.Active = False
                Game.HUDNameBtn.Location = New Vector2(500, 20)
                Game.HUDNameBtn.Font = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText"))

                'Move camera down and disable camera overtake
                BeginCam = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_CriticalDamping(1500), Game.StartCamPoses(0), Game.StartCamPoses(1), Sub() BeginTriggered = False)
                Automator.Add(BeginCam)

                Return 'End this looping hell
            End If

            'Play sound
            Game.Spielers(BeginCurrentPlayer).CustomSound(0).Play()

            'Set presentation stuff
            Game.HUDNameBtn.Color = hudcolors(BeginCurrentPlayer)
            Game.HUDNameBtn.Text = Game.Spielers(BeginCurrentPlayer).Name
            Game.HUDmotdLabel.Text = Game.Spielers(BeginCurrentPlayer).MOTD
            Game.HUDmotdLabel.Location = New Vector2(1920 / 2 - Game.HUDmotdLabel.Font.MeasureString(Game.HUDmotdLabel.Text).X / 2, 730)

            'Move camera down
            BeginCam = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_Linear(3000), GetIntroKeyframes(Game.Map, BeginCurrentPlayer, False), GetIntroKeyframes(Game.Map, BeginCurrentPlayer, True), AddressOf PlayerAnimation)
            Automator.Add(BeginCam)
        End Sub

        Private Function GetBufferedTime(afteraction As Action(Of ITimer)) As Transition(Of Single).FinishedDelegate
            Return Sub()
                       Core.Schedule(0.3, afteraction)
                   End Sub
        End Function

#End Region

        Private Function GetSpielfeldVector(player As Integer, figur As Integer, Optional increment As Integer = 0) As Vector2
            Return GetMapVectorPos(Game.Map, player, figur, Game.Spielers(player).Spielfiguren(figur) + increment)
        End Function
    End Class
End Namespace