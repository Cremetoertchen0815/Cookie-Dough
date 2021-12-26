Imports Microsoft.Xna.Framework

Namespace Framework.UI.Gamepad
    Public Interface ISelectableControl
        ReadOnly Property Active As Boolean
        ReadOnly Property Bounds As Rectangle
        Sub Activate()
    End Interface
End Namespace