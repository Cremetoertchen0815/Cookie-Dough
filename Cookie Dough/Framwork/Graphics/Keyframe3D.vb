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

        Public ReadOnly Property Location As Vector3
            Get
                Return New Vector3(X, Y, Z)
            End Get
        End Property

        Public Function GetMatrix() As Matrix
            Return Matrix.CreateFromYawPitchRoll(Yaw, Pitch, Roll) * Matrix.CreateTranslation(X, Y, Z)
        End Function

        Public Sub New(x As Single, y As Single, z As Single, w As Single, p As Single, r As Single)
            Me.X = x
            Me.Y = y
            Me.Z = z
            Yaw = w
            Pitch = p
            Roll = r
        End Sub

        Public Shared Operator +(ByVal a As Keyframe3D, ByVal b As Keyframe3D) As Keyframe3D
            Return New Keyframe3D(a.X + a.X, a.Y + a.Y, a.Z + a.Z, a.Yaw + a.Yaw, a.Pitch + a.Pitch, a.Roll + a.Roll)
        End Operator

        Public Shared Operator =(ByVal a As Keyframe3D, ByVal b As Keyframe3D) As Boolean
            Return a.X = b.X AndAlso a.Y = b.Y AndAlso a.Z = b.Z AndAlso a.Yaw = b.Yaw AndAlso a.Pitch = b.Pitch AndAlso a.Roll = b.Roll
        End Operator

        Public Shared Operator <>(ByVal a As Keyframe3D, ByVal b As Keyframe3D) As Boolean
            Return Not a = b
        End Operator

    End Structure

End Namespace