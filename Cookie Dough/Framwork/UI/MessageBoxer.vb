Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports Microsoft.Xna.Framework.Input

Namespace Framework.UI
    Public Class MessageBoxer
        Inherits GlobalManager
        Implements IDrawable

        'IDrabable implementation
        Public ReadOnly Property DrawOrder As Integer = 0 Implements IDrawable.DrawOrder
        Public Property Visible As Boolean = True Implements IDrawable.Visible
        Public Event DrawOrderChanged As EventHandler(Of EventArgs) Implements IDrawable.DrawOrderChanged
        Public Event VisibleChanged As EventHandler(Of EventArgs) Implements IDrawable.VisibleChanged

        'Rendering & assets
        Private batcher As Batcher
        Private ButtonBase As Texture2D
        Private DispFont As NezSpriteFont
        Private Material As New Material() With {.SamplerState = SamplerState.AnisotropicClamp}

        'Layout & data
        Private MsgBoxArea As New Rectangle(710, 440, 500, 170)
        Private CurrentMessage As Message
        Private ShowMessageBox As Boolean = False
        Private WasEnabled As Boolean
        Private MessageStack As New List(Of Message)

        Sub New()
            batcher = New Batcher(Core.GraphicsDevice)
            ButtonBase = Core.Content.LoadTexture("btn_base_blali")
            DispFont = New NezSpriteFont(Core.Content.Load(Of SpriteFont)("font/MsgText"))
        End Sub

        Public Sub Draw(gameTime As GameTime) Implements IDrawable.Draw
            If Not ShowMessageBox Then Return

            batcher.Begin(Material, ScaleMatrix)

            If CurrentMessage.IsInputbox Then
            Else
                'Draw base
                batcher.DrawRect(MsgBoxArea, New Color(90, 90, 90))
                batcher.DrawLine(New Vector2(MsgBoxArea.Left, MsgBoxArea.Top), New Vector2(MsgBoxArea.Left, MsgBoxArea.Bottom), New Color(20, 20, 20), 4)
                batcher.DrawLine(New Vector2(MsgBoxArea.Left, MsgBoxArea.Top), New Vector2(MsgBoxArea.Right, MsgBoxArea.Top), New Color(20, 20, 20), 3)
                batcher.DrawLine(New Vector2(MsgBoxArea.Right, MsgBoxArea.Top), New Vector2(MsgBoxArea.Right, MsgBoxArea.Bottom), New Color(180, 180, 180), 4)
                batcher.DrawLine(New Vector2(MsgBoxArea.Left, MsgBoxArea.Bottom), New Vector2(MsgBoxArea.Right, MsgBoxArea.Bottom), New Color(180, 180, 180), 3)

                'Draw text
                batcher.DrawString(DispFont, StaticFunctions.WrapTextDifferently(CurrentMessage.Message, 49, False), MsgBoxArea.Location.ToVector2 + New Vector2(30, 10), Color.White)

                'Get total button length
                Dim tot_len As Single = 0F
                Dim padding As Single = 25.0F
                Dim margin As Single = 30.0F
                For i As Integer = 0 To CurrentMessage.Buttons.Length - 1
                    tot_len += DispFont.MeasureString(CurrentMessage.Buttons(i)).X + padding 'width + padding
                Next
                tot_len += (CurrentMessage.Buttons.Length - 1) * margin 'Add margin

                'Draw buttons
                Dim x_offset As Single = 0F
                For i As Integer = 0 To CurrentMessage.Buttons.Length - 1
                    Dim txt_width As Single = DispFont.MeasureString(CurrentMessage.Buttons(i)).X + padding
                    Dim rect As New Rectangle(MsgBoxArea.Center.X - tot_len / 2 + x_offset, MsgBoxArea.Y + MsgBoxArea.Height * 0.7, txt_width, 30)
                    batcher.Draw(ButtonBase, rect, Color.White)
                    batcher.DrawString(DispFont, CurrentMessage.Buttons(i), rect.Location.ToVector2 + New Vector2(padding / 2, 5), Color.White)

                    'Update offset
                    x_offset += txt_width + margin
                Next
            End If
            batcher.End()
        End Sub

        Dim lastmstate As MouseState
        Public Overrides Sub Update()
            If MessageStack.Count > 0 And Not ShowMessageBox Then
                WasEnabled = Core.Scene.Enabled
                Core.Scene.Enabled = False
                ShowMessageBox = True
                CurrentMessage = MessageStack(0)
                MessageStack.RemoveAt(0)
            End If

            If Not ShowMessageBox Then Return

            Dim mstate As MouseState = Mouse.GetState()
            Dim mpos As Vector2 = Vector2.Transform(mstate.Position.ToVector2, Matrix.Invert(ScaleMatrix))

            If CurrentMessage.IsInputbox Then
            Else
                Dim tot_len As Single = 0F
                Dim padding As Single = 25.0F
                Dim margin As Single = 30.0F
                For i As Integer = 0 To CurrentMessage.Buttons.Length - 1
                    tot_len += DispFont.MeasureString(CurrentMessage.Buttons(i)).X + padding 'width + padding
                Next
                tot_len += (CurrentMessage.Buttons.Length - 1) * margin 'Add margin

                'Draw buttons
                Dim x_offset As Single = 0F
                For i As Integer = 0 To CurrentMessage.Buttons.Length - 1
                    Dim txt_width As Single = DispFont.MeasureString(CurrentMessage.Buttons(i)).X + padding
                    If mstate.LeftButton = ButtonState.Pressed And lastmstate.LeftButton = ButtonState.Released AndAlso New Rectangle(MsgBoxArea.Center.X - tot_len / 2 + x_offset, MsgBoxArea.Y + MsgBoxArea.Height * 0.7, txt_width, 30).Contains(mpos) Then
                        If CurrentMessage.FinalActionMsgBox IsNot Nothing Then CurrentMessage.FinalActionMsgBox(i)
                        CloseMsgBox()
                    End If
                    'Update offset
                    x_offset += txt_width + margin
                Next
            End If

            lastmstate = mstate
        End Sub

        Public Sub EnqueueMsgbox(Prompt As String, finalaction As FinalMsgAction, buttons As String())
            MessageStack.Add(New Message With {.Message = Prompt, .IsInputbox = False, .Buttons = buttons, .FinalActionMsgBox = finalaction})
        End Sub

        Public Sub EnqueueInputbox(Prompt As String, finalaction As FinalInputAction, Optional title As String = "", Optional def As String = "")
            MessageStack.Add(New Message With {.Message = Prompt, .IsInputbox = True, .Buttons = {"OK", "Cancel"}, .FinalActionInputBox = finalaction})
        End Sub


        Private Sub CloseMsgBox()
            If MessageStack.Count > 0 Then
                CurrentMessage = MessageStack(0)
                MessageStack.RemoveAt(0)
            Else
                Core.Scene.Enabled = WasEnabled
                ShowMessageBox = False
            End If
        End Sub

        Public Delegate Sub FinalMsgAction(ByVal button As Integer)
        Public Delegate Sub FinalInputAction(ByVal text As String, ByVal button As Integer)

        Private Structure Message
            Public Message As String
            Public Buttons As String()
            Public FinalActionMsgBox As FinalMsgAction
            Public FinalActionInputBox As FinalInputAction
            Public IsInputbox As Boolean
            Public DefaultResponse As String
        End Structure
    End Class
End Namespace