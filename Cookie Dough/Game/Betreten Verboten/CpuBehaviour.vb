Namespace Game.BetretenVerboten
    Public Structure CpuBehaviour
        Public Shared Behaviour As CpuBehaviour() = {New CpuBehaviour(0), New CpuBehaviour(1), New CpuBehaviour(2), New CpuBehaviour(3), New CpuBehaviour(4), New CpuBehaviour(5), New CpuBehaviour(6), New CpuBehaviour(7)}
        Public Sub New(index As Integer)
            Select Case index
                Case Else
                    DistanceMultiplier = 0.1F
                    PieceDzLeavingMultiplier = 1.4F
                    PieceDzEnteringMultiplier = 0.66F
                    ManifestDestinyMultiplier = 10.0F
                    AttackOpportunityMultiplier = 2.2F
                    HomeFieldEnteringMultiplier = 0.25F
                    HomeDzEnteringMultiplier = 0.5F
                    HomeDzLeavingMultiplier = 1.3F
            End Select
        End Sub

        'Fields
        Public DistanceMultiplier As Single
        Public PieceDzLeavingMultiplier As Single
        Public PieceDzEnteringMultiplier As Single
        Public HomeDzLeavingMultiplier As Single
        Public HomeDzEnteringMultiplier As Single
        Public HomeFieldEnteringMultiplier As Single
        Public ManifestDestinyMultiplier As Single
        Public AttackOpportunityMultiplier As Single
    End Structure
End Namespace