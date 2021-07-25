Imports Microsoft.Xna.Framework
Imports Nez.Tiled

Namespace Framework.Physics
    Public Class TiledMapCollisionResolver
        Inherits Component

        'Represents the current velocity and the velocity last frame
        Friend mOldPosition As Vector2 = Vector2.Zero
        Friend mOldSpeed As Vector2 = Vector2.Zero
        Friend mMap As TmxLayer
        Friend mAABB As New AABB(New Vector2(0, 0), New Vector2(0, 0)) 'The rectangle of the player
        Private mFastMover As TiledMapMover
        Private mTileSize As Integer = 16
        Private Const SpeedThreshold As Single = 2.3


        'Variables for collision
        Friend mEnableCollision As Boolean = True
        Friend mCollision As Boolean() = {False, False, False, False}  '{Floor, Ceiling, Left, Right, One-way plattform, Wall-jump Left, Wall-jump Right, Slope Left, Slope Right}
        Private mOldCollision As Boolean() = {False, False, False, False}

        Public Sub New(map As TmxMap, layer As String)
            mMap = map.GetLayer(Of TmxLayer)(layer)
            mTileSize = map.TileWidth
        End Sub

        Public Overrides Sub OnAddedToEntity()
            mFastMover = Entity.AddComponent(New TiledMapMover(mMap))
        End Sub

        Friend Sub Move(mSpeed As Vector2, collider As BoxCollider)
            If mSpeed.Length > SpeedThreshold Then mFastMover.Move(mSpeed, collider, New TiledMapMover.CollisionState) : Return

            Dim mPosition As Vector2 = collider.AbsolutePosition
            Dim groundY As Single = 0.0F
            Dim ceilingY As Single = 0.0F
            Dim rightWallX As Single = 0.0F
            Dim leftWallX As Single = 0.0F
            'Set and reset flags
            mCollision = {False, False, False, False}
            mOldPosition = mPosition
            mOldSpeed = mSpeed
            mOldCollision = mCollision

            'Move the object by its velocity
            mAABB = New AABB(mPosition, New Vector2(collider.Width / 2, collider.Height / 2))
            mPosition += mSpeed

            If mEnableCollision Then

                'Check ceiling
                If mSpeed.Y >= 0.0F AndAlso HasCeiling(mOldPosition, mPosition, ceilingY) Then
                    mPosition.Y = ceilingY - mAABB.halfSize.Y - 1.0F
                    mSpeed.Y = 0.0F
                    mCollision(1) = True
                End If

                'Check floor
                If mSpeed.Y <= 0.0F AndAlso HasGround(mOldPosition, mPosition, mSpeed, groundY) Then
                    mCollision(0) = True
                    mPosition.Y = groundY + mAABB.halfSize.Y
                    mSpeed.Y = 0.0F
                End If

                'Check left wall
                If mSpeed.X <= 0.0F AndAlso CollidesWithLeftWall(mOldPosition, mPosition, leftWallX) Then
                    If mOldPosition.X - mAABB.halfSize.X >= leftWallX Then
                        mPosition.X = leftWallX + mAABB.halfSize.X
                        mCollision(2) = True
                    End If
                    mSpeed.X = Math.Max(mSpeed.X, 0.0F)
                Else
                    mCollision(2) = False
                End If
                'Check right wall
                If mSpeed.X >= 0.0F AndAlso CollidesWithRightWall(mOldPosition, mPosition, rightWallX) Then
                    If mOldPosition.X + mAABB.halfSize.X <= rightWallX Then
                        mPosition.X = rightWallX - mAABB.halfSize.X
                        mCollision(3) = True
                    End If
                    mSpeed.X = Math.Min(mSpeed.X, 0.0F)
                Else
                    mCollision(3) = False
                End If
            End If

            Entity.Position = mPosition

            End Sub

        Public Shadows Property Enabled As Boolean
            Get
                Return MyBase.Enabled
            End Get
            Set(value As Boolean)
                MyBase.Enabled = value
                mFastMover.Enabled = value
            End Set
        End Property

        Public Property CollisionLayer As TmxLayer
            Get
                Return mMap
            End Get
            Set(value As TmxLayer)
                mMap = value
                If mFastMover IsNot Nothing Then mFastMover.CollisionLayer = value
            End Set
        End Property


        Private Function RoundVector(vc As Vector2) As Vector2
            Return New Vector2(Math.Round(vc.X), Math.Round(vc.Y))
        End Function

        Private Function HasGround(ByVal oldPosition As Vector2, ByVal position As Vector2, ByVal speed As Vector2, ByRef groundY As Integer) As Boolean
            Dim oldCenter As Vector2 = oldPosition
            Dim center As Vector2 = position

            Dim oldBottomLeft As Vector2 = RoundVector(oldCenter - mAABB.halfSize + New Vector2(1, -1))
            Dim newBottomLeft As Vector2 = RoundVector(center - mAABB.halfSize + New Vector2(1, -1))
            Dim newBottomRight As Vector2 = RoundVector(New Vector2(newBottomLeft.X + mAABB.halfSize.X * 2.0F - 2.0F, newBottomLeft.Y))


            Dim endY As Integer = GetMapTileYAtPoint(newBottomLeft.Y)
            Dim begY As Integer = Math.Max(GetMapTileYAtPoint(oldBottomLeft.Y) - 1, endY)
            Dim dist As Integer = Math.Max(Math.Abs(endY - begY), 1)

            Dim tileIndexX As Integer

            For tileIndexY As Integer = begY To endY Step -1
                Dim bottomLeft As Vector2 = Vector2.Lerp(newBottomLeft, oldBottomLeft, CSng(Math.Abs(endY - tileIndexY)) / dist)
                Dim bottomRight As Vector2 = New Vector2(bottomLeft.X + mAABB.halfSize.X * 2.0F - 2, bottomLeft.Y)

                Dim checkedTile As Vector2 = bottomLeft
                Do
                    checkedTile.X = Math.Min(checkedTile.X, bottomRight.X)

                    tileIndexX = GetMapTileXAtPoint(checkedTile.X)

                    groundY = tileIndexY * mTileSize + mTileSize

                    If mMap.GetTile(tileIndexX, tileIndexY) IsNot Nothing Then Return True
                    If checkedTile.X >= bottomRight.X Then Exit Do

                    checkedTile.X += mTileSize
                Loop
            Next tileIndexY

            Return False
        End Function

        Private Function HasCeiling(ByVal oldPosition As Vector2, ByVal position As Vector2, ByRef ceilingY As Integer) As Boolean
            Dim center As Vector2 = position
            Dim oldCenter As Vector2 = oldPosition

            ceilingY = 0.0F

            Dim oldTopRight As Vector2 = RoundVector(oldCenter + mAABB.halfSize + New Vector2(-1, 1))
            Dim newTopRight As Vector2 = RoundVector(center + mAABB.halfSize + New Vector2(-1, 1))
            Dim newTopLeft As Vector2 = RoundVector(New Vector2(newTopRight.X - mAABB.halfSize.X * 2 + 2.0F, newTopRight.Y))

            Dim endY As Integer = GetMapTileYAtPoint(newTopRight.Y)
            Dim begY As Integer = Math.Min(GetMapTileYAtPoint(oldTopRight.Y) + 1, endY)
            Dim dist As Integer = Math.Max(Math.Abs(endY - begY), 1)

            Dim tileIndexX As Integer

            For tileIndexY As Integer = begY To endY
                Dim topRight As Vector2 = Vector2.Lerp(newTopRight, oldTopRight, CSng(Math.Abs(endY - tileIndexY)) / dist)
                Dim topLeft As Vector2 = New Vector2(topRight.X - mAABB.halfSize.X * 2.0F + 2, topRight.Y)

                Dim checkedTile As Vector2 = topLeft
                Do
                    checkedTile.X = Math.Min(checkedTile.X, topRight.X)

                    tileIndexX = GetMapTileXAtPoint(checkedTile.X)

                    If mMap.GetTile(tileIndexX, tileIndexY) IsNot Nothing Then
                        ceilingY = tileIndexY * mTileSize
                        Return True
                    End If

                    If checkedTile.X >= topRight.X Then
                        Exit Do
                    End If
                    checkedTile.X += mTileSize
                Loop
            Next tileIndexY

            Return False
        End Function

        Private Function CollidesWithLeftWall(ByVal oldPosition As Vector2, ByVal position As Vector2, ByRef wallX As Integer) As Boolean
            Dim center As Vector2 = position
            Dim oldCenter As Vector2 = oldPosition

            wallX = 0.0F

            Dim oldBottomLeft As Vector2 = RoundVector(oldCenter - mAABB.halfSize - New Vector2(1, 0))
            Dim newBottomLeft As Vector2 = RoundVector(center - mAABB.halfSize - New Vector2(1, 0))
            Dim newTopLeft As Vector2 = RoundVector(newBottomLeft + New Vector2(0.0F, mAABB.halfSize.Y * 2.0F))

            Dim tileIndexY As Integer

            Dim endX As Integer = GetMapTileXAtPoint(newBottomLeft.X)
            Dim begX As Integer = Math.Max(GetMapTileXAtPoint(oldBottomLeft.X) - 1, endX)
            Dim dist As Integer = Math.Max(Math.Abs(endX - begX), 1)

            For tileIndexX As Integer = begX To endX Step -1
                Dim bottomLeft As Vector2 = Vector2.Lerp(newBottomLeft, oldBottomLeft, CSng(Math.Abs(endX - tileIndexX)) / dist)
                Dim topLeft As Vector2 = bottomLeft + New Vector2(0.0F, mAABB.halfSize.Y * 2.0F)

                Dim checkedTile As Vector2 = bottomLeft
                Do
                    checkedTile.Y = Math.Min(checkedTile.Y, topLeft.Y)

                    tileIndexY = GetMapTileYAtPoint(checkedTile.Y)

                    If mMap.GetTile(tileIndexX, tileIndexY) IsNot Nothing Then
                        wallX = tileIndexX * mTileSize + mTileSize
                        Return True
                    End If

                    If checkedTile.Y >= topLeft.Y Then
                        Exit Do
                    End If
                    checkedTile.Y += mTileSize
                Loop
            Next tileIndexX

            Return False
        End Function

        Private Function CollidesWithRightWall(ByVal oldPosition As Vector2, ByVal position As Vector2, ByRef wallX As Integer) As Boolean
            Dim center As Vector2 = position
            Dim oldCenter As Vector2 = oldPosition

            wallX = 0.0F

            Dim oldBottomRight As Vector2 = RoundVector(oldCenter + New Vector2(mAABB.halfSize.X, -mAABB.halfSize.Y) + New Vector2(1, 0))
            Dim newBottomRight As Vector2 = RoundVector(center + New Vector2(mAABB.halfSize.X, -mAABB.halfSize.Y) + New Vector2(1, 0))
            Dim newTopRight As Vector2 = RoundVector(newBottomRight + New Vector2(0.0F, mAABB.halfSize.Y * 2.0F))

            Dim endX As Integer = GetMapTileXAtPoint(newBottomRight.X)
            Dim begX As Integer = Math.Min(GetMapTileXAtPoint(oldBottomRight.X) + 1, endX)
            Dim dist As Integer = Math.Max(Math.Abs(endX - begX), 1)

            Dim tileIndexY As Integer

            For tileIndexX As Integer = begX To endX
                Dim bottomRight As Vector2 = Vector2.Lerp(newBottomRight, oldBottomRight, CSng(Math.Abs(endX - tileIndexX)) / dist)
                Dim topRight As Vector2 = bottomRight + New Vector2(0.0F, mAABB.halfSize.Y * 2.0F)

                Dim checkedTile As Vector2 = bottomRight
                Do
                    checkedTile.Y = Math.Min(checkedTile.Y, topRight.Y)

                    tileIndexY = GetMapTileYAtPoint(checkedTile.Y)

                    If mMap.GetTile(tileIndexX, tileIndexY) IsNot Nothing Then
                        wallX = tileIndexX * mTileSize
                        Return True
                    End If

                    If checkedTile.Y >= topRight.Y Then
                        Exit Do
                    End If
                    checkedTile.Y += mTileSize
                Loop
            Next tileIndexX
            Return False
        End Function


        Private Function GetMapTileYAtPoint(ByVal y As Single) As Integer
            Return CInt(Math.Truncate(y / CSng(mTileSize)))
        End Function

        Private Function GetMapTileXAtPoint(ByVal x As Single) As Integer
            Return CInt(Math.Truncate((x) / CSng(mTileSize)))
        End Function
    End Class
End Namespace