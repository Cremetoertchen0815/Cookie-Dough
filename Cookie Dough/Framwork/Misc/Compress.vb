Imports System.IO
Imports System.IO.Compression

Public Module Compress

    ''' <summary>
    ''' Compresses a Byte() array using the GZIP algorithm
    ''' </summary>
    Function Compress(ByVal toCompress As Byte()) As Byte()
        ' Get the stream of the source file.
        Using inputStream As MemoryStream = New MemoryStream(toCompress)

            ' Create the compressed stream.
            Using outputStream As MemoryStream = New MemoryStream()
                Using compressionStream As GZipStream =
                    New GZipStream(outputStream, CompressionLevel.Optimal)

                    ' Copy the source file into the compression stream.
                    inputStream.CopyTo(compressionStream)

                End Using

                Compress = outputStream.ToArray()

            End Using

        End Using
    End Function

    ''' <summary>
    ''' Decompresses a Byte() array using the GZIP algorithm.
    ''' </summary>
    Function Decompress(ByVal toDecompress As Byte()) As Byte()
        ' Get the stream of the source file.
        Using inputStream As MemoryStream = New MemoryStream(toDecompress)

            ' Create the decompressed stream.
            Using outputStream As MemoryStream = New MemoryStream()
                Using decompressionStream As GZipStream =
                    New GZipStream(inputStream, CompressionMode.Decompress)

                    ' Copy the decompression stream
                    ' into the output file.
                    decompressionStream.CopyTo(outputStream)

                End Using

                Decompress = outputStream.ToArray

            End Using
        End Using
    End Function
End Module
