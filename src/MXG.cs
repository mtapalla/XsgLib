/*
 ****************************************************************************
 * Author: Marc Tapalla
 * Email: marc.tapalla@gmail.com
 *   
 * Library for SCPI remote control of Agilent's X-Series Signal Generators.
 * 
 * Instruments Supported:
 *  MXG (N5182A/N5182B)
 *  EXG (N5172B) *untested*
 *  Untested, but should be compatible with other XSG models 
 * 
 ****************************************************************************
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using XsgLib;

namespace XsgLib
{
    public class MXG : Instrument
    {
        public MXG() : base() { }

        public void DownloadArbFile(string Catalog, string wfmName, string wfmFullPathName, int timeoutMs = 5000)
        {
            // MXG can only store file names <= 23 characters
            if (wfmName.Length > 23)
                throw new Exception("File name too long (>23 characters)");    

            byte[] data = FileUtil.ReadBinaryFile(wfmFullPathName);

            int digits = data.Length.ToString().Length;

            Catalog = Catalog.TrimEnd(':');

            string command = "MEM:DATA \"" + Catalog + ":" + wfmName + "\",";

            ScpiWriteIeeeBlock(command, data);
            WaitForOperationComplete(timeoutMs);
        }

        public void LoadWaveform(string waveformName)
        {
            ScpiCommand(String.Format("MEM:COPY \"SNVWFM:{0}\", \"SWFM1:{0}\"", waveformName));
        }

        public string GetMemoryCatalog(string CatalogName)
        {
            return ScpiQuery("MMEM:CAT? " + "'" + CatalogName + "'");
        }

        public bool IsWaveformLoaded(string Waveform)
        {
            string Catalog = GetMemoryCatalog("WFM1");
            return Catalog.Contains(Waveform.ToUpper());
        }

        public bool HasWaveform(string WaveformName)
        {
            WaveformName = WaveformName.ToUpper();
            return IsInVolatileMemory(WaveformName) || IsInNonvolatileMemory(WaveformName);
        }

        public string SelectWaveform
        {
            set
            {
                ScpiCommand("SOUR:RADio:ARB:WAVeform \"" + value + "\"");
            }
        }
        // Volatile memory = all waveforms on MXG that are LOADED
        public bool IsInVolatileMemory(string WaveformName)
        {
            return IsInCatalog("SWFM1", WaveformName, -1);
        }

        // Non-volatile memory = all waveforms on MXG
        public bool IsInNonvolatileMemory(string WaveformName)
        {
            return IsInCatalog("SNVWFM", WaveformName, -1);
        }

        public bool IsInCatalog(string catalogName, string waveformName, long waveformSize = -1)
        {
            string catalogStr = GetMemoryCatalog(catalogName);

            string[] delimiter = new string[] { "\",\"" };
            string[] tokens = catalogStr.Split(delimiter, StringSplitOptions.RemoveEmptyEntries);

            // Clean up the first token, because the MXG returns some extra data at the beginning
            tokens[0] = tokens[0].Substring(tokens[0].IndexOf('"') + 1);

            // Clean up the last token
            tokens[tokens.Length - 1] = CleanScpiQuery(tokens.Last());
            foreach (string token in tokens)
            {
                string[] parsed = token.Split(',');

                string catWfmName = parsed[0];
                long catWfmSize = long.Parse(parsed[2]);

                if (waveformSize > 0)
                {
                    // Check both waveform name & waveform size match
                    if (catWfmName.ToLower() == waveformName.ToLower() && catWfmSize == waveformSize)
                        return true;
                }
                else
                {
                    // We only want to check the waveform name
                    if (catWfmName.ToLower() == waveformName.ToLower())
                        return true;
                }
            }

            return false;
        }

        public bool ArbOutput
        {
            set
            {
                ScpiCommand("RAD:ARB " + (value ? "1" : "0"));
            }
        }

        public bool ModulationOutput
        {
            set
            {
                ScpiCommand("OUTP:MOD " + (value ? "1" : "0"));
            }
        }

        public bool RfOutput
        {
            set
            {
                ScpiCommand("OUTP " + (value ? "1" : "0"));
            }
        }

        public double Frequency
        {
            set
            {
                ScpiCommand("FREQ " + value);
            }
        }

        public double Power
        {
            set
            {
                ScpiCommand("POW " + value);
            }
        }
    }
}
