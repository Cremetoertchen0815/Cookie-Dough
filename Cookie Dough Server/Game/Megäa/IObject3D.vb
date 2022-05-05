
Imports Microsoft.Xna.Framework
Namespace Game.Megäa
    Public Interface IObject3D
        Property Distance As Single
        Property BoundingBox As BoundingBox
        Sub ClickedFunction(sender As GameRoom)
    End Interface
End Namespace