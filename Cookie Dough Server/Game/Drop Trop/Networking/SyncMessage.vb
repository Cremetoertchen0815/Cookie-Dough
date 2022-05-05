Imports System.Collections.Generic
Imports Microsoft.Xna.Framework

Namespace Game.DropTrop.Networking
    Public Structure SyncMessage
        Public Spielers As Player()
        Public Fields As Dictionary(Of Vector2, Integer)

        Public Sub New(Spielers As Player(), fields As Dictionary(Of Vector2, Integer))
            Me.Spielers = Spielers
            Me.Fields = New Dictionary(Of Vector2, Integer)
            For Each element In fields
                If element.Value > -1 Then Me.Fields.Add(element.Key, element.Value)
            Next
        End Sub
    End Structure
End Namespace