using System;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;


namespace WebDAVServer.FileSystemStorage.HttpListener
{
    /// <summary>
    /// Fixes Mac OS X Finder bug with 0Kb files upload.
    /// </summary>
    /// <remarks>
    /// <para>This class provides workaround for MAC OS X bug that appears in v10.0.3 or later.
    /// The Finder uploads files of 0 bytes in size. 
    /// The bug is caused by incorrect HTTP headers submitted by Mac OS X Finder.</para>
    /// <para>You do not need this class if your server is not based on HttpListener or you are not using MAC OS X Finder.</para>
    /// <para>Call the <see cref="Process"/> static method before using any <c>HttpListener</c> methods or properties 
    /// or calling <c>Engine.Run</c> method. Pass the <c>HttpListenerRequest</c> instance as a parameter. 
    /// This call will fix incorrect headers submitted by server.</para>
    /// </remarks>
    public class MacOsXPreprocessor
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_REQUEST
        {
            private uint Flags;
            private ulong ConnectionId;
            private ulong RequestId;
            private ulong UrlContext;
            private HTTP_VERSION Version;
            private HTTP_VERB Verb;
            private ushort UnknownVerbLength;
            private ushort RawUrlLength;
            private IntPtr pUnknownVerb;
            private IntPtr pRawUrl;
            private HTTP_COOKED_URL CookedUrl;
            private HTTP_TRANSPORT_ADDRESS Address;
            internal HTTP_REQUEST_HEADERS Headers;
            private ulong BytesReceived;
            private ushort EntityChunkCount;
            private IntPtr pEntityChunks;
            private ulong RawConnectionId;
            private IntPtr pSslInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_VERSION
        {
            private ushort MajorVersion;
            private ushort MinorVersion;
        }

        private enum HTTP_VERB
        {
            HttpVerbUnparsed,
            HttpVerbUnknown,
            HttpVerbInvalid,
            HttpVerbOPTIONS,
            HttpVerbGET,
            HttpVerbHEAD,
            HttpVerbPOST,
            HttpVerbPUT,
            HttpVerbDELETE,
            HttpVerbTRACE,
            HttpVerbCONNECT,
            HttpVerbTRACK,
            HttpVerbMOVE,
            HttpVerbCOPY,
            HttpVerbPROPFIND,
            HttpVerbPROPPATCH,
            HttpVerbMKCOL,
            HttpVerbLOCK,
            HttpVerbUNLOCK,
            HttpVerbSEARCH,
            HttpVerbMaximum
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_COOKED_URL
        {
            private ushort FullUrlLength;
            private ushort HostLength;
            private ushort AbsPathLength;
            private ushort QueryStringLength;
            private IntPtr pFullUrl;
            private IntPtr pHost;
            private IntPtr pAbsPath;
            private IntPtr pQueryString;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_TRANSPORT_ADDRESS
        {
            private IntPtr pRemoteAddress;
            private IntPtr pLocalAddress;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_REQUEST_HEADERS
        {
            private ushort UnknownHeaderCount;
            private IntPtr pUnknownHeaders;
            private ushort TrailerCount;
            private IntPtr pTrailers;
            private HTTP_KNOWN_HEADER KnownHeader01;
            private HTTP_KNOWN_HEADER KnownHeader02;
            private HTTP_KNOWN_HEADER KnownHeader03;
            private HTTP_KNOWN_HEADER KnownHeader04;
            private HTTP_KNOWN_HEADER KnownHeader05;
            private HTTP_KNOWN_HEADER KnownHeader06;
            internal HTTP_KNOWN_HEADER TransferEncoding;
            private HTTP_KNOWN_HEADER KnownHeader08;
            private HTTP_KNOWN_HEADER KnownHeader09;
            private HTTP_KNOWN_HEADER KnownHeader10;
            private HTTP_KNOWN_HEADER KnownHeader11;
            private HTTP_KNOWN_HEADER KnownHeader12;
            private HTTP_KNOWN_HEADER KnownHeader13;
            private HTTP_KNOWN_HEADER KnownHeader14;
            private HTTP_KNOWN_HEADER KnownHeader15;
            private HTTP_KNOWN_HEADER KnownHeader16;
            private HTTP_KNOWN_HEADER KnownHeader17;
            private HTTP_KNOWN_HEADER KnownHeader18;
            private HTTP_KNOWN_HEADER KnownHeader19;
            private HTTP_KNOWN_HEADER KnownHeader20;
            private HTTP_KNOWN_HEADER KnownHeader21;
            private HTTP_KNOWN_HEADER KnownHeader22;
            private HTTP_KNOWN_HEADER KnownHeader23;
            private HTTP_KNOWN_HEADER KnownHeader24;
            private HTTP_KNOWN_HEADER KnownHeader25;
            private HTTP_KNOWN_HEADER KnownHeader26;
            private HTTP_KNOWN_HEADER KnownHeader27;
            private HTTP_KNOWN_HEADER KnownHeader28;
            private HTTP_KNOWN_HEADER KnownHeader29;
            private HTTP_KNOWN_HEADER KnownHeader30;
            private HTTP_KNOWN_HEADER KnownHeader31;
            private HTTP_KNOWN_HEADER KnownHeader32;
            private HTTP_KNOWN_HEADER KnownHeader33;
            private HTTP_KNOWN_HEADER KnownHeader34;
            private HTTP_KNOWN_HEADER KnownHeader35;
            private HTTP_KNOWN_HEADER KnownHeader36;
            private HTTP_KNOWN_HEADER KnownHeader37;
            private HTTP_KNOWN_HEADER KnownHeader38;
            private HTTP_KNOWN_HEADER KnownHeader39;
            private HTTP_KNOWN_HEADER KnownHeader40;
            private HTTP_KNOWN_HEADER KnownHeader41;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HTTP_KNOWN_HEADER
        {
            private ushort RawValueLength;
            private IntPtr pRawValue;

            internal void FixFirstChar()
            {
                if (RawValueLength == 0)
                    return;
                byte[] c = new byte[] { 99 }; // 99 == 'c'
                Marshal.Copy(c, 0, pRawValue, 1);
            }
        }

        public static void Process(HttpListenerRequest request)
        {
            Type typeHttpListenerRequest = typeof(HttpListenerRequest);
            PropertyInfo propRequestBuffer = typeHttpListenerRequest.GetProperty(
                "RequestBuffer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            byte[] requestBuffer = (byte[])propRequestBuffer.GetValue(request, null);

            GCHandle pinnedRequestBuffer = GCHandle.Alloc(requestBuffer, GCHandleType.Pinned);
            try
            {
                HTTP_REQUEST httpRequest = (HTTP_REQUEST)Marshal.PtrToStructure(
                    pinnedRequestBuffer.AddrOfPinnedObject(),
                    typeof(HTTP_REQUEST));
                httpRequest.Headers.TransferEncoding.FixFirstChar();
            }
            finally
            {
                pinnedRequestBuffer.Free();
            }
        }
    }
}
