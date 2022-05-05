Imports Newtonsoft.Json

Namespace Game.Corridor
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
        ''' Repräsentiert die IO-Verbindung des Spielers zum Server
        ''' </summary>
        <JsonIgnore>
        Public Property Connection As Connection Implements IPlayer.Connection

        ''' <summary>
        ''' Gibt an, ob der Spieler die Verbindung korrekt hergestellt hat
        ''' </summary>
        Public Property Bereit As Boolean = True Implements IPlayer.Bereit

        ''' <summary>
        ''' Der Identifikationsstring des Spielers
        ''' </summary>
        Public Property ID As String Implements IPlayer.ID

        ''' <summary>
        ''' Ein ganz krasser Spruch
        ''' </summary>
        Public Property MOTD As String Implements IPlayer.MOTD

        Public Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub

    End Class
End Namespace