using Google.Cloud.Firestore;
using System;

public class FirestoreDbProvider
{
    private static FirestoreDb? _instance;
    private static readonly object _lock = new object();

    public static FirestoreDb GetInstance(string projectId, string credentialsPath)
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new FirestoreDbBuilder
                    {
                        ProjectId = projectId,
                        CredentialsPath = credentialsPath
                    }.Build();
                }
            }
        }
        return _instance;
    }
}