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
        Public Property Typ As SpielerTyp = SpielerTyp.CPU Implements IPlayer.Typ

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


        Public Property HandDeck As List(Of Card) = New List(Of Card) From {New Card(CardType.Ace, CardSuit.Diamonds), New Card(CardType.Queen, CardSuit.Hearts), New Card(CardType.Jack, CardSuit.Spades), New Card(CardType.Four, CardSuit.Diamonds), New Card(CardType.Ace, CardSuit.Diamonds), New Card(CardType.Queen, CardSuit.Hearts), New Card(CardType.Jack, CardSuit.Spades), New Card(CardType.Four, CardSuit.Diamonds), New Card(CardType.Ace, CardSuit.Diamonds), New Card(CardType.Queen, CardSuit.Hearts), New Card(CardType.Jack, CardSuit.Spades), New Card(CardType.Four, CardSuit.Diamonds), New Card(CardType.Ace, CardSuit.Diamonds), New Card(CardType.Queen, CardSuit.Hearts), New Card(CardType.Jack, CardSuit.Spades), New Card(CardType.Four, CardSuit.Diamonds)}

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