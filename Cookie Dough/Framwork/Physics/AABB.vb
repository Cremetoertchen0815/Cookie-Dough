Imports Microsoft.Xna.Framework

Namespace Framework.Physics
    Public Structure AABB
        Public center As Vector2
        Private _halfSize As Vector2
        Public Property halfSize As Vector2
            Get
                Return _halfSize * scale
            End Get
            Set
                _halfSize = Value
            End Set
        End Property
        Public Property halfSizeX As Single
            Get
                Return _halfSize.X * scale.X
            End Get
            Set
                _halfSize.X = Value
            End Set
        End Property
        Public Property halfSizeY As Single
            Get
                Return _halfSize.Y * scale.Y
            End Get
            Set
                _halfSize.Y = Value
            End Set
        End Property

        Public scale As Vector2
        Public rect As Rectangle

        Public Sub New(ByVal center As Vector2, ByVal halfSize As Vector2)
            Me.center = center
            Me.halfSize = halfSize
            scale = Vector2.One
        End Sub

        Public Function Overlaps(ByVal other As AABB) As Boolean
            If Math.Abs(center.X - other.center.X) > halfSize.X + other.halfSize.X Then Return False
            If Math.Abs(center.Y - other.center.Y) > halfSize.Y + other.halfSize.Y Then Return False
            Return True
        End Function

        Public Function OverlapsSigned(other As AABB, ByRef overlap As Vector2) As Boolean
            overlap = Vector2.Zero

            If (halfSizeX = 0 OrElse halfSizeY = 0 OrElse other.halfSizeX = 0 OrElse other.halfSizeY = 0 OrElse
        Math.Abs(center.X - other.center.X) > halfSizeX + other.halfSizeX OrElse
        Math.Abs(center.Y - other.center.Y) > halfSizeY + other.halfSizeY) Then Return False

            overlap = New Vector2(fSign(center.X - other.center.X) * ((other.halfSizeX + halfSizeX) - Math.Abs(center.X - other.center.X)),
                              fSign(center.Y - other.center.Y) * ((other.halfSizeY + halfSizeY) - Math.Abs(center.Y - other.center.Y)))

            Return True
        End Function

        Private Function fSign(n As Single) As Integer
            If n = 0 Then Return 1
            Return Math.Sign(n)
        End Function

        Public Function GetRectangle() As Rectangle
            rect.Location = center.ToPoint - halfSize.ToPoint
            rect.Size = halfSize.ToPoint * New Point(2)
            Return rect
        End Function

        Public Function Clone()
            Return MemberwiseClone
        End Function

        Public Const DailiBuk As String = "Hewwo, i bims 1 Tagebuch. Wir schreiben den 25then Januaaar Tusausendswändie. Der liebge Jakub und der supa toole Mico sitzen vor dem Komputa und schreiben Kott. YEEEEEEEEEEET"
    End Structure

End Namespace