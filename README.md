# wmi_sample
 MDM Bridge WMI sample

# Installation steps

1- Open project in Visual Studio

2- Build project

3- Open Visual Studio dev console (tools menu)

4- Install service with: installutil .\WindowsService1\bin\Debug\WindowsService1.exe

5- Run project (form app1)

# Usage

Right-click on tray icon (that appears on step 5 above)

You can change camera access state

Other menu entries will send command to server, but will not perform any action (see code for remaining TODOs)

You can simulate the change of password minimum length (popup dialog), command is sent to server, no action performed (see TODO)

You can reboot the computer, untested.

