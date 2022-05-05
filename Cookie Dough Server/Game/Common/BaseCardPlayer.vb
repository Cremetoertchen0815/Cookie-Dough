Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Newtonsoft.Json

Namespace Game.Common
    Public Class BaseCardPlayer
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


        Public Property HandDeck As New List(Of Card)
        Public Property ID As String Implements IPlayer.ID
        Public Property MOTD As String Implements IPlayer.MOTD

        Public Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub
    End Class
End Namespace