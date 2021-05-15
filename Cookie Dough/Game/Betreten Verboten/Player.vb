Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Newtonsoft.Json

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
        <Newtonsoft.Json.JsonIgnore>
        Public Shared DefaultArray As Integer() = {-1, -1, -1, -1}
        Public Property Spielfiguren As Integer() = CType(DefaultArray.Clone, Integer())


        ''' <summary>
        ''' Gibt an, wieviele Zusatzpunkte der Spieler hat.
        ''' </summary>
        Public Property AdditionalPoints As Integer = 0

        ''' <summary>
        ''' Gibt die Schwierigkeitstufe der CPU an
        ''' </summary>
        Public Property Schwierigkeit As Difficulty = Difficulty.Smart

        ''' <summary>
        ''' Repräsentiert die IO-Verbindung des Spielers zum Server
        ''' </summary>
        <JsonIgnore>
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
        ''' Gibt an, wie viele Züge der Spieler warten muss, bis er seinen Sacrifice-Button wieder nuitzen darf
        ''' </summary>
        Public Property SacrificeCounter As Integer = 0

        ''' <summary>
        ''' Der Sound, der abgespielt wird, wenn man gekickt wird
        ''' </summary>
        <JsonIgnore>
        Public Property CustomSound As SoundEffect = SFX(3) Implements IPlayer.CustomSound

        ''' <summary>
        ''' Das Thumbnail des Spielers
        ''' </summary>
        <JsonIgnore>
        Public Property Thumbnail As Texture2D Implements IPlayer.Thumbnail

        Public Property ID As String Implements IPlayer.ID

        Sub New(typ As SpielerTyp, Optional schwierigkeit As Difficulty = Difficulty.Smart)
            Me.Typ = typ
            Me.Schwierigkeit = schwierigkeit
        End Sub

    End Class
End Namespace