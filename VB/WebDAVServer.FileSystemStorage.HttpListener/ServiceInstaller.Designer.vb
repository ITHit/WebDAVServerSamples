Imports System
Imports System.ServiceProcess

Partial Class ServiceInstaller

    ''' <summary>
    ''' Required designer variable.
    ''' </summary>
    Private components As System.ComponentModel.IContainer = Nothing

    ''' <summary> 
    ''' Clean up any files being used.
    ''' </summary>
    ''' <param name="disposing">true if managed files should be disposed; otherwise, false.</param>
    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso (components IsNot Nothing) Then
            components.Dispose()
        End If

        MyBase.Dispose(disposing)
    End Sub

    ''' <summary>
    ''' Required method for Designer support - do not modify
    ''' the contents of this method with the code editor.
    ''' </summary>
    Private Sub InitializeComponent()
        Me.serviceInstaller1 = New System.ServiceProcess.ServiceInstaller()
        Me.serviceProcessInstaller = New System.ServiceProcess.ServiceProcessInstaller()
        Me.serviceInstaller1.ServiceName = "Class2 WebDAV Server with File System Storage"
        Me.serviceInstaller1.StartType = ServiceStartMode.Automatic
        Me.serviceProcessInstaller.Account = CType([Enum].Parse(GetType(ServiceAccount), "LocalSystem"), ServiceAccount)
        Me.serviceProcessInstaller.Password = Nothing
        Me.serviceProcessInstaller.Username = Nothing
        Me.Installers.AddRange(New System.Configuration.Install.Installer() {Me.serviceInstaller1,
                                                                            Me.serviceProcessInstaller})
    End Sub

    Private serviceInstaller1 As System.ServiceProcess.ServiceInstaller

    Private serviceProcessInstaller As System.ServiceProcess.ServiceProcessInstaller
End Class
