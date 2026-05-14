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
                return Ok(challenge);

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

                var from = json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["from"]?.ToString();

                var textMessage = json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["text"]?["body"]?.ToString();

                var selectedId = json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["interactive"]?["list_reply"]?["id"]?.ToString();

                if (string.IsNullOrEmpty(from))
                    return Ok();

                var message = !string.IsNullOrEmpty(selectedId)
                    ? selectedId.ToLower().Trim()
                    : textMessage?.ToLower().Trim();

                if (string.IsNullOrEmpty(message))
                    return Ok();

                if (message.Contains("hola") || message.Contains("menu") || message.Contains("menú"))
                {
                    await SendProgramList(from);
                }
                else if (message == "programa_1" || message == "1" || message.Contains("programa 1"))
                {
                    await SendTextMessage(from,
                        "📌 *Conect Internet*\n\n" +
                        "Aquí va la información del Programa 1:\n\n" +
                        "✅ Requisitos\n" +
                        "✅ Beneficios\n" +
                        "✅ Fechas\n" +
                        "✅ Cómo registrarse\n\n" +
                        "Escribe *menú* para volver a ver los programas.");
                }
                else if (message == "programa_2" || message == "2" || message.Contains("programa 2"))
                {
                    await SendTextMessage(from,
                        "📌 *Programa para pintura*\n\n" +
                        "Aquí va la información del Programa 2.\n\n" +
                        "Escribe *menú* para volver a ver los programas.");
                }
                else if (message == "programa_3" || message == "3" || message.Contains("programa 3"))
                {
                    await SendTextMessage(from,
                        "📌 *Programa para apoyos*\n\n" +
                        "Aquí va la información del Programa 3.\n\n" +
                        "Escribe *menú* para volver a ver los programas.");
                }
                else if (message == "programa_4" || message == "4" || message.Contains("programa 4"))
                {
                    await SendTextMessage(from,
                        "📌 *PROGRAMA 4*\n\n" +
                        "Aquí va la información del Programa 4.\n\n" +
                        "Escribe *menú* para volver a ver los programas.");
                }
                else if (message == "programa_5" || message == "5" || message.Contains("programa 5"))
                {
                    await SendTextMessage(from,
                        "📌 *PROGRAMA 5*\n\n" +
                        "Aquí va la información del Programa 5.\n\n" +
                        "Escribe *menú* para volver a ver los programas.");
                }
                else
                {
                    await SendTextMessage(from,
                        "No entendí tu mensaje.\n\nEscribe *hola* o *menú* para ver los programas disponibles.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return Ok();
        }

        private async Task SendProgramList(string to)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {ACCESS_TOKEN}");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "interactive",
                interactive = new
                {
                    type = "list",
                    header = new
                    {
                        type = "text",
                        text = "Programas sociales"
                    },
                    body = new
                    {
                        text = "Selecciona el programa que deseas consultar:"
                    },
                    footer = new
                    {
                        text = "Toca una opción para continuar"
                    },
                    action = new
                    {
                        button = "Ver programas",
                        sections = new[]
                        {
                            new
                            {
                                title = "Programas disponibles",
                                rows = new[]
                                {
                                    new
                                    {
                                        id = "programa_1",
                                        title = "Programa 1",
                                        description = "Información del Programa 1"
                                    },
                                    new
                                    {
                                        id = "programa_2",
                                        title = "Programa 2",
                                        description = "Información del Programa 2"
                                    },
                                    new
                                    {
                                        id = "programa_3",
                                        title = "Programa 3",
                                        description = "Información del Programa 3"
                                    },
                                    new
                                    {
                                        id = "programa_4",
                                        title = "Programa 4",
                                        description = "Información del Programa 4"
                                    },
                                    new
                                    {
                                        id = "programa_5",
                                        title = "Programa 5",
                                        description = "Información del Programa 5"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"https://graph.facebook.com/v25.0/{PHONE_NUMBER_ID}/messages",
                content);

            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"WhatsApp list response: {result}");
        }

        private async Task SendTextMessage(string to, string text)
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

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"https://graph.facebook.com/v25.0/{PHONE_NUMBER_ID}/messages",
                content);

            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"WhatsApp text response: {result}");
        }
    }
}