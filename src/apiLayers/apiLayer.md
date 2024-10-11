API Layer (Controllers)

Create an EmablerController to handle incoming requests from the Push API.
Use attribute routing for clear endpoint definitions.
Implement separate endpoints for different message types (ChargerState, Measurements).

```cs
[ApiController]
[Route("api/v1/emabler")]
public class EmablerController : ControllerBase
{
    [HttpPost("charger-state")]
    public async Task<IActionResult> ReceiveChargerState([FromBody] ChargerStateDto chargerState)
    {
        // Implementation
    }

    [HttpPost("measurements")]
    public async Task<IActionResult> ReceiveMeasurements([FromBody] MeasurementsDto measurements)
    {
        // Implementation
    }
}
```