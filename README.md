# FanCtrl
This is a windows service able to manage fan speeds on certain Dell laptops using SMM I/O calls. This has been tested on a Dell G5.
This makes use of the driver from [Aaron Kelley](https://github.com/AaronKelley/DellFanCmd) and was inspired by [this project](https://github.com/marcharding/DellFanControl).


# How it works
This is a service that will load at windows startup. When it starts it will load the smm driver and manage your fans. At system shutdown or service stop the stock regulation will be re-enabled and the smm driver removed.

### Algorithm
The fans will stop under 42 °C, run at 50% over 45 °C (30 seconds cooldown) and go full speed over 65 °C (5 seconds cooldown). You can customize this in `FanCtrl.cs`.
My goal was to keep my keyboard cool (I've got fairy fingers) without having to deal with SpeedFan.

# Installation

### Enable cross-signed drivers.
    REG ADD "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\CI\Policy" /V "UpgradedSystem" /t REG_DWORD /D 0 /F 

### Reboot
Self explanatory.

### Install
Create a directory, copy the release files in it and run:

    %windir%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe FanCtrl.exe

### Uninstall
    %windir%\Microsoft.NET\Framework64\v4.0.30319\InstallUtil.exe /u FanCtrl.exe

