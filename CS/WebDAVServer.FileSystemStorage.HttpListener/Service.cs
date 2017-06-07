using System.ServiceProcess;
using System.Threading;

namespace WebDAVServer.FileSystemStorage.HttpListener
{
    partial class Service : ServiceBase
    {
        private Thread thread;
        
        public Service()
        {
            InitializeComponent();
        }
        
        #region ServiceBase Methods Override

        protected override void OnStart(string[] args)
        {
            Program.Listening = true;
            thread = new Thread(Program.ThreadProcAsync);
            thread.Start();
        }

        protected override void OnStop()
        {
            Program.Listening = false;
            thread.Abort();
            thread.Join();
        }

        #endregion // ServiceBase Methods Override

    }
}
