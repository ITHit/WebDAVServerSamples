Imports System
Imports System.ComponentModel
Imports System.Configuration.Install

<RunInstaller(True)>
Public Partial Class ServiceInstaller
    Inherits Installer

    Public Sub New()
        InitializeComponent()
    End Sub
End Class
