// This interface defines the contract for interacting with the Dynamics 365
// environment (the Dataverse/CDS). This contract remains clean in the Core layer,
// while the Infrastructure layer will contain the actual SDK implementation logic.

using System.Collections.Generic;
using System.Threading.Tasks;
using SyncService.Core.Models;

namespace SyncService.Core.Interfaces
{
    /// Defines methods for updating records within the Dynamics 365 Dataverse.
    public interface ID365DataverseConnector
    {
        /// Attempts to update a batch of Product records in D365 Dataverse.
        Task<bool> UpdateProductInventoryBatchAsync(IEnumerable<ProductDto> products);
    }
}
