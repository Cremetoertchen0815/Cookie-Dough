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
                    HomeDzEnteringMultiplier = 0.5F
                    HomeDzLeavingMultiplier = 1.3F
                    HomeFieldEnteringMultiplier = 0.25F
                    HomeFieldLeavingMultiplier = 1.65F
                    AttackOpportunityMultiplier = 2.25F
                    AttackPartyMemberMultiplier = 0.35F
                    SuicideMultiplier = 0.15F
            End Select
        End Sub

        'Fields
        Public DistanceMultiplier As Single 'Gives figures an advantage if they are further
        Public PieceDzLeavingMultiplier As Single 'Gives figures an advantage if they are in front of an enemy figures, so they flee from that figure
        Public PieceDzEnteringMultiplier As Single 'Gives figures a disadvantage if they are going to go in front of an enemy figure this round
        Public HomeDzLeavingMultiplier As Single 'Gives figures an advantage if they are in the spawn danger zone and this move will bring them out of this danger zone
        Public HomeDzEnteringMultiplier As Single 'Gives figures a disadvantage if they are going to enter the danger zone of an enemy
        Public HomeFieldEnteringMultiplier As Single 'Gives figures a disadvantage if they are going to enter the spawn field of an enemy
        Public HomeFieldLeavingMultiplier As Single 'Gives figures an advantage if they are going to leave the spawn field of an enemy
        Public ManifestDestinyMultiplier As Single 'Gives figures an advantage if they'll enter the house with this move
        Public AttackOpportunityMultiplier As Single 'Gives figures an advantage if they'll kick an enemy figure with this move
        Public AttackPartyMemberMultiplier As Single 'Gives figures a disadvantage if they'll kick a figure from their own team with this move
        Public SuicideMultiplier As Single 'Gives figures a disadvantage if they'll land on their own or team mates' suicide field
        Public SacrificeCondition As CPUSacrificeCondition 'Gives figures a disadvantage if they'll land on their own or team mates' suicide field
        Public DeezNuts As Object ' Does absolutely nothing, but Jakob wanted me to add it, oh well...
    End Structure

    <Flags>
    Public Enum CPUSacrificeCondition As Integer
        EndGameWithNoWin = 0 'Sacrifice figure if next move would finish the game, but the CPU would loose due to lack of points
    End Enum
End Namespace