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
using System.Xml.Serialization;
using Ivi.Visa.Interop;

namespace XsgLib
{
    public abstract class Instrument
    {
        private ResourceManager resourceManager;
        private FormattedIO488 io;

        public enum DataFormatChoices { ASCII, INT_32, REAL_32, REAL_64 };


        public Header IDN = new Header();
        #region Accessors

        public int Timeout
        {
            get
            {
                return this.io.IO.Timeout;
            }
            set
            {
                this.io.IO.Timeout = value;
            }
        }

        public int getTimeout()
        {
            return this.io.IO.Timeout;
        }

        public int InputBufferSize
        {
            set
            {
                this.io.SetBufferSize(BufferMask.IO_IN_BUF, value);
            }
        }

        private DataFormatChoices dataFormat = DataFormatChoices.ASCII;
        public DataFormatChoices DataFormat
        {
            get { return dataFormat; }
            set
            {
                dataFormat = value;
                string format = value.ToString().Replace("_", ",");
                ScpiCommand("FORM:DATA " + format);
            }
        }

        private string address;
        public string Address
        {
            get
            {
                return this.address;
            }
            set
            {
                this.address = value;
            }
        }

        #endregion Accessors

        #region Methods
        public virtual void Connect(string inputAddress)
        {
            this.resourceManager = new ResourceManager();
            this.io = new FormattedIO488();

            this.Address = inputAddress;

            try
            {
                this.io.IO = (IMessage)resourceManager.Open(inputAddress, AccessMode.NO_LOCK, 2000, "");
                Timeout = 2000;
            }
            catch
            {
                throw new Exception("* Error connecting to: " + inputAddress);
            }
        }

        public static string CleanScpiQuery(string s)
        {
            return s.Trim(new char[] { '\"', ' ', '\n', '\r' }).Trim();
        }

        public void Reset()
        {
            ScpiCommand("*RST");
            ClearErrors();
        }

        public void ClearErrors()
        {
            ScpiCommand("*CLS");
        }

        public void DownloadHeader()
        {
            /*
             * Example IDN Strings:
             *
             * Agilent Technologies,N9020A,MY52091380,A.13.50_R0010
             * Agilent Technologies, N5182A, US51990230, A.01.86
             */

            ScpiCommand("*IDN?");
            string FullIDN = ReadString();

            string[] ParsedIDN = FullIDN.Split(',');

            for (int i = 1; i < ParsedIDN.Length; i++)
            {
                ParsedIDN[i] = ParsedIDN[i].Replace(" ", "");
            }

            IDN.Company = ParsedIDN[0];
            IDN.Model = ParsedIDN[1];
            IDN.Serial = ParsedIDN[2];
            IDN.Firmware = ParsedIDN[3];
        }

        public void Close()
        {
            try
            {
                if (this.io.IO.LockState == AccessMode.EXCLUSIVE_LOCK)
                    this.io.IO.UnlockRsrc();
                this.io.IO.Clear();
                this.io.IO.Close();
            }
            catch (Exception e)
            {
                throw new Exception("* Error closing IO " + IDN.Model + "," + IDN.Serial + ": " + e.Message);
            }
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(this.io);
            }
            catch (Exception e)
            {
                throw new Exception("* Error releasing IO " + IDN.Model + "," + IDN.Serial + ": " + e.Message);
            }
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(this.resourceManager);
            }
            catch (Exception e)
            {
                throw new Exception("* Error releasing ResourceManager " + IDN.Model + "," + IDN.Serial + ": " + e.Message);
            }
        }

        public FormattedIO488 getIO()
        {
            return this.io;
        }

        public void WaitForOperationComplete()
        {
            ScpiCommand("*OPC?");
            ReadBinary();
        }

        public void WaitForOperationComplete(int timeoutMs = 2000)
        {
            int orgTimeout = Timeout;

            if (orgTimeout < timeoutMs) // Don't decrease timeout
                Timeout = timeoutMs;

            WaitForOperationComplete();

            if (orgTimeout < timeoutMs) // Don't decrease timeout
                Timeout = orgTimeout;
        }

        public void ScpiCommand(string command, object data = null)
        {
            if (data == null)
                ScpiWriteString(command);
            else
                ScpiWriteIeeeBlock(command, data);
        }

        public void ScpiWriteString(string command)
        {
            this.io.WriteString(command, true);
        }

        public void ScpiWriteIeeeBlock(string command, object data)
        {
            this.io.WriteIEEEBlock(command, data, true);
        }

        public string ScpiQuery(string command, object data = null)
        {
            if (data == null)
                return ScpiQueryString(command);
            else
                return ScpiQueryIeeeBlock(command, data);
        }

        public string ScpiQueryString(string command)
        {
            this.io.WriteString(command, true);
            return ReadString();
        }

        public dynamic ScpiQueryIeeeBlock(string command, object data = null)
        {
            if (data == null)
                this.io.WriteString(command, true);
            else
                this.io.WriteIEEEBlock(command, data, true);

            return ReadBlock();
        }

        public void WriteByteArray(string command, byte[] data)
        {
            io.WriteIEEEBlock(command, data, true);
        }

        public byte[] ReadBytes()
        {
            return (byte[])this.io.ReadIEEEBlock(IEEEBinaryType.BinaryType_UI1);
        }

        public dynamic ReadBlock()
        {
            if (DataFormat.Equals(DataFormatChoices.INT_32))
                return (int[])this.io.ReadIEEEBlock(IEEEBinaryType.BinaryType_I4, true, true);
            else if (DataFormat.Equals(DataFormatChoices.REAL_32))
                return (float[])this.io.ReadIEEEBlock(IEEEBinaryType.BinaryType_R4, true, true);
            else if (DataFormat.Equals(DataFormatChoices.REAL_64))
                return (double[])this.io.ReadIEEEBlock(IEEEBinaryType.BinaryType_R8, true, true);
            else
                return (string)this.io.ReadString();
        }

        public dynamic ReadBinary()
        {
            if (DataFormat.Equals(DataFormatChoices.INT_32))
                return (Int32)this.io.ReadNumber(IEEEASCIIType.ASCIIType_I4, true);
            else if (DataFormat.Equals(DataFormatChoices.REAL_32))
                return (float)this.io.ReadNumber(IEEEASCIIType.ASCIIType_R4, true);
            else if (DataFormat.Equals(DataFormatChoices.REAL_64))
                return (float)this.io.ReadNumber(IEEEASCIIType.ASCIIType_R8, true);
            else
                return (string)this.io.ReadString();
        }

        public string ReadString()
        {
            return this.io.ReadString();
        }

        public string ReadString(int timeoutMs)
        {
            int orgTimeout = Timeout;

            if (orgTimeout < timeoutMs) // Don't decrease timeout
                Timeout = timeoutMs;

            string data = this.io.ReadString();

            if (orgTimeout < timeoutMs) // Don't decrease timeout
                Timeout = orgTimeout;

            return data;
        }

        public Int32[] ReadI32Block()
        {
            return (Int32[])this.io.ReadIEEEBlock(IEEEBinaryType.BinaryType_I4, false, false);
        }

        public float[] ReadR32Block()
        {
            return (float[])this.io.ReadIEEEBlock(IEEEBinaryType.BinaryType_R4, false, false);
        }

        #endregion

        #region Helpers

        public struct Header
        {
            private string company;
            private string model;
            private string serial;
            private string firmware;

            public string Company { get { return company; } set { company = value; } }

            public string Model { get { return model; } set { model = value; } }

            public string Serial { get { return serial; } set { serial = value; } }

            public string Firmware { get { return firmware; } set { firmware = value; } }
        }

        #endregion HELPERS
    }
}