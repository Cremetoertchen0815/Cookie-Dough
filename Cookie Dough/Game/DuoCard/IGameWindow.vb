﻿Imports System.Collections.Generic
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.DuoCard
    Public Interface IGameWindow
        ReadOnly Property Spielers As List(Of Player)
        ReadOnly Property Status As SpielStatus
        ReadOnly Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        ReadOnly Property SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property HUDColor As Color
        ReadOnly Property BGTexture As Texture2D
        Function GetCamPos() As Keyframe3D
    End Interface
End Namespace