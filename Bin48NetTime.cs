using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

// TODO: Switch to BinaryPrimitives
// https://docs.microsoft.com/en-us/dotnet/api/system.buffers.binary.binaryprimitives?view=netcore-3.0

namespace GEMEchoServer
{
    // GEM-PKT_Packet_Format_2_1.pdf
    public class Bin48NetTime
    {
        public Bin48NetTime()
        {
            Packet = new byte[PacketSize];
            Gem = new Gem();
        }

        public void Reset()
        {
            Array.Clear(Packet, 0, Packet.Length);
            Gem.Clear();
            Received = 0;
        }

        public bool Append(Memory<byte> buffer)
        {
            // Verify input data
            if (buffer.Length <= 0 ||
                buffer.Length > PacketSize - Received)
            {
                // Debug.Assert(false);
                return false;
            }

            // Copy received data into packet at current offset
            Span<byte> destination = Packet.AsSpan().Slice(Received, buffer.Length);
            Span<byte> source = buffer.Span;
            source.CopyTo(destination);
            Received += buffer.Length;
            
            // If we have a header it must match
            if (HasHeader() && !DoesHeaderMatch())
            {
                // Debug.Assert(false);
                return false;
            }

            return true;
        }

        public bool IsComplete()
        {
            // Received full packet
            return Received >= PacketSize;
        }

        public bool IsEmpty()
        {
            return Received == 0;
        }

        private bool HasHeader()
        {
            // Received header
            return Received >= Header.Length;
        }

        private bool DoesHeaderMatch()
        {
            // Must have a header to compare
            if (!HasHeader())
                return false;

            // Compare the header items
            return !Header.Where((t, i) => t != Packet[i]).Any();
        }

        private bool DoesFooterMatch()
        {
            // Must have items to compare
            if (!IsComplete())
                return false;

            // Compare the footer items
            // 623 - 624
            return !Footer.Where((t, i) => t != Packet[i + 622]).Any();
        }

        public bool Unpack()
        {
            try
            {
                // Do we have all the data
                // 625
                if (!IsComplete())
                    return false;

                // Does the header, footer, and the checksum match
                // 0xFE 0xFF 0x05
                if (!DoesHeaderMatch() ||
                    !DoesFooterMatch() ||
                    Packet[624] != MakeChecksum(Packet, 624))
                {
                    Debug.Assert(false);
                    return false;
                }

                // 4 - 5
                Gem.Voltage = MakeVoltage(Packet, 3);

                // 6 - 245
                MakeWattSecondsList(Packet, 5, 48, Gem.AbsoluteWattSecondsSensors);

                // 246 - 485
                MakeWattSecondsList(Packet, 245, 48, Gem.PolarizedWattSecondsSensors);

                // 486 - 487
                int serialnumber = MakeSerialNumber(Packet, 485);

                // 488
                // int reserved = Packet[487];

                // 489
                int deviceid = Packet[488];

                // id serialnumber
                Gem.SerialNumber = MakeSerialString(serialnumber, deviceid);

                // 490 - 585
                // 48 x 2 bytes
                MakeCurrentList(Packet, 489, 48, Gem.CurrentSensors);

                // 586 - 588
                // 3 bytes
                Gem.Seconds = MakeSeconds(Packet, 585);

                // 589 - 600
                // 4 x 3 bytes
                MakePulseCounterList(Packet, 588, 4, Gem.PulseCounterSensors);

                // 601 - 616
                // 8 x 2 bytes
                MakeTemperatureList(Packet, 600, 8, Gem.TemperatureSensors);

                // 617 - 622
                Gem.TimeStamp = MakeDate(Packet, 616);

                // 623 - 624, FFFE

                // 625, checksum
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.ToString());
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            return $"Received : {Received} : Outstanding : {Outstanding} : Data : {BitConverter.ToString(Packet, 0, Received)}";
        }

        private static byte MakeChecksum(byte[] packet, int size)
        {
            byte checksum = 0;
            for (int i = 0; i < size; i++)
            {
                checksum += packet[i];
            }
            return checksum;
        }

        private static double MakeVoltage(byte[] packet, int index)
        {
            // 2 bytes, reverse order
            byte[] voltage = new byte[2];
            Array.Copy(packet, index, voltage, 0, 2);
            Array.Reverse(voltage);

            return BitConverter.ToUInt16(voltage) / 10.0;
        }

        private static long MakeWattSeconds(byte[] packet, int index)
        {
            // 5 bytes, need 8
            byte[] wattseconds = new byte[8];
            wattseconds[0] = 0;
            wattseconds[1] = 0;
            wattseconds[2] = 0;
            Array.Copy(packet, index, wattseconds, 3, 5);

            // 5 of 8 bytes, can safely cast to signed long
            return Convert.ToInt64(BitConverter.ToUInt64(wattseconds));
        }

        private static void MakeWattSecondsList(byte[] packet, int index, int count, List<long> list)
        {
            list.Clear();
            for (int i = 0, j = index; i < count; i ++, j += 5)
            {
                list.Add(MakeWattSeconds(packet, j));
            }
        }

        private static int MakeSerialNumber(byte[] packet, int index)
        {
            // 2 bytes, reverse order
            byte[] serial = new byte[2];
            Array.Copy(packet, index, serial, 0, 2);
            Array.Reverse(serial);

            return BitConverter.ToUInt16(serial);
        }

        private static string MakeSerialString(int serialnumber, int deviceid)
        {
            return $"{deviceid:D2}{serialnumber:D5}";
        }

        private static long MakeSeconds(byte[] packet, int index)
        {
            // 3 bytes, need 4
            byte[] seconds = new byte[4];
            seconds[0] = 0;
            Array.Copy(packet, index, seconds, 1, 3);

            return BitConverter.ToUInt32(seconds);
        }

        private static long MakePulseCounter(byte[] packet, int index)
        {
            return MakeSeconds(packet, index);
        }

        private static void MakePulseCounterList(byte[] packet, int index, int count, List<long> list)
        {
            list.Clear();
            for (int i = 0, j = index; i < count; i ++, j += 3)
            {
                list.Add(MakePulseCounter(packet, j));
            }
        }

        private static double MakeTemperature(byte[] packet, int index)
        {
            // 2 bytes
            return BitConverter.ToUInt16(packet, index) / 2.0;
        }

        private static void MakeTemperatureList(byte[] packet, int index, int count, List<double> list)
        {
            list.Clear();
            for (int i = 0, j = index; i < count; i ++, j += 2)
            {
                list.Add(MakeTemperature(packet, j));
            }
        }

        private static double MakeCurrent(byte[] packet, int index)
        {
            // 2 bytes
            return BitConverter.ToUInt16(packet, index) / 50.0;
        }

        private static void MakeCurrentList(byte[] packet, int index, int count, List<double> list)
        {
            list.Clear();
            for (int i = 0, j = index; i < count; i ++, j += 2)
            {
                list.Add(MakeCurrent(packet, j));
            }
        }

        private static DateTime MakeDate(byte[] packet, int index)
        {
            return new DateTime(2000 + packet[index], packet[index + 1], packet[index + 2], packet[index + 3], packet[index + 4], packet[index + 5], DateTimeKind.Utc);
        }

        public int Received { get; private set; }
        public int Outstanding => PacketSize - Received;
        public Gem Gem { get; }

        private readonly byte[] Packet;

        private static readonly byte[] Header = { 0xFE, 0xFF, 0x05 };
        private static readonly byte[] Footer = { 0xFF, 0xFE };
        private const int PacketSize = 625;
    }
}

/*GENERATING KWH AND WATT VALUES
If prevSec > currSec
secDiff = 256^3 - prevSec
secDiff += currSec
Else
secDiff = currSec - prevSec
Function GenerateValues (prevWS, currWS, secDiff)
If prevWS > currWS
wsDiff = 256^5 - prevWS
wsDiff += currWS
Else
wsDiff = currWS – prevWS
watt = wsDiff/secDiff
kWh = wsDiff/3600000
Return watt, kWh
 */ 
