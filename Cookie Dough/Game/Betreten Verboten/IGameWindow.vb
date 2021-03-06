Imports System.Collections.Generic
Imports Cookie_Dough.Framework.UI
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.BetretenVerboten
    Public Interface IGameWindow
        ReadOnly Property Spielers As Player()
        ReadOnly Property FigurFaderScales As Dictionary(Of (Integer, Integer), Transition(Of Single))
        ReadOnly Property Status As SpielStatus
        ReadOnly Property Map As GaemMap
        ReadOnly Property SaucerFields As List(Of Integer)
        ReadOnly Property SelectFader As Single 'Fader, welcher die zur Auswahl stehenden Figuren blinken lässt
        ReadOnly Property FigurFaderZiel As (Integer, Integer) 'Gibt an welche Figur bewegt werden soll (Spieler ind., Figur ind.)
        ReadOnly Property SpielerIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property UserIndex As Integer 'Gibt den Index des Spielers an, welcher momentan an den Reihe ist.
        ReadOnly Property FigurFaderXY As Transition(Of Vector2)
        ReadOnly Property FigurFaderZ As Transition(Of Integer)
        ReadOnly Property WürfelAktuelleZahl As Integer
        ReadOnly Property WürfelWerte As Integer()
        ReadOnly Property DreifachWürfeln As Boolean
        ReadOnly Property HUDNameBtn As Controls.Button
        ReadOnly Property HUDmotdLabel As Controls.Label
        ReadOnly Property BGTexture As Texture2D
        ReadOnly Property GameTexture As Texture2D
        ReadOnly Property StartCamPoses As Keyframe3D()
        ReadOnly Property TeamNames As String()
        Function GetCamPos() As Keyframe3D
    End Interface
End Namespace