
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Framework.Misc
    Friend Class VertexExtractor
        Public Shared Function CreateBoundingBox(ByVal model As Model, ByVal DefMatrix As Matrix) As BoundingBox
            Dim boneTransforms As Matrix() = New Matrix(model.Bones.Count - 1) {}
            model.CopyAbsoluteBoneTransformsTo(boneTransforms)
            Dim result As BoundingBox = New BoundingBox()

            For Each mesh As ModelMesh In model.Meshes

                For Each meshPart As ModelMeshPart In mesh.MeshParts
                    Dim meshPartBoundingBox As BoundingBox? = GetBoundingBox(meshPart, boneTransforms(mesh.ParentBone.Index) * DefMatrix)
                    If meshPartBoundingBox IsNot Nothing Then result = BoundingBox.CreateMerged(result, meshPartBoundingBox.Value)
                Next
            Next

            Return result
        End Function
        Private Shared Function GetBoundingBox(ByVal meshPart As ModelMeshPart, ByVal transform As Matrix) As BoundingBox?
            If meshPart.VertexBuffer Is Nothing Then Return Nothing
            Dim positions As Vector3() = GetVertexElement(meshPart, VertexElementUsage.Position)
            If positions Is Nothing Then Return Nothing
            Dim transformedPositions As Vector3() = New Vector3(positions.Length - 1) {}
            Vector3.Transform(positions, transform, transformedPositions)
            Return BoundingBox.CreateFromPoints(transformedPositions)
        End Function

        Private Shared Function GetVertexElement(ByVal meshPart As ModelMeshPart, ByVal usage As VertexElementUsage) As Vector3()
            Dim vd As VertexDeclaration = meshPart.VertexBuffer.VertexDeclaration
            Dim elements As VertexElement() = vd.GetVertexElements()
            Dim elementPredicate As Func(Of VertexElement, Boolean) = Function(ve) ve.VertexElementUsage = usage AndAlso ve.VertexElementFormat = VertexElementFormat.Vector3
            If Not elements.Any(elementPredicate) Then Return Nothing
            Dim element As VertexElement = elements.First(elementPredicate)
            Dim vertexData As Vector3() = New Vector3(meshPart.NumVertices - 1) {}
            meshPart.VertexBuffer.GetData((meshPart.VertexOffset * vd.VertexStride) + element.Offset, vertexData, 0, vertexData.Length, vd.VertexStride)
            Return vertexData
        End Function
        Public Class BoundingBoxBuffers
            Public Vertices As VertexBuffer
            Public VertexCount As Integer
            Public Indices As IndexBuffer
            Public PrimitiveCount As Integer

            Public Shared Function CreateBoundingBoxBuffers(ByVal boundingBox As BoundingBox, ByVal graphicsDevice As GraphicsDevice) As BoundingBoxBuffers
                Dim boundingBoxBuffers As BoundingBoxBuffers = New BoundingBoxBuffers With {
                    .PrimitiveCount = 24,
                    .VertexCount = 48
                }
                Dim vertexBuffer As VertexBuffer = New VertexBuffer(graphicsDevice, GetType(VertexPositionColor), boundingBoxBuffers.VertexCount, BufferUsage.[WriteOnly])
                Dim vertices As List(Of VertexPositionColor) = New List(Of VertexPositionColor)()
                Const ratio As Single = 5.0F
                Dim xOffset As Vector3 = New Vector3((boundingBox.Max.X - boundingBox.Min.X) / ratio, 0, 0)
                Dim yOffset As Vector3 = New Vector3(0, (boundingBox.Max.Y - boundingBox.Min.Y) / ratio, 0)
                Dim zOffset As Vector3 = New Vector3(0, 0, (boundingBox.Max.Z - boundingBox.Min.Z) / ratio)
                Dim corners As Vector3() = boundingBox.GetCorners()
                AddVertex(vertices, corners(0))
                AddVertex(vertices, corners(0) + xOffset)
                AddVertex(vertices, corners(0))
                AddVertex(vertices, corners(0) - yOffset)
                AddVertex(vertices, corners(0))
                AddVertex(vertices, corners(0) - zOffset)
                AddVertex(vertices, corners(1))
                AddVertex(vertices, corners(1) - xOffset)
                AddVertex(vertices, corners(1))
                AddVertex(vertices, corners(1) - yOffset)
                AddVertex(vertices, corners(1))
                AddVertex(vertices, corners(1) - zOffset)
                AddVertex(vertices, corners(2))
                AddVertex(vertices, corners(2) - xOffset)
                AddVertex(vertices, corners(2))
                AddVertex(vertices, corners(2) + yOffset)
                AddVertex(vertices, corners(2))
                AddVertex(vertices, corners(2) - zOffset)
                AddVertex(vertices, corners(3))
                AddVertex(vertices, corners(3) + xOffset)
                AddVertex(vertices, corners(3))
                AddVertex(vertices, corners(3) + yOffset)
                AddVertex(vertices, corners(3))
                AddVertex(vertices, corners(3) - zOffset)
                AddVertex(vertices, corners(4))
                AddVertex(vertices, corners(4) + xOffset)
                AddVertex(vertices, corners(4))
                AddVertex(vertices, corners(4) - yOffset)
                AddVertex(vertices, corners(4))
                AddVertex(vertices, corners(4) + zOffset)
                AddVertex(vertices, corners(5))
                AddVertex(vertices, corners(5) - xOffset)
                AddVertex(vertices, corners(5))
                AddVertex(vertices, corners(5) - yOffset)
                AddVertex(vertices, corners(5))
                AddVertex(vertices, corners(5) + zOffset)
                AddVertex(vertices, corners(6))
                AddVertex(vertices, corners(6) - xOffset)
                AddVertex(vertices, corners(6))
                AddVertex(vertices, corners(6) + yOffset)
                AddVertex(vertices, corners(6))
                AddVertex(vertices, corners(6) + zOffset)
                AddVertex(vertices, corners(7))
                AddVertex(vertices, corners(7) + xOffset)
                AddVertex(vertices, corners(7))
                AddVertex(vertices, corners(7) + yOffset)
                AddVertex(vertices, corners(7))
                AddVertex(vertices, corners(7) + zOffset)
                vertexBuffer.SetData(vertices.ToArray())
                boundingBoxBuffers.Vertices = vertexBuffer
                Dim indexBuffer As IndexBuffer = New IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, boundingBoxBuffers.VertexCount, BufferUsage.[WriteOnly])
                indexBuffer.SetData(Enumerable.Range(0, boundingBoxBuffers.VertexCount).[Select](Function(i) CShort(i)).ToArray())
                boundingBoxBuffers.Indices = indexBuffer
                Return boundingBoxBuffers
            End Function

            Private Shared Sub AddVertex(ByVal vertices As List(Of VertexPositionColor), ByVal position As Vector3)
                vertices.Add(New VertexPositionColor(position, Color.White))
            End Sub
        End Class
    End Class
End Namespace