using System;
using System.Collections.Generic;
using System.Text;

namespace GEMEchoServer
{
    public class Gem
    {
        public Gem()
        {
            TemperatureSensors = new List<double>();
            PulseCounterSensors = new List<long>();
            CurrentSensors = new List<double>();
            AbsoluteWattSecondsSensors = new List<long>();
            PolarizedWattSecondsSensors = new List<long>();
        }

        public Gem(Gem clone)
        {
            SerialNumber = clone.SerialNumber;
            Voltage = clone.Voltage;
            Seconds = clone.Seconds;
            TimeStamp = clone.TimeStamp;
            TemperatureSensors = new List<double>(clone.TemperatureSensors);
            PulseCounterSensors = new List<long>(clone.PulseCounterSensors);
            CurrentSensors = new List<double>(clone.CurrentSensors);
            AbsoluteWattSecondsSensors = new List<long>(clone.AbsoluteWattSecondsSensors);
            PolarizedWattSecondsSensors = new List<long>(clone.PolarizedWattSecondsSensors);
        }

        public void Clear()
        {
            SerialNumber = default;
            Voltage = default;
            Seconds = default;
            TimeStamp = default;
            TemperatureSensors.Clear();
            PulseCounterSensors.Clear();
            CurrentSensors.Clear();
            AbsoluteWattSecondsSensors.Clear();
            PolarizedWattSecondsSensors.Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Serial Number : {SerialNumber}");
            sb.AppendLine($"Voltage : {Voltage}");
            sb.AppendLine($"Seconds : {Seconds}");
            sb.AppendLine($"TimeStamp : {TimeStamp}");
            sb.AppendLine("Temperature : " + string.Join(", ", TemperatureSensors));
            sb.AppendLine("Pulse Counter : " + string.Join(", ", PulseCounterSensors));
            sb.AppendLine("Current : " + string.Join(", ", CurrentSensors));
            sb.AppendLine("Abs W/s : " + string.Join(", ", AbsoluteWattSecondsSensors));
            sb.Append("Pol W/s : " + string.Join(", ", PolarizedWattSecondsSensors));
            return sb.ToString();
        }

        public string SerialNumber { get; set; }
        public double Voltage { get; set; }
        public long Seconds { get; set; }
        public DateTime TimeStamp { get; set; }
        public List<double> TemperatureSensors { get; }
        public List<long> PulseCounterSensors { get; }
        public List<double> CurrentSensors { get; }
        public List<long> AbsoluteWattSecondsSensors { get; }
        public List<long> PolarizedWattSecondsSensors { get; }
    }
}
