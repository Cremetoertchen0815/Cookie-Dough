Imports Newtonsoft.Json

Namespace Game.DropTrop
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
        ''' Gibt die Schwierigkeitstufe der CPU an
        ''' </summary>
        Public Property Schwierigkeit As Difficulty = Difficulty.Smart

        ''' <summary>
        ''' Gibt an, wieviele Zusatzpunkte der Spieler hat.
        ''' </summary>
        Public Property AdditionalPoints As Integer = 0


        Public Property MovePossible As Boolean = True

        ''' <summary>
        ''' Repräsentiert die IO-Verbindung des Spielers zum Server
        ''' </summary>
        <JsonIgnore>
        Public Property Connection As Connection Implements IPlayer.Connection

        ''' <summary>
        ''' Gibt an, ob der Spieler die Verbindung korrekt hergestellt hat
        ''' </summary>
        Public Property Bereit As Boolean = True Implements IPlayer.Bereit
        Public Property ID As String Implements IPlayer.ID
        Public Property MOTD As String Implements IPlayer.MOTD

        Public Sub New(typ As SpielerTyp, Optional schwierigkeit As Difficulty = Difficulty.Smart)
            Me.Typ = typ
            Me.Schwierigkeit = schwierigkeit
        End Sub

    End Class
End Namespace