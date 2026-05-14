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

    [HttpGet("send")]
    public async Task<IActionResult> Send()
    {
        var token = "EAAcbpf3utTcBRSGfOP244dBOAZCL3TZAasWpBuHZC3qepjpXOawZBugmb2rwGEy12L5LhtrR4lY5Y2CHvFj4wyqjz2yVM1YyPCTyLZBLhJENOLP7wHnZBALwfpk3EGfSGbtFcXkc2a8tTxCKb6QxTrQWxXROPiEvfzHAr0akWyqWJ0HepMQvBS0bRPt0oPvoZBjoCgId72QFGc9JXjNXRFTxwoLy785qwEXE0MlFfDX9zxFUeBi2IosgKZBt5qWlrwzldRZCxx3sRZAkqIX86YsZCp6";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = "5214181113121",
            type = "template",
            template = new
            {
                name = "hello_world",
                language = new
                {
                    code = "en_US"
                }
            }
        };

        using var http = new HttpClient();

        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var content = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json");

        var response = await http.PostAsync(
            "https://graph.facebook.com/v25.0/1098455723356170/messages",
            content);

        var result = await response.Content.ReadAsStringAsync();

        return Ok(result);
    }
}