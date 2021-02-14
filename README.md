# SaorCon
#### _Windows utility for Bose QC35 Headphones_
![SaorCon Screenshot](https://i.imgur.com/mXyAacB.jpg)

## Current Status
**Features:**
  * ANC Level Control
  * Battery Level Reporting
  * Concurrent-device support
  
**Possible Future Features:**
  * Timeout control
  * Device name control
  * Playback media information/control from other connected device
  * It's possible other Bose devices could work with this util with minor changes to our BT device query, but I haven't been able to test them. Feel free to try it out!
  
**Known Bugs:**
  * Occasionally the Windows Bluetooth stack won't let us receive RFCOMM data from connected devices, but only through C#. Connecting to the same device through equivalent code in C++ will allow receiving RFCOMM data, and future connections through C# will also work. ¯\\\_(ツ)\_/¯ Regardless, we're able to send data so the ANC level can still be controlled, we just won't know the current set ANC level or the battery level.
  * This utility is still in a very-early stage of development, so general stability issues/random crashes are probable.
