
Imports Microsoft.Xna.Framework
Namespace Game.Barrelled
    Public Interface IObject3D
        Property Distance As Single
        Property UserAlternateTrigger As Boolean
        ReadOnly Property BoundingBox As BoundingBox
        Sub ClickedFunction(sender As IGameWindow)
    End Interface
End Namespace