Imports System
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Namespace CalDav

    Module CalDavFactory

        ''' <summary>
        ''' Gets CalDAV items.
        ''' </summary>
        ''' <param name="itemPath">Relative path requested.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <returns>Object implementing various calendar items or null if no object corresponding to path is found.</returns>
        Async Function GetCalDavItemAsync(context As DavContext, itemPath As String) As Task(Of IHierarchyItemAsync)
            ' If this is [DAVLocation]/calendars - return folder that contains all calendars.
            If itemPath.Equals(CalendarsRootFolder.CalendarsRootFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Return New CalendarsRootFolder(context)
            End If

            Dim segments As String() = itemPath.Split({"/"c}, StringSplitOptions.RemoveEmptyEntries)
            ' If URL ends with .ics - return calendar file, which contains event or to-do.
            If itemPath.EndsWith(CalendarFile.Extension, System.StringComparison.InvariantCultureIgnoreCase) Then
                Dim uid As String = EncodeUtil.DecodeUrlPart(Path.GetFileNameWithoutExtension(segments.Last())).Normalize(NormalizationForm.FormC)
                Return(Await CalendarFile.LoadByUidsAsync(context, {uid}, PropsToLoad.All)).FirstOrDefault()
            End If

            ' If this is [DAVLocation]/calendars/[CalendarFolderId]/ return calendar.
            If itemPath.StartsWith(CalendarsRootFolder.CalendarsRootFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Dim calendarFolderId As Guid
                If Guid.TryParse(EncodeUtil.DecodeUrlPart(segments.Last()), calendarFolderId) Then
                    Return Await CalendarFolder.LoadByIdAsync(context, calendarFolderId)
                End If
            End If

            Return Nothing
        End Function
    End Module
End Namespace
