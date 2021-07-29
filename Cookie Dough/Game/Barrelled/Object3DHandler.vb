Imports System.Collections.Generic
Imports Cookie_Dough.Game.Barrelled.Players
Imports Microsoft.Xna.Framework

Namespace Game.Barrelled
    Public Class Object3DHandler
        Inherits SceneComponent

        Public Sub New(user As EgoPlayer, owner As GameRoom)
            Me.user = user
            Me.owner = owner
            InteractBtn = New VirtualButton(New VirtualButton.MouseLeftButton)
        End Sub

        Friend Property Objects As New List(Of IObject3D)
        Friend Property ViewRay As Ray

        Private InteractBtn As VirtualButton
        Private user As EgoPlayer
        Private owner As GameRoom
        Private Const ClickRange As Single = 8

        Public Overrides Sub Update()
            'Get view ray
            Dim camShift As Vector3 = user.Direction : camShift.Y = 0 : camShift.Normalize() : camShift *= 0.5
            Dim campos As Vector3 = user.Location + camShift + New Vector3(0, 5, 0)
            ViewRay = New Ray(campos, user.Direction)

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
            If topmost IsNot Nothing AndAlso InteractBtn.IsPressed Then topmost.ClickedFunction(owner)
        End Sub
    End Class
End Namespace