Imports System.ServiceProcess
Imports System.Threading

Partial Class Service
    Inherits ServiceBase

    Private thread As Thread

    Public Sub New()
        InitializeComponent()
    End Sub

    Protected Overrides Sub OnStart(args As String())
        Program.Listening = True
        thread = New Thread(AddressOf Program.ThreadProcAsync)
        thread.Start()
    End Sub

    Protected Overrides Sub OnStop()
        Program.Listening = False
        thread.Abort()
        thread.Join()
    End Sub
End Class
