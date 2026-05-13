using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;

namespace WhatsAppBotApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WhatsAppController : ControllerBase
{
    private const string VERIFY_TOKEN = "mi_token_seguro";

    [HttpGet]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        if (mode == "subscribe" && token == VERIFY_TOKEN)
        {
            return Ok(challenge);
        }

        return Unauthorized();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveMessage()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();

        Console.WriteLine(body);

        return Ok();
    }
}