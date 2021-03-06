Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics

Namespace Framework.Graphics
    ''' <summary>
    ''' Custom Vertex that allows the use of Position, Color and a Normal.
    ''' </summary>
    ''' <remarks>  Thanks to Riemer's XNA Tutorials for this format.  
    ''' http://www.riemers.net/eng/Tutorials/XNA/Csharp/Series1/Lighting_basics.php 
    ''' 
    ''' </remarks>
    Public Structure VertexPositionColorNormal

        ''' <summary>
        ''' Vector3 describing the Vertex Location in 3D space.
        ''' </summary>
        ''' <remarks></remarks>
        Public Position As Vector3

        ''' <summary>
        ''' Color at the Vertex positon.
        ''' </summary>
        ''' <remarks></remarks>
        Public Color As Color

        ''' <summary>
        ''' Normal Coordinates for the Vertex
        ''' </summary>
        ''' <remarks></remarks>
        Public Normal As Vector3

        Public Sub New(ByVal vPos As Vector3, ByVal vCol As Color, ByVal vNor As Vector3)
            Position = vPos
            Color = vCol
            Normal = vNor
        End Sub


        ''' <summary>
        ''' Necessary VertexDeclaration information to use this Vertex with GraphicsDevice.DrawUserPrimitives.
        ''' </summary>
        ''' <value></value>
        ''' <returns>Valid VertexDeclaration defining the elements of this structure.</returns>
        ''' <remarks>
        ''' Example use:
        ''' GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, NumTriangles, VertexPositionColorNormal.VertexDeclaration)
        '''                                                                                                                    ^^^^^^^^^^^^^^^^^
        ''' </remarks>
        Public Shared ReadOnly Property VertexDeclaration() As VertexDeclaration = New VertexDeclaration({
                New VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                New VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                New VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)})

        Public Shared ReadOnly Property VertexDeclarationSuccy() As VertexDeclaration
            Get
                Return New VertexDeclaration({
                New VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                New VertexElement(12, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                New VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0)})
            End Get
        End Property
    End Structure

End Namespace