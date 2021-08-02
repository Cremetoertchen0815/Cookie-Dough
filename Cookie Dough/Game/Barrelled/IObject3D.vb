
Imports Microsoft.Xna.Framework
Namespace Game.Barrelled
    Public Interface IObject3D
        Property Distance As Single
        ReadOnly Property BoundingBox As BoundingBox
        Sub ClickedFunction(sender As IGameWindow)
    End Interface
End Namespace