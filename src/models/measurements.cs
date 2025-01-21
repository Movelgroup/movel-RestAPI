using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using Swashbuckle.AspNetCore.Annotations;

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

        [SwaggerSchema("The value of the measurement.", Description = "String: decimal value in the form of a String")]
        public string? Value { get; set; }
        
        
        public string? TypeOfMeasurement { get; set; }


        public string? Phase { get; set; }


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
