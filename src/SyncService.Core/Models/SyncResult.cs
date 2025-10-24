// This model will be used to return the outcome of the synchronization
// process from the core business logic layer to the API layer.

namespace SyncService.Core.Models
{
    /// Represents the result of a synchronization operation.
    public class SyncResult
    {
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public int ItemsProcessed { get; set; }

        public static SyncResult Success(int itemsProcessed)
        {
            return new SyncResult { IsSuccessful = true, ItemsProcessed = itemsProcessed };
        }

        public static SyncResult Failure(string errorMessage)
        {
            return new SyncResult { IsSuccessful = false, ErrorMessage = errorMessage };
        }
    }
}
