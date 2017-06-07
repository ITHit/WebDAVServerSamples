Imports ITHit.WebDAV.Server

Namespace CardDav

    Module CardDavFactory

        Friend Function GetCardDavItem(context As DavContext, path As String) As IHierarchyItemAsync
            Dim item As IHierarchyItemAsync = Nothing
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
