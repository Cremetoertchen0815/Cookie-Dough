Imports System.Collections.Generic
Imports Cookie_Dough.Framework.UI
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.CarCrash
    Public Interface IGameWindow
        ReadOnly Property Status As SpielStatus
        ReadOnly Property BGTexture As Texture2D
        ReadOnly Property EmuTexture As Texture2D
        Function GetCamPos() As Keyframe3D
    End Interface
End Namespace