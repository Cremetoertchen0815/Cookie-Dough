Imports Microsoft.Xna.Framework

Namespace Framework.Graphics

    <TestState(TestState.NearCompletion)>
    Public Structure Keyframe3D
        Public X As Single
        Public Y As Single
        Public Z As Single
        Public Yaw As Single
        Public Pitch As Single
        Public Roll As Single
        Public DefaultOrder As Boolean

        Public ReadOnly Property Location As Vector3
            Get
                Return New Vector3(X, Y, Z)
            End Get
        End Property

        Public Function GetMatrix() As Matrix
            If DefaultOrder Then
                Return Matrix.CreateFromYawPitchRoll(Yaw, Pitch, Roll) * Matrix.CreateTranslation(X, Y, Z)
            Else
                Return Matrix.CreateFromYawPitchRoll(0, 0, Yaw) * Matrix.CreateFromYawPitchRoll(0, Pitch, Roll) * Matrix.CreateTranslation(Location)
            End If
        End Function

        Public Sub New(x As Single, y As Single, z As Single, w As Single, p As Single, r As Single, DefaultOrder As Boolean)
            Me.X = x
            Me.Y = y
            Me.Z = z
            Yaw = w
            Pitch = p
            Roll = r
            Me.DefaultOrder = DefaultOrder
        End Sub

        Public Shared Operator +(ByVal a As Keyframe3D, ByVal b As Keyframe3D) As Keyframe3D
            Return New Keyframe3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.Yaw + b.Yaw, a.Pitch + b.Pitch, a.Roll + b.Roll, a.DefaultOrder And b.DefaultOrder)
        End Operator

        Public Shared Operator =(ByVal a As Keyframe3D, ByVal b As Keyframe3D) As Boolean
            Return a.X = b.X AndAlso a.Y = b.Y AndAlso a.Z = b.Z AndAlso a.Yaw = b.Yaw AndAlso a.Pitch = b.Pitch AndAlso a.Roll = b.Roll
        End Operator

        Public Shared Operator <>(ByVal a As Keyframe3D, ByVal b As Keyframe3D) As Boolean
            Return Not a = b
        End Operator

        Public Overrides Function ToString() As String
            Return "{X: " & X.ToString & ", Y: " & Y.ToString & ", Z: " & Z.ToString & ", Yaw: " & Yaw.ToString & ", Pitch: " & Pitch.ToString & ", Roll: " & Roll.ToString & "}"
        End Function
    End Structure

End Namespace