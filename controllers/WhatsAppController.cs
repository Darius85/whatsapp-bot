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

        // ⚠️ REEMPLAZA POR TU TOKEN NUEVO
        private readonly string ACCESS_TOKEN = Environment.GetEnvironmentVariable("WHATSAPP_TOKEN") ?? "";

        // ⚠️ TU PHONE NUMBER ID
        private const string PHONE_NUMBER_ID = "1098455723356170";

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

                var from =
                    json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["from"]
                    ?.ToString();

                if (string.IsNullOrEmpty(from))
                    return Ok();

                var textMessage =
                    json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["text"]?["body"]
                    ?.ToString();

                var selectedId =
                    json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0]?["interactive"]?["list_reply"]?["id"]
                    ?.ToString();

                var message = !string.IsNullOrEmpty(selectedId)
                    ? selectedId.ToLower().Trim()
                    : textMessage?.ToLower().Trim();

                Console.WriteLine($"Mensaje recibido: {message}");

                if (string.IsNullOrEmpty(message))
                    return Ok();

                // =========================
                // MENU PRINCIPAL
                // =========================
                if (message.Contains("hola")
                    || message.Contains("menu")
                    || message.Contains("menú")
                    || message.Contains("programas"))
                {
                    await SendWelcomeImage(from);

                    await Task.Delay(1500);

                    await SendProgramList(from);
                }

                // =========================
                // PINTURA
                // =========================
                else if (message == "pintura")
                {
                    await SendTextMessage(
                        from,
                        "🎨 *Programa Pintura*\n\n" +
                        "✅ Apoyo para pintar viviendas.\n" +
                        "✅ Requisitos básicos.\n" +
                        "✅ Registro disponible.\n\n" +
                        "Escribe *menú* para volver.");
                }

                // =========================
                // CONECTABASCO
                // =========================
                else if (message == "conectabasco")
                {
                    await SendTextMessage(
                        from,
                        "🔌 *ConecTabasco*\n\n" +
                        "✅ Programa de conectividad.\n" +
                        "✅ Acceso a beneficios tecnológicos.\n\n" +
                        "Escribe *menú* para volver.");
                }

                // =========================
                // MENTE 360
                // =========================
                else if (message == "mente360")
                {
                    await SendTextMessage(
                        from,
                        "🧠 *Mente 360*\n\n" +
                        "✅ Atención y orientación.\n" +
                        "✅ Bienestar emocional.\n\n" +
                        "Escribe *menú* para volver.");
                }

                // =========================
                // AUDIENCIA
                // =========================
                else if (message == "audiencia")
                {
                    await SendTextMessage(
                        from,
                        "📣 *Audiencia*\n\n" +
                        "✅ Solicita audiencia.\n" +
                        "✅ Consulta información.\n\n" +
                        "Escribe *menú* para volver.");
                }

                else
                {
                    await SendTextMessage(
                        from,
                        "❌ No entendí tu mensaje.\n\nEscribe *hola* o *menú*.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return Ok();
        }

        // =========================================
        // IMAGEN DE BIENVENIDA
        // =========================================
        private async Task SendWelcomeImage(string to)
        {
            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {ACCESS_TOKEN}");

            var payload = new
            {
                messaging_product = "whatsapp",
                to = to,
                type = "image",
                image = new
                {
                    link = "https://whatsapp-bot-4wyj.onrender.com/assets/Erubiel.jpeg",
                    caption =
                        "👋 Hola, soy tu amigo José.\n\n" +
                        "Te ayudaré a consultar los programas sociales disponibles."
                }
            };

            var json =
                Newtonsoft.Json.JsonConvert.SerializeObject(payload);
 
            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"https://graph.facebook.com/v25.0/{PHONE_NUMBER_ID}/messages",
                content);

            var result =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Image response: {result}");
        }

        // =========================================
        // LISTA DE PROGRAMAS
        // =========================================
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
                        text = "Programas Sociales"
                    },

                    body = new
                    {
                        text =
                            "Selecciona el programa que deseas consultar:"
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
                                        id = "pintura",
                                        title = "Pintura",
                                        description = "Apoyo para viviendas"
                                    },

                                    new
                                    {
                                        id = "conectabasco",
                                        title = "ConecTabasco",
                                        description = "Conectividad y apoyo"
                                    },

                                    new
                                    {
                                        id = "mente360",
                                        title = "Mente 360",
                                        description = "Bienestar y atención"
                                    },

                                    new
                                    {
                                        id = "audiencia",
                                        title = "Audiencia",
                                        description = "Solicita audiencia"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var json =
                Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"https://graph.facebook.com/v25.0/{PHONE_NUMBER_ID}/messages",
                content);

            var result =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine($"List response: {result}");
        }

        // =========================================
        // MENSAJES DE TEXTO
        // =========================================
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

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync(
                $"https://graph.facebook.com/v25.0/{PHONE_NUMBER_ID}/messages",
                content);

            var result =
                await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Text response: {result}");
        }
    }
}