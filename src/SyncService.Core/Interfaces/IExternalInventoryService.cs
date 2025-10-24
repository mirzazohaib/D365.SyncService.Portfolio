// This interface defines the contract for fetching data from the simulated
// external Warehouse Management System (WMS) or e-commerce platform.
// This contract lives in the Core layer, separating the business logic
// from the actual implementation.

using System.Collections.Generic;
using System.Threading.Tasks;
using SyncService.Core.Models;

namespace SyncService.Core.Interfaces
{
    /// Defines methods for retrieving product data from a third-party source.
    public interface IExternalInventoryService
    {
        /// Retrieves the current inventory status for all active products.
        /// A list of Product Data Transfer Objects (DTOs).
        Task<IEnumerable<ProductDto>> GetCurrentInventoryAsync();
    }
}
