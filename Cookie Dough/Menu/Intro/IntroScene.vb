Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input
Imports Nez
Imports Nez.UI

Namespace Menu.Intro
    Public Class IntroScene
        Inherits Scene

        Dim uicanvas As UICanvas
        Public Overrides Sub Initialize()
            MyBase.Initialize()
            ClearColor = Color.Black

            AddRenderer(New DefaultRenderer)

            uicanvas = CreateEntity("UI").AddComponent(Of UICanvas)
            uicanvas.Stage.KeyboardActionKey = Keys.Space
            uicanvas.Stage.GamepadActionButton = Buttons.A


            Dim table = uicanvas.Stage.AddElement(New VerticalGroup)
            table.SetPosition(500, 50)
            table.SetSpacing(50)
            table.AddElement(New Label("Laaaaaaaal").SetFontScale(5))
            table.AddElement(New Label("Looooooooool").SetFontScale(5))
            table.AddElement(New Label("jklghjkgfuikjfifui").SetFontScale(2))
            table.AddElement(New TextButton("Saas", Skin.CreateDefaultSkin))
            Dim lool = table.AddElement(New TextButton("Sooi", Skin.CreateDefaultSkin))
            AddHandler lool.OnClicked, Sub() Return

        End Sub

        Private Sub LoadSubmenu(button As Button)
            System.Console.WriteLine()
        End Sub
    End Class
End Namespace