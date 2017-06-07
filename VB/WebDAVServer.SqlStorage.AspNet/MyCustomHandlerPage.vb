Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Web
Imports System.Web.UI
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Public Class MyCustomHandlerPage
    Inherits Page

    Protected Sub New()
        If Type.GetType("Mono.Runtime") Is Nothing Then
            AddHandler Me.Load, AddressOf Page_LoadAsync
        End If
    End Sub

    Private Sub Page_LoadAsync(sender As Object, e As EventArgs)
        RegisterAsyncTask(New PageAsyncTask(AddressOf GetPageDataAsync))
    End Sub

    Public Async Function GetPageDataAsync() As Task
    End Function
End Class
