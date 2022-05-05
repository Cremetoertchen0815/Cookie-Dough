Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.DropTrop
    Public Interface IGameWindow
        ReadOnly Property Spielers As Player()
        ReadOnly Property Status As SpielStatus
        ReadOnly Property SpielfeldSize As Vector2
        ReadOnly Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        ReadOnly Property SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property HUDColor As Color
        ReadOnly Property Map As GaemMap
        ReadOnly Property BGTexture As Texture2D
        ReadOnly Property CurrentCursor As Vector2
        ReadOnly Property Spielfeld As Dictionary(Of Vector2, Integer)
        Function GetCamPos() As Keyframe3D
    End Interface
End Namespace