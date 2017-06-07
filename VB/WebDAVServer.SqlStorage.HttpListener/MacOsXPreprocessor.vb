Imports System
Imports System.Net
Imports System.Reflection
Imports System.Runtime.InteropServices

''' <summary>
''' Fixes Mac OS X Finder bug with 0Kb files upload.
''' </summary>
''' <remarks>
''' <para>This class provides workaround for MAC OS X bug that appears in v10.0.3 or later.
''' The Finder uploads files of 0 bytes in size. 
''' The bug is caused by incorrect HTTP headers submitted by Mac OS X Finder.</para>
''' <para>You do not need this class if your server is not based on HttpListener or you are not using MAC OS X Finder.</para>
''' <para>Call the <see cref="Process"/>  static method before using any <c>HttpListener</c> methods or properties 
''' or calling <c>Engine.Run</c> method. Pass the <c>HttpListenerRequest</c> instance as a parameter. 
''' This call will fix incorrect headers submitted by server.</para>
''' </remarks>
Public Class MacOsXPreprocessor

    <StructLayout(LayoutKind.Sequential)>
    Private Structure HTTP_REQUEST

        Private Flags As UInteger

        Private ConnectionId As ULong

        Private RequestId As ULong

        Private UrlContext As ULong

        Private Version As HTTP_VERSION

        Private Verb As HTTP_VERB

        Private UnknownVerbLength As UShort

        Private RawUrlLength As UShort

        Private pUnknownVerb As IntPtr

        Private pRawUrl As IntPtr

        Private CookedUrl As HTTP_COOKED_URL

        Private Address As HTTP_TRANSPORT_ADDRESS

        Friend Headers As HTTP_REQUEST_HEADERS

        Private BytesReceived As ULong

        Private EntityChunkCount As UShort

        Private pEntityChunks As IntPtr

        Private RawConnectionId As ULong

        Private pSslInfo As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure HTTP_VERSION

        Private MajorVersion As UShort

        Private MinorVersion As UShort
    End Structure

    Private Enum HTTP_VERB
        HttpVerbUnparsed
        HttpVerbUnknown
        HttpVerbInvalid
        HttpVerbOPTIONS
        HttpVerbGET
        HttpVerbHEAD
        HttpVerbPOST
        HttpVerbPUT
        HttpVerbDELETE
        HttpVerbTRACE
        HttpVerbCONNECT
        HttpVerbTRACK
        HttpVerbMOVE
        HttpVerbCOPY
        HttpVerbPROPFIND
        HttpVerbPROPPATCH
        HttpVerbMKCOL
        HttpVerbLOCK
        HttpVerbUNLOCK
        HttpVerbSEARCH
        HttpVerbMaximum
    End Enum

    <StructLayout(LayoutKind.Sequential)>
    Private Structure HTTP_COOKED_URL

        Private FullUrlLength As UShort

        Private HostLength As UShort

        Private AbsPathLength As UShort

        Private QueryStringLength As UShort

        Private pFullUrl As IntPtr

        Private pHost As IntPtr

        Private pAbsPath As IntPtr

        Private pQueryString As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure HTTP_TRANSPORT_ADDRESS

        Private pRemoteAddress As IntPtr

        Private pLocalAddress As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure HTTP_REQUEST_HEADERS

        Private UnknownHeaderCount As UShort

        Private pUnknownHeaders As IntPtr

        Private TrailerCount As UShort

        Private pTrailers As IntPtr

        Private KnownHeader01 As HTTP_KNOWN_HEADER

        Private KnownHeader02 As HTTP_KNOWN_HEADER

        Private KnownHeader03 As HTTP_KNOWN_HEADER

        Private KnownHeader04 As HTTP_KNOWN_HEADER

        Private KnownHeader05 As HTTP_KNOWN_HEADER

        Private KnownHeader06 As HTTP_KNOWN_HEADER

        Friend TransferEncoding As HTTP_KNOWN_HEADER

        Private KnownHeader08 As HTTP_KNOWN_HEADER

        Private KnownHeader09 As HTTP_KNOWN_HEADER

        Private KnownHeader10 As HTTP_KNOWN_HEADER

        Private KnownHeader11 As HTTP_KNOWN_HEADER

        Private KnownHeader12 As HTTP_KNOWN_HEADER

        Private KnownHeader13 As HTTP_KNOWN_HEADER

        Private KnownHeader14 As HTTP_KNOWN_HEADER

        Private KnownHeader15 As HTTP_KNOWN_HEADER

        Private KnownHeader16 As HTTP_KNOWN_HEADER

        Private KnownHeader17 As HTTP_KNOWN_HEADER

        Private KnownHeader18 As HTTP_KNOWN_HEADER

        Private KnownHeader19 As HTTP_KNOWN_HEADER

        Private KnownHeader20 As HTTP_KNOWN_HEADER

        Private KnownHeader21 As HTTP_KNOWN_HEADER

        Private KnownHeader22 As HTTP_KNOWN_HEADER

        Private KnownHeader23 As HTTP_KNOWN_HEADER

        Private KnownHeader24 As HTTP_KNOWN_HEADER

        Private KnownHeader25 As HTTP_KNOWN_HEADER

        Private KnownHeader26 As HTTP_KNOWN_HEADER

        Private KnownHeader27 As HTTP_KNOWN_HEADER

        Private KnownHeader28 As HTTP_KNOWN_HEADER

        Private KnownHeader29 As HTTP_KNOWN_HEADER

        Private KnownHeader30 As HTTP_KNOWN_HEADER

        Private KnownHeader31 As HTTP_KNOWN_HEADER

        Private KnownHeader32 As HTTP_KNOWN_HEADER

        Private KnownHeader33 As HTTP_KNOWN_HEADER

        Private KnownHeader34 As HTTP_KNOWN_HEADER

        Private KnownHeader35 As HTTP_KNOWN_HEADER

        Private KnownHeader36 As HTTP_KNOWN_HEADER

        Private KnownHeader37 As HTTP_KNOWN_HEADER

        Private KnownHeader38 As HTTP_KNOWN_HEADER

        Private KnownHeader39 As HTTP_KNOWN_HEADER

        Private KnownHeader40 As HTTP_KNOWN_HEADER

        Private KnownHeader41 As HTTP_KNOWN_HEADER
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure HTTP_KNOWN_HEADER

        Private RawValueLength As UShort

        Private pRawValue As IntPtr

        Friend Sub FixFirstChar()
            If RawValueLength = 0 Then Return
            Dim c As Byte() = New Byte() {99}
            Marshal.Copy(c, 0, pRawValue, 1)
        End Sub
    End Structure

    Public Shared Sub Process(request As HttpListenerRequest)
        Dim typeHttpListenerRequest As Type = GetType(HttpListenerRequest)
        Dim propRequestBuffer As PropertyInfo = typeHttpListenerRequest.GetProperty("RequestBuffer",
                                                                                   BindingFlags.NonPublic Or BindingFlags.Instance)
        Dim requestBuffer As Byte() = CType(propRequestBuffer.GetValue(request, Nothing), Byte())
        Dim pinnedRequestBuffer As GCHandle = GCHandle.Alloc(requestBuffer, GCHandleType.Pinned)
        Try
            Dim httpRequest As HTTP_REQUEST = CType(Marshal.PtrToStructure(pinnedRequestBuffer.AddrOfPinnedObject(),
                                                                          GetType(HTTP_REQUEST)), HTTP_REQUEST)
            httpRequest.Headers.TransferEncoding.FixFirstChar()
        Finally
            pinnedRequestBuffer.Free()
        End Try
    End Sub
End Class
