Namespace Game.BetretenVerboten
    Friend Module Maps
        Private RNG As New Random
        Friend Function RollDice() As Integer
            Return 7 - RNG.Next(1, 7)
        End Function

        Public Function GetMapName(map As GaemMap) As String
            Select Case map
                Case GaemMap.Plus
                    Return "Plus"
                Case GaemMap.Star
                    Return "Star"
                Case GaemMap.Octagon
                    Return "Octagon"
                Case GaemMap.Snakes
                    Return "Le snek
"
                Case Else
                    Return "Invalid Map"
            End Select
        End Function

        Public Function GetMapSize(map As GaemMap) As Integer
            Select Case map
                Case GaemMap.Plus
                    Return 4
                Case GaemMap.Star
                    Return 6
                Case GaemMap.Octagon
                    Return 8
                Case GaemMap.Snakes
                    Return 4
                Case Else
                    Return 0
            End Select
        End Function

    End Module
End Namespace