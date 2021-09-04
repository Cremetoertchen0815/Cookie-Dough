Imports Cookie_Dough.Game.Common
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.DuoCard
    Public Interface IGameWindow
        ReadOnly Property Spielers As BaseCardPlayer()
        ReadOnly Property Status As CardGameState
        ReadOnly Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        ReadOnly Property SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property BGTexture As Texture2D
        Function GetCamPos() As Keyframe3D
    End Interface
End Namespace