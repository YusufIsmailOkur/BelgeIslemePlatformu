namespace StoreApp.Models.Enums
{
    public enum DocumentStatus
    {
        Uploaded,
        Processing,
        WaitingValidation,
        Approved,
        Rejected,
        SavedToSql,
        ExportedToTarget,
        FailedRetry
    }
}
