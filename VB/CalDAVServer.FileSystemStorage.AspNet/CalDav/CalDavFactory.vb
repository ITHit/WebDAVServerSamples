Imports ITHit.WebDAV.Server

Namespace CalDav

    ''' <summary>
    ''' Represents a factory for creating CalDAV items.
    ''' </summary>
    Module CalDavFactory

        ''' <summary>
        ''' Gets CalDAV items.
        ''' </summary>
        ''' <param name="path">Relative path requested.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <returns>Object implementing various CalDAV items or null if no object corresponding to path is found.</returns>
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
