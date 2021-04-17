Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.BetretenVerboten
    ''' <summary>
    ''' Kapselt alle wichtigen Eigenschaften und Methoden eine Spielers
    ''' </summary>
    Public Class Player
        Implements IPlayer

        ''' <summary>
        ''' Identifiziert den Spieler in der Anwendung
        ''' </summary>
        Public Property Name As String = "Player" Implements IPlayer.Name

        ''' <summary>
        ''' Deklariert ob der Spieler lokal, durch eine KI, oder über eine Netzwerkverbindung gesteuert wird
        ''' </summary>
        Public Property Typ As SpielerTyp = SpielerTyp.CPU Implements IPlayer.Typ

        ''' <summary>
        ''' Positionen der vier Spielfiguren.<br></br>
        ''' Positionen der Spielfiguren relativ zur Homebase angegeben(-1 = Homebase, 0 = Start-Feld, 1 = erstes Feld nach Start-Feld, ..., 39 = letztes Feld vor Start-Feld, 40 = erstes Feld im Haus, ..., 43 = letztes Feld in Haus)!
        ''' </summary>
        Public Property Spielfiguren As Integer() = {-1, -1, -1, -1}

        ''' <summary>
        ''' Positionen der vier Spielfiguren.<br></br>
        ''' Positionen der Spielfiguren relativ zur Homebase angegeben(-1 = Homebase, 0 = Start-Feld, 1 = erstes Feld nach Start-Feld, ..., 39 = letztes Feld vor Start-Feld, 40 = erstes Feld im Haus, ..., 43 = letztes Feld in Haus)!
        ''' </summary>
        Public Property Kicks As Integer = 0

        ''' <summary>
        ''' Gibt die Schwierigkeitstufe der CPU an
        ''' </summary>
        Public Property Schwierigkeit As Difficulty = Difficulty.Smart

        ''' <summary>
        ''' Repräsentiert die IO-Verbindung des Spielers zum Server
        ''' </summary>
        Public Property Connection As Connection Implements IPlayer.Connection

        ''' <summary>
        ''' Gibt an, ob der Spieler die Verbindung korrekt hergestellt hat
        ''' </summary>
        Public Property Bereit As Boolean = True Implements IPlayer.Bereit

        ''' <summary>
        ''' Gibt an, ob der Spieler seinen Angerbutton benutzt hat
        ''' </summary>
        Public Property Angered As Boolean = False

        ''' <summary>
        ''' Der Sound, der abgespielt wird, wenn man gekickt wird
        ''' </summary>
        Public Property CustomSound As SoundEffect Implements IPlayer.CustomSound

        ''' <summary>
        ''' Das Thumbnail des Spielers
        ''' </summary>
        Public Property Thumbnail As Texture2D Implements IPlayer.Thumbnail

        Sub New(typ As SpielerTyp, Optional schwierigkeit As Difficulty = Difficulty.Smart)
            Me.Typ = typ
            Me.Schwierigkeit = schwierigkeit
        End Sub

    End Class
End Namespace