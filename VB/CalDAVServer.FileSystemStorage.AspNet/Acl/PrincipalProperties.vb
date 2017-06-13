Imports ITHit.WebDAV.Server

Namespace Acl

    ''' <summary>
    ''' Contains description of principal's properties.
    ''' </summary>
    Module PrincipalProperties

        Public ReadOnly FullName As PropertyName = New PropertyName("full-name", "ithit")

        Public ReadOnly Description As PropertyName = New PropertyName("description", "ithit")

        Public ReadOnly ALL As PropertyName()

        Sub New()
            ALL = {FullName, Description}
        End Sub
    End Module
End Namespace
