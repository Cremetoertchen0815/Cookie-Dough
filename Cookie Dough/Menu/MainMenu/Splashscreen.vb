Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Menu.MainMenu
    Public Class SplashScreen
        Inherits Scene

        Private enterBtn As VirtualButton
        Private transitioning As Boolean = False
        Public Overrides Sub Initialize()
            MyBase.Initialize()
            AddRenderer(New DefaultRenderer)
            AddPostProcessor(New QualityBloomPostProcessor(1)).SetPreset(QualityBloomPostProcessor.BloomPresets.SuperWide).SetStrengthMultiplayer(0.6).SetThreshold(0)
            ClearColor = Color.Black

            enterBtn = New VirtualButton().AddGamePadButton(0, Buttons.Start).AddKeyboardKey(Keys.Enter)

            Dim renderer = CreateEntity("renderer").AddComponent(Of SplashScreenRenderer)
            renderer.Tween("TransparancyTitle", 1.0F, 1).SetDelay(0.5).SetEaseType(Tweens.EaseType.QuadIn).Start()
            renderer.Tween("TransparancySubtitle", 1.0F, 1).SetDelay(1.5).SetEaseType(Tweens.EaseType.QuadIn).Start()
            renderer.Tween("TransparancyDescription", 1.0F, 1).SetDelay(2.5).SetEaseType(Tweens.EaseType.QuadIn).SetLoops(Tweens.LoopType.PingPong, -1).Start()
        End Sub

        Public Overrides Sub Update()
            MyBase.Update()
            If enterBtn.IsDown And Not transitioning Then transitioning = True : Core.StartSceneTransition(New FadeTransition(Function() New MainMenuScene))
        End Sub

        Private Class SplashScreenRenderer
            Inherits RenderableComponent

            Public Overrides Sub OnAddedToEntity()
                MyBase.OnAddedToEntity()
                Material = New Material() With {.SamplerState = SamplerState.PointClamp}
                topFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuTitle"))
                subtitleFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuSmol"))
                bottomFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MenuMain"))
            End Sub

            Private topFont As NezSpriteFont
            Private bottomFont As NezSpriteFont
            Private subtitleFont As NezSpriteFont
            Friend Property TransparancyTitle As Single = 0
            Friend Property TransparancySubtitle As Single = 0
            Friend Property TransparancyDescription As Single = 0
            Public Overrides ReadOnly Property Height As Single = 1080
            Public Overrides ReadOnly Property Width As Single = 1920F

            Public Overrides Sub Render(batcher As Batcher, camera As Camera)
                'Zeichne Startbildschirm
                Dim titletxt As String = "Cookie Dough"
                Dim subtitletxt As String = "Just another games collection."
                Dim starttxt As String = "---PRESS ENTER---"
                Dim copyrighttxt As String = "Copyright © Luminous Friends 2021"
                batcher.DrawString(topFont, titletxt, New Vector2((1920.0F - topFont.MeasureString(titletxt).X) / 2, 200), Color.Lime * TransparancyTitle)
                batcher.DrawString(subtitleFont, subtitletxt, New Vector2((1920.0F - subtitleFont.MeasureString(subtitletxt).X) / 2, 350), Color.Lime * TransparancySubtitle)
                batcher.DrawString(bottomFont, starttxt, New Vector2((1920.0F - bottomFont.MeasureString(starttxt).X) / 2, 800), Color.Lime * TransparancyDescription)
                batcher.DrawString(bottomFont, copyrighttxt, New Vector2((1920F - bottomFont.MeasureString(copyrighttxt).X) / 2, 950), Color.Lime * TransparancyTitle)
            End Sub
        End Class
    End Class
End Namespace