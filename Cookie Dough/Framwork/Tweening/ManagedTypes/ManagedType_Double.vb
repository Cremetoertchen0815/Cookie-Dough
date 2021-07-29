Namespace Framework.Tweening.ManagedTypes

    <TestState(TestState.Finalized)>
    Friend Class ManagedType_Double
        Implements IManagedType

        Public Function getManagedType() As Type Implements IManagedType.getManagedType
            Return GetType(Double)
        End Function

        Public Function copy(ByVal o As Object) As Object Implements IManagedType.copy
            Dim d As Double = o
            Return d
        End Function

        Public Function getIntermediateValue(ByVal start As Object, ByVal [end] As Object, ByVal dPercentage As Double) As Object Implements IManagedType.getIntermediateValue
            Dim dStart As Double = start
            Dim dEnd As Double = [end]
            Return interpolate(dStart, dEnd, dPercentage)
        End Function
    End Class
End Namespace
