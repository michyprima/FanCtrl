using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace FanCtrl
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
        protected override void OnAfterUninstall(IDictionary savedState)
        {
            new DellSMMIO().BDSID_RemoveDriver();
            base.OnAfterUninstall(savedState);
        }
        protected override void OnAfterInstall(IDictionary savedState)
        {
            using (ServiceController sc = new ServiceController(serviceInstaller1.ServiceName))
            {
                sc.Start();
            }
            base.OnAfterInstall(savedState);
        }
    }
}
