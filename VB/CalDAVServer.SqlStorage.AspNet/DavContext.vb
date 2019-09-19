Imports System
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.Security.Principal
Imports System.Web
Imports System.Diagnostics
Imports System.Threading.Tasks
Imports ITHit.Server
Imports ITHit.WebDAV.Server
Imports CalDAVServer.SqlStorage.AspNet.Acl
Imports CalDAVServer.SqlStorage.AspNet.CalDav

''' <summary>
''' WebDAV request context. Is used by WebDAV engine to resolve path into items.
''' Implements abstract methods from <see cref="DavContextBaseAsync"/> ,
''' contains useful methods for working with transactions, connections, reading
''' varios items from database.
''' </summary>
Public Class DavContext
    Inherits ContextWebAsync(Of IHierarchyItemAsync)
    Implements IDisposable

    ''' <summary>
    ''' Database connection string.
    ''' </summary>
    Private Shared ReadOnly connectionString As String = ConfigurationManager.ConnectionStrings("WebDAV").ConnectionString

    ''' <summary>
    ''' Cached connection for the request.
    ''' </summary>
    Private connection As SqlConnection

    ''' <summary>
    ''' Transaction for the request.
    ''' </summary>
    Private transaction As SqlTransaction

    ''' <summary>
    ''' Currently logged-in identity.
    ''' </summary>
    Friend Property Identity As IIdentity

    ''' <summary>
    ''' Currently logged-in user ID.
    ''' </summary>
    Friend ReadOnly Property UserId As String
        Get
            Return Identity.Name.ToLower()
        End Get
    End Property

    ''' <summary>
    ''' Gets <see cref="ILogger"/>  instance.
    ''' </summary>
    Friend Property Logger As ILogger

    ''' <summary>
    ''' Initializes a new instance of the <see cref="DavContext"/>  class from <see cref="HttpContext"/> .
    ''' </summary>
    ''' <param name="context">Instance of <see cref="HttpContext"/> .</param>
    Public Sub New(context As HttpContext)
        MyBase.New(context)
        Identity = context.User.Identity
        Logger = CalDAVServer.SqlStorage.AspNet.Logger.Instance
    End Sub

    ''' <summary>
    ''' Resolves path to instance of <see cref="IHierarchyItem"/> .
    ''' This method is called by WebDAV engine to resolve paths it encounters
    ''' in request.
    ''' </summary>
    ''' <param name="path">Relative path to the item including query string.</param>
    ''' <returns><see cref="IHierarchyItem"/>  instance if item is found, <c>null</c> otherwise.</returns>
    Public Overrides Async Function GetHierarchyItemAsync(path As String) As Task(Of IHierarchyItemAsync)
        path = path.Trim({" "c, "/"c})
        'remove query string.
        Dim ind As Integer = path.IndexOf("?"c)
        If ind > -1 Then
            path = path.Remove(ind)
        End If

        Dim item As IHierarchyItemAsync = Nothing
        ' Return items from [DAVLocation]/acl/ folder and subfolders.
        item = Await AclFactory.GetAclItemAsync(Me, path)
        If item IsNot Nothing Then Return item
        ' Return items from [DAVLocation]/calendars/ folder and subfolders.
        item = Await CalDavFactory.GetCalDavItemAsync(Me, path)
        If item IsNot Nothing Then Return item
        ' Return folder that corresponds to [DAVLocation] path. If no DavLocation section is defined in web.config/app.config [DAVLocation] is a website root.
        Dim davLocation As String = DavLocationFolder.DavLocationFolderPath.Trim("/"c)
        If davLocation.Equals(path, StringComparison.InvariantCultureIgnoreCase) Then
            Return New DavLocationFolder(Me)
        End If

        ' Return any folders above [DAVLocation] path.
        ' Root folder with ICalendarDiscovery/IAddressbookDiscovery implementation is required for calendars and address books discovery by CalDAV/CardDAV clients.
        ' All other folders are returned just for folders structure browsing convenience using WebDAV clients during dev time, not required by CalDAV/CardDAV clients.
        If davLocation.StartsWith(path, StringComparison.InvariantCultureIgnoreCase) Then
            Dim childFolderPathLength As Integer =(davLocation & "/").IndexOf("/"c, path.Length + 1)
            Dim childFolderPath As String = davLocation.Substring(0, childFolderPathLength)
            Return New LogicalFolder(Me, path, {New LogicalFolder(Me, childFolderPath)})
        End If

        Logger.LogDebug("Could not find item that corresponds to path: " & path)
        Return Nothing
    End Function

    ''' <summary>
    ''' The method is called by WebDAV engine right before starting sending response to client.
    ''' It is good point to either commit or rollback a transaction depending on whether
    ''' and exception occurred.
    ''' </summary>
    Public Overrides Async Function BeforeResponseAsync() As Task
        ' Analyze Exception property to see if there was an exception during request execution.
        ' The property is set by engine.
        If Exception IsNot Nothing Then
            'rollback the transaction if something went wrong.
            RollBackTransaction()
        Else
            'commit the transaction if everything is ok.
            CommitTransaction()
        End If
    End Function

    ''' <summary>
    ''' We implement <see cref="IDisposable"/>  to have connection closed.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        CloseConnection()
    End Sub

    ''' <summary>
    ''' Commits active transaction.
    ''' </summary>
    Public Sub CommitTransaction()
        If transaction IsNot Nothing Then
            transaction.Commit()
        End If
    End Sub

    ''' <summary>
    ''' Rollbacks active transaction.
    ''' </summary>
    Public Sub RollBackTransaction()
        If transaction IsNot Nothing Then
            transaction.Rollback()
        End If
    End Sub

    ''' <summary>
    ''' Closes connection.
    ''' </summary>
    Public Sub CloseConnection()
        If connection IsNot Nothing AndAlso connection.State <> ConnectionState.Closed Then
            connection.Close()
        End If
    End Sub

    ''' <summary>
    ''' Executes SQL command which returns scalar result.
    ''' </summary>
    ''' <param name="command">Command text.</param>
    ''' <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
    ''' <typeparam name="T">Type of object SQL command returns.</typeparam>
    ''' <returns>Command result of type <typeparamref name="T"/> .</returns>
    Public Async Function ExecuteScalarAsync(Of T)(command As String, ParamArray prms As Object()) As Task(Of T)
        Dim sqlCommand As SqlCommand = Await prepareCommandAsync(command, prms)
        Dim o As Object = Await sqlCommand.ExecuteScalarAsync()
        Return If(TypeOf o Is DBNull, Nothing, CType(o, T))
    End Function

    ''' <summary>
    ''' Executes SQL command which returns no results.
    ''' </summary>
    ''' <param name="command">Command text.</param>
    ''' <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
    ''' <returns>The number of rows affected.</returns>
    Public Async Function ExecuteNonQueryAsync(command As String, ParamArray prms As Object()) As Task(Of Integer)
        Dim sqlCommand As SqlCommand = Await prepareCommandAsync(command, prms)
        Logger.LogDebug(String.Format("Executing SQL: {0}", sqlCommand.CommandText))
        Dim stopWatch As Stopwatch = Stopwatch.StartNew()
        Dim rowsAffected As Integer = Await sqlCommand.ExecuteNonQueryAsync()
        Logger.LogDebug(String.Format("SQL took: {0}ms", stopWatch.ElapsedMilliseconds))
        Return rowsAffected
    End Function

    ''' <summary>
    ''' Executes specified command and returns <see cref="SqlDataReader"/> .
    ''' </summary>
    ''' <param name="commandBehavior">Value of <see cref="CommandBehavior"/>  enumeration.</param>
    ''' <param name="command">Command text.</param>
    ''' <param name="prms">Parameter pairs: SQL param name, SQL param value</param>
    ''' <returns>Instance of <see cref="SqlDataReader"/> .</returns>
    Public Async Function ExecuteReaderAsync(command As String, ParamArray prms As Object()) As Task(Of SqlDataReader)
        Dim sqlCommand As SqlCommand = Await prepareCommandAsync(command, prms)
        Logger.LogDebug(String.Format("Executing SQL: {0}", sqlCommand.CommandText))
        Return Await sqlCommand.ExecuteReaderAsync()
    End Function

    ''' <summary>
    ''' Executes specified command and returns <see cref="SqlDataReader"/> .
    ''' </summary>
    ''' <param name="commandBehavior">Value of <see cref="CommandBehavior"/>  enumeration.</param>
    ''' <param name="command">Command text.</param>
    ''' <param name="prms">Parameter pairs: SQL param name, SQL param value.</param>
    ''' <returns>Instance of <see cref="SqlDataReader"/> .</returns>
    Public Async Function ExecuteReaderAsync(commandBehavior As CommandBehavior, command As String, ParamArray prms As Object()) As Task(Of SqlDataReader)
        Dim sqlCommand As SqlCommand = Await prepareCommandAsync(command, prms)
        Logger.LogDebug(String.Format("Executing SQL: {0}", sqlCommand.CommandText))
        Return Await sqlCommand.ExecuteReaderAsync(commandBehavior)
    End Function

    ''' <summary>
    ''' Creates <see cref="SqlCommand"/> .
    ''' </summary>
    ''' <returns>Instance of <see cref="SqlCommand"/> .</returns>
    Private Async Function createNewCommandAsync() As Task(Of SqlCommand)
        If Me.connection Is Nothing Then
            Me.connection = New SqlConnection(connectionString)
            Await Me.connection.OpenAsync()
            Me.transaction = Me.connection.BeginTransaction(IsolationLevel.ReadUncommitted)
        End If

        Dim newCmd As SqlCommand = connection.CreateCommand()
        newCmd.Transaction = transaction
        Return newCmd
    End Function

    ''' <summary>
    ''' Creates <see cref="SqlCommand"/> .
    ''' </summary>
    ''' <param name="command">Command text.</param>
    ''' <param name="prms">Either list of SqlParameters or command parameters in pairs: name, value</param>
    ''' <returns>Instace of <see cref="SqlCommand"/> .</returns>
    Private Async Function prepareCommandAsync(command As String, ParamArray prms As Object()) As Task(Of SqlCommand)
        Dim cmd As SqlCommand = Await createNewCommandAsync()
        cmd.CommandText = command
        For i As Integer = 0 To prms.Length - 1
            If TypeOf prms(i) Is String Then
                ' name-value pair
                cmd.Parameters.AddWithValue(CStr(prms(i)), If(prms(i + 1), DBNull.Value))
                i += 1
            ElseIf TypeOf prms(i) Is SqlParameter Then
                ' SqlParameter
                cmd.Parameters.Add(TryCast(prms(i), SqlParameter))
            Else
                Throw New ArgumentException(prms(i) & "is invalid parameter name")
            End If
        Next

        Return cmd
    End Function
End Class
