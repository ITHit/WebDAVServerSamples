Imports System
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Namespace CalDav

    Module CalDavFactory

        Async Function GetCalDavItemAsync(context As DavContext, itemPath As String) As Task(Of IHierarchyItemAsync)
            If itemPath.Equals(CalendarsRootFolder.CalendarsRootFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Return New CalendarsRootFolder(context)
            End If

            Dim segments As String() = itemPath.Split({"/"c}, StringSplitOptions.RemoveEmptyEntries)
            If itemPath.EndsWith(CalendarFile.Extension, System.StringComparison.InvariantCultureIgnoreCase) Then
                Dim uid As String = EncodeUtil.DecodeUrlPart(Path.GetFileNameWithoutExtension(segments.Last()))
                Return(Await CalendarFile.LoadByUidsAsync(context, {uid}, PropsToLoad.All)).FirstOrDefault()
            End If

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
