using System;
using System.Collections.Generic;

namespace apiEndpointNameSpace.Models
{
    public class ChargerStateMessage
    {
        public string ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Status { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class MeasurementsMessage
    {
        public string ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime TimeStamp { get; set; }
        public List<Measurement> Measurements { get; set; }
    }

    public class Measurement
    {
        public string Value { get; set; }
        public string TypeOfMeasurement { get; set; }
        public string Phase { get; set; }
        public string Unit { get; set; }
    }

    public class ProcessedChargerState
    {
        public string ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime Timestamp { get; set; }
        public ChargerStatus Status { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class ProcessedMeasurements
    {
        public string ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime Timestamp { get; set; }
        public List<ProcessedMeasurement> Measurements { get; set; }
    }

    public class ProcessedMeasurement
    {
        public decimal Value { get; set; }
        public string TypeOfMeasurement { get; set; }
        public string Phase { get; set; }
        public string Unit { get; set; }
    }

    public class ChargerData
    {
        public string ChargerId { get; set; }
        public string OwnerId { get; set; }
        public List<string> AssociatedUserIds { get; set; }
        // Add other properties as needed
    }

    public enum ChargerStatus
    {
        Available,
        Error,
        Offline,
        Info,
        Charging,
        SuspendedCAR,
        SuspendedCHARGER,
        Preparing,
        Finishing,
        Booting,
        Unavailable
    }
}