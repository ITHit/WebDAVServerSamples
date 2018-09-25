Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Data
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CardDav
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Paging

Namespace CardDav

    ''' <summary>
    ''' Represents a CardDAV address book (address book folder).
    ''' Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[AddressbookFolderId]
    ''' </summary>
    Public Class AddressbookFolder
        Inherits DavHierarchyItem
        Implements IAddressbookFolderAsync, ICurrentUserPrincipalAsync, IAclHierarchyItemAsync

        ''' <summary>
        ''' Loads address book folder by ID. Returns null if addressbook folder was not found.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="addressbookFolderId">ID of the address book folder to load.</param>
        ''' <returns><see cref="IAddressbookFolderAsync"/>  instance.</returns>
        Public Shared Async Function LoadByIdAsync(context As DavContext, addressbookFolderId As Guid) As Task(Of IAddressbookFolderAsync)
            ' Load only address book that the use has access to. 
            ' Also load complete ACL for this address book.
            Dim sql As String = "SELECT * FROM [card_AddressbookFolder] 
                  WHERE [AddressbookFolderId] = @AddressbookFolderId
                  AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [card_Access]
                  WHERE [AddressbookFolderId] = @AddressbookFolderId
                  AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)"
            Return(Await LoadAsync(context, sql,
                                  "@UserId", context.UserId,
                                  "@AddressbookFolderId", addressbookFolderId
                                  )).FirstOrDefault()
        End Function

        ''' <summary>
        ''' Loads all address books.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <returns>List of <see cref="IAddressbookFolderAsync"/>  items.</returns>
        Public Shared Async Function LoadAllAsync(context As DavContext) As Task(Of IEnumerable(Of IAddressbookFolderAsync))
            ' Load only address books that the use has access to. 
            ' Also load complete ACL for each address book, but only if user has access to that address book.
            Dim sql As String = "SELECT * FROM [card_AddressbookFolder] 
                  WHERE [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)

                ; SELECT * FROM [card_Access] 
                  WHERE [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)"
            Return Await LoadAsync(context, sql, "@UserId", context.UserId)
        End Function

        ''' <summary>
        ''' Loads address book by SQL.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="sql">SQL that queries [card_AddressbookFolder] table.</param>
        ''' <param name="prms">List of SQL parameters.</param>
        ''' <returns>List of <see cref="IAddressbookFolderAsync"/>  items.</returns>
        Private Shared Async Function LoadAsync(context As DavContext, sql As String, ParamArray prms As Object()) As Task(Of IEnumerable(Of IAddressbookFolderAsync))
            Dim addressbookFolders As IList(Of IAddressbookFolderAsync) = New List(Of IAddressbookFolderAsync)()
            Using reader As SqlDataReader = Await context.ExecuteReaderAsync(sql, prms)
                Dim addressbooks As DataTable = New DataTable()
                addressbooks.Load(reader)
                Dim access As DataTable = New DataTable()
                access.Load(reader)
                For Each rowAddressbookFolder As DataRow In addressbooks.Rows
                    Dim addressbookFolderId As Guid = rowAddressbookFolder.Field(Of Guid)("AddressbookFolderId")
                    Dim filter As String = String.Format("AddressbookFolderId = '{0}'", addressbookFolderId)
                    Dim rowsAccess As DataRow() = access.Select(filter)
                    addressbookFolders.Add(New AddressbookFolder(context, addressbookFolderId, rowAddressbookFolder, rowsAccess))
                Next
            End Using

            Return addressbookFolders
        End Function

        ''' <summary>
        ''' Creates a new address book folder.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param> 
        ''' <param name="name">Address book name.</param>
        ''' <param name="description">Address book description.</param>
        Friend Shared Async Function CreateAddressbookFolderAsync(context As DavContext, name As String, description As String) As Task
            ' 1. Create address book.
            ' 2. Grant owner privileges to the user on the created address book(s).
            Dim sql As String = "INSERT INTO [card_AddressbookFolder] (
                          [AddressbookFolderId]
                        , [Name]
                        , [Description]
                    ) VALUES (
                          @AddressbookFolderId
                        , @Name
                        , @Description
                    )
                    ; INSERT INTO [card_Access] (
                          [AddressbookFolderId]
                        , [UserId]
                        , [Owner]
                        , [Read]
                        , [Write]
                    ) VALUES (
                          @AddressbookFolderId
                        , @UserId
                        , @Owner
                        , @Read
                        , @Write
                    )"
            Dim addressbookFolderId As Guid = Guid.NewGuid()
            Await context.ExecuteNonQueryAsync(sql,
                                              "@AddressbookFolderId", addressbookFolderId,
                                              "@Name", name,
                                              "@Description", description,
                                              "@UserId", context.UserId,
                                              "@Owner", True, "@Read", True, "@Write", True)
        End Function

        ''' <summary>
        ''' This address book folder ID.
        ''' </summary>
        Private ReadOnly addressbookFolderId As Guid

        ''' <summary>
        ''' Contains data from [card_AddressbookFolder] table.
        ''' </summary>
        Private ReadOnly rowAddressbookFolder As DataRow

        ''' <summary>
        ''' Contains data from [card_Access] table.
        ''' </summary>
        Private ReadOnly rowsAccess As DataRow()

        ''' <summary>
        ''' Gets display name of the address book.
        ''' </summary>
        ''' <remarks>CalDAV clients typically never request this property.</remarks>
        Public Overrides ReadOnly Property Name As String Implements IHierarchyItemAsync.Name
            Get
                Return If(rowAddressbookFolder IsNot Nothing, rowAddressbookFolder.Field(Of String)("Name"), Nothing)
            End Get
        End Property

        ''' <summary>
        ''' Gets item path.
        ''' </summary>
        Public Overrides ReadOnly Property Path As String Implements IHierarchyItemAsync.Path
            Get
                Return String.Format("{0}{1}/", AddressbooksRootFolder.AddressbooksRootFolderPath, addressbookFolderId)
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AddressbookFolder"/>  class from database source.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="addressbookFolderId">Address book folder ID.</param>
        ''' <param name="addressbook">Contains data from [card_AddressbookFolder] table.</param>
        ''' <param name="rowsAccess">Contains data from [card_Access] table for this address book.</param>
        Private Sub New(context As DavContext, addressbookFolderId As Guid, addressbook As DataRow, rowsAccess As DataRow())
            MyBase.New(context)
            Me.addressbookFolderId = addressbookFolderId
            Me.rowAddressbookFolder = addressbook
            Me.rowsAccess = rowsAccess
        End Sub

        ''' <summary>
        ''' Returns a list of address book files that correspont to the specified list of item paths.
        ''' </summary>
        ''' <remarks>
        ''' <para>
        ''' This method is called by the Engine during <b>addressbook-multiget</b> call.
        ''' </para>
        ''' <para>
        ''' For each item from the <b>pathList</b> parameter return an item that corresponds to path or <b>null</b> if the item is not found.
        ''' </para>
        ''' </remarks>
        ''' <param name="pathList">Addressbook files path list.</param>
        ''' <param name="propNames">
        ''' Properties requested by the client. You can use this as a hint about what properties will be called by 
        ''' the Engine for each item that are returned from this method.
        ''' </param>
        ''' <returns>List of address book files. Returns <b>null</b> for any item that is not found.</returns>
        Public Async Function MultiGetAsync(pathList As IEnumerable(Of String), propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICardFileAsync)) Implements IAddressbookReportAsync.MultiGetAsync
            ' Get list of file names from path list.
            Dim fileNames As IEnumerable(Of String) = pathList.Select(Function(a) System.IO.Path.GetFileNameWithoutExtension(a))
            Return Await CardFile.LoadByFileNamesAsync(Context, fileNames, PropsToLoad.All)
        End Function

        ''' <summary>
        ''' Returns a list of address book files that match specified filter. 
        ''' </summary>
        ''' <remarks>
        ''' <para>
        ''' This method is called by the Engine during <b>addressbook-query</b> call.
        ''' </para>
        ''' </remarks>
        ''' <param name="rawQuery">
        ''' Raw query sent by the client.
        ''' </param>
        ''' <param name="propNames">
        ''' Properties requested by the client. You can use this as a hint about what properties will be called by 
        ''' the Engine for each item that are returned from this method.
        ''' </param>
        ''' <returns>List of address book files. Returns <b>null</b> for any item that is not found.</returns>
        Public Async Function QueryAsync(rawQuery As String, propNames As IEnumerable(Of PropertyName)) As Task(Of IEnumerable(Of ICardFileAsync)) Implements IAddressbookReportAsync.QueryAsync
            ' For the sake of simplicity we just call GetChildren returning all items. 
            ' Typically you will return only items that match the query.
            Return(Await GetChildrenAsync(propNames.ToList(), Nothing, Nothing, Nothing)).Page.Cast(Of ICardFileAsync)()
        End Function

        ''' <summary>
        ''' Provides a human-readable description of the address book collection.
        ''' </summary>
        Public ReadOnly Property AddressbookDescription As String Implements IAddressbookFolderAsync.AddressbookDescription
            Get
                Return rowAddressbookFolder.Field(Of String)("Description")
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
            ' Here we enumerate all business cards contained in this address book.
            ' You can filter children items in this implementation and 
            ' return only items that you want to be available for this 
            ' particular user.
            ' Typically only getcontenttype and getetag properties are requested in GetChildren call by CalDAV/CardDAV clients.
            ' The iCalendar/vCard (calendar-data/address-data) is typically requested not in GetChildren, but in a separate multiget 
            ' report, in MultiGetAsync() method call, that follows this request.
            ' Bynari submits PROPFIND without props - Engine will request getcontentlength
            Dim children As IList(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
            Return New PageResults((Await CardFile.LoadByAddressbookFolderIdAsync(Context, addressbookFolderId, PropsToLoad.Minimum)), Nothing)
        End Function

        ''' <summary>
        ''' Creates a file that contains business card item in this address book.
        ''' </summary>
        ''' <param name="name">Name of the new file. Unlike with CalDAV it is NOT equel to vCard UID.</param>
        ''' <returns>The newly created file.</returns>
        ''' <remarks></remarks>
        Public Async Function CreateFileAsync(name As String) As Task(Of IFileAsync) Implements IFolderAsync.CreateFileAsync
            ' The actual business card file is created in datatbase in CardFile.Write call.
            Dim fileName As String = System.IO.Path.GetFileNameWithoutExtension(name)
            Return CardFile.CreateCardFile(Context, addressbookFolderId, fileName)
        End Function

        ''' <summary>
        ''' Creating new folders is not allowed in address book folders.
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
            ' Here we support only addressbooks renaming. Check that user has permissions to write.
            Dim sql As String = "UPDATE [card_AddressbookFolder] SET Name=@Name 
                WHERE [AddressbookFolderId]=@AddressbookFolderId
                AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId AND [Write] = 1)"
            If Await Context.ExecuteNonQueryAsync(sql, 
                                                 "@Name", destName,
                                                 "@UserId", Context.UserId,
                                                 "@AddressbookFolderId", addressbookFolderId) < 1 Then
                Throw New DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN)
            End If
        End Function

        ''' <summary>
        ''' Deletes this address book.
        ''' </summary>
        ''' <param name="multistatus"><see cref="MultistatusException"/>  to populate with child files and folders failed to delete.</param>
        Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
            ' Delete address book and all vCards associated with it. Check that user has permissions to delete.
            Dim sql As String = "DELETE FROM [card_AddressbookFolder] 
                WHERE [AddressbookFolderId]=@AddressbookFolderId
                AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId AND [Owner] = 1)"
            If Await Context.ExecuteNonQueryAsync(sql,
                                                 "@UserId", Context.UserId,
                                                 "@AddressbookFolderId", addressbookFolderId) < 1 Then
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
            Dim propVals As IList(Of PropertyValue) = Await GetPropertyValuesAsync("SELECT [Name], [Namespace], [PropVal] FROM [card_AddressbookFolderProperty] WHERE [AddressbookFolderId] = @AddressbookFolderId",
                                                                                  "@AddressbookFolderId", addressbookFolderId)
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
                While reader.Read()
                    Dim name As String = reader.GetString(reader.GetOrdinal("Name"))
                    Dim ns As String = reader.GetString(reader.GetOrdinal("Namespace"))
                    Dim value As String = reader.GetString(reader.GetOrdinal("PropVal"))
                    l.Add(New PropertyValue(New PropertyName(name, ns), value))
                End While
            End Using

            Return l
        End Function

        Private Async Function SetPropertyAsync(prop As PropertyValue) As Task
            Dim selectCommand As String = "SELECT Count(*) FROM [card_AddressbookFolderProperty]
                  WHERE [AddressbookFolderId] = @AddressbookFolderId AND [Name] = @Name AND [Namespace] = @Namespace"
            Dim count As Integer = Await Context.ExecuteScalarAsync(Of Integer)(selectCommand,
                                                                               "@AddressbookFolderId", addressbookFolderId,
                                                                               "@Name", prop.QualifiedName.Name,
                                                                               "@Namespace", prop.QualifiedName.Namespace)
            ' insert
            If count = 0 Then
                Dim insertCommand As String = "INSERT INTO [card_AddressbookFolderProperty] ([AddressbookFolderId], [Name], [Namespace], [PropVal])
                                          VALUES(@AddressbookFolderId, @Name, @Namespace, @PropVal)"
                Await Context.ExecuteNonQueryAsync(insertCommand,
                                                  "@PropVal", prop.Value,
                                                  "@AddressbookFolderId", addressbookFolderId,
                                                  "@Name", prop.QualifiedName.Name,
                                                  "@Namespace", prop.QualifiedName.Namespace)
            Else
                ' update
                Dim command As String = "UPDATE [card_AddressbookFolderProperty]
                      SET [PropVal] = @PropVal
                      WHERE [AddressbookFolderId] = @AddressbookFolderId AND [Name] = @Name AND [Namespace] = @Namespace"
                Await Context.ExecuteNonQueryAsync(command,
                                                  "@PropVal", prop.Value,
                                                  "@AddressbookFolderId", addressbookFolderId,
                                                  "@Name", prop.QualifiedName.Name,
                                                  "@Namespace", prop.QualifiedName.Namespace)
            End If
        End Function

        Private Async Function RemovePropertyAsync(name As String, ns As String) As Task
            Dim command As String = "DELETE FROM [card_AddressbookFolderProperty]
                              WHERE [AddressbookFolderId] = @AddressbookFolderId
                              AND [Name] = @Name
                              AND [Namespace] = @Namespace"
            Await Context.ExecuteNonQueryAsync(command,
                                              "@AddressbookFolderId", addressbookFolderId,
                                              "@Name", name,
                                              "@Namespace", ns)
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
