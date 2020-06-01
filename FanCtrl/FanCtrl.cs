using System;
using System.ServiceProcess;
using System.Timers;

namespace FanCtrl
{
    public partial class FanCtrl : ServiceBase
    {
        DellSMMIO io;
        Timer timer;

        public FanCtrl()
        {
            InitializeComponent();
            io = new DellSMMIO();
            timer = new Timer();
            timer.Interval = 1000;
            timer.Elapsed += Timer_Elapsed;
        }

        ushort ticksToSkip = 0;
        private void Timer_Elapsed(object sender, EventArgs e)
        {
            uint maxTemp = io.MaxTemperature();

            if(maxTemp == 0)
            {
                //Something is very wrong
                Stop();
                return;
            }
            else if(maxTemp >= 65)
            {
                ticksToSkip = 5;
            } 
            else if (ticksToSkip > 0)
            {
                ticksToSkip--;
            }

            if (ticksToSkip > 0)
            {
                io.dell_smm_io_set_fan_lv(DellSMMIO.DELL_SMM_IO_FAN1, DellSMMIO.DELL_SMM_IO_FAN_LV2);
                io.dell_smm_io_set_fan_lv(DellSMMIO.DELL_SMM_IO_FAN2, DellSMMIO.DELL_SMM_IO_FAN_LV2);
            }
            else
            {
                io.dell_smm_io_set_fan_lv(DellSMMIO.DELL_SMM_IO_FAN1, DellSMMIO.DELL_SMM_IO_FAN_LV1);
                io.dell_smm_io_set_fan_lv(DellSMMIO.DELL_SMM_IO_FAN2, DellSMMIO.DELL_SMM_IO_FAN_LV1);
            }
        }

        protected override void OnStart(string[] args)
        {
            io.BDSID_InstallDriver();
            io.BDSID_StartDriver();

            if(!io.Open())
            {
                Stop();
                return;
            }

            io.dell_smm_io(DellSMMIO.DELL_SMM_IO_DISABLE_FAN_CTL1, DellSMMIO.DELL_SMM_IO_NO_ARG);
            io.dell_smm_io(DellSMMIO.DELL_SMM_IO_DISABLE_FAN_CTL2, DellSMMIO.DELL_SMM_IO_NO_ARG);

            timer.Start();

            Timer_Elapsed(null, null);
        }

        protected override void OnStop()
        {
            timer.Stop();

            io.dell_smm_io(DellSMMIO.DELL_SMM_IO_ENABLE_FAN_CTL1, DellSMMIO.DELL_SMM_IO_NO_ARG);
            io.dell_smm_io(DellSMMIO.DELL_SMM_IO_ENABLE_FAN_CTL2, DellSMMIO.DELL_SMM_IO_NO_ARG);
            io.BDSID_Shutdown();
        }
    }
}
