Imports ITHit.WebDAV.Server

Namespace CalDav

    Module CalDavFactory

        Friend Function GetCalDavItem(context As DavContext, path As String) As IHierarchyItemAsync
            Dim item As IHierarchyItemAsync = Nothing
            item = CalendarsRootFolder.GetCalendarsRootFolder(context, path)
            If item IsNot Nothing Then Return item
            item = CalendarFolder.GetCalendarFolder(context, path)
            If item IsNot Nothing Then Return item
            item = CalendarFile.GetCalendarFile(context, path)
            If item IsNot Nothing Then Return item
            Return Nothing
        End Function
    End Module
End Namespace
