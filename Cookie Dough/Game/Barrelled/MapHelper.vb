Imports Microsoft.Xna.Framework

Namespace Game.Barrelled
    Public Module MapHelper
        Public Function GetMapSize(mp As Map) As Integer
            Return 3
        End Function
        Public Function GetMapName(mp As Map) As String
            Select Case mp
                Case Map.Classic
                    Return "Classic"
                Case Map.Mainland
                    Return "Mainland"
                Case Else
                    Return "WTF???"
            End Select
        End Function

        Public PlayerColors As Color() = {playcolor(2), playcolor(3), playcolor(4)}
        Public PlayerHUDColors As Color() = {hudcolors(2), hudcolors(3), hudcolors(4)}
    End Module
End Namespace