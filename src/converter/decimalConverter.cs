using Google.Cloud.Firestore;

public class DecimalConverter : IFirestoreConverter<decimal>
{
    public object ToFirestore(decimal value) => (double)value; // Convert to double for Firestore

    public decimal FromFirestore(object value)
    {
        return value switch
        {
            double d => (decimal)d, // Convert back to decimal
            long l => (decimal)l,
            _ => throw new ArgumentException($"Cannot convert {value.GetType()} to decimal")
        };
    }
}
