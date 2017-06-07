Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Namespace CalDav

    ''' <summary>
    ''' Folder that contains user folders which contain calendars.
    ''' Instances of this class correspond to the following path: [DAVLocation]/calendars/
    ''' </summary>
    ''' <example>
    ''' [DAVLocation]
    '''  |-- ...
    '''  |-- calendars  -- this class
    '''      |-- [User1]
    '''      |-- ...
    '''      |-- [UserX]
    ''' </example>
    Public Class CalendarsRootFolder
        Inherits DavFolder

        ''' <summary>
        ''' This folder name.
        ''' </summary>
        Private Shared ReadOnly calendarsRootFolderName As String = "calendars"

        ''' <summary>
        ''' Path to this folder.
        ''' </summary>
        Public Shared CalendarsRootFolderPath As String = String.Format("{0}{1}/", DavLocationFolder.DavLocationFolderPath, calendarsRootFolderName)

        Public Shared Function GetCalendarsRootFolder(context As DavContext, path As String) As CalendarsRootFolder
            If Not path.Equals(CalendarsRootFolderPath, StringComparison.InvariantCultureIgnoreCase) Then Return Nothing
            Dim folder As DirectoryInfo = New DirectoryInfo(context.MapPath(path))
            If Not folder.Exists Then Return Nothing
            Return New CalendarsRootFolder(folder, context, path)
        End Function

        Private Sub New(directory As DirectoryInfo, context As DavContext, path As String)
            MyBase.New(directory, context, path)
        End Sub

        Overrides Public Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task
            Throw New DavException("Moving or renaming this folder is not allowed.", DavStatus.NOT_ALLOWED)
        End Function

        Overrides Public Async Function DeleteAsync(multistatus As MultistatusException) As Task
            Throw New DavException("Deleting this folder is not allowed.", DavStatus.NOT_ALLOWED)
        End Function
    End Class
End Namespace
