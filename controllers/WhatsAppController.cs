using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;

namespace WhatsAppBotApi.Controllers;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private const string VERIFY_TOKEN = "token_chatbot_dario";

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
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        Console.WriteLine(body);

        return Ok();
    }
}