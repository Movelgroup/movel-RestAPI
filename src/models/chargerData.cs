using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace apiEndpointNameSpace.Models.ChargerData
{
    public class ErrorResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }
    }

    public class ChargerStateMessage
    {
        public string? ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? Status { get; set; }
        public string? ErrorCode { get; set; }
        public string? Message { get; set; }
    }

    [FirestoreData]
    public class ProcessedChargerState
    {
        [FirestoreProperty]
        public string? ChargerId { get; set; }
        
        [FirestoreProperty]
        public int? SocketId { get; set; }
        
        [FirestoreProperty]
        public DateTime? Timestamp { get; set; }
        
        [FirestoreProperty]
        public string? Status { get; set; }
        
        [FirestoreProperty]
        public string? ErrorCode { get; set; }
        [FirestoreProperty]
        public string? MessageType { get; set; }
        
        [FirestoreProperty]
        public string? Message { get; set; }

        public ProcessedChargerState() {}
    }


    public class ChargerData
    {
        public string? ChargerId { get; set; }
        public string? OwnerId { get; set; }
        public List<string>? AssociatedUserIds { get; set; }
        // Add other properties as needed
    }

    public class FullChargingTransaction
    {
        public string? ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime TimeStampStart { get; set; }
        public DateTime TimeStampEnd { get; set; }
        public long TransactionId { get; set; }
        public string? AuthorizedIdTag { get; set; }
        public decimal MeterReadStart { get; set; }
        public decimal MeterReadEnd { get; set; }
        public decimal ConsumptionWh { get; set; }
    }

    public class ProcessedFullChargingTransaction
    {
        public string? ChargerId { get; set; }
        public string? MessageType { get; set; }
        public int SocketId { get; set; }
        public DateTime? TimeStampStart { get; set; }
        public DateTime? TimeStampEnd { get; set; }
        public long TransactionId { get; set; }
        public string? AuthorizedIdTag { get; set; }
        public decimal MeterReadStart { get; set; }
        public decimal MeterReadEnd { get; set; }
        public decimal ConsumptionWh { get; set; }
    }

    public class ChargingTransaction
    {
        public string? ChargerId { get; set; }
        public int SocketId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? Action { get; set; } // "transaction_start" or "transaction_stop"
        public long TransactionId { get; set; }
        public string? AuthorizedIdTag { get; set; }
        public decimal MeterRead { get; set; }
    }

    public class ProcessedChargingTransaction
    {
        public string? ChargerId { get; set; }
        public string? MessageType { get; set; }

        public int SocketId { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? Action { get; set; }
        public long TransactionId { get; set; }
        public string? AuthorizedIdTag { get; set; }
        public decimal MeterRead { get; set; }
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
