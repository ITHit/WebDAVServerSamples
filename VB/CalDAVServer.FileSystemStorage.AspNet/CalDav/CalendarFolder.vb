Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CalDav
Imports ITHit.WebDAV.Server.Acl
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal
Imports CalDAVServer.FileSystemStorage.AspNet.Acl

Namespace CalDav

    ''' <summary>
    ''' Represents CalDAV calendar (calendar folder).
    ''' Instances of this class correspond to the following path: [DAVLocation]/calendars/[user_name]/[calendar_name]/
    ''' </summary>
    ''' <remarks>Mozilla Thunderbird Lightning requires ICurrentUserPrincipal on calendar folder, it does not support discovery.</remarks>
    ''' <example>
    ''' [DAVLocation]
    '''  |-- ...
    '''  |-- calendars
    '''      |-- ...
    '''      |-- [User2]
    '''           |-- [Calendar 1]  -- this class
    '''           |-- ...
    '''           |-- [Calendar X]  -- this class
    ''' </example>
    ''' <remarks>
    ''' IAclHierarchyItem is required by OS X Calendar.
    ''' </remarks>
    Public Class CalendarFolder
        Inherits DavFolder
        Implements ICalendarFolder, IAppleCalendar, ICurrentUserPrincipal, IAclHierarchyItem

        ''' <summary>
        ''' Returns calendar folder that corresponds to path.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="path">Encoded path relative to WebDAV root.</param>
        ''' <returns>CalendarFolder instance or null if not found.</returns>
        Public Shared Function GetCalendarFolder(context As DavContext, path As String) As CalendarFolder
            Dim pattern As String = String.Format("^/?{0}/(?<user_name>[^/]+)/(?<calendar_name>[^/]+)/?",
                                                 CalendarsRootFolder.CalendarsRootFolderPath.Trim(New Char() {"/"c}).Replace("/", "/?"))
            If Not Regex.IsMatch(path, pattern) Then Return Nothing
            Dim folderPath As String = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar)
            Dim folder As DirectoryInfo = New DirectoryInfo(folderPath)
            ' to block vulnerability when "%20" folder can be injected into path and folder.Exists returns 'true'
            If Not folder.Exists OrElse [String].Compare(folder.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), folderPath, StringComparison.OrdinalIgnoreCase) <> 0 Then Return Nothing
            Return New CalendarFolder(folder, context, path)
        End Function

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CalendarFolder"/>  class.
        ''' </summary>
        ''' <param name="directoryInfo">Instance of <see cref="DirectoryInfo"/>  class with information about the folder in file system.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
        ''' <param name="path">Relative to WebDAV root folder path.</param>
        Private Sub New(directoryInfo As DirectoryInfo, context As DavContext, path As String)
            MyBase.New(directoryInfo, context, path)
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
        Public Async Function MultiGetAsync(pathList As IEnumerable(Of String), propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICalendarFile)) Implements ICalendarReport.MultiGetAsync
            ' Here you can load all items from pathList in one request to your storage, instead of 
            ' getting items one-by-one using GetHierarchyItem call.
            Dim calendarFileList As IList(Of ICalendarFile) = New List(Of ICalendarFile)()
            For Each path As String In pathList
                Dim calendarFile As ICalendarFile = TryCast(Await context.GetHierarchyItemAsync(path), ICalendarFile)
                calendarFileList.Add(calendarFile)
            Next

            Return calendarFileList
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
        Public Async Function QueryAsync(rawQuery As String, propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICalendarFile)) Implements ICalendarReport.QueryAsync
            ' For the sake of simplicity we just call GetChildren returning all items. 
            ' Typically you will return only items that match the query.
            Return(Await GetChildrenAsync(propNames.ToList(), Nothing, Nothing, Nothing)).Page.Cast(Of ICalendarFile)()
        End Function

        ''' <summary>
        ''' Specifies the calendar component types (e.g., VEVENT, VTODO, etc.) 
        ''' that calendar object resources can contain in the calendar collection.
        ''' </summary>
        Public ReadOnly Property SupportedComponentTypes As IEnumerable(Of CalendarComponentType) Implements ICalendarFolder.SupportedComponentTypes
            Get
                Return {CalendarComponentType.VEVENT,
                       CalendarComponentType.VTODO}
            End Get
        End Property

        ''' <summary>
        ''' Provides a human-readable description of the calendar collection.
        ''' </summary>
        Public ReadOnly Property CalendarDescription As String Implements ICalendarFolder.CalendarDescription
            Get
                Return String.Format("Some {0} description.", Name)
            End Get
        End Property

        ''' <summary>
        ''' Gets a numeric value indicating the maximum size of a
        ''' resource in bytes that the server is willing to accept when a
        ''' calendar object resource is stored in a calendar collection.
        ''' More details at http://tools.ietf.org/html/rfc4791#section-5.2.5
        ''' </summary>
        Public ReadOnly Property MaxResourceSize As ULong Implements ICalendarFolder.MaxResourceSize
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
        Public ReadOnly Property MaxInstances As ULong Implements ICalendarFolder.MaxInstances
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
        Public ReadOnly Property MaxAttendeesPerInstance As ULong Implements ICalendarFolder.MaxAttendeesPerInstance
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
        Public ReadOnly Property UtcMinDateTime As DateTime Implements ICalendarFolder.UtcMinDateTime
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
        Public ReadOnly Property UtcMaxDateTime As DateTime Implements ICalendarFolder.UtcMaxDateTime
            Get
                Return DateTime.MaxValue.ToUniversalTime()
            End Get
        End Property

        ''' <summary>
        ''' Indicates which sharing or publishing capabilities are supported 
        ''' by this calendar collection.
        ''' </summary>
        Public ReadOnly Property AllowedSharingModes As IEnumerable(Of AppleAllowedSharingMode) Implements IAppleCalendar.AllowedSharingModes
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
        Public Async Function UpdateSharingAsync(sharesToAddAndRemove As IList(Of AppleShare)) As Task Implements IAppleCalendar.UpdateSharingAsync
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
        Public Async Function GetInviteAsync() As Task(Of IEnumerable(Of SharingInvite)) Implements IAppleCalendar.GetInviteAsync
            Return Nothing
        End Function

        ''' <summary>
        ''' Indicates that the calendar is shared and if it is shared by the current user who is the owner of the calendar.
        ''' </summary>
        Public Async Function GetSharedByAsync() As Task(Of CalendarSharedBy) Implements IAppleCalendar.GetSharedByAsync
            Return CalendarSharedBy.NotShared
        End Function

        Public Function SetOwnerAsync(value As IPrincipal) As Task Implements IAclHierarchyItem.SetOwnerAsync
            Throw New NotImplementedException()
        End Function

        ''' <summary>
        ''' Retrieves a particular principal as being the "owner" of the item. 
        ''' </summary>
        ''' <remarks>Required by OS X.</remarks>
        ''' <returns>
        ''' Item that represents owner of this item and implements <see cref="IPrincipal"/> .
        ''' </returns>
        Public Async Function GetOwnerAsync() As Task(Of IPrincipal) Implements IAclHierarchyItem.GetOwnerAsync
            Return context.FileOperation(Me,
                                        Function()
                Dim acl As FileSecurity = File.GetAccessControl(fileSystemInfo.FullName)
                Return AclFactory.GetPrincipalFromSid(acl.GetOwner(GetType(SecurityIdentifier)).Value, context)
            End Function,
                                        Privilege.Read)
        End Function

        Public Function SetGroupAsync(value As IPrincipal) As Task Implements IAclHierarchyItem.SetGroupAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetGroupAsync() As Task(Of IPrincipal) Implements IAclHierarchyItem.GetGroupAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetSupportedPrivilegeSetAsync() As Task(Of IEnumerable(Of SupportedPrivilege)) Implements IAclHierarchyItem.GetSupportedPrivilegeSetAsync
            Throw New NotImplementedException()
        End Function

        ''' <summary>
        ''' Retrieves the exact set of privileges (as computed by
        ''' the server) granted to the currently authenticated HTTP user. Aggregate privileges and their contained
        ''' privileges are listed.
        ''' </summary>
        ''' <returns>
        ''' List of current user privileges.
        ''' </returns>        
        Public Async Function GetCurrentUserPrivilegeSetAsync() As Task(Of IEnumerable(Of Privilege)) Implements IAclHierarchyItem.GetCurrentUserPrivilegeSetAsync
            Return {Privilege.Write, Privilege.Read}
        End Function

        Public Function GetAclAsync(propertyNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of ReadAce)) Implements IAclHierarchyItem.GetAclAsync
            Throw New NotImplementedException()
        End Function

        Public Function SetAclAsync(aces As IList(Of WriteAce)) As Task Implements IAclHierarchyItem.SetAclAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetAclRestrictionsAsync() As Task(Of AclRestriction) Implements IAclHierarchyItem.GetAclRestrictionsAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetInheritedAclSetAsync() As Task(Of IEnumerable(Of IHierarchyItem)) Implements IAclHierarchyItem.GetInheritedAclSetAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetPrincipalCollectionSetAsync() As Task(Of IEnumerable(Of IPrincipalFolder)) Implements IAclHierarchyItem.GetPrincipalCollectionSetAsync
            Throw New NotImplementedException()
        End Function

        Public Function ResolveWellKnownPrincipalAsync(wellKnownPrincipal As WellKnownPrincipal) As Task(Of IPrincipal) Implements IAclHierarchyItem.ResolveWellKnownPrincipalAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetItemsByPropertyAsync(matchBy As MatchBy, props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IAclHierarchyItem)) Implements IAclHierarchyItem.GetItemsByPropertyAsync
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
