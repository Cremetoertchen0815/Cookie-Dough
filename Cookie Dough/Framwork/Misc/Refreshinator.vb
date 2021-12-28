Imports System.Collections.Generic

Namespace Framework.Misc
    Public Class Refreshinator(Of T)
        Public Property RefreshAction As Action = Sub() Return
        Public Property EqualityComparerer As Func(Of T, T, Boolean) = Function(a, b) a.Equals(b)

        Private _conditions As New List(Of Func(Of T))
        Private _lastValues As New List(Of T)

        Public Sub Update()
            For i As Integer = 0 To _conditions.Count - 1
                Dim newval As T = _conditions(i).Invoke()
                If Not EqualityComparerer(newval, _lastValues(i)) Then RefreshAction.Invoke()
                _lastValues(i) = newval
            Next
        End Sub

        Public Sub AddCondition(value As Func(Of T), Optional comparer As Func(Of T, T, Boolean) = Nothing)
            _conditions.Add(value)
            _lastValues.Add(value())
        End Sub
        Public Sub ClearConditions()
            _conditions.Clear()
            _lastValues.Clear()
        End Sub
    End Class
End Namespace