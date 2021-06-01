Namespace Game.CommonCards
    Public Class CardRenderer
        Inherits Renderer

        Public Wndow As ICardRendererWindow
        Sub New(window As ICardRendererWindow, Optional order As Integer = 0)
            MyBase.New(order)
            Wndow = window
        End Sub

        Public Overrides Sub Render(scene As Scene)

        End Sub

    End Class
End Namespace