// This file defines the Product Data Transfer Object (DTO).
// A DTO is a simple class used to transfer data between layers and systems.
// It contains no business logic, only properties. This model is shared
// between the Core, Infrastructure, and API layers.

using System;

namespace SyncService.Core.Models
{
    /// Represents a product's inventory data transferred between systems.
    public class ProductDto
    {
        /// The unique Stock Keeping Unit (SKU) for the product.
        /// This would be the primary key for matching records between systems.
        public required string Sku { get; set; }

        /// The current quantity on hand in the external system.
        public int QuantityOnHand { get; set; }

        /// The last time this inventory record was modified in the external system.
        public DateTime LastModified { get; set; }
    }
}
