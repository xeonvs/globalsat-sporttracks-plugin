/*
 *  Globalsat/Keymaze SportTracks Plugin
 *  Copyright 2009 John Philip 
 * 
 *  This software may be used and distributed according to the terms of the
 *  GNU Lesser General Public License version 2 or any later version.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

using ZoneFiveSoftware.Common.Data.GPS;

namespace ZoneFiveSoftware.SportTracks.Device.Globalsat
{
    public class GlobalsatPacket : GhPacketBase
    {
        //public GlobalsatPacket() { }

        private static string InvalidOperation = "Invalid Operation";

        public class GlobalsatSystemInformation
        {
            public string UserName;
            public bool IsFemale;
            public int Age;
            public int WeightPounds;
            public int WeightKg;
            public int HeightInches;
            public int HeightCm;
            public DateTime BirthDate;

            public string DeviceName;
            public double Version;
            public string Firmware;
            public int WaypointCount;
            public int TrackpointCount;
            public int ManualRouteCount;
            public int PcRouteCount;
            public int CourseeCount;

            public GlobalsatSystemInformation(string deviceName, double version, string firmware,
                string userName, bool isFemale, int age, int weightPounds, int weightKg, int heightInches, int heightCm, DateTime birthDate,
                int waypointCount, int trackpointCount, int manualRouteCount, int pcRouteCount, int courseCount)
            {
                this.UserName = userName;
                this.IsFemale = isFemale;
                this.Age = age;
                this.WeightPounds = weightPounds;
                this.WeightKg = weightKg;
                this.HeightInches = heightInches;
                this.HeightCm = heightCm;
                this.BirthDate = birthDate;
                this.DeviceName = deviceName;
                this.Version = version;
                this.Firmware = firmware;
                this.WaypointCount = waypointCount;
                this.TrackpointCount = trackpointCount;
                this.ManualRouteCount = manualRouteCount;
                this.PcRouteCount = pcRouteCount;
                this.CourseeCount = courseCount;
            }

            public GlobalsatSystemInformation(string deviceName, string firmware,
                int waypointCount, int pcRouteCount)
            {
                this.DeviceName = deviceName;
                this.Firmware = firmware;
                this.WaypointCount = waypointCount;
                this.PcRouteCount = pcRouteCount;
            }
        }

        public class GlobalsatSystemConfiguration
        {
            public string UserName;
            public bool IsFemale;
            public int Age;
            public int WeightPounds;
            public int WeightKg;
            public int HeightInches;
            public int HeightCm;
            public DateTime BirthDate;

            public GlobalsatSystemConfiguration()
            {
            }

            public GlobalsatSystemConfiguration(string deviceName, double version, string firmware,
                string userName, bool isFemale, int age, int weightPounds, int weightKg, int heightInches, int heightCm, DateTime birthDate,
                int waypointCount, int trackpointCount, int manualRouteCount, int pcRouteCount)
            {
                this.UserName = userName;
                this.IsFemale = isFemale;
                this.Age = age;
                this.WeightPounds = weightPounds;
                this.WeightKg = weightKg;
                this.HeightInches = heightInches;
                this.HeightCm = heightCm;
                this.BirthDate = birthDate;
            }
        }

        public GlobalsatPacket GetWhoAmI()
        {
            InitPacket(CommandWhoAmI, 0);
            return this;
        }

        public GlobalsatPacket GetSystemInformation()
        {
            InitPacket(CommandGetSystemInformation, 0);
            return this;
        }

        public virtual GlobalsatSystemInformation ResponseGetSystemInformation()
        {
            string deviceName = ByteArr2String(0, 20 + 1);
            string firmware = ByteArr2String(21, 16 + 1);
            int waypointCount = (int)PacketData[38];
            int pcRouteCount = (int)PacketData[39];

            GlobalsatSystemInformation systemInfo = new GlobalsatSystemInformation(deviceName, firmware, waypointCount, pcRouteCount);

            return systemInfo;
        }

        public GlobalsatPacket GetSystemConfiguration()
        {
            InitPacket(CommandGetSystemConfiguration, 0);
            return this;
        }

        public virtual GlobalsatSystemConfiguration ResponseGetSystemConfiguration() { throw new Exception(InvalidOperation); }

        public GlobalsatPacket GetNextSection()
        {
            InitPacket(CommandGetNextSection, 0);
            return this;
        }

        public GlobalsatPacket GetTrackFileHeaders()
        {
            InitPacket(CommandGetTrackFileHeaders, 0);
            return this;
        }
        public GlobalsatPacket GetTrackFileSections(IList<Int16> trackPointIndexes)
        {
            this.InitPacket(CommandGetTrackFileSections, (Int16)(2 + trackPointIndexes.Count * 2));
            this.Write(0, (Int16)trackPointIndexes.Count);
            int offset = 2;
            foreach (Int16 index in trackPointIndexes)
            {
                this.Write(offset, index);
                offset += 2;
            }
            return this;
        }

        public GlobalsatPacket GetScreenshot()
        {
            InitPacket(CommandGetScreenshot, 0);
            return this;
        }

        public GlobalsatPacket GetWaypoints()
        {
            InitPacket(CommandGetWaypoints, 0);
            return this;
        }

        public virtual IList<GlobalsatWaypoint> ResponseGetWaypoints()
        {
            int nrWaypoints = PacketLength / (LocationLength + GetWptOffset);
            IList<GlobalsatWaypoint> waypoints = new List<GlobalsatWaypoint>(nrWaypoints);

            for (int i = 0; i < nrWaypoints; i++)
            {
                int index = i * (LocationLength + GetWptOffset);

                string waypointName = ByteArr2String(index, 6);
                int iconNr = (int)PacketData[index + 7];
                short altitude = ReadInt16(index + 8);
                int latitudeInt = ReadInt32(index + 10 + GetWptOffset);
                int longitudeInt = ReadInt32(index + 14 + GetWptOffset);
                double latitude = (double)latitudeInt / 1000000.0;
                double longitude = (double)longitudeInt / 1000000.0;

                GlobalsatWaypoint waypoint = new GlobalsatWaypoint(waypointName, iconNr, altitude, latitude, longitude);
                waypoints.Add(waypoint);
            }

            return waypoints;
        }

        public virtual GlobalsatPacket SendWaypoints(int MaxNrWaypoints, IList<GlobalsatWaypoint> waypoints)
        {
            int nrWaypointsLength = 2;
            int waypointNameLength = 6;

            int nrWaypoints = Math.Min(MaxNrWaypoints, waypoints.Count);

            Int16 totalLength = (Int16)(nrWaypointsLength + SendWptOffset + nrWaypoints * (LocationLength + SendWptOffset));
            this.InitPacket(CommandSendWaypoint, totalLength);

            int offset = 0;

            this.Write(offset, (short)nrWaypoints);
            offset += 2;

            offset += SendWptOffset; //pad -some only?

            int waypOffset = offset;
            for (int i = 0; i < nrWaypoints; i++)
            {
                GlobalsatWaypoint waypoint = waypoints[i];
                offset = waypOffset + i * (LocationLength + 2);

                this.Write(offset, waypointNameLength + 1, waypoint.WaypointName);
                offset += waypointNameLength+1;

                this.PacketData[offset] = (byte)waypoint.IconNr;
                offset++;

                this.Write(offset, waypoint.Altitude);
                offset += 2;

                offset += SendWptOffset; //pad?

                int latitude = (int)Math.Round(waypoint.Latitude * 1000000);
                int longitude = (int)Math.Round(waypoint.Longitude * 1000000);

                this.Write32(offset, latitude);
                this.Write32(offset + 4, longitude);
            }
            return this;
        }

        public virtual int GetSentWaypoints()
        {
            //TODO: Size check
            int nrSentWaypoints = this.ReadInt16(0);
            return nrSentWaypoints;
        }

        public virtual GlobalsatPacket DeleteAllWaypoints()
        {
            this.InitPacket(CommandDeleteWaypoints, 2);
            this.Write(0, (Int16)100);
            return this;
        }

        public virtual GlobalsatPacket DeleteWaypoints(int MaxNrWaypoints, IList<GlobalsatWaypoint> waypoints)
        {
            int nrWaypointsLength = 2;
            int waypointNameLength = 6;

            int nrWaypoints = Math.Min(MaxNrWaypoints, waypoints.Count);

            Int16 totalLength = (Int16)(nrWaypointsLength + nrWaypoints * 7);
            this.InitPacket(CommandDeleteWaypoints, totalLength);

            int offset = 0;

            this.Write(offset, (short)nrWaypoints);
            offset += 2;

            for (int i = 0; i < nrWaypoints && nrWaypoints < MaxNrWaypoints; i++)
            {
                string waypointName = waypoints[i].WaypointName;

                this.Write(offset, waypointNameLength + 1, waypointName);
                offset += waypointNameLength+1;
            }

            return this;
        }

        public virtual GlobalsatPacket SendRoute(GlobalsatRoute route) { throw new Exception(InvalidOperation); }
        public virtual GlobalsatPacket SetSystemInformation(byte[] data) { throw new Exception(InvalidOperation); }
        public virtual GlobalsatPacket SetSystemConfiguration(byte[] data) { throw new Exception(InvalidOperation); }
        public virtual IList<GlobalsatPacket> SendTrack(IGPSRoute gpsRoute) { throw new Exception(InvalidOperation); }

        public virtual System.Drawing.Bitmap ResponseScreenshot() { return GlobalsatBitmap.GetBitmap(this.ScreenBpp, this.ScreenSize, this.PacketData); }

    //Packetsizes
        protected virtual int LocationLength { get { return 18; } }
        protected virtual int GetWptOffset { get { return 0; } }
        protected virtual int SendWptOffset { get { return 0; } }
    }
}
