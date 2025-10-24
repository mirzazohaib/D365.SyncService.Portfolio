// This is the starting point for the Web API, which will expose the single
// endpoint used to trigger the synchronization process.

using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks; // Added for Task<IActionResult>

namespace SyncService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        // Constructor and service injection will be added later

        // public SyncController(...) { ... }


        /// Triggers the full inventory synchronization process.
        /// (Placeholder implementation for Day 1)
        [HttpPost("trigger")] // Use "trigger" as per final design
        public async Task<IActionResult> TriggerSync()
        {
            // Placeholder: Simulate success for now.
            await Task.Delay(10); // Simulate async work
            return Ok(new { Status = "Success", Message = "Sync triggered (placeholder)." });
        }
    }
}