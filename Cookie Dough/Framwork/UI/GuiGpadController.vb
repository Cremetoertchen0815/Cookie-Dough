Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports System.Linq

Namespace Framework.UI
    Public Class GuiGpadController

        Private _btnAcc As VirtualButton
        Private _btnBack As VirtualButton
        Private _btnDir As VirtualJoystick

        Private lastDir As Vector2
        Private _controls As New List(Of GuiControl)
        Public SelectedIndex As Integer

        Public Sub New()
            _btnAcc = New VirtualButton(New VirtualButton.GamePadButton(0, Microsoft.Xna.Framework.Input.Buttons.A))
            _btnBack = New VirtualButton(New VirtualButton.GamePadButton(0, Microsoft.Xna.Framework.Input.Buttons.B))
            _btnDir = New VirtualJoystick(True, New VirtualJoystick.GamePadDpad(0, True), New VirtualJoystick.GamePadLeftStick(0, 0.4F))
        End Sub

        Public Sub RegisterControl(c As GuiControl)
            _controls.Add(c)
        End Sub

        Public Sub DeregisterControl(c As GuiControl)
            If _controls.Contains(c) Then _controls.Remove(c)
        End Sub

        Public Sub Update()
            If 0 > _controls.Count Then Return

            'Hozizontal shit
            Dim curr = _controls(SelectedIndex)
            If _btnDir.Value.X < 0 And lastDir.X >= 0 Then
                Dim lst = _controls.Where(Function(x) x.Active And x.OuterBounds.Right < curr.OuterBounds.Left).OrderByDescending(Function(x) x.OuterBounds.X).ThenBy(Function(x) Math.Abs(x.OuterBounds.Y - curr.OuterBounds.Y))(0)
                If lst IsNot Nothing Then SelectedIndex = _controls.IndexOf(lst)
            ElseIf _btnDir.Value.X > 0 And lastDir.X <= 0 Then
                Dim lst = _controls.Where(Function(x) x.Active And x.OuterBounds.Left > curr.OuterBounds.Right).OrderBy(Function(x) x.OuterBounds.X).ThenBy(Function(x) Math.Abs(x.OuterBounds.Y - curr.OuterBounds.Y))(0)
                If lst IsNot Nothing Then SelectedIndex = _controls.IndexOf(lst)
            End If

            'Vertical shit
            curr = _controls(SelectedIndex)
            If _btnDir.Value.Y > 0 And lastDir.Y <= 0 Then
                Dim lst = _controls.Where(Function(x) x.Active And x.OuterBounds.Bottom < curr.OuterBounds.Top).OrderByDescending(Function(x) x.OuterBounds.Y).ThenBy(Function(x) Math.Abs(x.OuterBounds.X - curr.OuterBounds.X))(0)
                If lst IsNot Nothing Then SelectedIndex = _controls.IndexOf(lst)
            ElseIf _btnDir.Value.Y < 0 And lastDir.Y >= 0 Then
                Dim lst = _controls.Where(Function(x) x.Active And x.OuterBounds.Top > curr.OuterBounds.Bottom).OrderBy(Function(x) x.OuterBounds.Y).ThenBy(Function(x) Math.Abs(x.OuterBounds.X - curr.OuterBounds.X))(0)
                If lst IsNot Nothing Then SelectedIndex = _controls.IndexOf(lst)
            End If


            lastDir = _btnDir.Value
            SelectedIndex = Mathf.Clamp(SelectedIndex, 0, _controls.Count - 1)

            'Change selected control when current control is inactive
            If Not _controls(SelectedIndex).Active Then
                Dim lst = _controls.Where(Function(x) x.Active).OrderBy(Function(x) Math.Abs(x.OuterBounds.X - curr.OuterBounds.X)).ThenBy(Function(x) Math.Abs(x.OuterBounds.Y - curr.OuterBounds.Y))(0)
                SelectedIndex = If(lst IsNot Nothing, _controls.IndexOf(lst), -1)
            End If

            If SelectedIndex > -1 And SelectedIndex < _controls.Count AndAlso _btnAcc.IsPressed Then
                _controls(SelectedIndex).Activate()
            End If
        End Sub
        Public Sub Render(batcher As Batcher)
            If SelectedIndex > -1 And SelectedIndex < _controls.Count Then
                Dim bounds = _controls(SelectedIndex).OuterBounds
                bounds.Inflate(5, 5)
                batcher.DrawHollowRect(bounds, Color.White, 7)
            End If
        End Sub
    End Class
End Namespace