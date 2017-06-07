
using System;
using System.ComponentModel;
using System.Configuration.Install;

namespace WebDAVServer.FileSystemStorage.HttpListener
{
    [RunInstaller(true)]
    public partial class ServiceInstaller : Installer
    {
        public ServiceInstaller()
        {
            InitializeComponent();

            //string serviceAccount = ConfigurationManager.AppSettings["ServiceAccount"];
            //this.serviceProcessInstaller.Account = (ServiceAccount)Enum.Parse(typeof(ServiceAccount), serviceAccount);
            //this.serviceProcessInstaller.Password = null;
            //this.serviceProcessInstaller.Username = null;
            
            //string serviceName = ConfigurationManager.AppSettings["ServiceName"];
            //this.serviceInstaller.ServiceName = serviceName;
            //this.serviceInstaller.DisplayName = serviceName;
            //this.serviceInstaller.StartType = ServiceStartMode.Automatic;
        }
    }
}
