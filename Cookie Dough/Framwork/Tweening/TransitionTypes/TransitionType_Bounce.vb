Imports System.Collections.Generic

Namespace Framework.Tweening.TransitionTypes

    <TestState(TestState.Finalized)>
    Public Class TransitionType_Bounce
        Inherits TransitionType_UserDefined

        Public Sub New(ByVal iTransitionTime As Integer)
            Dim elements As IList(Of TransitionElement) = New List(Of TransitionElement) From {
                New TransitionElement(50, 100, InterpolationMethod.Accleration),
                New TransitionElement(100, 0, InterpolationMethod.Deceleration)
            }
            MyBase.setup(elements, iTransitionTime)
        End Sub
    End Class
End Namespace
