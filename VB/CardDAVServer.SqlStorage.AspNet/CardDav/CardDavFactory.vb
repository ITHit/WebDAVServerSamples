Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Namespace CardDav

    Module CardDavFactory

        ''' <summary>
        ''' Gets CardDAV items.
        ''' </summary>
        ''' <param name="path">Relative path requested.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <returns>Object implementing various business card items or null if no object corresponding to path is found.</returns>
        Async Function GetCardDavItemAsync(context As DavContext, path As String) As Task(Of IHierarchyItemAsync)
            ' If this is [DAVLocation]/addressbooks - return folder that contains all addressbooks.
            If path.Equals(AddressbooksRootFolder.AddressbooksRootFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Return New AddressbooksRootFolder(context)
            End If

            Dim segments As String() = path.Split({"/"c}, StringSplitOptions.RemoveEmptyEntries)
            ' If URL ends with .vcf - return address book file, which contains vCard.
            If path.EndsWith(CardFile.Extension, System.StringComparison.InvariantCultureIgnoreCase) Then
                Dim fileName As String = EncodeUtil.DecodeUrlPart(System.IO.Path.GetFileNameWithoutExtension(segments.Last()))
                Return(Await CardFile.LoadByFileNamesAsync(context, {fileName}, PropsToLoad.All)).FirstOrDefault()
            End If

            ' If this is [DAVLocation]/addressbooks/[AddressbookFolderId]/ return address book.
            If path.StartsWith(AddressbooksRootFolder.AddressbooksRootFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Dim addressbookFolderId As Guid
                If Guid.TryParse(EncodeUtil.DecodeUrlPart(segments.Last()), addressbookFolderId) Then
                    Return Await AddressbookFolder.LoadByIdAsync(context, addressbookFolderId)
                End If
            End If

            Return Nothing
        End Function
    End Module
End Namespace
