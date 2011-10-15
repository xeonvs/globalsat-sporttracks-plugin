/*
Copyright (C) 2010 Zone Five Software

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 3 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library. If not, see <http://www.gnu.org/licenses/>.
 */
// Author: Aaron Averill


using System;
using System.Collections.Generic;
using System.Text;

using System.IO.Ports;
using ZoneFiveSoftware.Common.Visuals;
using ZoneFiveSoftware.Common.Visuals.Fitness;
using ZoneFiveSoftware.Common.Data.Fitness;

namespace ZoneFiveSoftware.SportTracks.Device.Globalsat
{
    abstract class GhDeviceBase
    {
        public GhDeviceBase(DeviceConfigurationInfo configInfo)
        {
            this.configInfo = configInfo;
        }

        public GhDeviceBase(FitnessDevice_Globalsat fitDev)
        {
            this.configInfo = fitDev.DefaultConfig;
            foreach (IConfiguredDevice c in Plugin.Instance.Application.SystemPreferences.FitnessDevices)
            {
                if (c.Id == fitDev.Id)
                {
                    this.configInfo = DeviceConfigurationInfo.Parse(configInfo, c.Configuration);
                }
            }
        }

        //Import kept in separate structure
        public virtual ImportJob ImportJob(string sourceDescription, IJobMonitor monitor, IImportResults importResults)
        {
            return null;
        }

        public string Open()
        {
            if (port == null)
            {
                OpenPort(configInfo.ComPorts);
            }
            return devId;
        }

        public void Close()
        {
            if (port != null)
            {
                port.Close();
                port = null;
            }
        }

        public SerialPort Port
        {
            get { return port; }
        }

        public void CopyPort(GhDeviceBase b)
        {
            this.port = b.port;
            this.devId = b.devId;
        }

        protected string ValidGlobalsatPort(SerialPort port)
        {
            port.ReadTimeout = 1000;
            port.Open();
            byte[] packet = GhPacketBase.GetWhoAmI();
            //Get the commandid, to match to returned packet
            byte commandId = GhPacketBase.SendPacketCommandId(packet);
            GhPacketBase.Response response = SendPacket(port, packet);
            string res = "";
            if (response.CommandId == commandId && response.PacketLength > 1)
            {
                string devId = GhPacketBase.ByteArr2String(response.PacketData, 0, 8);
                if (!string.IsNullOrEmpty(devId))
                {
                    if (configInfo.AllowedIds == null)
                    {
                        res = devId;
                    }
                    else
                    {
                        foreach (string aId in configInfo.AllowedIds)
                        {
                            if (devId.StartsWith(aId))
                            {
                                res = devId;
                                break;
                            }
                        }
                    }
                }
            }
            return res;
        }

        protected GhPacketBase.Response SendPacket(SerialPort port, byte[] packet)
        {
            return SendPacket(port, packet, configInfo);
        }
        protected static GhPacketBase.Response SendPacket(SerialPort port, byte[] packet, DeviceConfigurationInfo configInfo)
        {
            byte sendCommandId = GhPacketBase.SendPacketCommandId(packet);
            if (sendCommandId == GhPacketBase.CommandGetScreenshot)
            {
                port.ReadTimeout = 3000;
            }
            try
            {
                port.Write(packet, 0, packet.Length);
            }
            catch (Exception e)
            {
                throw e;
            }

            GhPacketBase.Response received = new GhPacketBase.Response();

            received.CommandId = (byte)port.ReadByte();
            int hiPacketLen = port.ReadByte();
            int loPacketLen = port.ReadByte();
            received.PacketLength = (Int16)((hiPacketLen << 8) + loPacketLen);
            if (received.PacketLength > configInfo.MaxPacketPayload)
            {
                throw new Exception(CommonResources.Text.Devices.ImportJob_Status_ImportError);
            }
            received.PacketData = new byte[received.PacketLength];
            try
            {
                for (Int16 b = 0; b < received.PacketLength; b++)
                {
                    received.PacketData[b] = (byte)port.ReadByte();
                }
                received.Checksum = (byte)port.ReadByte();
            }
            catch(Exception e)
            {
            //TODO: DEBUG timeout often occurs for GH-505
                throw e;
            }
            if (!GhPacketBase.ValidResponseCrc(received))
            {
                throw new Exception(CommonResources.Text.Devices.ImportJob_Status_ImportError);
            }
            if (received.CommandId != sendCommandId &&
                !((received.CommandId == GhPacketBase.CommandGetTrackFileSections ||
                received.CommandId == GhPacketBase.CommandId_FINISH) &&
                sendCommandId == GhPacketBase.CommandGetNextSection))
            {
                throw new Exception(CommonResources.Text.Devices.ImportJob_Status_ImportError);
            }
            return received;
        }

        protected virtual void OpenPort(IList<string> comPorts)
        {
            if (comPorts == null || comPorts.Count == 0)
            {
                comPorts = new List<string>();

                if (Environment.OSVersion.Platform == PlatformID.Win32Windows || Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    for (int i = 1; i <= 30; i++)
                    {
                        comPorts.Add("COM" + i);
                    }
                }
                else if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    /* Linux */
                    for (int i = 0; i <= 30; i++)
                    {
                        comPorts.Add("/dev/ttyUSB" + i);
                        comPorts.Add("/dev/ttyACM" + i);
                    }
                    /* OSX */
                    for (int i = 0; i <= 30; i++)
                    {
                        comPorts.Add("/dev/tty.usbserial" + i);
                    }
                }
            }

            Exception lastException = new Exception();
            foreach (int baudRate in configInfo.BaudRates)
            {
                foreach (string comPort in comPorts)
                {
                    port = null;
                    try
                    {
                        port = new SerialPort(comPort, baudRate);
                        string id = ValidGlobalsatPort(port);
                        if (!string.IsNullOrEmpty(id))
                        {
                            this.devId = id;
                            return;
                        }
                        else if (port != null)
                        {
                            port.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        if (port != null)
                        {
                            port.Close();
                        }
                        //info about the last exception only
                        lastException = e;
                    }
                }
            }
            string lastExceptionText = System.Environment.NewLine + System.Environment.NewLine + lastException.Message;
            throw new Exception(CommonResources.Text.Devices.ImportJob_Status_CouldNotOpenDeviceError+
            lastExceptionText);
        }

        public DeviceConfigurationInfo configInfo;
        private SerialPort port = null;
        private string devId = "";
    }
}
