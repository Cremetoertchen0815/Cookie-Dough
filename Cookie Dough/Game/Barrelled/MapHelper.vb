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
    End Module
End Namespace