using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    }

    public class Measurement
    {
        /// <summary>
        /// The value of the measurement. Should be a string representing a decimal (e.g., "2.5").
        /// </summary>
        [RegularExpression(@"^-?\d+(\.\d+)?$", ErrorMessage = "Value must be a valid decimal number.")]
        public string? Value { get; set; }
        
        /// <summary>
        /// The type of measurement (e.g., "power").
        /// </summary>
        public string? TypeOfMeasurement { get; set; }

        /// <summary>
        /// The phase of the measurement (e.g., "L1").
        /// </summary>
        public string? Phase { get; set; }

        /// <summary>
        /// The unit of the measurement (e.g., "kW").
        /// </summary>
        public string? Unit { get; set; }
    }

    [FirestoreData] // Marks this class as Firestore-compatible
    public class ProcessedMeasurements
    {
        [FirestoreProperty]
        public required string ChargerId { get; set; }

        [FirestoreProperty]
        public int? SocketId { get; set; }

        [FirestoreProperty]
        public DateTime Timestamp { get; set; }

        [FirestoreProperty]
        public required string MessageType { get; set; }

        [FirestoreProperty]
        public required List<ProcessedMeasurement> Measurements { get; set; }
    }

    [FirestoreData]
    public class ProcessedMeasurement
    {
        [FirestoreProperty(ConverterType = typeof(DecimalConverter))]
        public decimal Value { get; set; }

        [FirestoreProperty]
        public required string TypeOfMeasurement { get; set; }

        [FirestoreProperty]
        public required string Phase { get; set; }

        [FirestoreProperty]
        public required string Unit { get; set; }
    }
}
