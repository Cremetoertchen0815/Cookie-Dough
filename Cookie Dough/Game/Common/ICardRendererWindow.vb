Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Common
    Public Interface ICardRendererWindow
        ReadOnly Property HandDeck As List(Of Card)
        ReadOnly Property TableCard As Card
        ReadOnly Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        ReadOnly Property SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property BGTexture As Texture2D
        ReadOnly Property DeckScroll As Integer
        ReadOnly Property State As CardGameState
        ReadOnly Property Spielers As BaseCardPlayer()
        Function GetCamPos() As Keyframe3D
    End Interface
End Namespace