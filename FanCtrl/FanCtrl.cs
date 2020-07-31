using FanCtrlCommon;
using System;
using System.ServiceModel;
using System.ServiceProcess;
using System.Timers;

namespace FanCtrl
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public partial class FanCtrl : ServiceBase, IFanCtrlInterface
    {
        DellSMMIO io;
        Timer timer;
        ServiceHost host;

        public FanCtrl()
        {
            InitializeComponent();
            io = new DellSMMIO();
            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
            host = new ServiceHost(this, new Uri[] { new Uri("net.pipe://localhost") });
            host.AddServiceEndpoint(typeof(IFanCtrlInterface), new NetNamedPipeBinding(NetNamedPipeSecurityMode.None), "FanCtrlInterface");
        }

        sbyte fanlvl = -1;
        uint maxTemp;
        ushort startTries = 5;
        ushort ticksToSkip = 0;
        ushort ticksToSkip2 = 0;
        private void Timer_Elapsed(object sender, EventArgs e)
        {
            if (!io.Opened)
            {
                if (!io.BDSID_InstallDriver() || !io.BDSID_StartDriver() || !io.Open())
                {
                    startTries--;

                    if (startTries == 0)
                        Stop();

                    return;
                }

                io.dell_smm_io(DellSMMIO.DELL_SMM_IO_DISABLE_FAN_CTL1, DellSMMIO.DELL_SMM_IO_NO_ARG);
                io.dell_smm_io(DellSMMIO.DELL_SMM_IO_DISABLE_FAN_CTL2, DellSMMIO.DELL_SMM_IO_NO_ARG);
            }

            maxTemp = io.MaxTemperature();

            if(maxTemp == 0)
            {
                //Something is very wrong
                Stop();
                return;
            }
            else if(maxTemp >= 65)
            {
                ticksToSkip = 5;
                ticksToSkip2 = 30;
            }
            else if (ticksToSkip > 0)
            {
                ticksToSkip--;
            }
            else if (maxTemp >= 45)
            {
                ticksToSkip2 = 30;
            }
            else if (ticksToSkip2 > 0 && maxTemp <= 42)
            {
                ticksToSkip2--;
            }

            if (ticksToSkip > 0)
            {
                SetFanLevel(DellSMMIO.DELL_SMM_IO_FAN_LV2);
            }
            else if (ticksToSkip2 > 0)
            {
                SetFanLevel(DellSMMIO.DELL_SMM_IO_FAN_LV1);
            }
            else
            {
                SetFanLevel(DellSMMIO.DELL_SMM_IO_FAN_LV0);
            }
        }

        protected override void OnStart(string[] args)
        {
            timer.Start();
            Timer_Elapsed(null, null);
            host.Open();
        }

        protected override void OnStop()
        {
            host.Close();
            timer.Stop();

            io.dell_smm_io(DellSMMIO.DELL_SMM_IO_ENABLE_FAN_CTL1, DellSMMIO.DELL_SMM_IO_NO_ARG);
            io.dell_smm_io(DellSMMIO.DELL_SMM_IO_ENABLE_FAN_CTL2, DellSMMIO.DELL_SMM_IO_NO_ARG);
            fanlvl = -1;
            io.BDSID_Shutdown();
        }

        public FanCtrlData GetData()
        {
            return new FanCtrlData(maxTemp,fanlvl);
        }

        void SetFanLevel(uint level)
        {
            io.dell_smm_io_set_fan_lv(DellSMMIO.DELL_SMM_IO_FAN1, level);
            io.dell_smm_io_set_fan_lv(DellSMMIO.DELL_SMM_IO_FAN2, level);
            fanlvl = (sbyte)level;
        }
    }
}
