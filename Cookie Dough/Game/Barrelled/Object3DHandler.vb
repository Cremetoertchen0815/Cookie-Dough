Imports System.Collections.Generic
Imports Cookie_Dough.Game.Barrelled.Players
Imports Microsoft.Xna.Framework

Namespace Game.Barrelled
    Public Class Object3DHandler
        Inherits SceneComponent

        Public Sub New(user As EgoPlayer, owner As IGameWindow)
            Me.user = user
            Me.owner = owner
            InteractABtn = New VirtualButton(New VirtualButton.MouseLeftButton)
            InteractBBtn = New VirtualButton(New VirtualButton.KeyboardKey(Input.Keys.E))
        End Sub

        Friend Property Objects As New List(Of IObject3D)
        Friend Property ViewRay As Ray

        Private InteractABtn As VirtualButton
        Private InteractBBtn As VirtualButton
        Private user As EgoPlayer
        Private owner As IGameWindow
        Private Const ClickRange As Single = 10

        Public Overrides Sub Update()
            'Get view ray
            ViewRay = New Ray(owner.EgoPlayer.CameraPosition, user.Direction)

            'Check interaction with objects
            Dim topmost As IObject3D = Nothing
            For Each obj In Objects
                Dim distance As Single? = ViewRay.Intersects(obj.BoundingBox)
                If distance IsNot Nothing AndAlso distance <= ClickRange Then
                    obj.Distance = CSng(distance)
                    If topmost Is Nothing OrElse topmost.Distance > distance Then topmost = obj
                End If
            Next

            'Interact with object
            If topmost IsNot Nothing AndAlso InteractABtn.IsPressed AndAlso Not topmost.UserAlternateTrigger Then topmost.ClickedFunction(owner)
            If topmost IsNot Nothing AndAlso InteractBBtn.IsPressed AndAlso topmost.UserAlternateTrigger Then topmost.ClickedFunction(owner)
        End Sub
    End Class
End Namespace