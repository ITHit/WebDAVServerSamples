Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CardDav
Imports ITHit.WebDAV.Server.Acl
Imports CardDAVServer.FileSystemStorage.AspNet.Acl

Namespace CardDav

    ''' <summary>
    ''' Represents CrdDAV address book (address book folder).
    ''' Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[user_name]/[addressbook_name]/
    ''' </summary>
    ''' <example>
    ''' [DAVLocation]
    '''  |-- ...
    '''  |-- addressbooks
    '''      |-- ...
    '''      |-- [User2]
    '''           |-- [Address Book 1]  -- this class
    '''           |-- ...
    '''           |-- [Address Book X]  -- this class
    ''' </example>
    ''' <remarks>
    ''' IAclHierarchyItemAsync is required by OS X Contacts.
    ''' </remarks>
    Public Class AddressbookFolder
        Inherits DavFolder
        Implements IAddressbookFolderAsync, IAclHierarchyItemAsync

        ''' <summary>
        ''' Returns address book folder that corresponds to path.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="path">Encoded path relative to WebDAV root.</param>
        ''' <returns>AddressbookFolder instance or null if not found.</returns>
        Public Shared Function GetAddressbookFolder(context As DavContext, path As String) As AddressbookFolder
            Dim pattern As String = String.Format("^/?{0}/(?<user_name>[^/]+)/(?<addressbook_name>[^/]+)/?",
                                                 AddressbooksRootFolder.AddressbooksRootFolderPath.Trim(New Char() {"/"c}).Replace("/", "/?"))
            If Not Regex.IsMatch(path, pattern) Then Return Nothing
            Dim folderPath As String = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar)
            Dim folder As DirectoryInfo = New DirectoryInfo(folderPath)
            ' to block vulnerability when "%20" folder can be injected into path and folder.Exists returns 'true'
            If Not folder.Exists OrElse [String].Compare(folder.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), folderPath, StringComparison.OrdinalIgnoreCase) <> 0 Then Return Nothing
            Return New AddressbookFolder(folder, context, path)
        End Function

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AddressbookFolder"/>  class.
        ''' </summary>
        ''' <param name="directoryInfo">Instance of <see cref="DirectoryInfo"/>  class with information about the folder in file system.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
        ''' <param name="path">Relative to WebDAV root folder path.</param>
        Private Sub New(directoryInfo As DirectoryInfo, context As DavContext, path As String)
            MyBase.New(directoryInfo, context, path)
        End Sub

        Public ReadOnly Property AddressbookDescription As String Implements IAddressbookFolderAsync.AddressbookDescription
            Get
                Return String.Format("Some {0} description.", Name)
            End Get
        End Property

        ''' <summary>
        ''' Returns a list of business card files that correspont to the specified list of item paths.
        ''' </summary>
        ''' <remarks>
        ''' <para>
        ''' This method is called by the Engine during <b>addressbook-multiget</b> call.
        ''' </para>
        ''' <para>
        ''' For each item from the <b>pathList</b> parameter return an item that corresponds to path or <b>null</b> if the item is not found.
        ''' </para>
        ''' </remarks>
        ''' <param name="pathList">Business card files path list.</param>
        ''' <param name="propNames">
        ''' Properties requested by the client. You can use this as a hint about what properties will be called by 
        ''' the Engine for each item that are returned from this method.
        ''' </param>
        ''' <returns>List of business card files. Returns <b>null</b> for any item that is not found.</returns>
        Public Async Function MultiGetAsync(pathList As IEnumerable(Of String), propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICardFileAsync)) Implements IAddressbookReportAsync.MultiGetAsync
            ' Here you can load all items from pathList in one request to your storage, instead of 
            ' getting items one-by-one using GetHierarchyItem call.
            Dim cardFileList As IList(Of ICardFileAsync) = New List(Of ICardFileAsync)()
            For Each path As String In pathList
                Dim cardFile As ICardFileAsync = TryCast(Await context.GetHierarchyItemAsync(path), ICardFileAsync)
                cardFileList.Add(cardFile)
            Next

            Return cardFileList
        End Function

        ''' <summary>
        ''' Returns a list of business card files that match specified filter. 
        ''' </summary>
        ''' <remarks>
        ''' <param name="rawQuery">
        ''' Raw query sent by the client.
        ''' </param>
        ''' <param name="propNames">
        ''' Properties requested by the client. You can use this as a hint about what properties will be called by 
        ''' the Engine for each item that are returned from this method.
        ''' </param>
        ''' <returns>List of  business card files. Returns <b>null</b> for any item that is not found.</returns>
        Public Async Function QueryAsync(rawQuery As String, propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICardFileAsync)) Implements IAddressbookReportAsync.QueryAsync
            ' For the sake of simplicity we just call GetChildren returning all items. 
            ' Typically you will return only items that match the query.
            Return(Await GetChildrenAsync(propNames.ToList())).Cast(Of ICardFileAsync)()
        End Function

        Public Function SetOwnerAsync(value As IPrincipalAsync) As Task Implements IAclHierarchyItemAsync.SetOwnerAsync
            Throw New NotImplementedException()
        End Function

        ''' <summary>
        ''' Retrieves a particular principal as being the "owner" of the item. 
        ''' </summary>
        ''' <remarks>Required by OS X.</remarks>
        ''' <returns>
        ''' Item that represents owner of this item and implements <see cref="IPrincipalAsync"/> .
        ''' </returns>
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

        ''' <summary>
        ''' Retrieves the exact set of privileges (as computed by
        ''' the server) granted to the currently authenticated HTTP user. Aggregate privileges and their contained
        ''' privileges are listed.
        ''' </summary>
        ''' <returns>
        ''' List of current user privileges.
        ''' </returns>        
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
