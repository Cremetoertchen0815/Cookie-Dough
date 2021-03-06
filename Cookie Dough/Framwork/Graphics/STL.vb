Imports System.IO
Imports Microsoft.Xna.Framework

Namespace Framework.Graphics
    Public Class STLDefinition

        Public Class STLObject
            Public STLHeader As New STLHeader
            Public Vertices() As VertexPositionColorNormal
            Public Facets() As Facet
        End Class

        Public Class STLHeader
            'id' is a null-terminated string of the form "filename.stl", where filename is the name of the converted ".bin" file.
            Public Id(22) As Char
            'date' is the date stamp in UNIX ctime() format.
            Public DateCreated(26) As Char
            'xmin' - 'zmax' are the geometric bounds on the data 
            Public xmin As Single
            Public xmax As Single
            Public ymin As Single
            Public ymax As Single
            Public zmin As Single
            Public zmax As Single
            Public xpixelsize As Single  ' Dimensions of grid for this model
            Public ypixelsize As Single  ' in user units.
            Public nfacets As UInt32
            Public CenterMatrix As Matrix

            Public ReadOnly Property XCenter As Single
                Get
                    Return (xmax - xmin) / 2 + xmin
                End Get
            End Property

            Public ReadOnly Property YCenter As Single
                Get
                    Return (ymax - ymin) / 2 + ymin
                End Get
            End Property

            Public ReadOnly Property ZCenter As Single
                Get
                    Return (zmax - zmin) / 2 + zmin
                End Get
            End Property
        End Class

        Public Class Vertex
            Public x As Single
            Public y As Single
            Public z As Single
        End Class

        Public Class Facet
            Public normal As New Vertex '  facet surface normal
            Public v1 As New Vertex     '  vertex 1
            Public v2 As New Vertex     '  vertex 2
            Public v3 As New Vertex     '  vertex 3
        End Class

        ''' <summary>
        ''' LoadSTL reads a stream and converts the data to an STLObject.  
        ''' Stream must contain an STL formatted file.
        ''' </summary>
        ''' <param name="stream"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Shared Function LoadSTL(ByVal stream As Stream) As STLObject
            Dim vStl As New STLDefinition.STLObject
            Try
                Dim index As Int32 = 0
                Dim tb(4) As Byte
                Dim tb2(30) As Byte
                Dim sr As Stream = stream
                Dim x1 As Single, x2 As Single
                Dim y1 As Single, y2 As Single
                Dim z1 As Single, z2 As Single
                stream.Read(tb2, 0, 22)
                stream.Read(tb2, 0, 26)
                With vStl
                    With .STLHeader
                        .xmin = rc(sr)
                        .xmax = rc(sr)

                        .ymin = rc(sr)
                        .ymax = rc(sr)

                        .xmin = rc(sr)
                        .xmax = rc(sr)

                        .xpixelsize = rc(sr)
                        .ypixelsize = rc(sr)
                        sr.Read(tb, 0, 4)
                        .nfacets = BitConverter.ToUInt32(tb, 0)
                    End With
                    Dim retval As Facet
                    ReDim .Facets(.STLHeader.nfacets)
                    For i As Integer = 0 To CInt(.STLHeader.nfacets) - 1
                        retval = New Facet
                        With retval
                            With .normal
                                .x = rc(sr)
                                .y = rc(sr)
                                .z = -rc(sr) ' Negative Z value Flips to our coordinate system
                            End With
                            With .v1
                                .x = rc(sr)
                                If .x < x1 Then x1 = .x
                                If .x > x2 Then x2 = .x
                                .y = rc(sr)
                                If .y < y1 Then y1 = .y
                                If .y > y2 Then y2 = .y
                                .z = -rc(sr)
                                If .z < z1 Then z1 = .z
                                If .z > z2 Then z2 = .z
                            End With
                            With .v2
                                .x = rc(sr)
                                If .x < x1 Then x1 = .x
                                If .x > x2 Then x2 = .x
                                .y = rc(sr)
                                If .y < y1 Then y1 = .y
                                If .y > y2 Then y2 = .y
                                .z = -rc(sr)
                                If .z < z1 Then z1 = .z
                                If .z > z2 Then z2 = .z
                            End With
                            With .v3
                                .x = rc(sr)
                                If .x < x1 Then x1 = .x
                                If .x > x2 Then x2 = .x
                                .y = rc(sr)
                                If .y < y1 Then y1 = .y
                                If .y > y2 Then y2 = .y
                                .z = -rc(sr)
                                If .z < z1 Then z1 = .z
                                If .z > z2 Then z2 = .z
                            End With
                        End With
                        sr.Read(tb, 0, 2) ' just padding bytes not used.
                        .Facets(i) = retval
                    Next
                End With
                With vStl.STLHeader
                    If x1 > x2 Then
                        .xmin = x2
                        .xmax = x1
                    Else
                        .xmin = x1
                        .xmax = x2
                    End If
                    If y1 > y2 Then
                        .ymin = y2
                        .ymax = y1
                    Else
                        .ymin = y1
                        .ymax = y2
                    End If
                    If z1 > z2 Then
                        .zmin = z2
                        .zmax = z1
                    Else
                        .zmin = z1
                        .zmax = z2
                    End If
                    .CenterMatrix = Matrix.CreateTranslation(- .XCenter, - .YCenter, .ZCenter)
                End With

                Dim NumFacets = CInt(vStl.STLHeader.nfacets)
                Dim vn As Vector3
                Dim lVertices(NumFacets * 3) As VertexPositionColorNormal
                With vStl
                    For i As Int32 = 0 To NumFacets - 1
                        With .Facets(i)
                            With .normal
                                vn = New Vector3(.x, .y, .z)
                            End With
                            With .v1
                                lVertices(i * 3) = New VertexPositionColorNormal(New Vector3(.x, .y, .z), Color.White, vn)
                            End With
                        End With
                        With .Facets(i)
                            With .normal
                                vn = New Vector3(.x, .y, .z)
                            End With
                            With .v2
                                lVertices(i * 3 + 1) = New VertexPositionColorNormal(New Vector3(.x, .y, .z), Color.White, vn)
                            End With
                        End With
                        With .Facets(i)
                            With .normal
                                vn = New Vector3(.x, .y, .z)
                            End With
                            With .v3
                                lVertices(i * 3 + 2) = New VertexPositionColorNormal(New Vector3(.x, .y, .z), Color.White, vn)
                            End With
                        End With
                    Next
                End With
                vStl.Vertices = lVertices
            Catch
                vStl = Nothing
            End Try
            Return vStl
        End Function

        ''' <summary>
        ''' Reads 4 bytes from the supplied Stream,
        ''' converts them to a Single and returns the result.
        ''' </summary>
        ''' <param name="sr">A valid Stream object</param>
        ''' <returns>A Single value read from the stream.</returns>
        ''' <remarks></remarks>
        Private Shared Function rc(ByVal sr As Stream) As Single
            Dim tb(4) As Byte
            sr.Read(tb, 0, 4)
            Return BitConverter.ToSingle(tb, 0)
        End Function

    End Class
End Namespace