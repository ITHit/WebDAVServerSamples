Imports System
Imports System.Configuration
Imports System.IO
Imports System.Web
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Logger

''' <summary>
''' Logger singleton.
''' We need this singleton because logging is used in various modules, like authentication etc.
''' </summary>
Module Logger

    ''' <summary>
    ''' Whether debug logging is enabled. In particular most request/response bodies will
    ''' be logged if debug logging is on.
    ''' </summary>
    Private ReadOnly debugLoggingEnabled As Boolean = "true".Equals(ConfigurationManager.AppSettings("DebugLoggingEnabled"),
                                                                   StringComparison.InvariantCultureIgnoreCase)

    ''' <summary>
    ''' Path where log files will be stored.
    ''' </summary>
    Private ReadOnly logPath As String = ConfigurationManager.AppSettings("LogPath")

    ''' <summary>
    ''' Synchronization object.
    ''' </summary>
    Private ReadOnly syncRoot As Object = New Object()

    ''' <summary>
    ''' Gets logger instace.
    ''' </summary>
    Public ReadOnly Property Instance As ILogger
        Get
            Dim context = HttpContext.Current
            Const LOGGER_KEY As String = "$DavLogger$"
            If context.Application(LOGGER_KEY) Is Nothing Then
                SyncLock syncRoot
                    If context.Application(LOGGER_KEY) Is Nothing Then
                        context.Application(LOGGER_KEY) = initLogger()
                    End If

                End SyncLock
            End If

            Return CType(context.Application(LOGGER_KEY), ILogger)
        End Get
    End Property

    ''' <summary>
    ''' Initializes logger.
    ''' </summary>
    ''' <returns>Instance of <see cref="ILogger"/> .</returns>
    Private Function initLogger() As ILogger
        Dim logger = New DefaultLoggerImpl()
        Dim context = HttpContext.Current
        If Not String.IsNullOrEmpty(logPath) Then
            logger.LogFile = Path.Combine(context.Server.MapPath(logPath), "WebDAVlog.txt")
        Else
            logger.LogFile = Path.Combine(context.Request.PhysicalApplicationPath, "WebDAVlog.txt")
        End If

        logger.IsDebugEnabled = debugLoggingEnabled
        Return logger
    End Function
End Module
