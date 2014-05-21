XsgLib
======

.NET library for SCPI remote control of Agilent's X-Series Signal Generators (Signal Sources)

**Requirements**
* [Agilent IO Libraries Suite v16.3](http://www.home.agilent.com/en/pd-1985909/io-libraries-suite-162?&cc=US&lc=eng) - Collection of libraries and utility programs. The IO libraries (SICL, VISA, and VISA COM) enable instrument communication for a variety of development environments (Agilent VEE Pro, Microsoft Visual Studio, etc.) that are compatible with GPIB, USB, LAN, RS-232, PXI, AXIe, and VXI test instruments from a variety of manufacturers. Several utility programs help you quickly and easily connect your instruments to your PC.

**Instruments Supported**
* MXG (N5182A/N5182B)
* EXG (N5172B) *untested*
* Untested, but should be compatible with other XSG models 

Examples
-----
**Establish connection & query IDN header**
``` C#
MXG Mxg = new MXG();
Mxg.Connect("tcpip0::IpAddress::instr"); // Replace with your instrument's VISA address

// Populate the Mxg.IDN fields by downloading info from instrument
Mxg.DownloadHeader();
Console.WriteLine(Mxg.IDN.Company);
Console.WriteLine(Mxg.IDN.Firmware);
Console.WriteLine(Mxg.IDN.Model);
Console.WriteLine(Mxg.IDN.Serial);
```

**Upload and Play W-CDMA waveform**

This example shows how one would upload a waveform onto an MXG and then play it
``` C#
MXG Mxg = new MXG();
Mxg.Connect("tcpip0::IpAddress::instr"); // Replace with your instrument's VISA address

Mxg.Reset();

// Make sure source is turned off
Mxg.ArbOutput = false;
Mxg.ModulationOutput = false;
Mxg.RfOutput = false;

string WaveformPath = "..\\MyWaveforms\\WCDMA_TM1_64DPCH_DL.WFM"; // Relative path to where waveform is located
string WaveformName = WaveformPath.Split('\\').Last(); // Parse out waveform name: "WCDMA_TM1_64DPCH_DL.WFM"

// Download waveform to MXG if it is not found in either volatile or non-volatile memory
if (!Mxg.HasWaveform(WaveformName))
    Mxg.DownloadArbFile("SNVWFM", WaveformName, WaveformPath, 10000);

// Load the waveform if it isn't already loaded
if (!Mxg.IsWaveformLoaded(WaveformName))
    Mxg.LoadWaveform(WaveformName);

// If the waveform is large, it may take a while to upload
// Make sure you set your timeout large enough to allow for transfer
// In this case, we are setting it to 10 seconds
Mxg.WaitForOperationComplete(10000);
Mxg.SelectWaveform = WaveformName;
Mxg.WaitForOperationComplete(5000);

// Set: Frequency = 1 GHz, Power = -10 dBm
Mxg.Frequency = 1e9;
Mxg.Power = -10;    

// Now that everything is configured, turn on the source
Mxg.ArbOutput = true;
Mxg.ModulationOutput = true;
Mxg.RfOutput = true;

```
