using System;
using System.IO;

namespace GEMEchoServer
{
    public class GemOutputFile
    {
        public GemOutputFile(string fileName)
        {
            FileName = fileName;
        }

        public void WriteHeader()
        {
            if (string.IsNullOrEmpty(FileName))
                return;

            // Header
            string header = "UTC,Serial,Voltage,Seconds,TimeStamp";
            for (int i = 0; i < 8; i ++)
                header += $",Temperature-{i + 1}";
            for (int i = 0; i < 4; i ++)
                header += $",PulseCounter-{i + 1}";
            for (int i = 0; i < 48; i++)
                header += $",Current-{i + 1}";
            for (int i = 0; i < 48; i++)
                header += $",AbsoluteWattSeconds-{i + 1}";
            for (int i = 0; i < 48; i++)
                header += $",PolarizedWattSeconds-{i + 1}";
            File.WriteAllText(FileName, header + Environment.NewLine);
        }

        public void LogPacket(Gem gem)
        {
            if (gem == null)
                throw new ArgumentNullException(nameof(gem));

            if (string.IsNullOrEmpty(FileName))
                return;

            // Log a packet
            string result = $"{DateTime.UtcNow:s},{gem.SerialNumber},{gem.Voltage},{gem.Seconds},{gem.TimeStamp:s}" +
                "," + string.Join(",", gem.TemperatureSensors) + 
                "," + string.Join(",", gem.PulseCounterSensors) +
                "," + string.Join(",", gem.CurrentSensors) +
                "," + string.Join(",", gem.AbsoluteWattSecondsSensors) +
                "," + string.Join(",", gem.PolarizedWattSecondsSensors);
            File.AppendAllText(FileName, result + Environment.NewLine);
        }

        public string FileName { get; }
    }
}
