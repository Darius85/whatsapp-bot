using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace WhatsAppBotApi.Controllers
{
    [ApiController]
    [Route("api/whatsapp")]
    public class WhatsAppController : ControllerBase
    {
        private readonly string VERIFY_TOKEN =
            Environment.GetEnvironmentVariable("WHATSAPP_VERIFY_TOKEN")
            ?? "token_chatbot_dario";

        private readonly string ACCESS_TOKEN =
            Environment.GetEnvironmentVariable("WHATSAPP_TOKEN")
            ?? "";

        private readonly string PHONE_NUMBER_ID =
            Environment.GetEnvironmentVariable("WHATSAPP_PHONE_NUMBER_ID")
            ?? "1210618685459002";

        private readonly string WELCOME_IMAGE_URL =
            Environment.GetEnvironmentVariable("WHATSAPP_WELCOME_IMAGE_URL")
            ?? "https://autotyre.com.mx/assets/auto-tyre-welcome.jpg";

        private readonly string ADVISOR_WHATSAPP_NUMBER =
            Environment.GetEnvironmentVariable("WHATSAPP_ADVISOR_NUMBER")
            ?? "525579640165";

        private const string GRAPH_VERSION = "v25.0";

        // =========================================
        // VERIFICACIÓN DEL WEBHOOK DE META
        // GET /api/whatsapp
        // =========================================
        [HttpGet]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.verify_token")] string token,
            [FromQuery(Name = "hub.challenge")] string challenge)
        {
            if (mode == "subscribe" && token == VERIFY_TOKEN)
            {
                Console.WriteLine("Webhook verificado correctamente.");
                return Ok(challenge);
            }

            Console.WriteLine("Intento de verificación no autorizado.");
            return Unauthorized();
        }

        // =========================================
        // RECEPCIÓN DE MENSAJES DE WHATSAPP
        // POST /api/whatsapp
        // =========================================
        [HttpPost]
        public async Task<IActionResult> ReceiveMessage()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            Console.WriteLine("========== WEBHOOK BODY ==========");
            Console.WriteLine(body);
            Console.WriteLine("==================================");

            try
            {
                var json = JObject.Parse(body);

                var messageNode =
                    json["entry"]?[0]?["changes"]?[0]?["value"]?["messages"]?[0];

                if (messageNode == null)
                    return Ok();

                var from = messageNode["from"]?.ToString();

                if (string.IsNullOrWhiteSpace(from))
                    return Ok();

                var messageType = messageNode["type"]?.ToString();

                var textMessage =
                    messageNode["text"]?["body"]?.ToString();

                var listReplyId =
                    messageNode["interactive"]?["list_reply"]?["id"]?.ToString();

                var buttonReplyId =
                    messageNode["interactive"]?["button_reply"]?["id"]?.ToString();

                var selectedId =
                    !string.IsNullOrWhiteSpace(listReplyId)
                        ? listReplyId
                        : buttonReplyId;

                var message =
                    !string.IsNullOrWhiteSpace(selectedId)
                        ? selectedId.Trim().ToLower()
                        : textMessage?.Trim().ToLower();

                Console.WriteLine($"From: {from}");
                Console.WriteLine($"Type: {messageType}");
                Console.WriteLine($"Message: {message}");

                await MarkMessageAsRead(messageNode["id"]?.ToString());

                if (string.IsNullOrWhiteSpace(message))
                    return Ok();

                // =========================================
                // MENÚ PRINCIPAL
                // =========================================
                if (IsMenuIntent(message))
                {
                    await SendWelcomeMessage(from);
                    await Task.Delay(700);
                    await SendMainMenuButtons(from);
                }

                // =========================================
                // COTIZAR
                // =========================================
                else if (message == "cotizar" ||
                         message == "comprar_llantas" ||
                         message.Contains("cotizar") ||
                         message.Contains("comprar") ||
                         message.Contains("llanta") ||
                         message.Contains("llantas") ||
                         message.Contains("precio") ||
                         message.Contains("precios"))
                {
                    await SendTireQuoteMenu(from);
                }

                // =========================================
                // MEDIDA DE LLANTA
                // =========================================
                else if (LooksLikeTireSize(message))
                {
                    await SendTireSizeResponse(from, message);
                }

                // =========================================
                // AUXILIO VIAL
                // =========================================
                else if (message == "auxilio_vial" ||
                         message.Contains("auxilio") ||
                         message.Contains("asistencia") ||
                         message.Contains("vial") ||
                         message.Contains("ponchada") ||
                         message.Contains("ponchado") ||
                         message.Contains("emergencia"))
                {
                    await SendRoadAssistanceMessage(from);
                }

                // =========================================
                // ASESOR
                // =========================================
                else if (message == "asesor" ||
                         message == "contactar_asesor" ||
                         message.Contains("asesor") ||
                         message.Contains("humano") ||
                         message.Contains("persona") ||
                         message.Contains("agente"))
                {
                    await SendAdvisorCtaMessage(from);
                }

                // =========================================
                // HORARIOS
                // =========================================
                else if (message.Contains("horario") ||
                         message.Contains("abierto") ||
                         message.Contains("cerrado"))
                {
                    await SendTextMessage(
                        from,
                        "🕘 *Horarios de atención Auto Tyre*\n\n" +
                        "📞 *Call Center:*\n" +
                        "Lunes a viernes: 8:00 am - 7:00 pm\n" +
                        "Sábado: 8:00 am - 3:00 pm\n\n" +
                        "Para atención personalizada, selecciona *Asesor*.\n\n" +
                        "Escribe *menú* para volver al inicio.");
                }

                // =========================================
                // DEFAULT
                // =========================================
                else
                {
                    await SendTextMessage(
                        from,
                        "No logré identificar tu solicitud.\n\n" +
                        "Puedes seleccionar una opción del menú principal:");
                    
                    await Task.Delay(600);
                    await SendMainMenuButtons(from);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error procesando webhook:");
                Console.WriteLine(ex.ToString());
            }

            return Ok();
        }

        // =========================================
        // INTENCIONES
        // =========================================
        private static bool IsMenuIntent(string message)
        {
            return message.Contains("hola")
                || message.Contains("buenos")
                || message.Contains("buenas")
                || message.Contains("menu")
                || message.Contains("menú")
                || message.Contains("inicio")
                || message.Contains("opciones")
                || message == "hi"
                || message == "hello";
        }

        private static bool LooksLikeTireSize(string message)
        {
            var normalized = message
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .ToLower();

            return normalized.Contains("/")
                && normalized.Contains("r")
                && normalized.Any(char.IsDigit);
        }

        // =========================================
        // MENSAJE DE BIENVENIDA
        // =========================================
        private async Task SendWelcomeMessage(string to)
        {
            var text =
                "👋 ¡Hola! Bienvenido a *Auto Tyre*.\n\n" +
                "Soy tu asistente virtual y puedo ayudarte rápido con:\n\n" +
                "🛞 *Cotizar llantas*\n" +
                "🛟 *Auxilio vial*\n" +
                "👨‍💼 *Hablar con un asesor*\n\n" +
                "Selecciona una opción para continuar.";

            await SendTextMessage(to, text);
        }

        // =========================================
        // MENÚ PRINCIPAL CON 3 BOTONES DIRECTOS
        // =========================================
        private async Task SendMainMenuButtons(string to)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "interactive",
                interactive = new
                {
                    type = "button",
                    header = new
                    {
                        type = "text",
                        text = "Auto Tyre"
                    },
                    body = new
                    {
                        text =
                            "¿Qué necesitas hacer hoy?\n\n" +
                            "🛞 *Cotizar:* precios, medidas y disponibilidad.\n" +
                            "🛟 *Auxilio vial:* apoyo por llanta ponchada o emergencia.\n" +
                            "👨‍💼 *Asesor:* atención personalizada."
                    },
                    footer = new
                    {
                        text = "Selecciona una opción"
                    },
                    action = new
                    {
                        buttons = new[]
                        {
                            new
                            {
                                type = "reply",
                                reply = new
                                {
                                    id = "cotizar",
                                    title = "Cotizar"
                                }
                            },
                            new
                            {
                                type = "reply",
                                reply = new
                                {
                                    id = "auxilio_vial",
                                    title = "Auxilio vial"
                                }
                            },
                            new
                            {
                                type = "reply",
                                reply = new
                                {
                                    id = "asesor",
                                    title = "Asesor"
                                }
                            }
                        }
                    }
                }
            };

            await SendPayload(payload, "Main menu buttons response");
        }

        // =========================================
        // COTIZACIÓN DE LLANTAS
        // =========================================
        private async Task SendTireQuoteMenu(string to)
        {
            var text =
                "🛞 *Cotización de llantas Auto Tyre*\n\n" +
                "Para cotizar más rápido, envíame la medida de tu llanta.\n\n" +
                "Ejemplos:\n" +
                "• *205/55 R16*\n" +
                "• *225/60 R17*\n" +
                "• *265/70 R16*\n\n" +
                "También puedes enviarme los datos de tu vehículo:\n\n" +
                "🚗 Marca\n" +
                "🚙 Modelo\n" +
                "📅 Año\n\n" +
                "Ejemplo:\n" +
                "*Toyota Hilux 2021*\n\n" +
                "Un asesor puede confirmar disponibilidad, precio final, instalación, envío y promociones vigentes.";

            await SendTextMessage(to, text);

            await Task.Delay(700);
            await SendAdvisorCtaMessage(to);
        }

        private async Task SendTireSizeResponse(string to, string tireSize)
        {
            var text =
                $"✅ Recibí la medida: *{tireSize.ToUpper()}*\n\n" +
                "Para prepararte una cotización, por favor compárteme:\n\n" +
                "1️⃣ Cantidad de llantas\n" +
                "2️⃣ Ciudad o estado\n" +
                "3️⃣ ¿Requieres instalación?\n\n" +
                "Ejemplo:\n" +
                "*4 llantas, CDMX, con instalación*\n\n" +
                "Un asesor puede confirmar disponibilidad, precio final y promociones.";

            await SendTextMessage(to, text);

            await Task.Delay(700);
            await SendAdvisorCtaMessage(to);
        }

        // =========================================
        // AUXILIO VIAL
        // =========================================
        private async Task SendRoadAssistanceMessage(string to)
        {
            var text =
                "🛟 *Auxilio vial Auto Tyre*\n\n" +
                "Para apoyarte lo más rápido posible, envíanos estos datos:\n\n" +
                "📍 Ubicación actual o referencia\n" +
                "🛞 Tipo de problema\n" +
                "🚗 Tipo de vehículo\n" +
                "📞 Teléfono de contacto\n\n" +
                "Ejemplo:\n" +
                "*Estoy en Periférico Sur, llanta ponchada, camioneta SUV, 55...*\n\n" +
                "Un asesor revisará la disponibilidad de apoyo en tu zona.";

            await SendTextMessage(to, text);

            await Task.Delay(700);
            await SendAdvisorCtaMessage(to);
        }

        // =========================================
        // CONTACTAR ASESOR - BOTÓN CTA URL
        // =========================================
        private async Task SendAdvisorCtaMessage(string to)
        {
            var advisorText =
                "Hola, quiero hablar con un asesor de Auto Tyre.";

            var advisorUrl =
                $"https://wa.me/{ADVISOR_WHATSAPP_NUMBER}?text={Uri.EscapeDataString(advisorText)}";

            var payload = new
            {
                messaging_product = "whatsapp",
                recipient_type = "individual",
                to,
                type = "interactive",
                interactive = new
                {
                    type = "cta_url",
                    header = new
                    {
                        type = "text",
                        text = "Asesor Auto Tyre"
                    },
                    body = new
                    {
                        text =
                            "👨‍💼 *Atención personalizada Auto Tyre*\n\n" +
                            "Un asesor puede apoyarte con:\n\n" +
                            "🛞 Cotización de llantas\n" +
                            "📍 Sucursal más cercana\n" +
                            "🚚 Flotillas\n" +
                            "🛟 Auxilio vial\n" +
                            "🏁 Franquicias\n\n" +
                            "Presiona el botón para abrir el chat directo con un asesor."
                    },
                    footer = new
                    {
                        text = "Respuesta por WhatsApp"
                    },
                    action = new
                    {
                        name = "cta_url",
                        parameters = new
                        {
                            display_text = "Chatear con asesor",
                            url = advisorUrl
                        }
                    }
                }
            };

            await SendPayload(payload, "Advisor CTA URL response");
        }

        // =========================================
        // ENVÍO DE TEXTO
        // =========================================
        private async Task SendTextMessage(string to, string text)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "text",
                text = new
                {
                    preview_url = false,
                    body = text
                }
            };

            await SendPayload(payload, "Text response");
        }

        // =========================================
        // ENVÍO DE IMAGEN OPCIONAL
        // =========================================
        private async Task SendWelcomeImage(string to)
        {
            if (string.IsNullOrWhiteSpace(WELCOME_IMAGE_URL))
                return;

            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "image",
                image = new
                {
                    link = WELCOME_IMAGE_URL,
                    caption =
                        "👋 Bienvenido a *Auto Tyre*.\n\n" +
                        "La red llantera más grande de México."
                }
            };

            await SendPayload(payload, "Image response");
        }

        // =========================================
        // MARCAR MENSAJE COMO LEÍDO
        // =========================================
        private async Task MarkMessageAsRead(string? messageId)
        {
            if (string.IsNullOrWhiteSpace(messageId))
                return;

            var payload = new
            {
                messaging_product = "whatsapp",
                status = "read",
                message_id = messageId
            };

            await SendPayload(payload, "Read response");
        }

        // =========================================
        // MÉTODO GENERAL PARA ENVIAR PAYLOAD A META
        // =========================================
        private async Task SendPayload(object payload, string logName)
        {
            if (string.IsNullOrWhiteSpace(ACCESS_TOKEN))
            {
                Console.WriteLine("ERROR: WHATSAPP_TOKEN no está configurado.");
                return;
            }

            if (string.IsNullOrWhiteSpace(PHONE_NUMBER_ID))
            {
                Console.WriteLine("ERROR: WHATSAPP_PHONE_NUMBER_ID no está configurado.");
                return;
            }

            using var client = new HttpClient();

            client.DefaultRequestHeaders.Add(
                "Authorization",
                $"Bearer {ACCESS_TOKEN}");

            var json = JsonConvert.SerializeObject(payload);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            var url =
                $"https://graph.facebook.com/{GRAPH_VERSION}/{PHONE_NUMBER_ID}/messages";

            var response = await client.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"{logName}: {result}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ERROR HTTP {(int)response.StatusCode}: {result}");
            }
        }
    }
}