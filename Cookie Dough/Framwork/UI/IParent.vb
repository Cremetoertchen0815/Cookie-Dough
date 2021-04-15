﻿Imports Microsoft.Xna.Framework
Imports Nez

Namespace Framework.UI
    Public Interface IParent
        Sub Init(parent As IParent)
        Sub Update(cstate As GuiInput, offset As Vector2)
        Sub Render(batcher As Batcher)
        Property Font As NezSpriteFont
        ReadOnly Property Bounds As Rectangle
    End Interface
End Namespace