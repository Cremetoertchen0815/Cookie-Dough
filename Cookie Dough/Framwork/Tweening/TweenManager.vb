Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Tweening.ManagedTypes

Namespace Framework.Tweening

    <TestState(TestState.Finalized)>
    Public Class TweenManager
        Inherits GlobalManager

        Public Sub New()
            'Register Managed Types
            registerType(New ManagedType_Int())
            registerType(New ManagedType_Single())
            registerType(New ManagedType_Double())
            registerType(New ManagedType_Color())
            registerType(New ManagedType_String())
            registerType(New ManagedType_Vector2())
            registerType(New ManagedType_Vector3())
            registerType(New ManagedType_Vector4())
            registerType(New ManagedType_CamKeyframe())

            m_Transitions = New List(Of ITransition)
        End Sub

        Private Sub registerType(ByVal transitionType As IManagedType)
            Dim type As Type = transitionType.getManagedType()
            m_mapManagedTypes.Add(type, transitionType)
        End Sub

        Public Sub Add(ByVal transition As ITransition)
            SyncLock m_Lock
                transition.State = TransitionState.InProgress
                transition.Prepare()
                m_TransCommands.Add((TransCommand.Add, transition))
            End SyncLock
        End Sub
        Public Sub Remove(ByVal transition As ITransition)
            SyncLock m_Lock
                m_TransCommands.Add((TransCommand.Remove, transition))
            End SyncLock
        End Sub
        Public Sub Clear(Optional stubborn As Boolean = False)
            SyncLock m_Lock
                m_TransCommands.Add((TransCommand.Clear, stubborn))
            End SyncLock
        End Sub

        Public Overrides Sub Update()
            MyBase.Update()

            SyncLock m_Lock
                For Each transition In m_Transitions
                    If Not (Suspend And Not transition.Stubborn) Then transition.Update()
                    If transition.State = TransitionState.Done Then m_TransCommands.Add((TransCommand.Remove, transition))
                Next

                For Each com In m_TransCommands
                    Select Case com.Item1
                        Case TransCommand.Add
                            If Not m_Transitions.Contains(com.Item2) Then m_Transitions.Add(com.Item2)
                        Case TransCommand.Remove
                            If m_Transitions.Contains(com.Item2) Then m_Transitions.Remove(com.Item2)
                        Case TransCommand.Clear
                            Dim stubborn = CBool(com.Item2)
                            Dim removables As New List(Of ITransition)
                            For Each element In m_Transitions
                                If Not (Not stubborn And element.Stubborn) Then removables.Add(element)
                            Next

                            For Each element In removables
                                m_Transitions.Remove(element)
                            Next
                    End Select
                Next
                m_TransCommands.Clear()
            End SyncLock
        End Sub

        Friend Function GetCount() As Integer
            Return m_Transitions.Count
        End Function

        Public Property Suspend As Boolean = False
        Friend m_mapManagedTypes As IDictionary(Of Type, IManagedType) = New Dictionary(Of Type, IManagedType)()
        Private m_Transitions As List(Of ITransition)
        Private m_TransCommands As New List(Of (TransCommand, Object))
        Private m_Lock As Object = New Object()

        Private Enum TransCommand
            Add
            Remove
            Clear
        End Enum
    End Class
End Namespace
