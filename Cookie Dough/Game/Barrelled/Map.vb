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

        Public PlayerColors As Color() = {playcolor(2), playcolor(3), playcolor(4)}
        Public PlayerHUDColors As Color() = {hudcolors(2), hudcolors(3), hudcolors(4)}
    End Module

    Public Class RoomTriggerBox
        Implements IObject3D

        Dim ac As Action

        Sub New(bb As BoundingBox, action As Action)
            BoundingBox = bb
            ac = action
        End Sub

        Public Property Distance As Single Implements IObject3D.Distance
        Public Property UserAlternateTrigger As Boolean = True Implements IObject3D.UserAlternateTrigger

        Public ReadOnly Property BoundingBox As BoundingBox Implements IObject3D.BoundingBox

        Public Sub ClickedFunction(sender As IGameWindow) Implements IObject3D.ClickedFunction
            ac()
        End Sub
    End Class
End Namespace