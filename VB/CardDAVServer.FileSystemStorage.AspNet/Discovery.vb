Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CardDav
Imports CardDAVServer.FileSystemStorage.AspNet.CardDav

''' <summary>
''' Assists in finding folders that contain calendars and address books.
''' </summary>
Public Class Discovery
    Implements IAddressbookDiscoveryAsync

    ''' <summary>
    ''' Instance of <see cref="DavContext"/> .
    ''' </summary>
    Private context As DavContext

    Public Sub New(context As DavContext)
        Me.context = context
    End Sub

    Public Async Function GetAddressbookHomeSetAsync() As Task(Of IEnumerable(Of IItemCollectionAsync)) Implements IAddressbookDiscoveryAsync.GetAddressbookHomeSetAsync
        Dim addressbooksUserFolder As String = String.Format("{0}{1}/", AddressbooksRootFolder.AddressbooksRootFolderPath, context.UserName)
        Return {Await DavFolder.GetFolderAsync(context, addressbooksUserFolder)}
    End Function

    ''' <summary>
    ''' Returns <b>true</b> if <b>addressbook-home-set</b> feature is enabled, <b>false</b> otherwise.
    ''' </summary>
    ''' <remarks>
    ''' <para>
    ''' In this method you can analyze User-Agent header and enable/disable <b>addressbook-home-set</b> feature for specific client. 
    ''' </para>
    ''' <para>
    ''' iOS and OS X does require <b>addressbook-home-set</b> feature to be always enabled. On the other hand it may consume extra 
    ''' resources especially with iOS CardDAV client. iOS starts immediate synchronization of all address books found on the server 
    ''' via home-set request. Typically you will always enable heome-set for iOS and OS X CardDAV clients, but may disable it for other clients.
    ''' </para>
    ''' </remarks>
    Public ReadOnly Property AddressbookHomeSetEnabled As Boolean Implements IAddressbookDiscoveryAsync.AddressbookHomeSetEnabled
        Get
            Return True
        End Get
    End Property
End Class
