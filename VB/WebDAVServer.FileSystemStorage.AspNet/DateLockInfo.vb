Imports System
Imports ITHit.WebDAV.Server.Class2

''' <summary>
''' Stores information about lock.
''' </summary>
Public Class DateLockInfo

    ''' <summary>
    ''' Gets or sets lock owner as specified by client.
    ''' </summary>
    Public Property ClientOwner As String = String.Empty

    ''' <summary>
    ''' Gets or sets time when the lock expires.
    ''' </summary>
    Public Property Expiration As DateTime

    ''' <summary>
    ''' Gets or sets lock token.
    ''' </summary>
    Public Property LockToken As String = String.Empty

    ''' <summary>
    ''' Gets or sets lock level.
    ''' </summary>
    Public Property Level As LockLevel

    ''' <summary>
    ''' Gets or sets a value indicating whether the lock is deep.
    ''' </summary>
    Public Property IsDeep As Boolean

    ''' <summary>
    ''' Gets or sets path of item item which has the lock specified explicitly.
    ''' </summary>
    Public Property LockRoot As String = String.Empty

    ''' <summary>
    ''' Gets or sets timeout for the lock requested by client.
    ''' </summary>
    Public Property TimeOut As TimeSpan
End Class
