using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;

namespace WhatsAppBotApi.Controllers
{
    [ApiController]
    [Route("api/whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private const string VERIFY_TOKEN = "token_chatbot_dario";

        // Token temporal de Meta
        private readonly string ACCESS_TOKEN = Environment.GetEnvironmentVariable("WHATSAPP_TOKEN")!;

        // Identificador del número (sale en Meta)
        private readonly string PHONE_NUMBER_ID = Environment.GetEnvironmentVariable("WHATSAPP_PHONE_ID")!;

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

            try
            {
                var json = JObject.Parse(body);

                var message =
                    json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["text"]?["body"]
                    ?.ToString();

                var from =
                    json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["from"]
                    ?.ToString();

                if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(from))
                    return Ok();

                message = message.ToLower().Trim();

                //----------------------------------
                // MENÚ PRINCIPAL
                //----------------------------------

                if (message.Contains("hola") ||
                    message.Contains("menu") ||
                    message.Contains("menú"))
                {
                    await SendTextMessage(
                        from,
                        "👋 Bienvenido\n\n" +
                        "Selecciona un programa:\n\n" +
                        "1️⃣ Programa 1\n" +
                        "2️⃣ Programa 2\n" +
                        "3️⃣ Programa 3\n" +
                        "4️⃣ Programa 4\n" +
                        "5️⃣ Programa 5\n\n" +
                        "Responde con el número."
                    );
                }

                //----------------------------------

                else if (message == "1" || message.Contains("programa 1"))
                {
                    await SendTextMessage(
                        from,
                        "📌 PROGRAMA 1\n\n" +
                        "Información:\n" +
                        "• Requisito 1\n" +
                        "• Requisito 2\n" +
                        "• Beneficio\n" +
                        "• Fecha límite"
                    );
                }

                //----------------------------------

                else if (message == "2" || message.Contains("programa 2"))
                {
                    await SendTextMessage(
                        from,
                        "📌 PROGRAMA 2\n\n" +
                        "Información del programa."
                    );
                }

                //----------------------------------

                else if (message == "3" || message.Contains("programa 3"))
                {
                    await SendTextMessage(
                        from,
                        "📌 PROGRAMA 3\n\n" +
                        "Información del programa."
                    );
                }

                //----------------------------------

                else if (message == "4" || message.Contains("programa 4"))
                {
                    await SendTextMessage(
                        from,
                        "📌 PROGRAMA 4\n\n" +
                        "Información del programa."
                    );
                }

                //----------------------------------

                else if (message == "5" || message.Contains("programa 5"))
                {
                    await SendTextMessage(
                        from,
                        "📌 PROGRAMA 5\n\n" +
                        "Información del programa."
                    );
                }

                //----------------------------------

                else
                {
                    await SendTextMessage(
                        from,
                        "No entendí tu mensaje.\n\nEscribe *hola* o *menú*."
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Ok();
        }


        private async Task SendTextMessage(
            string to,
            string text)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {ACCESS_TOKEN}");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "text",
                text = new
                {
                    body = text
                }
            };

            var json =
                Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await client.PostAsync(
                    $"https://graph.facebook.com/v22.0/{PHONE_NUMBER_ID}/messages",
                    content);

            var result =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine(result);
        }
    }
}