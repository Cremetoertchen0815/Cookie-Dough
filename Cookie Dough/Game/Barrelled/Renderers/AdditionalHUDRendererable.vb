Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Barrelled.Renderers
    Public Class AdditionalHUDRendererable
        Inherits RenderableComponent

        Private r As Renderer

        'Begin animation
        Private SAnimCurrentText As String = ""
        Private SAnimFader As New Transition(Of Single)
        Private SAnimFont As NezSpriteFont
        Private SAnimSoundA As SoundEffect
        Private SAnimSoundB As SoundEffect

        Public Sub New(r As Renderer)
            Me.r = r
            SAnimFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/ButtonText"))
            SAnimSoundA = Core.Content.Load(Of SoundEffect)("sfx/countdown_A")
            SAnimSoundB = Core.Content.Load(Of SoundEffect)("sfx/countdown_B")
        End Sub

        Public Overrides Sub Render(batcher As Batcher, camera As Camera)
            'Render start animation
            Dim siz As Vector2 = SAnimFont.MeasureString(SAnimCurrentText)
            batcher.DrawString(SAnimFont, SAnimCurrentText, New Vector2(1920, 900) / 2, Color.White * SAnimFader.Value, 0, siz / 2, New Vector2((1 - SAnimFader.Value) + 2), SpriteEffects.None, 1)

            'Render minimap
            batcher.End()
            batcher.Begin(Entity.Transform.LocalToWorldTransform)
            batcher.Draw(r.RenderTexture.RenderTarget, LocalOffset)
        End Sub

        Public Overrides ReadOnly Property Bounds As RectangleF
            Get
                Return New RectangleF(LocalOffset, r.RenderTexture.RenderTarget.Bounds.Size.ToVector2)
            End Get
        End Property

#Region "Animation"
        Friend Sub TriggerStartAnimation(steps As String(), goaction As Action)
            For i As Integer = 0 To steps.Length - 1
                Dim i1 As Integer = i
                Core.Schedule(i, Sub()
                                     SAnimCurrentText = steps(i1)
                                     SAnimFader = New Transition(Of Single)(New TransitionTypes.TransitionType_Acceleration(600), 0F, 1.0F, Sub()
                                                                                                                                                If i1 = steps.Length - 1 Then
                                                                                                                                                    SAnimSoundB.Play()
                                                                                                                                                    If goaction IsNot Nothing Then goaction()
                                                                                                                                                Else
                                                                                                                                                    SAnimSoundA.Play()
                                                                                                                                                End If
                                                                                                                                            End Sub)
                                     Automator.Add(SAnimFader)
                                 End Sub)
            Next

            'Remove text
            Core.Schedule(steps.Length, Sub() SAnimFader = New Transition(Of Single))
        End Sub
#End Region
    End Class
End Namespace