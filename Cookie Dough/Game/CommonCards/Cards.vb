Imports System.Collections.Generic

Namespace Game.CommonCards
    Public Enum CardType
        Ace = 1
        Two = 2
        Three = 3
        Four = 4
        Five = 5
        Six = 6
        Seven = 7
        Eight = 8
        Nine = 9
        Ten = 10
        Jack = 11
        Queen = 12
        King = 13
    End Enum

    Public Enum CardSuit
        Hearts = 0
        Spades = 1
        Diamonds = 2
        Clubs = 3
    End Enum

    Public Structure Card
        Public Property [Type] As CardType
        Public Property Suit As CardSuit
        Public Property Visible As Boolean

        Public Sub New([type] As CardType, suit As CardSuit, Optional visible As Boolean = True)
            Me.Type = type
            Me.Suit = suit
            Me.Visible = visible
        End Sub

        Public Function GetTextureName() As String
            Dim appnd As String = ""
            Select Case Suit
                Case CardSuit.Spades
                    appnd &= "a"
                Case CardSuit.Diamonds
                    appnd &= "b"
                Case CardSuit.Clubs
                    appnd &= "c"
                Case CardSuit.Hearts
                    appnd &= "d"
            End Select

            appnd &= CInt(Type).ToString

            Return appnd
        End Function

        Public Property ID As Integer
            Get
                Return 13 * Suit + Type - 1
            End Get
            Set(value As Integer)
                Suit = Math.Floor(value / 13)
                Type = (value Mod 13) + 1
            End Set
        End Property

        Shared Function GetAllCards() As Card()
            Dim lst As New List(Of Card)
            For a As Integer = 0 To 3
                For b As Integer = 1 To 13
                    lst.Add(New Card(b, a))
                Next
            Next
            Return lst.ToArray
        End Function

        Public Overrides Function ToString() As String
            Return Type.ToString & "/" & Suit.ToString
        End Function


    End Structure
End Namespace