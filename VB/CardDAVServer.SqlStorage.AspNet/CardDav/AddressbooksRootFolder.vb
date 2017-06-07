Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Web
Imports System.IO
Imports System.Data
Imports System.Data.SqlClient
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CardDav
Imports ITHit.WebDAV.Server.Class1

Namespace CardDav

    ''' <summary>
    ''' Folder that contains address books.
    ''' Instances of this class correspond to the following path: [DAVLocation]/addressbooks/
    ''' </summary>
    Public Class AddressbooksRootFolder
        Inherits LogicalFolder
        Implements IFolderAsync

        ''' <summary>
        ''' This folder name.
        ''' </summary>
        Private Shared ReadOnly addressbooksRootFolderName As String = "addressbooks"

        ''' <summary>
        ''' Path to this folder.
        ''' </summary>
        Public Shared AddressbooksRootFolderPath As String = DavLocationFolder.DavLocationFolderPath & addressbooksRootFolderName & "/"c

        Public Sub New(context As DavContext)
            MyBase.New(context, AddressbooksRootFolderPath)
        End Sub

        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
            Return(Await AddressbookFolder.LoadAllAsync(Context)).OrderBy(Function(x) x.Name)
        End Function

        Public Function CreateFileAsync(name As String) As Task(Of IFileAsync) Implements IFolderAsync.CreateFileAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function CreateFolderAsync(name As String) As Task Implements IFolderAsync.CreateFolderAsync
            Await AddressbookFolder.CreateAddressbookFolderAsync(Context, name, "")
        End Function
    End Class
End Namespace
