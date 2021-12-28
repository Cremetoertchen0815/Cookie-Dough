Imports Microsoft.Xna.Framework

Namespace Framework.UI.Gamepad
    Public Class IDPControl
        Implements ISelectableControl

        Public Property Active As Boolean = True Implements ISelectableControl.Active

        Public Property Bounds As Rectangle Implements ISelectableControl.Bounds

        Public Sub Activate() Implements ISelectableControl.Activate
            ActivationDelegate()
        End Sub

        Public ActivationDelegate As Action

        Public Sub New(bounds As Rectangle, activation As Action)
            Me.Bounds = bounds
            ActivationDelegate = activation
        End Sub
    End Class
End Namespace