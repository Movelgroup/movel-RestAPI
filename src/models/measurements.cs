using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace apiEndpointNameSpace.Models.Measurements
{

    public class MeasurementsMessage
    {
        public string? ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime TimeStamp { get; set; }
        public List<Measurement>? Measurements { get; set; }
        public string? Message { get; set; }
    }

    public class Measurement
    {
        public string? Value { get; set; }
        public string? TypeOfMeasurement { get; set; }
        public string? Phase { get; set; }
        public string? Unit { get; set; }
    }

    public class ProcessedMeasurements
    {
        public string? ChargerId { get; set; }
        public string? MessageType { get; set; }
        public int? SocketId { get; set; }
        public DateTime? Timestamp { get; set; }
        public List<ProcessedMeasurement>? Measurements { get; set; }
    }

    public class ProcessedMeasurement
    {
        public decimal? Value { get; set; }
        public string? TypeOfMeasurement { get; set; }
        public string? Phase { get; set; }
        public string? Unit { get; set; }
    } 
}
