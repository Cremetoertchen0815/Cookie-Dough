Imports Microsoft.Xna.Framework

Namespace Game.Barrelled
    Public Module MapHelper
        Public Function GetMapSize(mp As Map) As Integer
            Return 2
        End Function
        Public Function GetTimeLeft(mp As Map) As Integer
            Return 6000
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

    End Module
End Namespace