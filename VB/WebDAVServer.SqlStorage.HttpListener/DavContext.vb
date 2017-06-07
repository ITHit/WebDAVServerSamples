Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Reflection
Imports System.Security.Principal
Imports System.Web
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Class2

''' <summary>
''' WebDAV request context. Is used by WebDAV engine to resolve path into items.
''' Implements abstract methods from <see cref="DavContextBaseAsync"/> ,
''' contains useful methods for working with transactions, connections, reading
''' varios items from database.
''' </summary>
Public Class DavContext
    Inherits DavContextHttpListenerBaseAsync
    Implements IDisposable

    ''' <summary>
    ''' Database connection string.
    ''' </summary>
    Private Shared ReadOnly connectionString As String = ConfigurationManager.ConnectionStrings("WebDAV").ConnectionString

    ''' <summary>
    ''' Id of root folder.
    ''' </summary>
    Private ReadOnly rootId As Guid = New Guid("00000000-0000-0000-0000-000000000001")

    ''' <summary>
    ''' Currently authenticated user.
    ''' </summary>
    Private ReadOnly currentUser As IPrincipal

    ''' <summary>
    ''' Singleton instance of <see cref="WebSocketsService"/> .
    ''' </summary>
    Public Property socketService As WebSocketsService
        Get
            Return WebSocketsService.Service
        End Get

        Private Set(ByVal value As WebSocketsService)
        End Set
    End Property

    ''' <summary>
    ''' Cached connection for the request.
    ''' </summary>
    Private connection As SqlConnection

    ''' <summary>
    ''' Transaction for the request.
    ''' </summary>
    Private transaction As SqlTransaction

    ''' <summary>
    ''' Initializes DavContext class.
    ''' </summary>
    Shared Sub New()
        Dim exePath = Path.GetDirectoryName(New Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath)
        Dim dataPath = Path.Combine(Directory.GetParent(exePath).FullName, "App_Data")
        connectionString = connectionString.Replace("|DataDirectory|", dataPath)
    End Sub

    ''' <summary>
    ''' Initializes a new instance of the DavContext class.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="HttpListenerContext"/> .</param>
    ''' <param name="prefixes">Collection of http listener prefixes.</param>
    ''' <param name="user">Current use principal. null if anonymous.</param>
    Public Sub New(context As HttpListenerContext, prefixes As HttpListenerPrefixCollection, user As IPrincipal, logger As ILogger)
        MyBase.New(context, prefixes)
        Me.currentUser = user
    End Sub

    ''' <summary>
    ''' Gets currently logged in user. <c>null</c> if request is anonymous.
    ''' </summary>
    Public ReadOnly Property User As IPrincipal
        Get
            Return currentUser
        End Get
    End Property

    Public Overrides Async Function GetHierarchyItemAsync(path As String) As Task(Of IHierarchyItemAsync)
        path = path.Trim({" "c, "/"c})
        Dim ind As Integer = path.IndexOf("?"c)
        If ind > -1 Then
            path = path.Remove(ind)
        End If

        If path = "" Then
            Return Await getRootFolderAsync()
        End If

        Return Await getItemByPathAsync(path)
    End Function

    Public Overrides Async Function BeforeResponseAsync() As Task
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

    Public Async Function ExecuteItemAsync(Of T As {Class, IHierarchyItemAsync})(parentPath As String, command As String, ParamArray prms As Object()) As Task(Of IList(Of T))
        Dim children As IList(Of T) = New List(Of T)()
        Using reader As SqlDataReader = Await prepareCommand(command, prms).ExecuteReaderAsync()
            While reader.Read()
                Dim itemId As Guid = CType(reader("ItemID"), Guid)
                Dim parentId As Guid = CType(reader("ParentItemID"), Guid)
                Dim itemType As ItemType = CType(reader.GetInt32(reader.GetOrdinal("ItemType")), ItemType)
                Dim name As String = reader.GetString(reader.GetOrdinal("Name"))
                Dim created As DateTime = reader.GetDateTime(reader.GetOrdinal("Created"))
                Dim modified As DateTime = reader.GetDateTime(reader.GetOrdinal("Modified"))
                Select Case itemType
                    Case ItemType.File
                        children.Add(TryCast(New DavFile(Me,
                                                        itemId,
                                                        parentId,
                                                        name,
                                                        parentPath & EncodeUtil.EncodeUrlPart(name),
                                                        created,
                                                        modified), T))
                    Case ItemType.Folder
                        children.Add(TryCast(New DavFolder(Me,
                                                          itemId,
                                                          parentId,
                                                          name,
                                                          (parentPath & EncodeUtil.EncodeUrlPart(name) & "/").TrimStart("/"c),
                                                          created,
                                                          modified), T))
                End Select
            End While
        End Using

        Return children
    End Function

    Public Async Function ExecutePropertyValueAsync(command As String, ParamArray prms As Object()) As Task(Of IList(Of PropertyValue))
        Dim l As List(Of PropertyValue) = New List(Of PropertyValue)()
        Using reader As SqlDataReader = Await prepareCommand(command, prms).ExecuteReaderAsync()
            While reader.Read()
                Dim name As String = reader.GetString(reader.GetOrdinal("Name"))
                Dim ns As String = reader.GetString(reader.GetOrdinal("Namespace"))
                Dim value As String = reader.GetString(reader.GetOrdinal("PropVal"))
                l.Add(New PropertyValue(New PropertyName(name, ns), value))
            End While
        End Using

        Return l
    End Function

    Public Function ExecuteScalar(Of T)(command As String, ParamArray prms As Object()) As T
        Dim o As Object = prepareCommand(command, prms).ExecuteScalar()
        Return If(TypeOf o Is DBNull, Nothing, CType(o, T))
    End Function

    Public Async Function ExecuteScalarAsync(Of T)(command As String, ParamArray prms As Object()) As Task(Of T)
        Dim o As Object = Await prepareCommand(command, prms).ExecuteScalarAsync()
        Return If(TypeOf o Is DBNull, Nothing, CType(o, T))
    End Function

    ''' <summary>
    ''' Executes SQL command which returns no results.
    ''' </summary>
    ''' <param name="command">Command text.</param>
    ''' <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value.</param>
    Public Sub ExecuteNonQuery(command As String, ParamArray prms As Object())
        prepareCommand(command, prms).ExecuteNonQuery()
    End Sub

    Public Async Function ExecuteNonQueryAsync(command As String, ParamArray prms As Object()) As Task
        Await prepareCommand(command, prms).ExecuteNonQueryAsync()
    End Function

    Public Async Function ExecuteNonQueryAsync(command As String, ParamArray prms As SqlParameter()) As Task
        Dim cmd As SqlCommand = createNewCommand()
        cmd.CommandText = command
        cmd.Parameters.AddRange(prms)
        Await cmd.ExecuteNonQueryAsync()
    End Function

    Public Async Function ExecuteReaderAsync(commandBehavior As CommandBehavior, command As String, ParamArray prms As Object()) As Task(Of SqlDataReader)
        Return Await prepareCommand(command, prms).ExecuteReaderAsync(commandBehavior)
    End Function

    Public Function ExecuteLockInfo(command As String, ParamArray prms As Object()) As List(Of LockInfo)
        Dim l As List(Of LockInfo) = New List(Of LockInfo)()
        Using reader As SqlDataReader = prepareCommand(command, prms).ExecuteReader()
            While reader.Read()
                Dim li As LockInfo = New LockInfo()
                li.Token = reader.GetString(reader.GetOrdinal("Token"))
                li.Level = If(reader.GetBoolean(reader.GetOrdinal("Shared")), LockLevel.Shared, LockLevel.Exclusive)
                li.IsDeep = reader.GetBoolean(reader.GetOrdinal("Deep"))
                Dim expires As DateTime = reader.GetDateTime(reader.GetOrdinal("Expires"))
                If expires <= DateTime.UtcNow Then
                    li.TimeOut = TimeSpan.Zero
                Else
                    li.TimeOut = expires - DateTime.UtcNow
                End If

                li.Owner = reader.GetString(reader.GetOrdinal("Owner"))
                l.Add(li)
            End While
        End Using

        Return l
    End Function

    Private Async Function getItemByPathAsync(path As String) As Task(Of IHierarchyItemAsync)
        Dim id As Guid = rootId
        Dim names As String() = path.Split("/"c)
        Dim last As Integer = names.Length - 1
        While last > 0 AndAlso names(last) = String.Empty
            last -= 1
        End While

        For i As Integer = 0 To last - 1
            If Not String.IsNullOrEmpty(names(i)) Then
                Dim result As Object = Await ExecuteScalarAsync(Of Object)("SELECT 
                             ItemId
                          FROM Item
                          WHERE Name = @Name AND ParentItemId = @Parent",
                                                                          "@Name", EncodeUtil.DecodeUrlPart(names(i)),
                                                                          "@Parent", id)
                If result IsNot Nothing Then
                    id = CType(result, Guid)
                Else
                    Return Nothing
                End If
            End If
        Next

        Dim command As String = "SELECT
                       ItemId
                     , ParentItemId
                     , ItemType
                     , Name
                     , Created
                     , Modified
                           
                     FROM Item
                  WHERE Name = @Name AND ParentItemId = @Parent"
        Dim davHierarchyItems As IList(Of DavHierarchyItem) = Await ExecuteItemAsync(Of DavHierarchyItem)(String.Join("/", names, 0, last) & "/",
                                                                                                         command,
                                                                                                         "@Name", EncodeUtil.DecodeUrlPart(names(last)),
                                                                                                         "@Parent", id)
        Return davHierarchyItems.FirstOrDefault()
    End Function

    Public Async Function getRootFolderAsync() As Task(Of IHierarchyItemAsync)
        Dim command As String = "SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , '' as Name
                    , Created
                    , Modified
                          
                    FROM Item
                 WHERE ItemId = @ItemId"
        Dim hierarchyItems As IList(Of IHierarchyItemAsync) = Await ExecuteItemAsync(Of IHierarchyItemAsync)("",
                                                                                                            command,
                                                                                                            "@ItemId", rootId)
        Return hierarchyItems.FirstOrDefault()
    End Function

    Private Function createNewCommand() As SqlCommand
        If Me.connection Is Nothing Then
            Me.connection = New SqlConnection(connectionString)
            Me.connection.Open()
            Me.transaction = Me.connection.BeginTransaction(IsolationLevel.ReadUncommitted)
        End If

        Dim newCmd As SqlCommand = connection.CreateCommand()
        newCmd.Transaction = transaction
        Return newCmd
    End Function

    Private Function prepareCommand(command As String, ParamArray prms As Object()) As SqlCommand
        If prms.Length Mod 2 <> 0 Then
            Throw New ArgumentException("Incorrect number of parameters")
        End If

        Dim cmd As SqlCommand = createNewCommand()
        cmd.CommandText = command
        For i As Integer = 0 To prms.Length - 1 Step 2
            If Not(TypeOf prms(i) Is String) Then
                Throw New ArgumentException(prms(i) & "is invalid parameter name")
            End If

            cmd.Parameters.AddWithValue(CStr(prms(i)), prms(i + 1))
        Next

        Return cmd
    End Function
End Class
