Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Data
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CalDav
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Paging
Imports ITHit.Server

Namespace CalDav

    ' Note:
    '  - Mozilla Thunderbird Lightning requires ICurrentUserPrincipalAsync on calendar folder, it does not support discovery.
    '  - Outlook CalDAV Synchronizer requires IAclHierarchyItemAsync on calendar folder.
    ''' <summary>
    ''' Represents a CalDAV calendar (calendar folder).
    ''' Instances of this class correspond to the following path: [DAVLocation]/calendars/[CalendarFolderId]
    ''' </summary>
    Public Class CalendarFolder
        Inherits DavHierarchyItem
        Implements ICalendarFolderAsync, IAppleCalendarAsync, ICurrentUserPrincipalAsync, IAclHierarchyItemAsync

        ''' <summary>
        ''' Loads calendar folder by ID. Returns null if calendar folder was not found.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="calendarFolderId">ID of the calendar folder to load.</param>
        ''' <returns><see cref="ICalendarFolderAsync"/>  instance.</returns>
        Public Shared Async Function LoadByIdAsync(context As DavContext, calendarFolderId As Guid) As Task(Of ICalendarFolderAsync)
            ' Load only calendar that the use has access to. 
            ' Also load complete ACL for this calendar.
            Dim sql As String = "SELECT * FROM [cal_CalendarFolder] 
                  WHERE [CalendarFolderId] = @CalendarFolderId
                  AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [cal_Access]
                  WHERE [CalendarFolderId] = @CalendarFolderId
                  AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)"
            Return(Await LoadAsync(context, sql,
                                  "@UserId", context.UserId,
                                  "@CalendarFolderId", calendarFolderId
                                  )).FirstOrDefault()
        End Function

        ''' <summary>
        ''' Loads all calendars.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <returns>List of <see cref="ICalendarFolderAsync"/>  items.</returns>
        Public Shared Async Function LoadAllAsync(context As DavContext) As Task(Of IEnumerable(Of ICalendarFolderAsync))
            ' Load only calendars that the use has access to. 
            ' Also load complete ACL for each calendar, but only if user has access to that calendar.
            Dim sql As String = "SELECT * FROM [cal_CalendarFolder] 
                  WHERE [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [cal_Access] 
                  WHERE [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId]=@UserId)"
            Return Await LoadAsync(context, sql, "@UserId", context.UserId)
        End Function

        ''' <summary>
        ''' Loads calendars by SQL.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="sql">SQL that queries [cal_CalendarFolder] table.</param>
        ''' <param name="prms">List of SQL parameters.</param>
        ''' <returns>List of <see cref="ICalendarFolderAsync"/>  items.</returns>
        Private Shared Async Function LoadAsync(context As DavContext, sql As String, ParamArray prms As Object()) As Task(Of IEnumerable(Of ICalendarFolderAsync))
            Dim calendarFolders As IList(Of ICalendarFolderAsync) = New List(Of ICalendarFolderAsync)()
            Using reader As SqlDataReader = Await context.ExecuteReaderAsync(sql, prms)
                Dim calendars As DataTable = New DataTable()
                calendars.Load(reader)
                Dim access As DataTable = New DataTable()
                access.Load(reader)
                For Each rowCalendarFolder As DataRow In calendars.Rows
                    Dim calendarFolderId As Guid = rowCalendarFolder.Field(Of Guid)("CalendarFolderId")
                    Dim filter As String = String.Format("CalendarFolderId = '{0}'", calendarFolderId)
                    Dim rowsAccess As DataRow() = access.Select(filter)
                    calendarFolders.Add(New CalendarFolder(context, calendarFolderId, rowCalendarFolder, rowsAccess))
                Next
            End Using

            Return calendarFolders
        End Function

        ''' <summary>
        ''' Creates a new calendar folder.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param> 
        ''' <param name="name">Calendar folder name.</param>
        Public Shared Async Function CreateCalendarFolderAsync(context As DavContext, name As String, description As String) As Task
            ' 1. Create calendar.
            ' 2. Grant owner privileges to the user on the created calendar.
            Dim sql As String = "INSERT INTO [cal_CalendarFolder] (
                          [CalendarFolderId]
                        , [Name]
                        , [Description]
                    ) VALUES (
                          @CalendarFolderId
                        , @Name
                        , @Description
                    )
                    ; INSERT INTO [cal_Access] (
                          [CalendarFolderId]
                        , [UserId]
                        , [Owner]
                        , [Read]
                        , [Write]
                    ) VALUES (
                          @CalendarFolderId
                        , @UserId
                        , @Owner
                        , @Read
                        , @Write
                    )"
            Dim calendarFolderId As Guid = Guid.NewGuid()
            Await context.ExecuteNonQueryAsync(sql,
                                              "@CalendarFolderId", calendarFolderId,
                                              "@Name", name,
                                              "@Description", description,
                                              "@UserId", context.UserId,
                                              "@Owner", True, "@Read", True, "@Write", True)
        End Function

        ''' <summary>
        ''' This calendar folder ID.
        ''' </summary>
        Private ReadOnly calendarFolderId As Guid

        ''' <summary>
        ''' Contains data from [cal_CalendarFolder] table.
        ''' </summary>
        Private ReadOnly rowCalendarFolder As DataRow

        ''' <summary>
        ''' Contains data from [card_Access] table.
        ''' </summary>
        Private ReadOnly rowsAccess As DataRow()

        ''' <summary>
        ''' Gets display name of the calendar.
        ''' </summary>
        ''' <remarks>CalDAV clients typically never request this property.</remarks>
        Public Overrides ReadOnly Property Name As String Implements IHierarchyItemBaseAsync.Name
            Get
                Return If(rowCalendarFolder IsNot Nothing, rowCalendarFolder.Field(Of String)("Name"), Nothing)
            End Get
        End Property

        ''' <summary>
        ''' Gets item path.
        ''' </summary>
        Public Overrides ReadOnly Property Path As String Implements IHierarchyItemBaseAsync.Path
            Get
                Return String.Format("{0}{1}/", CalendarsRootFolder.CalendarsRootFolderPath, calendarFolderId)
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CalendarFolder"/>  class from database source.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="calendarFolderId">Calendar folder ID.</param>
        ''' <param name="calendar">Contains data from [cal_CalendarFolder] table.</param>
        ''' <param name="rowsAccess">Contains data from [cal_Access] table for this calendar.</param>
        Private Sub New(context As DavContext, calendarFolderId As Guid, calendar As DataRow, rowsAccess As DataRow())
            MyBase.New(context)
            Me.calendarFolderId = calendarFolderId
            Me.rowCalendarFolder = calendar
            Me.rowsAccess = rowsAccess
        End Sub

        ''' <summary>
        ''' Returns a list of calendar files that correspont to the specified list of item paths.
        ''' </summary>
        ''' <remarks>
        ''' <para>
        ''' This method is called by the Engine during <b>calendar-multiget</b> call.
        ''' </para>
        ''' <para>
        ''' For each item from the <b>pathList</b> parameter return an item that corresponds to path or <b>null</b> if the item is not found.
        ''' </para>
        ''' </remarks>
        ''' <param name="pathList">Calendar files path list.</param>
        ''' <param name="propNames">
        ''' Properties requested by the client. You can use this as a hint about what properties will be called by 
        ''' the Engine for each item that are returned from this method.
        ''' </param>
        ''' <returns>List of calendar files. Returns <b>null</b> for any item that is not found.</returns>
        Public Async Function MultiGetAsync(pathList As IEnumerable(Of String), propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICalendarFileAsync)) Implements ICalendarReportAsync.MultiGetAsync
            ' Get list of UIDs from path list.
            Dim uids As IEnumerable(Of String) = pathList.Select(Function(a) System.IO.Path.GetFileNameWithoutExtension(a))
            Return Await CalendarFile.LoadByUidsAsync(Context, uids, PropsToLoad.All)
        End Function

        ''' <summary>
        ''' Returns a list of calendar files that match specified filter. 
        ''' </summary>
        ''' <remarks>
        ''' <para>
        ''' This method is called by the Engine during <b>calendar-query</b> call.
        ''' </para>
        ''' </remarks>
        ''' <param name="rawQuery">
        ''' Raw query sent by the client.
        ''' </param>
        ''' <param name="propNames">
        ''' Properties requested by the client. You can use this as a hint about what properties will be called by 
        ''' the Engine for each item that are returned from this method.
        ''' </param>
        ''' <returns>List of calendar files. Returns <b>null</b> for any item that is not found.</returns>
        Public Async Function QueryAsync(rawQuery As String, propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICalendarFileAsync)) Implements ICalendarReportAsync.QueryAsync
            ' For the sake of simplicity we just call GetChildren returning all items. 
            ' Typically you will return only items that match the query.
            Return(Await GetChildrenAsync(propNames.ToList(), Nothing, Nothing, Nothing)).Page.Cast(Of ICalendarFileAsync)()
        End Function

        ''' <summary>
        ''' Specifies the calendar component types (e.g., VEVENT, VTODO, etc.) 
        ''' that calendar object resources can contain in the calendar collection.
        ''' </summary>
        Public ReadOnly Property SupportedComponentTypes As IEnumerable(Of CalendarComponentType) Implements ICalendarFolderAsync.SupportedComponentTypes
            Get
                Return {CalendarComponentType.VEVENT,
                       CalendarComponentType.VTODO}
            End Get
        End Property

        ''' <summary>
        ''' Provides a human-readable description of the calendar collection.
        ''' </summary>
        Public ReadOnly Property CalendarDescription As String Implements ICalendarFolderAsync.CalendarDescription
            Get
                Return rowCalendarFolder.Field(Of String)("Description")
            End Get
        End Property

        ''' <summary>
        ''' Gets a numeric value indicating the maximum size of a
        ''' resource in bytes that the server is willing to accept when a
        ''' calendar object resource is stored in a calendar collection.
        ''' More details at http://tools.ietf.org/html/rfc4791#section-5.2.5
        ''' </summary>
        Public ReadOnly Property MaxResourceSize As ULong Implements ICalendarFolderAsync.MaxResourceSize
            Get
                Return ULong.MaxValue
            End Get
        End Property

        ''' <summary>
        ''' Gets a numeric value indicating the maximum number of
        ''' recurrence instances that a calendar object resource stored in a
        ''' calendar collection can generate.
        ''' More details at http://tools.ietf.org/html/rfc4791#section-5.2.8
        ''' </summary>
        Public ReadOnly Property MaxInstances As ULong Implements ICalendarFolderAsync.MaxInstances
            Get
                Return ULong.MaxValue
            End Get
        End Property

        ''' <summary>
        ''' Provides a numeric value indicating the maximum number of
        ''' ATTENDEE properties in any instance of a calendar object resource
        ''' stored in a calendar collection.
        ''' More details at http://tools.ietf.org/html/rfc4791#section-5.2.9
        ''' </summary>
        Public ReadOnly Property MaxAttendeesPerInstance As ULong Implements ICalendarFolderAsync.MaxAttendeesPerInstance
            Get
                Return ULong.MaxValue
            End Get
        End Property

        ''' <summary>
        ''' Gets a DATE-TIME value indicating the earliest date and
        ''' time (in UTC) that the server is willing to accept for any DATE or
        ''' DATE-TIME value in a calendar object resource stored in a calendar
        ''' collection.
        ''' More details at http://tools.ietf.org/html/rfc4791#section-5.2.6
        ''' </summary>
        Public ReadOnly Property UtcMinDateTime As DateTime Implements ICalendarFolderAsync.UtcMinDateTime
            Get
                Return DateTime.MinValue.ToUniversalTime()
            End Get
        End Property

        ''' <summary>
        ''' Gets a DATE-TIME value indicating the latest date and
        ''' time (in UTC) that the server is willing to accept for any DATE or
        ''' DATE-TIME value in a calendar object resource stored in a calendar
        ''' collection.
        ''' More details at http://tools.ietf.org/html/rfc4791#section-5.2.7
        ''' </summary>
        Public ReadOnly Property UtcMaxDateTime As DateTime Implements ICalendarFolderAsync.UtcMaxDateTime
            Get
                Return DateTime.MaxValue.ToUniversalTime()
            End Get
        End Property

        ''' <summary>
        ''' Retrieves children of this folder.
        ''' </summary>
        ''' <param name="propNames">List of properties to retrieve with the children. They will be queried by the engine later.</param>
        ''' <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        ''' <param name="nResults">The number of items to return.</param>
        ''' <param name="orderProps">List of order properties requested by the client.</param>
        ''' <returns>Children of the folder.</returns>
        Public Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollectionAsync.GetChildrenAsync
            ' Here we enumerate all events and to-dos contained in this calendar.
            ' You can filter children items in this implementation and 
            ' return only items that you want to be available for this 
            ' particular user.
            ' Typically only getcontenttype and getetag properties are requested in GetChildren call by CalDAV/CardDAV clients.
            ' The iCalendar/vCard (calendar-data/address-data) is typically requested not in GetChildren, but in a separate multiget 
            ' report, in MultiGetAsync, that follow this request.
            ' Bynari submits PROPFIND without props - Engine will request getcontentlength
            Dim children As IList(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
            Return New PageResults((Await CalendarFile.LoadByCalendarFolderIdAsync(Context, calendarFolderId, PropsToLoad.Minimum)), Nothing)
        End Function

        ''' <summary>
        ''' Creates a file that contains event or to-do item in this calendar.
        ''' </summary>
        ''' <param name="name">Name of the file. Same as event/to-do UID but ending with '.ics'.</param>
        ''' <returns>The newly created file.</returns>
        ''' <remarks></remarks>
        Public Async Function CreateFileAsync(name As String) As Task(Of IFileAsync) Implements IFolderAsync.CreateFileAsync
            ' The actual event or to-do object is created in datatbase in CardFile.Write call.
            Return CalendarFile.CreateCalendarFile(Context, calendarFolderId)
        End Function

        ''' <summary>
        ''' Creating new folders is not allowed in calendar folders.
        ''' </summary>
        ''' <param name="name">Name of the folder.</param>
        Public Async Function CreateFolderAsync(name As String) As Task Implements IFolderAsync.CreateFolderAsync
            Throw New DavException("Not allowed.", DavStatus.NOT_ALLOWED)
        End Function

        ''' <summary>
        ''' Move this folder to folder <paramref name="destFolder"/> .
        ''' </summary>
        ''' <param name="destFolder">Destination folder.</param>
        ''' <param name="destName">Name for this folder at destination.</param>
        ''' <param name="multistatus">Instance of <see cref="MultistatusException"/> 
        ''' to fill with errors ocurred while moving child items.</param>
        ''' <returns></returns>
        Public Overrides Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
            ' Here we support only calendars renaming. Check that user has permissions to write.
            Dim sql As String = "UPDATE [cal_CalendarFolder] SET Name=@Name
                WHERE [CalendarFolderId]=@CalendarFolderId
                AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId] = @UserId AND [Write] = 1)"
            If Await Context.ExecuteNonQueryAsync(sql,
                                                 "@UserId", Context.UserId,
                                                 "@CalendarFolderId", calendarFolderId,
                                                 "@Name", destName) < 1 Then
                Throw New DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN)
            End If
        End Function

        ''' <summary>
        ''' Deletes this calendar.
        ''' </summary>
        ''' <param name="multistatus"><see cref="MultistatusException"/>  to populate with child files and folders failed to delete.</param>
        Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
            ' Delete calendar and all events / to-dos associated with it. Check that user has permissions to delete.
            Dim sql As String = "DELETE FROM [cal_CalendarFolder] 
                WHERE [CalendarFolderId]=@CalendarFolderId
                AND [CalendarFolderId] IN (SELECT [CalendarFolderId] FROM [cal_Access] WHERE [UserId] = @UserId AND [Owner] = 1)"
            If Await Context.ExecuteNonQueryAsync(sql,
                                                 "@UserId", Context.UserId,
                                                 "@CalendarFolderId", calendarFolderId) < 1 Then
                Throw New DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN)
            End If
        End Function

        ''' <summary>
        ''' Gets values of all properties or selected properties for this item.
        ''' </summary>
        ''' <param name="names">Property names which values are
        ''' requested by WebDAV client. If a property does not exist for this hierarchy item
        ''' then the property value shall not be returned.
        ''' </param>
        ''' <param name="allprop">If it is true, besides properties listed in props you need to return
        ''' all properties you think may be useful to client.
        ''' </param>
        ''' <returns></returns>
        Public Overrides Async Function GetPropertiesAsync(names As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync
            Dim propVals As IList(Of PropertyValue) = Await GetPropertyValuesAsync("SELECT [Name], [Namespace], [PropVal] FROM [cal_CalendarFolderProperty] WHERE [CalendarFolderId] = @CalendarFolderId",
                                                                                  "@CalendarFolderId", calendarFolderId)
            If allprop Then
                Return propVals
            Else
                Dim requestedPropVals As IList(Of PropertyValue) = New List(Of PropertyValue)()
                For Each p As PropertyValue In propVals
                    If names.Contains(p.QualifiedName) Then
                        requestedPropVals.Add(p)
                    End If
                Next

                Return requestedPropVals
            End If
        End Function

        ''' <summary>
        ''' Adds, modifies and removes properties for this item.
        ''' </summary>
        ''' <param name="setProps">List of properties to be set.</param>
        ''' <param name="delProps">List of property names to be removed. Properties that don't exist shall be skipped.</param>
        ''' <param name="multistatus">Information about errors.</param>
        Public Overrides Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue),
                                                             delProps As IList(Of PropertyName),
                                                             multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync
            For Each p As PropertyValue In setProps
                Await SetPropertyAsync(p)
            Next

            For Each p As PropertyName In delProps
                Await RemovePropertyAsync(p.Name, p.Namespace)
            Next
        End Function

        ''' <summary>
        ''' Reads <see cref="PropertyValue"/>  from database by executing SQL command.
        ''' </summary>
        ''' <param name="command">Command text.</param>
        ''' <param name="prms">SQL parameter pairs: SQL parameter name, SQL parameter value</param>
        ''' <returns>List of <see cref="PropertyValue"/> .</returns>
        Private Async Function GetPropertyValuesAsync(command As String, ParamArray prms As Object()) As Task(Of IList(Of PropertyValue))
            Dim l As List(Of PropertyValue) = New List(Of PropertyValue)()
            Using reader As SqlDataReader = Await Context.ExecuteReaderAsync(command, prms)
                While Await reader.ReadAsync()
                    Dim name As String = reader.GetString(reader.GetOrdinal("Name"))
                    Dim ns As String = reader.GetString(reader.GetOrdinal("Namespace"))
                    Dim value As String = reader.GetString(reader.GetOrdinal("PropVal"))
                    l.Add(New PropertyValue(New PropertyName(name, ns), value))
                End While
            End Using

            Return l
        End Function

        Private Async Function SetPropertyAsync(prop As PropertyValue) As Task
            Dim selectCommand As String = "SELECT Count(*) FROM [cal_CalendarFolderProperty]
                  WHERE [CalendarFolderId] = @CalendarFolderId AND [Name] = @Name AND [Namespace] = @Namespace"
            Dim count As Integer = Await Context.ExecuteScalarAsync(Of Integer)(selectCommand,
                                                                               "@CalendarFolderId", calendarFolderId,
                                                                               "@Name", prop.QualifiedName.Name,
                                                                               "@Namespace", prop.QualifiedName.Namespace)
            ' insert
            If count = 0 Then
                Dim insertCommand As String = "INSERT INTO [cal_CalendarFolderProperty] ([CalendarFolderId], [Name], [Namespace], [PropVal])
                                          VALUES(@CalendarFolderId, @Name, @Namespace, @PropVal)"
                Await Context.ExecuteNonQueryAsync(insertCommand,
                                                  "@PropVal", prop.Value,
                                                  "@CalendarFolderId", calendarFolderId,
                                                  "@Name", prop.QualifiedName.Name,
                                                  "@Namespace", prop.QualifiedName.Namespace)
            Else
                ' update
                Dim command As String = "UPDATE [cal_CalendarFolderProperty]
                      SET [PropVal] = @PropVal
                      WHERE [CalendarFolderId] = @CalendarFolderId AND [Name] = @Name AND [Namespace] = @Namespace"
                Await Context.ExecuteNonQueryAsync(command,
                                                  "@PropVal", prop.Value,
                                                  "@CalendarFolderId", calendarFolderId,
                                                  "@Name", prop.QualifiedName.Name,
                                                  "@Namespace", prop.QualifiedName.Namespace)
            End If
        End Function

        Private Async Function RemovePropertyAsync(name As String, ns As String) As Task
            Dim command As String = "DELETE FROM [cal_CalendarFolderProperty]
                              WHERE [CalendarFolderId] = @CalendarFolderId
                              AND [Name] = @Name
                              AND [Namespace] = @Namespace"
            Await Context.ExecuteNonQueryAsync(command,
                                              "@CalendarFolderId", calendarFolderId,
                                              "@Name", name,
                                              "@Namespace", ns)
        End Function

        ''' <summary>
        ''' Indicates which sharing or publishing capabilities are supported 
        ''' by this calendar collection.
        ''' </summary>
        Public ReadOnly Property AllowedSharingModes As IEnumerable(Of AppleAllowedSharingMode) Implements IAppleCalendarAsync.AllowedSharingModes
            Get
                Return {AppleAllowedSharingMode.CanBePublished,
                       AppleAllowedSharingMode.CanBeShared}
            End Get
        End Property

        ''' <summary>
        ''' This metod is called when user is granting or 
        ''' withdrowing acces to the calendar. 
        ''' </summary>
        ''' <remarks>
        ''' In this metod implementation you will grant 
        ''' or withdraw acces to the calendar as well as you will send sharing invitation.
        ''' </remarks>
        ''' <param name="sharesToAddAndRemove">Each item in this list describes the share to 
        ''' add or delete.</param>
        Public Async Function UpdateSharingAsync(sharesToAddAndRemove As IList(Of AppleShare)) As Task Implements IAppleCalendarAsync.UpdateSharingAsync
            ' Drop all shares first regardless of operation order. When resending 
            ' invitations Apple Calendar drops and adds shares for the user in one \
            ' request.
            For Each share As AppleShare In sharesToAddAndRemove
                If share.Operation = AppleSharingOperation.Withdraw Then
                    ' remove sharing here
                    ' share.Address
                    ' share.CommonName
                     End If
            Next

            ' Add new shares
            For Each share As AppleShare In sharesToAddAndRemove
                If share.Operation <> AppleSharingOperation.Withdraw Then
                    ' enable sharing and send invitation here
                    ' share.Address
                    ' share.CommonName
                     End If
            Next
        End Function

        ''' <summary>
        ''' Provides a list of users to whom the calendar has been shared.
        ''' </summary>
        ''' <remarks>
        ''' http://svn.calendarserver.org/repository/calendarserver/CalendarServer/trunk/doc/Extensions/caldav-sharing.txt
        ''' (Section 5.2.2)        
        Public Async Function GetInviteAsync() As Task(Of IEnumerable(Of SharingInvite)) Implements IAppleCalendarAsync.GetInviteAsync
            Dim invites As IList(Of SharingInvite) = New List(Of SharingInvite)()
            For Each rowAccess As DataRow In rowsAccess
                If rowAccess.Field(Of Boolean)("Owner") Then Continue For
                Dim userId As String = rowAccess.Field(Of String)("UserId")
                Dim user As System.Web.Security.MembershipUser = System.Web.Security.Membership.GetUser(userId)
                Dim ace As SharingInvite = New SharingInvite With {.Address = String.Format("email:{0}", user.Email),
                                                             .Access = If(rowAccess.Field(Of Boolean)("Write"), SharingInviteAccess.ReadWrite, SharingInviteAccess.Read),
                                                             .CommonName = user.UserName,
                                                             .Status = SharingInviteStatus.Accepted
                                                             }
            Next

            Return invites
        End Function

        ''' <summary>
        ''' Indicates that the calendar is shared and if it is shared by the current user who is the owner of the calendar.
        ''' </summary>
        Public Async Function GetSharedByAsync() As Task(Of CalendarSharedBy) Implements IAppleCalendarAsync.GetSharedByAsync
            If rowsAccess.Any(Function(x) Not x.Field(Of Boolean)("Owner")) Then
                Return CalendarSharedBy.NotShared
            End If

            Dim ownerId As String = rowsAccess.First(Function(x) x.Field(Of Boolean)("Owner")).Field(Of String)("UserId")
            If ownerId.Equals(Context.UserId, StringComparison.InvariantCultureIgnoreCase) Then
                Return CalendarSharedBy.SharedByOwner
            Else
                Return CalendarSharedBy.Shared
            End If
        End Function

        Public Function SetOwnerAsync(value As IPrincipalAsync) As Task Implements IAclHierarchyItemAsync.SetOwnerAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Retrieves a particular principal as being the "owner" of the item. 
        ''' </summary>
        ''' <remarks>Required by OS X.</remarks>
        ''' <returns>
        ''' Item that represents owner of this item and implements <see cref="IPrincipalAsync"/> .
        ''' </returns>
        Public Async Function GetOwnerAsync() As Task(Of IPrincipalAsync) Implements IAclHierarchyItemAsync.GetOwnerAsync
            Dim rowOwner As DataRow = rowsAccess.FirstOrDefault(Function(x) x.Field(Of Boolean)("Owner") = True)
            If rowOwner Is Nothing Then Return Nothing
            Return Await Acl.User.GetUserAsync(Context, rowOwner.Field(Of String)("UserId"))
        End Function

        ''' <summary>
        ''' Retrieves a particular principal as being the "group" of the item. This property is commonly
        ''' found on repositories that implement the Unix privileges model.
        ''' </summary>
        ''' <param name="value">Identifies whether to search by owner or group.</param>
        Public Function SetGroupAsync(value As IPrincipalAsync) As Task Implements IAclHierarchyItemAsync.SetGroupAsync
            Throw New DavException("Group cannot be set", DavStatus.FORBIDDEN)
        End Function

        ''' <summary>
        ''' Retrieves a particular principal as being the "group" of the item. This property is commonly
        ''' found on repositories that implement the Unix privileges model.
        ''' </summary>
        ''' <returns>
        ''' Group principal that implements <see cref="IPrincipalAsync"/> .
        ''' </returns>
        ''' <remarks>
        ''' Can return null if group is not assigned.
        ''' </remarks>
        Public Async Function GetGroupAsync() As Task(Of IPrincipalAsync) Implements IAclHierarchyItemAsync.GetGroupAsync
            Return Nothing
        End Function

        ''' <summary>
        ''' Retrieves list of all privileges (permissions) which can be set for the item.
        ''' </summary>
        ''' <returns>Enumerable with supported permissions.</returns>
        Public Async Function GetSupportedPrivilegeSetAsync() As Task(Of IEnumerable(Of SupportedPrivilege)) Implements IAclHierarchyItemAsync.GetSupportedPrivilegeSetAsync
            Return {New SupportedPrivilege With {.Privilege = Privilege.Read, .IsAbstract = False, .DescriptionLanguage = "en",
                                           .Description = "Allows or denies the user the ability to read content and properties of files/folders."},
                   New SupportedPrivilege With {.Privilege = Privilege.Write, .IsAbstract = False, .DescriptionLanguage = "en",
                                                                                                                                                                                                                                                                 .Description = "Allows or denies locking an item or modifying the content, properties, or membership of a collection."}}
        End Function

        ''' <summary>
        ''' Retrieves the exact set of privileges (as computed by
        ''' the server) granted to the currently authenticated HTTP user. Aggregate privileges and their contained
        ''' privileges are listed.
        ''' </summary>
        ''' <returns>
        ''' List of current user privileges.
        ''' </returns>
        Public Async Function GetCurrentUserPrivilegeSetAsync() As Task(Of IEnumerable(Of Privilege)) Implements IAclHierarchyItemAsync.GetCurrentUserPrivilegeSetAsync
            Dim rowAccess As DataRow = rowsAccess.FirstOrDefault(Function(x) x.Field(Of String)("UserId") = Context.UserId)
            If rowAccess Is Nothing Then Return Nothing
            Dim privileges As List(Of Privilege) = New List(Of Privilege)()
            If rowAccess.Field(Of Boolean)("Read") Then privileges.Add(Privilege.Read)
            If rowAccess.Field(Of Boolean)("Write") Then privileges.Add(Privilege.Write)
            Return privileges
        End Function

        ''' <summary>
        ''' Retrieves access control list for this file or folder.
        ''' </summary>
        ''' <param name="propertyNames">Properties which will be retrieved from users/groups specified in
        ''' access control list returned.</param>
        ''' <returns>Enumerable with access control entries.</returns>
        Public Async Function GetAclAsync(propertyNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of ReadAce)) Implements IAclHierarchyItemAsync.GetAclAsync
            Dim aceList As IList(Of ReadAce) = New List(Of ReadAce)()
            For Each rowAccess As DataRow In rowsAccess
                Dim ace As ReadAce = New ReadAce()
                ace.Principal = Await Acl.User.GetUserAsync(Context, rowAccess.Field(Of String)("UserId"))
                If rowAccess.Field(Of Boolean)("Read") Then ace.GrantPrivileges.Add(Privilege.Read)
                If rowAccess.Field(Of Boolean)("Write") Then ace.GrantPrivileges.Add(Privilege.Write)
                ace.IsProtected = rowAccess.Field(Of Boolean)("Owner")
                aceList.Add(ace)
            Next

            Return aceList
        End Function

        Public Function SetAclAsync(aces As IList(Of WriteAce)) As Task Implements IAclHierarchyItemAsync.SetAclAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Retrieves list of restrictions for access control entries.
        ''' We don't support WebDAV inverted permissions.
        ''' </summary>
        ''' <returns>ACL restrictions.</returns>
        Public Async Function GetAclRestrictionsAsync() As Task(Of AclRestriction) Implements IAclHierarchyItemAsync.GetAclRestrictionsAsync
            Return New AclRestriction With {.NoInvert = True, .GrantOnly = True}
        End Function

        ''' <summary>
        ''' Gets all folders, from which this file/folder has inherited access control entries.
        ''' </summary>
        ''' <returns>Enumerable with files/folders from which this file/folder has inherited
        ''' access control entries.</returns>
        Public Async Function GetInheritedAclSetAsync() As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IAclHierarchyItemAsync.GetInheritedAclSetAsync
            Return New IHierarchyItemAsync() {}
        End Function

        ''' <summary>
        ''' Gets collections which contain principals.
        ''' </summary>
        ''' <returns>Folders which contain users/groups.</returns>
        Public Async Function GetPrincipalCollectionSetAsync() As Task(Of IEnumerable(Of IPrincipalFolderAsync)) Implements IAclHierarchyItemAsync.GetPrincipalCollectionSetAsync
            Return New IPrincipalFolderAsync() {New Acl.UsersFolder(Context)}
        End Function

        ''' <summary>
        ''' Retrieves user or group which correspond to a well known principal
        ''' (defined in <see cref="WellKnownPrincipal"/> .)
        ''' </summary>
        ''' <param name="wellKnownPrincipal">Well known principal type.</param>
        ''' <returns>Instance of corresponding user/group or <c>null</c> if corresponding user/group
        ''' is not supported.</returns>
        Public Async Function ResolveWellKnownPrincipalAsync(wellKnownPrincipal As WellKnownPrincipal) As Task(Of IPrincipalAsync) Implements IAclHierarchyItemAsync.ResolveWellKnownPrincipalAsync
            Return Nothing
        End Function

        Public Function GetItemsByPropertyAsync(matchBy As MatchBy, props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IAclHierarchyItemAsync)) Implements IAclHierarchyItemAsync.GetItemsByPropertyAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function
    End Class
End Namespace
