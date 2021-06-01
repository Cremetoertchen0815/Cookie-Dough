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
        Hearts
        Spades
        Diamonds
        Clubs
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
    End Structure
End Namespace