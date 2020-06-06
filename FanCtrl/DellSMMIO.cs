using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DellFanControl;

//Parts taken from https://github.com/marcharding/DellFanControl

namespace FanCtrl
{
    public class DellSMMIO
    {
        IntPtr hDriver;

        public const uint DELL_SMM_IO_FAN1 = 0;
        public const uint DELL_SMM_IO_FAN2 = 1;

        public const uint DELL_SMM_IO_GET_POWER_STATUS = 0x0069;
        public const uint DELL_SMM_IO_POWER_STATUS_AC = 0x05;
        public const uint DELL_SMM_IO_POWER_STATUS_BATTERY = 0x01;

        public const uint DELL_SMM_IO_GET_SENSOR_TEMP = 0x10a3;
        public const uint DELL_SMM_IO_SENSOR_CPU = 0; // Probably Core 1
        public const uint DELL_SMM_IO_SENSOR_GPU = 5; // ?? how many sensors
        public const uint DELL_SMM_IO_SENSOR_MAX_TEMP = 127;

        public const uint DELL_SMM_IO_SET_FAN_LV = 0x01a3;
        public const uint DELL_SMM_IO_GET_FAN_LV = 0x00a3;
        public const uint DELL_SMM_IO_GET_FAN_RPM = 0x02a3;

        public const uint DELL_SMM_IO_FAN_LV0 = 0;
        public const uint DELL_SMM_IO_FAN_LV1 = 1;
        public const uint DELL_SMM_IO_FAN_LV2 = 2;

        public const uint DELL_SMM_IO_DISABLE_FAN_CTL1 = 0x30a3;
        public const uint DELL_SMM_IO_ENABLE_FAN_CTL1 = 0x31a3;
        public const uint DELL_SMM_IO_DISABLE_FAN_CTL2 = 0x34a3;
        public const uint DELL_SMM_IO_ENABLE_FAN_CTL2 = 0x35a3;
        public const uint DELL_SMM_IO_NO_ARG = 0x0;

        public Boolean BDSID_InstallDriver()
        {
            BDSID_RemoveDriver();

            IntPtr hService = new IntPtr();

            IntPtr hSCManager = Interop.OpenSCManager(null, null, (uint)Interop.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (hSCManager != IntPtr.Zero)
            {
                hService = Interop.CreateService(
                    hSCManager,
                    "BZHDELLSMMIO",
                    "BZHDELLSMMIO",
                    Interop.SERVICE_ALL_ACCESS,
                    Interop.SERVICE_KERNEL_DRIVER,
                    Interop.SERVICE_DEMAND_START,
                    Interop.SERVICE_ERROR_NORMAL,
                    this.getDriverPath(),
                    null,
                    null,
                    null,
                    null,
                    null
                );

                Interop.CloseServiceHandle(hSCManager);

                if (hService == IntPtr.Zero)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            Interop.CloseServiceHandle(hService);

            return true;
        }

        ushort[] badSensors = new ushort[DELL_SMM_IO_SENSOR_GPU+1];

        public uint MaxTemperature()
        {
            uint result = 0;

            for(uint i = 0; i <= DELL_SMM_IO_SENSOR_GPU; i++)
            {
                uint current = dell_smm_io(DELL_SMM_IO_GET_SENSOR_TEMP, i) & 0xff;

                if (current == 0 || current > DELL_SMM_IO_SENSOR_MAX_TEMP)
                    badSensors[i] = 15;
                else if (badSensors[i] > 0)
                    badSensors[i]--;
                else
                    result = Math.Max(result, current);
            }

            return result;
        }

        public bool IsOnAC()
        {
            return dell_smm_io(DELL_SMM_IO_GET_POWER_STATUS, DELL_SMM_IO_NO_ARG) == DELL_SMM_IO_POWER_STATUS_AC;
        }

        public bool Open()
        {
            hDriver = Interop.CreateFile(@"\\.\BZHDELLSMMIO",
                    Interop.GENERIC_READ | Interop.GENERIC_WRITE,
                    Interop.FILE_SHARE_READ | Interop.FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    Interop.OPEN_EXISTING,
                    Interop.FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

            return hDriver != IntPtr.Zero;
        }

        public Boolean BDSID_StartDriver()
        {
            Boolean bResult;
            IntPtr hSCManager = Interop.OpenSCManager(null, null, (uint)Interop.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);
            if (hSCManager != IntPtr.Zero)
            {
                IntPtr hService = Interop.OpenService(hSCManager, "BZHDELLSMMIO", Interop.SERVICE_ALL_ACCESS);

                Interop.CloseServiceHandle(hSCManager);

                if (hService != IntPtr.Zero)
                {

                    bResult = Interop.StartService(hService, 0, null); // || GetLastError() == ERROR_SERVICE_ALREADY_RUNNING;
                    Interop.CloseServiceHandle(hService);
                }
                else
                {
                    return false;
                }

            }
            else
            {
                return false;
            }

            return bResult;
        }

        public uint dell_smm_io_get_cpu_temperature()
        {
            return dell_smm_io(DELL_SMM_IO_GET_SENSOR_TEMP, DELL_SMM_IO_SENSOR_CPU);
        }

        public uint dell_smm_io_get_gpu_temperature()
        {
            return dell_smm_io(DELL_SMM_IO_GET_SENSOR_TEMP, DELL_SMM_IO_SENSOR_GPU);
        }

        public void dell_smm_io_set_fan_lv(uint fan_no, uint lv)
        {
            uint arg = (lv << 8) | fan_no;
            dell_smm_io(DELL_SMM_IO_SET_FAN_LV, arg);
        }

        public uint dell_smm_io_get_fan_lv(uint fan_no)
        {
            return dell_smm_io(DELL_SMM_IO_SET_FAN_LV, fan_no);
        }

        public uint dell_smm_io(uint cmd, uint data)
        {
            Process.GetCurrentProcess().ProcessorAffinity = (System.IntPtr)1;

            Interop.SMBIOS_PKG cam = new Interop.SMBIOS_PKG
            {
                cmd = cmd,
                data = data,
                stat1 = 0,
                stat2 = 0
            };

            uint IOCTL_BZH_DELL_SMM_RWREG = Interop.CTL_CODE(Interop.FILE_DEVICE_BZH_DELL_SMM, Interop.BZH_DELL_SMM_IOCTL_KEY, 0, 0);

            uint result_size = 0;

            bool status_dic = Interop.DeviceIoControl(this.hDriver,
                IOCTL_BZH_DELL_SMM_RWREG,
                ref cam,
                (uint)Marshal.SizeOf(cam),
                ref cam,
                (uint)Marshal.SizeOf(cam),
                ref result_size,
                IntPtr.Zero);

            if (status_dic == false)
            {
                return 0;
            }
            else
            {
                uint foo = cam.cmd;

                return foo;
            }
        }

        public Boolean BDSID_RemoveDriver()
        {
            UInt32 dwBytesNeeded;
            UInt32 cbBufSize;

            bool bResult;

            BDSID_StopDriver();

            IntPtr hSCManager = Interop.OpenSCManager(null, null, (uint)Interop.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);

            if (hSCManager == IntPtr.Zero)
            {
                return false;
            }

            IntPtr hService = Interop.OpenService(hSCManager, "BZHDELLSMMIO", Interop.SERVICE_ALL_ACCESS);
            Interop.CloseServiceHandle(hSCManager);

            if (hService == IntPtr.Zero)
            {
                return false;
            }

            bResult = Interop.QueryServiceConfig(hService, IntPtr.Zero, 0, out dwBytesNeeded);

            if (Interop.GetLastError() == Interop.ERROR_INSUFFICIENT_BUFFER)
            {
                cbBufSize = dwBytesNeeded;
                IntPtr ptr = Marshal.AllocCoTaskMem((int)dwBytesNeeded);

                bResult = Interop.QueryServiceConfig(hService, ptr, cbBufSize, out dwBytesNeeded);

                if (!bResult)
                {
                    Marshal.FreeCoTaskMem(ptr);
                    Interop.CloseServiceHandle(hService);
                    return bResult;
                }

                Interop.QUERY_SERVICE_CONFIG pServiceConfig = (Interop.QUERY_SERVICE_CONFIG)Marshal.PtrToStructure(ptr, typeof(Interop.QUERY_SERVICE_CONFIG));

                // If service is set to load automatically, don't delete it!
                if (pServiceConfig.dwStartType == Interop.SERVICE_DEMAND_START)
                {
                    bResult = Interop.DeleteService(hService);
                }

                Marshal.FreeCoTaskMem(ptr);
            }

            Interop.CloseServiceHandle(hService);

            return bResult;
        }

        public Boolean BDSID_StopDriver()
        {

            Interop.SERVICE_STATUS serviceStatus = new Interop.SERVICE_STATUS();

            IntPtr hSCManager = Interop.OpenSCManager(null, null, (uint)Interop.SCM_ACCESS.SC_MANAGER_ALL_ACCESS);

            if (hSCManager != IntPtr.Zero)
            {
                IntPtr hService = Interop.OpenService(hSCManager, "BZHDELLSMMIO", Interop.SERVICE_ALL_ACCESS);

                Interop.CloseServiceHandle(hSCManager);

                if (hService != IntPtr.Zero)
                {
                    Boolean bResult = Interop.ControlService(hService, Interop.SERVICE_CONTROL.STOP, ref serviceStatus);
                    Interop.CloseServiceHandle(hService);
                }
                else
                    return false;
            }
            else
                return false;

            return true;
        }

        public void Close()
        {
            IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            if (hDriver != INVALID_HANDLE_VALUE)
            {
                Interop.CloseHandle(this.hDriver);
            }
        }
        public Boolean BDSID_Shutdown()
        {
            Close();
            return BDSID_RemoveDriver();
        }

        public string getDriverPath()
        {
            return System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\bzh_dell_smm_io_x64.sys";
        }
    }
}
