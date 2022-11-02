Imports ITHit.WebDAV.Server

Namespace CardDav

    ''' <summary>
    ''' Represents a factory for creating CardDAV items.
    ''' </summary>
    Module CardDavFactory

        ''' <summary>
        ''' Gets CardDAV items.
        ''' </summary>
        ''' <param name="path">Relative path requested.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <returns>Object implementing various CardDAV items or null if no object corresponding to path is found.</returns>
        Friend Function GetCardDavItem(context As DavContext, path As String) As IHierarchyItem
            Dim item As IHierarchyItem = Nothing
            item = AddressbooksRootFolder.GetAddressbooksRootFolder(context, path)
            If item IsNot Nothing Then Return item
            item = AddressbookFolder.GetAddressbookFolder(context, path)
            If item IsNot Nothing Then Return item
            item = CardFile.GetCardFile(context, path)
            If item IsNot Nothing Then Return item
            Return Nothing
        End Function
    End Module
End Namespace
