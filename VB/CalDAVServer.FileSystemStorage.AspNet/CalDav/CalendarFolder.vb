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
Imports CalDAVServer.FileSystemStorage.AspNet.Acl

Namespace CalDav

    ''' <summary>
    ''' Represents CalDAV calendar (calendar folder).
    ''' Instances of this class correspond to the following path: [DAVLocation]/calendars/[user_name]/[calendar_name]/
    ''' </summary>
    ''' <remarks>Mozilla Thunderbird Lightning requires ICurrentUserPrincipalAsync on calendar folder, it does not support discovery.</remarks>
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
    ''' IAclHierarchyItemAsync is required by OS X Calendar.
    ''' </remarks>
    Public Class CalendarFolder
        Inherits DavFolder
        Implements ICalendarFolderAsync, IAppleCalendarAsync, ICurrentUserPrincipalAsync, IAclHierarchyItemAsync

        Public Shared Function GetCalendarFolder(context As DavContext, path As String) As CalendarFolder
            Dim pattern As String = String.Format("^/?{0}/(?<user_name>[^/]+)/(?<calendar_name>[^/]+)/?",
                                                 CalendarsRootFolder.CalendarsRootFolderPath.Trim(New Char() {"/"c}).Replace("/", "/?"))
            If Not Regex.IsMatch(path, pattern) Then Return Nothing
            Dim folderPath As String = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar)
            Dim folder As DirectoryInfo = New DirectoryInfo(folderPath)
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

        Public Async Function MultiGetAsync(pathList As IEnumerable(Of String), propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICalendarFileAsync)) Implements ICalendarReportAsync.MultiGetAsync
            Dim calendarFileList As IList(Of ICalendarFileAsync) = New List(Of ICalendarFileAsync)()
            For Each path As String In pathList
                Dim calendarFile As ICalendarFileAsync = TryCast(Await context.GetHierarchyItemAsync(path), ICalendarFileAsync)
                calendarFileList.Add(calendarFile)
            Next

            Return calendarFileList
        End Function

        Public Async Function QueryAsync(rawQuery As String, propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICalendarFileAsync)) Implements ICalendarReportAsync.QueryAsync
            Return(Await GetChildrenAsync(propNames.ToList())).Cast(Of ICalendarFileAsync)()
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
                Return String.Format("Some {0} description.", Name)
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
        ''' Indicates which sharing or publishing capabilities are supported 
        ''' by this calendar collection.
        ''' </summary>
        Public ReadOnly Property AllowedSharingModes As IEnumerable(Of AppleAllowedSharingMode) Implements IAppleCalendarAsync.AllowedSharingModes
            Get
                Return {AppleAllowedSharingMode.CanBePublished,
                       AppleAllowedSharingMode.CanBeShared}
            End Get
        End Property

        Public Async Function UpdateSharingAsync(sharesToAddAndRemove As IList(Of AppleShare)) As Task Implements IAppleCalendarAsync.UpdateSharingAsync
            For Each share As AppleShare In sharesToAddAndRemove
                If share.Operation = AppleSharingOperation.Withdraw Then
                End If
            Next

            For Each share As AppleShare In sharesToAddAndRemove
                If share.Operation <> AppleSharingOperation.Withdraw Then
                End If
            Next
        End Function

        Public Async Function GetInviteAsync() As Task(Of IEnumerable(Of SharingInvite)) Implements IAppleCalendarAsync.GetInviteAsync
            Return Nothing
        End Function

        Public Async Function GetSharedByAsync() As Task(Of CalendarSharedBy) Implements IAppleCalendarAsync.GetSharedByAsync
            Return CalendarSharedBy.NotShared
        End Function

        Public Function SetOwnerAsync(value As IPrincipalAsync) As Task Implements IAclHierarchyItemAsync.SetOwnerAsync
            Throw New NotImplementedException()
        End Function

        Public Async Function GetOwnerAsync() As Task(Of IPrincipalAsync) Implements IAclHierarchyItemAsync.GetOwnerAsync
            Return context.FileOperation(Me,
                                        Function()
                Dim acl As FileSecurity = File.GetAccessControl(fileSystemInfo.FullName)
                Return AclFactory.GetPrincipalFromSid(acl.GetOwner(GetType(SecurityIdentifier)).Value, context)
            End Function,
                                        Privilege.Read)
        End Function

        Public Function SetGroupAsync(value As IPrincipalAsync) As Task Implements IAclHierarchyItemAsync.SetGroupAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetGroupAsync() As Task(Of IPrincipalAsync) Implements IAclHierarchyItemAsync.GetGroupAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetSupportedPrivilegeSetAsync() As Task(Of IEnumerable(Of SupportedPrivilege)) Implements IAclHierarchyItemAsync.GetSupportedPrivilegeSetAsync
            Throw New NotImplementedException()
        End Function

        Public Async Function GetCurrentUserPrivilegeSetAsync() As Task(Of IEnumerable(Of Privilege)) Implements IAclHierarchyItemAsync.GetCurrentUserPrivilegeSetAsync
            Return {Privilege.Write, Privilege.Read}
        End Function

        Public Function GetAclAsync(propertyNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of ReadAce)) Implements IAclHierarchyItemAsync.GetAclAsync
            Throw New NotImplementedException()
        End Function

        Public Function SetAclAsync(aces As IList(Of WriteAce)) As Task Implements IAclHierarchyItemAsync.SetAclAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetAclRestrictionsAsync() As Task(Of AclRestriction) Implements IAclHierarchyItemAsync.GetAclRestrictionsAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetInheritedAclSetAsync() As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IAclHierarchyItemAsync.GetInheritedAclSetAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetPrincipalCollectionSetAsync() As Task(Of IEnumerable(Of IPrincipalFolderAsync)) Implements IAclHierarchyItemAsync.GetPrincipalCollectionSetAsync
            Throw New NotImplementedException()
        End Function

        Public Function ResolveWellKnownPrincipalAsync(wellKnownPrincipal As WellKnownPrincipal) As Task(Of IPrincipalAsync) Implements IAclHierarchyItemAsync.ResolveWellKnownPrincipalAsync
            Throw New NotImplementedException()
        End Function

        Public Function GetItemsByPropertyAsync(matchBy As MatchBy, props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IAclHierarchyItemAsync)) Implements IAclHierarchyItemAsync.GetItemsByPropertyAsync
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
