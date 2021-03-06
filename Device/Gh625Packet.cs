/*
Copyright (C) 2010 Zone Five Software

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2 of the License, or (at your option) any later version.

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

namespace ZoneFiveSoftware.SportTracks.Device.Globalsat
{
    public class Gh625Packet : GlobalsatPacket
    {
        public Gh625Packet(FitnessDevice_Globalsat device) : base(device) { }

        public class Header625M : GhPacketBase.Header
        {
            public Int16 TotalCalories;
            public double MaximumSpeed;
            public byte MaximumHeartRate;
            public byte AverageHeartRate;
        }

        public class TrackFileHeader625M : Header625M
        {
            public Int16 TrackPointIndex;
        }

        public class TrackFileSection625M : Header625M
        {
            public Int16 StartPointIndex;
            public Int16 EndPointIndex;
            public IList<TrackPoint> TrackPoints = new List<TrackPoint>();
            public IList<Lap> Laps = new List<Lap>();
        }

        public IList<TrackFileHeader625M> UnpackTrackHeaders()
        {
            int numHeaders = this.PacketLength / TrackHeaderLength;
            IList<TrackFileHeader625M> headers = new List<TrackFileHeader625M>();
            for (int i = 0; i < numHeaders; i++)
            {
                int trackStart = i * TrackHeaderLength;
                TrackFileHeader625M header = new TrackFileHeader625M();
                ReadHeader(header, trackStart);
                header.TrackPointCount = ReadInt16(trackStart + 25);
                header.TrackPointIndex = ReadInt16(trackStart + 27);
                headers.Add(header);
            }
            return headers;
        }

        public TrackFileSection625M UnpackTrackSectionLaps()
        {
            if (this.PacketLength < TrackHeaderLength) return null;

            TrackFileSection625M section = new TrackFileSection625M();
            ReadHeader(section, 0);
            section.LapCount = this.PacketData[6];
            section.TrackPointCount = ReadInt16(25);
            section.StartPointIndex = ReadInt16(27);
            section.EndPointIndex = ReadInt16(29);

            int offset = TrackHeaderLength;
            while (offset <= this.PacketLength - TrackLapLength)
            {
                Lap lap = new Lap();
                lap.EndTime = TimeSpan.FromSeconds(FromGlobTime(ReadInt32(offset)));
                lap.LapTime = TimeSpan.FromSeconds(FromGlobTime(ReadInt32(offset + 4)));
                lap.LapDistanceMeters = ReadInt32(offset + 8);
                lap.LapCalories = ReadInt16(offset + 12);
                lap.MaximumSpeed = FromGlobSpeed(ReadInt16(offset + 14));
                lap.MaximumHeartRate = this.PacketData[offset + 16];
                lap.AverageHeartRate = this.PacketData[offset + 17];
                //lap.StartPointIndex = ReadInt16(18);
                //lap.EndPointIndex = ReadInt16(20);
                section.Laps.Add(lap);
                offset += TrackLapLength;
            }
            return section;
        }

        public TrackFileSection625M UnpackTrackSection()
        {
            if (this.PacketLength < TrackHeaderLength) return null;

            TrackFileSection625M section = new TrackFileSection625M();
            ReadHeader(section, 0);
            section.TrackPointCount = ReadInt16(25);
            section.StartPointIndex = ReadInt16(27);
            section.EndPointIndex = ReadInt16(29);

            int offset = TrackHeaderLength;
            while (offset <= this.PacketLength - TrackPointLength)
            {
                TrackPoint point = new TrackPoint();
                point.Latitude = (double)ReadLatLon(offset);
                point.Longitude = (double)ReadLatLon(offset + 4);
                point.Altitude = ReadInt16(offset + 8);
                point.Speed = FromGlobSpeed(ReadInt16(offset + 10));
                point.HeartRate = this.PacketData[offset + 12];
                point.IntervalTime = FromGlobTime(ReadInt16(offset + 13));
                section.TrackPoints.Add(point);
                offset += TrackPointLength;
            }
            return section;
        }

        private void ReadHeader(Header625M header, int offset)
        {
            header.StartTime = ReadDateTime(offset).ToUniversalTime().AddHours(this.FitnessDevice.configInfo.HoursAdjustment);
            header.TotalTime = TimeSpan.FromSeconds(FromGlobTime(ReadInt32(offset + 7)));
            header.TotalDistanceMeters = ReadInt32(offset + 11);
            header.TotalCalories = ReadInt16(offset + 15);
            header.MaximumSpeed = FromGlobSpeed(ReadInt16(offset + 17));
            header.MaximumHeartRate = this.PacketData[offset + 19];
            header.AverageHeartRate = this.PacketData[offset + 20];
        }

        //Trackstart with laps
        public override GlobalsatPacket SendTrackStart(Train trackFile)
        {
            Int16 nrLaps = 1;
            Int16 totalLength = (Int16)(TrackHeaderLength + nrLaps * TrackLapLength);
            this.InitPacket(CommandSendTrackStart, totalLength);

            int offset = 0;

            offset += WriteTrackHeader(offset, nrLaps, trackFile);

            // send only one lap
            int totalTimeSecondsTimes10 = ToGlobTime(trackFile.TotalTime.TotalSeconds);
            offset += this.Write32(offset, totalTimeSecondsTimes10);
            offset += this.Write32(offset, totalTimeSecondsTimes10);
            offset += this.Write32(offset, (Int32)trackFile.TotalDistanceMeters);
            offset += this.Write(offset, trackFile.TotalCalories);
            offset += this.Write(offset, ToGlobSpeed(trackFile.MaximumSpeed));
            this.PacketData[offset++] = (byte)trackFile.MaximumHeartRate;
            this.PacketData[offset++] = (byte)trackFile.AverageHeartRate;

            // start/end index
            offset += Write(offset, 0);
            offset += Write(offset, (Int16)(trackFile.TrackPointCount - 1));

            CheckOffset(totalLength, offset);
            return this;
        }

        private int WriteTrackHeader(int offset, int noOfLaps, Train trackFile)
        {
            int startOffset = offset;
            offset += this.Write(offset, trackFile.StartTime.ToLocalTime().AddHours(-this.FitnessDevice.configInfo.HoursAdjustment));

            this.PacketData[offset++] = (byte)noOfLaps;
            int totalTimeSecondsTimes10 = ToGlobTime(trackFile.TotalTime.TotalSeconds);
            offset += this.Write32(offset, totalTimeSecondsTimes10);
            offset += this.Write32(offset, (Int32)trackFile.TotalDistanceMeters);
            offset += this.Write(offset, trackFile.TotalCalories);
            offset += this.Write(offset, ToGlobSpeed(trackFile.MaximumSpeed));
            this.PacketData[offset++] = (byte)trackFile.MaximumHeartRate;
            this.PacketData[offset++] = (byte)trackFile.AverageHeartRate;
            offset += this.Write(offset, trackFile.TotalAscend);
            offset += this.Write(offset, trackFile.TotalDescend);
            offset += this.Write(offset, (Int16)trackFile.TrackPointCount);

            //unused in some headers
            offset += this.Write(offset, 0);
            offset += this.Write(offset, 0);

            return CheckOffset(TrackHeaderLength, offset - startOffset);
        }

        protected override int WriteTrackPointHeader(int offset, Train trackFile, int StartPointIndex, int EndPointIndex)
        {
            int startOffset = offset;
            offset += WriteTrackHeader(offset, 1, trackFile);

            //write to the offsets "unused fields in some headers" 
            this.Write(offset - 4, (Int16)StartPointIndex);
            this.Write(offset - 2, (Int16)EndPointIndex);
            return CheckOffset(TrackHeaderLength, offset - startOffset);
        }

        protected override int WriteTrackPoint(int offset, TrackPoint trackpoint)
        {
            offset += this.Write32(offset, ToGlobLatLon(trackpoint.Latitude));
            offset += this.Write32(offset, ToGlobLatLon(trackpoint.Longitude)); 
            offset += this.Write(offset, (Int16)trackpoint.Altitude);
            offset += this.Write(offset, ToGlobSpeed(trackpoint.Speed));
            this.PacketData[offset++] = (byte)trackpoint.HeartRate;
            offset += this.Write(offset, ToGlobTime16(trackpoint.IntervalTime));
            return TrackPointLength;
        }

        //Details unused
        //public override GlobalsatSystemConfiguration ResponseGetSystemConfiguration()
        //{
        //    string deviceName = ByteArr2String(0, 20 + 1);

        //    int versionInt = ReadInt16(21);
        //    double version = (double)versionInt / 100.0;

        //    // 23-24: version hex ? - update code/flag

        //    string firmware = ByteArr2String(25, 16 + 1);

        //    string userName = ByteArr2String(42, 10 + 1);

        //    bool isFemale = PacketData[53] != 0x00;
        //    int age = PacketData[54];

        //    int weightPounds = ReadInt16(55);
        //    int weightKg = ReadInt16(57);

        //    int heightInches = ReadInt16(59);
        //    int heightCm = ReadInt16(61);

        //    int waypointCount = PacketData[63];
        //    int trainCount = PacketData[64];
        //    int manualRouteCount = PacketData[65];

        //    int birthYear = ReadInt16(66);
        //    int birthMonth = PacketData[68] + 1;
        //    int birthDay = PacketData[69];
        //    DateTime birthDate = new DateTime(birthYear, birthMonth, birthDay);

        //    int pcRouteCount = 0;
        //    int courseCount = 0;
        //    try
        //    {
        //        pcRouteCount = PacketData[70];
        //        courseCount = PacketData[71];
        //    }
        //    catch { }

        //    GlobalsatSystemConfiguration systemInfo = new GlobalsatSystemConfiguration(deviceName, version, firmware, userName, isFemale, age, weightPounds, weightKg, heightInches, heightCm, birthDate,
        //        waypointCount, trainCount, manualRouteCount, pcRouteCount, courseCount);

        //    return systemInfo;
        //}

        //Decoded packet not used now
        //public override GlobalsatSystemConfiguration2 ResponseGetSystemConfiguration2()
        //{
        //    string userName = ByteArr2String(0, 10 + 1);

        //    bool isFemale = PacketData[11] != 0x00;
        //    int age = (int)PacketData[12];

        //    int weightPounds = ReadInt16(13);
        //    int weightKg = ReadInt16(15);

        //    int heightInches = ReadInt16(17);
        //    int heightCm = ReadInt16(19);

        //    int birthYear = ReadInt16(21);
        //    int birthMonth = (int)PacketData[23] + 1;
        //    int birthDay = (int)PacketData[24];
        //    DateTime birthDate = new DateTime(birthYear, birthMonth, birthDay);

        //    int languageIndex = (int)PacketData[25];
        //    int timezoneIndex = (int)PacketData[26];
        //    int utcOffsetTimeIndex = (int)PacketData[27];
        //    bool summertime = PacketData[28] != 0x00;
        //    bool isTimeFormat24h = PacketData[29] != 0x00;
        //    int unitIndex = (int)PacketData[30];
        //    int beeperIndex = (int)PacketData[31];
        //    bool waasOn = PacketData[32] != 0x00;
        //    int recordSamplingIndex = (int)PacketData[33];
        //    int recordSamplingCustomTime = (int)PacketData[34];
        //    int sportTypeIndex = (int)PacketData[35];
        //    int timeAlertIndex = (int)PacketData[36];
        //    int timeAlertInterval = ReadInt32(37); // 0.1 secs
        //    int distanceAlertIndex = (int)PacketData[41];
        //    int distanceAlertInterval = ReadInt32(42); // cm
        //    bool fastSpeedAlertOn = PacketData[46] != 0x00;
        //    int fastSpeedMiles = ReadInt32(47);
        //    int fastSpeedKm = ReadInt32(51);
        //    int fastSpeedKnots = ReadInt32(55);
        //    bool slowSpeedAlertOn = PacketData[59] != 0x00;
        //    int slowSpeedMiles = ReadInt32(60);
        //    int slowSpeedKm = ReadInt32(64);
        //    int slowSpeedKnots = ReadInt32(68);
        //    bool fastPaceAlertOn = PacketData[72] != 0x00;
        //    int fastPace = ReadInt32(73); // by units in byte 30
        //    bool slowPaceAlertOn = PacketData[77] != 0x00;
        //    int slowPace = ReadInt32(78); // by units in byte 30
        //    int autoPauseIndex = (int)PacketData[82];
        //    int pauseSpeedMiles = ReadInt32(83);
        //    int pauseSpeedKm = ReadInt32(87);
        //    int pauseSpeedKnots = ReadInt32(91);
        //    bool calculateCalorieByHeartrate = PacketData[95] != 0x00;
        //    bool heartMaxAlertOn = PacketData[96] != 0x00;
        //    int maxHeartrate = (int)PacketData[97];
        //    bool heartMinAlertOn = PacketData[98] != 0x00;
        //    int minHeartrate = (int)PacketData[99];
        //    int coordinationIndex = (int)PacketData[100];
        //    int sleepModeIndex = (int)PacketData[101];
        //    int heartAlertLevelIndex = (int)PacketData[102];
        //    int trainingLevelIndex = (int)PacketData[103];
        //    int declinationIndex = (int)PacketData[104];
        //    int declinationManualValue = ReadInt32(105);
        //    int autoLapIndex = (int)PacketData[109];
        //    int autoLapDistance = ReadInt32(110);
        //    int autoLapTime = ReadInt32(114);
        //    int extraWeightPounds = ReadInt16(118);
        //    int extraWeightKg = ReadInt16(120);
        //    int heartZoneLow = (int)PacketData[122];
        //    int heartZoneHigh = (int)PacketData[123];
        //    int switchDistanceIndex = (int)PacketData[124];
        //    bool switchCorrectionOn = PacketData[125] != 0x00;

        //    GlobalsatSystemConfiguration2 systemInfo = new GlobalsatSystemConfiguration2();

        //    return systemInfo;
        //}

        protected override int TrackHeaderLength { get { return 31; } }
        protected override int TrainDataHeaderLength { get { return TrackHeaderLength; } }
        protected override int TrackPointLength { get { return 15; } }
        protected override int TrackLapLength { get { return 22; } }
    }
}