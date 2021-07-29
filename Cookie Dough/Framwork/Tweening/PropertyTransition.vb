Imports System.Reflection

Namespace Framework.Tweening

    <TestState(TestState.Finalized)>
    Public Class PropertyTransition
        Implements ITransition

        Public Sub New(TransitionMethod As ITransitionType, target As Object, propertyName As String, EndValue As Object, FinishAction As FinishedDelegate, Optional stubborn As Boolean = False)
            'Check whether the tweening manager supports this type

            Dim targetType As Type = target.GetType()
            Dim propertyInfo As PropertyInfo = targetType.GetProperty(propertyName)

            If propertyInfo Is Nothing Then
                Throw New Exception("Object: " & target.ToString() & " does not have the property: " & propertyName)
            End If

            Dim type As Type = propertyInfo.PropertyType
            If Automator.m_mapManagedTypes.ContainsKey(type) Then
                ManagedType = Automator.m_mapManagedTypes(type)

                [Property] = propertyInfo
                PropertyType = type
                Me.Target = target
                StartValue = propertyInfo.GetValue(target)
                Me.EndValue = EndValue
                Method = TransitionMethod
                Me.FinishAction = FinishAction
                State = TransitionState.Idle
                Value = StartValue
                Me.Stubborn = stubborn
            Else
                Throw New NotImplementedException("The tweening manager doesn't support " & type.Name & ".")
            End If
        End Sub

        Public Sub New(TransitionMethod As ITransitionType, target As Object, propertyInfo As PropertyInfo, EndValue As Object, FinishAction As FinishedDelegate, Optional stubborn As Boolean = False)
            'Check whether the tweening manager supports this type

            Dim type As Type = propertyInfo.PropertyType
            If Automator.m_mapManagedTypes.ContainsKey(type) Then
                ManagedType = Automator.m_mapManagedTypes(type)

                [Property] = propertyInfo
                PropertyType = type
                Me.Target = target
                Me.EndValue = EndValue
                Method = TransitionMethod
                Me.FinishAction = FinishAction
                State = TransitionState.Idle
                Value = StartValue
                Me.Stubborn = stubborn
            Else
                Throw New NotImplementedException("The tweening manager doesn't support " & type.Name & ".")
            End If
        End Sub

        Public Sub Update() Implements ITransition.Update
            If Enabled And State = TransitionState.InProgress Then
                Timer += CInt(Time.DeltaTime * 1000)

                'Calculate values
                Dim percentage As Double
                Dim completed As Boolean
                Method.onTimer(Timer, percentage, completed)

                'Set value
                Value = ManagedType.copy(ManagedType.getIntermediateValue(StartValue, EndValue, percentage))
                [Property].SetValue(Target, Value)

                If completed Then
                    Select Case Repeat
                        Case RepeatJob.None
                            State = TransitionState.Done
                        Case RepeatJob.Reverse
                            'Swap Start and Stop values
                            Dim tEnd As Object = ManagedType.copy(EndValue)
                            EndValue = ManagedType.copy(StartValue)
                            StartValue = tEnd
                            'Reset Values
                            Value = ManagedType.copy(StartValue)
                            Timer = 0
                        Case RepeatJob.JumpBack
                            'Reset Values
                            Value = ManagedType.copy(StartValue)
                            Timer = 0
                    End Select
                    TriggerAction()
                End If
            End If
        End Sub

        Public Sub Prepare() Implements ITransition.Prepare
            StartValue = [Property].GetValue(Target)
        End Sub

        Private Sub TriggerAction()
            If FinishAction IsNot Nothing Then FinishAction.Invoke(Me)
            RaiseEvent TransitionCompletedEvent(Me, New EventArgs)
        End Sub

        Private Property PropertyType As Type
        Public Property StartValue As Object
        Public Property EndValue As Object
        Public Property Value As Object
        Public Property Target As Object
        Public Property [Property] As PropertyInfo
        Public Property Method As ITransitionType Implements ITransition.Method
        Public Property FinishAction As FinishedDelegate 'A delegate to be executed when the transition is complete/the transition loops
        Public Property Enabled As Boolean = True
        Public Property Stubborn As Boolean = False Implements ITransition.Stubborn 'Indicates that a transition can't be removed by a non-stubborn clear command
        Public Property Repeat As RepeatJob = RepeatJob.None
        Public Property State As TransitionState Implements ITransition.State
        Public ReadOnly Property ElapsedTime As Integer
            Get
                Return Timer
            End Get
        End Property

        Private Timer As Integer 'Keeps track of the elapsed time
        Private ManagedType As IManagedType 'Interface for converting/calculating values for the specified type

        Public Event TransitionCompletedEvent(sender As Object, e As EventArgs) 'An event to be executed when the transition is complete/the transition loops
        Public Delegate Sub FinishedDelegate(sender As PropertyTransition) 'A delegate to be executed when the transition is complete/the transition loops

    End Class

End Namespace
