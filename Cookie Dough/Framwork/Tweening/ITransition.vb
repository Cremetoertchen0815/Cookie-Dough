Imports Microsoft.Xna.Framework

Namespace Framework.Tweening

    <TestState(TestState.Finalized)>
    Public Interface ITransition
        Sub Prepare()
        Sub Update()

        Property Method As ITransitionType
        Property State As TransitionState
        Property Stubborn As Boolean
    End Interface
    Public Enum TransitionState
        Idle
        InProgress
        Done
    End Enum

    Public Enum RepeatJob
        None
        JumpBack
        Reverse
    End Enum
End Namespace