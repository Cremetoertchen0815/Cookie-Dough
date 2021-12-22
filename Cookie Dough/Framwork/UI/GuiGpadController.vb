Imports System.Collections.Generic

Namespace Framework.UI
    Public Class GuiGpadController

        Private _btnAcc As VirtualButton
        Private _btnBack As VirtualButton
        Private _btnDir As VirtualJoystick

        Private _controls As New List(Of GuiControl)
        Public SelectedIndex As Integer

        Public Sub New()
            _btnAcc = New VirtualButton(New VirtualButton.GamePadButton(0, Microsoft.Xna.Framework.Input.Buttons.A))
            _btnBack = New VirtualButton(New VirtualButton.GamePadButton(0, Microsoft.Xna.Framework.Input.Buttons.B))
            _btnDir = New VirtualJoystick(True, New VirtualJoystick.GamePadDpad(), New VirtualJoystick.GamePadLeftStick())
        End Sub

        Public Sub RegisterControl(c As GuiControl)
            _controls.Add(c)
        End Sub

        Public Sub DeregisterControl(c As GuiControl)
            If _controls.Contains(c) Then _controls.Remove(c)
        End Sub

        Public Sub Update()

        End Sub
        Public Sub Render(batcher As Batcher)

        End Sub
    End Class
End Namespace