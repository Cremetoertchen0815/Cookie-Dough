Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Cookie_Dough.Game.CommonCards
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics
Imports Newtonsoft.Json

Namespace Game.DuoCard
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
        ''' Gibt an, wieviele Zusatzpunkte der Spieler hat.
        ''' </summary>
        Public Property AdditionalPoints As Integer = 0

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

        ''' <summary>
        ''' Der Sound, der abgespielt wird, wenn man gekickt wird
        ''' </summary>
        <JsonIgnore>
        Public Property CustomSound As SoundEffect() = {SFX(3), SFX(4)} Implements IPlayer.CustomSound

        ''' <summary>
        ''' Das Thumbnail des Spielers
        ''' </summary>
        <JsonIgnore>
        Public Property Thumbnail As Texture2D Implements IPlayer.Thumbnail
        Public Property ID As String Implements IPlayer.ID
        Public Property MOTD As String Implements IPlayer.MOTD


        Sub New(typ As SpielerTyp)
            Me.Typ = typ
        End Sub

    End Class
End Namespace