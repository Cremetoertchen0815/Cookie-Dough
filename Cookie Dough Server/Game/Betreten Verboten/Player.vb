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
        <Newtonsoft.Json.JsonIgnore>
        Friend Property Typ As SpielerTyp Implements IPlayer.Typ
            Get
                Return If(IsAFK And OriginalType <> SpielerTyp.CPU And OriginalType <> SpielerTyp.None, SpielerTyp.CPU, OriginalType)
            End Get
            Set(value As SpielerTyp)
                OriginalType = value
            End Set
        End Property
        Public Property OriginalType As SpielerTyp = SpielerTyp.CPU

        Public Property IsAFK As Boolean = False


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
        Public Property SuicideField As Integer = -1

        ''' <summary>
        ''' Gibt an, wieviele Zusatzpunkte der Spieler hat.
        ''' </summary>
        Public Property AdditionalPoints As Integer = 0

        ''' <summary>
        ''' Gibt an, ob der Spieler die nächste Runde aussetzen muss
        ''' </summary>
        ''' <returns></returns>
        Public Property MissingTurn As Boolean = False

        ''' <summary>
        ''' Repräsentiert die IO-Verbindung des Spielers zum Server
        ''' </summary>
        <JsonIgnore>
        Public Property Connection As Connection Implements IPlayer.Connection

        ''' <summary>
        ''' Gibt an, ob der Spieler die Verbindung korrekt hergestellt hat
        ''' </summary>
        Public Property Bereit As Boolean = True Implements IPlayer.Bereit

        Private _AngerCount As Integer = 1

        ''' <summary>
        ''' Gibt an, ob der Spieler angeren kann
        ''' </summary>
        Public Property AngerCount() As Integer
            Get
                Return _AngerCount
            End Get
            Set(value As Integer)
                _AngerCount = value
            End Set
        End Property

        ''' <summary>
        ''' Gibt an, wie viele Züge der Spieler warten muss, bis er seinen Sacrifice-Button wieder nuitzen darf
        ''' </summary>
        Public Property SacrificeCounter As Integer = 0

        ''' <summary>
        ''' Der Identifikationsstring des Spielers
        ''' </summary>
        Public Property ID As String Implements IPlayer.ID

        ''' <summary>
        ''' Ein ganz krasser Spruch
        ''' </summary>
        Public Property MOTD As String Implements IPlayer.MOTD

        Friend TeamNameWish As String

        Public Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub

    End Class
End Namespace