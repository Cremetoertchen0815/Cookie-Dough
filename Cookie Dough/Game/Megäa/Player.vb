Imports System.Collections.Generic
Imports Cookie_Dough.Framework.Networking
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Audio
Imports Microsoft.Xna.Framework.Graphics

Namespace Game.Megäa
    Public Class Player
        Implements IPlayer

        Public Property Connection As Connection Implements IPlayer.Connection
        Public Property Bereit As Boolean = True Implements IPlayer.Bereit
        Public Property Typ As SpielerTyp = SpielerTyp.Local Implements IPlayer.Typ
        Public Property Name As String = "Soos" Implements IPlayer.Name
        Public Property PositionLocked As Boolean = False
        Public Property Location As Vector3 = New Vector3(0, 4, 0)
        Public Property Direction As Vector3 = Vector3.Forward
        'Game data
        Public Property Deck As New List(Of Card)
        Public Property HandCard As Card = Card.NoCard
        Public Property HandCardTransform As New Transition(Of Keyframe3D)
        Public Property TableCard As Card = Card.NoCard
        Public Property TableCardTransform As New Keyframe3D

        Public Property CustomSound As SoundEffect() = {SFX(3), SFX(4)} Implements IPlayer.CustomSound
        Public Property Thumbnail As Texture2D Implements IPlayer.Thumbnail

        Public Property ID As String Implements IPlayer.ID
        Public Property MOTD As String Implements IPlayer.MOTD

        Private HandTransformOrigin As Keyframe3D

        Sub New(type As SpielerTyp)
            Me.Typ = type
        End Sub

        Friend Sub SetLockPosition()
            If PositionLocked Then Return
            PositionLocked = True

            'Set the transform for the adjecent table card of the player
            Dim cardPosUnit As New Vector2(Location.X, Location.Z)
            cardPosUnit.Normalize()
            Dim cardRotation As Single = Mathf.AngleBetweenVectors(cardPosUnit, Vector2.UnitY) * -2
            TableCardTransform = New Keyframe3D(cardPosUnit.X * 2.8F, 2.604F, cardPosUnit.Y * 2.8F, cardRotation, 0F, 0F)

            'Set the transform for the hand card
            HandTransformOrigin = New Keyframe3D(Location.X, 4, Location.Z, 2, 0.75, MathHelper.PiOver2)
        End Sub

        Friend Function GetRelativeAngle(Optional preangle As Single = 0) As Single
            Dim cardPosUnit As New Vector2(Location.X, Location.Z)
            cardPosUnit.Normalize()
            Dim cardRotation As Single = Mathf.AngleBetweenVectors(cardPosUnit, Vector2.UnitY) - preangle
            If cardRotation < 0 Then cardRotation += CSng(Math.Floor(-cardRotation / MathHelper.TwoPi) + 1) * MathHelper.TwoPi
            If cardRotation > 0 Then cardRotation = cardRotation Mod MathHelper.TwoPi
            Return cardRotation
        End Function

        Friend Shared CardBeingPlaced As Boolean

        Friend Function GetWorldMatrix() As Matrix
            Dim rotation As Single = Mathf.AngleBetweenVectors(New Vector2(Direction.X, -Direction.Z), Vector2.UnitY) * -2
            Return Matrix.CreateRotationY(rotation) * Matrix.CreateTranslation(Location)
        End Function

        Friend Sub LayCard(switch As Action)
            If CardBeingPlaced Then Return
            CardBeingPlaced = True
            HandCard = Deck(0)
            Deck.RemoveAt(0)
            HandCardTransform = New Transition(Of Keyframe3D)(New TransitionTypes.TransitionType_EaseInEaseOut(800), HandTransformOrigin, TableCardTransform, Sub()
                                                                                                                                                                  TableCard = HandCard
                                                                                                                                                                  CardBeingPlaced = False
                                                                                                                                                                  switch()
                                                                                                                                                              End Sub)
            Automator.Add(HandCardTransform)
        End Sub
    End Class
End Namespace