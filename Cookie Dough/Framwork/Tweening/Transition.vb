Namespace Framework.Tweening

    <TestState(TestState.Finalized)>
    Public Class Transition(Of T)
        Implements ITransition

        Public Sub New(TransitionMethod As ITransitionType, StartValue As T, EndValue As T, FinishAction As FinishedDelegate, Optional stubborn As Boolean = False)
            'Check whether the tweening manager supports this type
            Dim type As Type = GetType(T)
            If Automator.m_mapManagedTypes.ContainsKey(type) Then
                ManagedType = Automator.m_mapManagedTypes(type)

                Me.StartValue = StartValue
                Me.EndValue = EndValue
                Method = TransitionMethod
                Me.FinishAction = FinishAction
                State = TransitionState.Idle
                Value = StartValue
                Me.Stubborn = stubborn
            Else
                Throw New NotImplementedException("The tweening manager doesn't support " & GetType(T).Name & ".")
            End If
        End Sub

        Public Sub New()
            'Check whether the tweening manager supports this type
            Dim type As Type = GetType(T)
            If Automator.m_mapManagedTypes.ContainsKey(type) Then
                ManagedType = Automator.m_mapManagedTypes(type)

                State = TransitionState.Idle
            Else
                Throw New NotImplementedException("The tweening manager doesn't support " & GetType(T).Name & ".")
            End If
        End Sub

        Public Sub Update() Implements ITransition.Update
            If Enabled And State = TransitionState.InProgress Then
                Timer += CInt(Nez.Time.DeltaTime * 1000)

                'Calculate values
                Dim percentage As Double
                Dim completed As Boolean
                Method.onTimer(Timer, percentage, completed)

                'Set value
                Value = ManagedType.copy(ManagedType.getIntermediateValue(StartValue, EndValue, percentage))

                If completed Then
                    Select Case Repeat
                        Case RepeatJob.None
                            State = TransitionState.Done
                        Case RepeatJob.Reverse
                            'Swap Start and Stop values
                            Dim tEnd As T = ManagedType.copy(EndValue)
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

        End Sub

        Private Sub TriggerAction()
            If FinishAction IsNot Nothing Then FinishAction.Invoke(Me)
            RaiseEvent TransitionCompletedEvent(Me, New EventArgs)
        End Sub

        Public Property StartValue As T
        Public Property EndValue As T
        Public Property Value As T
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
        Public Delegate Sub FinishedDelegate(sender As Transition(Of T)) 'A delegate to be executed when the transition is complete/the transition loops

    End Class

End Namespace
