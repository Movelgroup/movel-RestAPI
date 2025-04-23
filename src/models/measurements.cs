using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace apiEndpointNameSpace.Models.Measurements
{

    public class MeasurementsMessage
    {
        /// <summary>
        /// ChargerId. Datatype:String.
        /// </summary>
        [JsonPropertyName("chargerId")]
        public string? ChargerId { get; set; }


        /// <summary>
        /// SocketId. Datatype:String.
        /// </summary>
        [JsonPropertyName("socketId")]
        public int SocketId { get; set; }

        /// <summary>
        /// Time when measurement is made. Datatype:DateTime.
        /// </summary>
        [JsonPropertyName("timeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// List Of Measurements. Datatype:List<Measurement>
        /// </summary>
        [JsonPropertyName("measurements")]
        public List<Measurement>? Measurements { get; set; }
    }

    public class Measurement
    {
        /// <summary>
        /// The value of the measurement. Datatype:String representing a float.
        /// </summary>
        public string? Value { get; set; }
        
        
        /// <summary>
        /// Type of measurment. Datatype:String.
        /// </summary>
        public string? TypeOfMeasurement { get; set; }

        /// <summary>
        /// Phase where the measurement is made. Datatype:String.
        /// </summary>
        public string? Phase { get; set; }

        /// <summary>
        /// Unit of the measurement. Datatype:String.
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
