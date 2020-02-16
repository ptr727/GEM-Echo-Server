# Brultech GreenEye Energy Monitor Echo Server

This is a .NET Core console utility to help diagnose and echo [GEM](https://www.brultech.com/greeneye/) data packets.  

## License

[![GitHub](https://img.shields.io/github/license/ptr727/GEM-Echo-Server)](https://github.com/ptr727/GEM-Echo-Server/blob/master/LICENSE)

## Usage

- I am not publishing binaries, you need to compile your own, [follow](#build) the build instructions.
- Run `GEMEchoServer.exe` or `GEMEchoServer.exe [Listening Port] [Output CSV File]`.

## Notes

- See [greeneye-monitor](https://github.com/jkeljo/greeneye-monitor) for a Python implementation, this is the library used by [Home Assistant](https://www.home-assistant.io/integrations/greeneye_monitor/).
- GEM [Packet Format](https://www.brultech.com/software/files/downloadSoft/GEM-PKT_Packet_Format_2_1.pdf) specification.
- Brultech [downloads](https://www.brultech.com/software/files/getsoft/1/1) and configuration tools.

## GEM Configuration

- Make sure the GEM [firmare](https://www.brultech.com/software/files/checksn/3/1) and WIZ110SR TCP to Serial bridge [firmware](https://www.wiznet.io/product-item/wiz110sr/) is up to date.
  - This is an excessively cumbersome process due to the use of a TCP to serial bridge that sits between the GEM and the network.
  - If you can no longer login to the web console after updating the firmware, a manual [reset](http://www.brultech.com/community/viewtopic.php?f=3&t=799&view=previous) may be required.
  - Use the GEM network utility and set the baudrate to 19200 in preparation of the firmware updates.
    - Start at a point where you can click open and get info and get good results.
    - If you cannot get info, change the baudrate on the ethernet tab until you get good info.
    - Once you can read the info, change the baudrate to 19200 on the firmware tab, then change the baudrate on the ethernet tab to 19200.
    - Verify that you can open and read info at 19200.
  - Update the COM firmware, re-boot COM after update.
  - Update the ENG firmware.
  - Change the baudrate back to 115200, using the same process as changing to 19200, make sure you can get info at 115200.
- Use the GEM Network Utility to configure the GEM settings:
  - Ethernet Mode : Mixed
  - Local Port : 80
  - Remote Port : 8000
  - Server IP : Set to the address of the server that will receive the data
  - Packetsize Time : 10, Packetsize Size : 255, Idle Time : 3
- Configure the GEM web setup:
  - On the Packet Send tab, set the format to Bin48-Net-Time and the Packet Send Interval to 15s, or your choice of update frequency.
  - On the Data Post tab, enable all the sensors that are connected.
  - Make sure to exit setup mode to have the GEM return to client mode and send data.

## Build

Install [GIT](https://git-scm.com/download) and [.NET Core SDK 3.1](https://dotnet.microsoft.com/download).  
You could use [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/download), or simply compile from the console:
- Create a project directory.
- Initialize GIT.
- Pull the [reposity](https://github.com/ptr727/GEM-Echo-Server.git).
- Compile the code.

```shell
C:\Users\piete\source>md tmp
C:\Users\piete\source>cd tmp

C:\Users\piete\source\tmp>git init
Initialized empty Git repository in C:/Users/piete/source/tmp/.git/

C:\Users\piete\source\tmp>git pull https://github.com/ptr727/GEM-Echo-Server.git
remote: Enumerating objects: 32, done.
remote: Counting objects: 100% (32/32), done.
remote: Compressing objects: 100% (29/29), done.
                                                              Unpacking objects:  62% (20/32)
Unpacking objects: 100% (32/32), done.
From https://github.com/ptr727/GEM-Echo-Server
 * branch            HEAD       -> FETCH_HEAD

C:\Users\piete\source\tmp>
C:\Users\piete\source\tmp>dotnet build
Microsoft (R) Build Engine version 16.5.0-preview-20064-06+86d9494e4 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.
  Restore completed in 160.89 ms for C:\Users\piete\source\tmp\GEMEchoServer.csproj.
  You are using a preview version of .NET Core. See: https://aka.ms/dotnet-core-preview
...
Build succeeded.
...
    4 Warning(s)
    0 Error(s)

Time Elapsed 00:00:01.05

C:\Users\piete\source\tmp>cd bin\Debug\netcoreapp3.1
C:\Users\piete\source\tmp\bin\Debug\netcoreapp3.1>
C:\Users\piete\source\tmp\bin\Debug\netcoreapp3.1>GEMEchoServer.exe
Listening on port 8000...
0.0.0.0:8000 : Listening for connections
Press Enter to stop...
192.168.1.148:2065 : Connected
192.168.1.148:2065 : Received : 22
```
