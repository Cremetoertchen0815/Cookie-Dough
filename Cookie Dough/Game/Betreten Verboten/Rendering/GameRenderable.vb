Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.BetretenVerboten.Rendering
    Public Class GameRenderable
        Inherits Framework.UI.GuiControl

        Private window As IGameWindow
        Private WürfelMask As Texture2D
        Private WürfelAugen As Texture2D
        Private WürfelRahmen As Texture2D

        Public Overrides ReadOnly Property InnerBounds As Rectangle
            Get
                Return Rectangle.Empty
            End Get
        End Property

        Public Sub New(window As IGameWindow)
            MyBase.New()
            Me.window = window
            Me.GamepadInteractable = False
        End Sub

        Public Overrides Sub Init(system As Framework.UI.IParent)
            WürfelMask = Core.Content.Load(Of Texture2D)("games/BV/würfel_mask")
            WürfelAugen = Core.Content.Load(Of Texture2D)("games/BV/würfel_augen")
            WürfelRahmen = Core.Content.Load(Of Texture2D)("games/BV/würfel_rahmen")
        End Sub

        Public Overrides Sub Update(cstate As Framework.UI.GuiInput, offset As Vector2)

        End Sub

        Public Overrides Sub Render(batcher As Batcher, color As Color)
            Core.GraphicsDevice.DepthStencilState = DepthStencilState.None

            'Draw BG
            batcher.Draw(window.GameTexture, New Rectangle(0, 0, 1920, 1080), Nothing, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0)

            'Zeichne Haupt-Würfel
            If window.ShowDice And window.SpielerIndex = window.UserIndex Then
                Dim rect = New Rectangle(1570, 700, 300, 300)
                If RedrawBackground AndAlso BackgroundImage IsNot Nothing Then
                    DrawBlur(batcher, rect, rect.Size.ToVector2 / 5)
                End If
                batcher.Draw(WürfelMask, rect, BackgroundColor)
                batcher.Draw(WürfelAugen, rect, GetWürfelSourceRectangle(window.WürfelAktuelleZahl), color)
                batcher.Draw(WürfelRahmen, rect, Color.Lerp(color, Color.White, 0.4))
            End If
            'Zeichne Mini-Würfel
            If (window.Status = SpielStatus.Würfel Or window.Status = SpielStatus.WähleFigur Or window.Status = SpielStatus.FahreFelder) And window.SpielerIndex = window.UserIndex And window.UserIndex > -1 Then
                For i As Integer = 0 To window.WürfelWerte.Length - 1
                    If window.SpielerIndex = window.UserIndex And window.WürfelWerte(i) > 0 Then
                        batcher.Draw(WürfelAugen, New Rectangle(1590 + i * 70, 600, 50, 50), GetWürfelSourceRectangle(window.WürfelWerte(i)), color)
                        batcher.Draw(WürfelRahmen, New Rectangle(1590 + i * 70, 600, 50, 50), Color.Lerp(color, Color.White, 0.4))
                    End If
                    If window.Spielers(window.SpielerIndex).Typ = SpielerTyp.CPU And Not window.DreifachWürfeln And window.WürfelWerte(i) <> 6 Then Exit For
                Next
            End If
        End Sub

        Private Sub DrawBlur(batcher As Batcher, rect As Rectangle, zoom As Vector2)
            batcher.Draw(BackgroundImage, rect, New Rectangle(rect.X + zoom.X * 0.5F, rect.Y + zoom.Y * 0.5F, rect.Width - zoom.X, rect.Height - zoom.Y), Color.White)
        End Sub

        Private Function GetWürfelSourceRectangle(augenzahl As Integer) As Rectangle
            Select Case augenzahl
                Case 1
                    Return New Rectangle(0, 0, 260, 260)
                Case 2
                    Return New Rectangle(260, 0, 260, 260)
                Case 3
                    Return New Rectangle(520, 0, 260, 260)
                Case 4
                    Return New Rectangle(0, 260, 260, 260)
                Case 5
                    Return New Rectangle(260, 260, 260, 260)
                Case 6
                    Return New Rectangle(520, 260, 260, 260)
                Case Else
                    Return New Rectangle(0, 0, 0, 0)
            End Select
        End Function

        Public Overrides Sub Activate()
            Throw New NotImplementedException()
        End Sub
    End Class
End Namespace