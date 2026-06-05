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

                if (string.IsNullOrWhiteSpace(message))
                    return Ok();

                await MarkMessageAsRead(messageNode["id"]?.ToString());

                // =========================================
                // MENÚ PRINCIPAL
                // =========================================
                if (IsMenuIntent(message))
                {
                    await SendWelcomeMessage(from);
                    await Task.Delay(800);
                    await SendMainMenu(from);
                }

                // =========================================
                // COMPRAR / COTIZAR LLANTAS
                // =========================================
                else if (message == "comprar_llantas" ||
                         message.Contains("comprar") ||
                         message.Contains("cotizar") ||
                         message.Contains("llanta") ||
                         message.Contains("llantas"))
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
                // BUSCAR SUCURSAL
                // =========================================
                else if (message == "buscar_sucursal" ||
                         message.Contains("sucursal") ||
                         message.Contains("ubicacion") ||
                         message.Contains("ubicación"))
                {
                    await SendBranchSearchMessage(from);
                }

                // =========================================
                // FLOTILLAS
                // =========================================
                else if (message == "flotillas" ||
                         message.Contains("flotilla") ||
                         message.Contains("empresa") ||
                         message.Contains("unidades"))
                {
                    await SendFleetMessage(from);
                }

                // =========================================
                // FRANQUICIAS / EXPANSIÓN
                // =========================================
                else if (message == "franquicias" ||
                         message == "expansion" ||
                         message == "expansión" ||
                         message.Contains("franquicia") ||
                         message.Contains("invertir"))
                {
                    await SendFranchiseMessage(from);
                }

                // =========================================
                // AUXILIO VIAL
                // =========================================
                else if (message == "auxilio_vial" ||
                         message.Contains("auxilio") ||
                         message.Contains("asistencia") ||
                         message.Contains("vial") ||
                         message.Contains("ponchada") ||
                         message.Contains("ponchado"))
                {
                    await SendRoadAssistanceMessage(from);
                }

                // =========================================
                // CONTACTAR ASESOR
                // =========================================
                else if (message == "contactar_asesor" ||
                         message.Contains("asesor") ||
                         message.Contains("humano") ||
                         message.Contains("persona") ||
                         message.Contains("agente"))
                {
                    await SendAdvisorMessage(from);
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
                        "Para consultar una sucursal específica, selecciona *Buscar sucursal* en el menú.\n\n" +
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
                        "Puedes escribir *menú* para ver las opciones disponibles:\n\n" +
                        "🔎 Comprar llantas\n" +
                        "📍 Buscar sucursal\n" +
                        "🚚 Flotillas\n" +
                        "🏁 Franquicias\n" +
                        "🛟 Auxilio vial\n" +
                        "👨‍💼 Contactar asesor");
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
                "Soy el asistente virtual de Auto Tyre y puedo ayudarte con:\n\n" +
                "🔎 Cotización de llantas\n" +
                "📍 Búsqueda de sucursales\n" +
                "🚚 Atención a flotillas\n" +
                "🏁 Información de franquicias\n" +
                "🛟 Auxilio vial\n" +
                "👨‍💼 Contactar a un asesor\n\n" +
                "Selecciona una opción del menú.";

            await SendTextMessage(to, text);
        }

        // =========================================
        // MENÚ PRINCIPAL INTERACTIVO
        // =========================================
        private async Task SendMainMenu(string to)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "interactive",
                interactive = new
                {
                    type = "list",
                    header = new
                    {
                        type = "text",
                        text = "Auto Tyre"
                    },
                    body = new
                    {
                        text = "¿Qué necesitas hacer hoy?"
                    },
                    footer = new
                    {
                        text = "Selecciona una opción para continuar"
                    },
                    action = new
                    {
                        button = "Ver opciones",
                        sections = new[]
                        {
                            new
                            {
                                title = "Atención Auto Tyre",
                                rows = new[]
                                {
                                    new
                                    {
                                        id = "comprar_llantas",
                                        title = "Comprar llantas",
                                        description = "Cotiza por medida o vehículo"
                                    },
                                    new
                                    {
                                        id = "buscar_sucursal",
                                        title = "Buscar sucursal",
                                        description = "Encuentra atención cercana"
                                    },
                                    new
                                    {
                                        id = "flotillas",
                                        title = "Flotillas",
                                        description = "Atención para empresas"
                                    },
                                    new
                                    {
                                        id = "franquicias",
                                        title = "Franquicias",
                                        description = "Invierte en Auto Tyre"
                                    },
                                    new
                                    {
                                        id = "auxilio_vial",
                                        title = "Auxilio vial",
                                        description = "Soporte en carretera"
                                    },
                                    new
                                    {
                                        id = "contactar_asesor",
                                        title = "Contactar asesor",
                                        description = "Habla con una persona"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            await SendPayload(payload, "Main menu response");
        }

        // =========================================
        // COTIZACIÓN DE LLANTAS
        // =========================================
        private async Task SendTireQuoteMenu(string to)
        {
            var text =
                "🔎 *Cotización de llantas*\n\n" +
                "Para cotizar más rápido, envíame la medida de tu llanta.\n\n" +
                "Ejemplos:\n" +
                "• 205/55 R16\n" +
                "• 225/60 R17\n" +
                "• 265/70 R16\n\n" +
                "También puedes enviarme:\n" +
                "🚗 Marca del vehículo\n" +
                "🚙 Modelo\n" +
                "📅 Año\n\n" +
                "Ejemplo:\n" +
                "*Toyota Hilux 2021*";

            await SendTextMessage(to, text);
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
                "Un asesor puede confirmar disponibilidad, precio final, envío y promociones vigentes.";

            await SendTextMessage(to, text);
        }

        // =========================================
        // SUCURSALES
        // =========================================
        private async Task SendBranchSearchMessage(string to)
        {
            var text =
                "📍 *Buscar sucursal Auto Tyre*\n\n" +
                "Tenemos cobertura nacional con más de 60 puntos de atención.\n\n" +
                "Para ayudarte a encontrar la sucursal más cercana, envíame:\n\n" +
                "📌 Ciudad o estado\n" +
                "📌 Código postal, si lo tienes\n\n" +
                "Ejemplo:\n" +
                "*Mérida, Yucatán*\n\n" +
                "También puedes escribir *Contactar asesor* para que una persona te apoye.";

            await SendTextMessage(to, text);
        }

        // =========================================
        // FLOTILLAS
        // =========================================
        private async Task SendFleetMessage(string to)
        {
            var text =
                "🚚 *Atención a flotillas Auto Tyre*\n\n" +
                "Ofrecemos soluciones para empresas con unidades operativas:\n\n" +
                "✅ Suministro de llantas\n" +
                "✅ Montaje y balanceo\n" +
                "✅ Alineación\n" +
                "✅ Suspensión y frenos\n" +
                "✅ Inspección de seguridad\n" +
                "✅ Precios preferenciales por volumen\n" +
                "✅ Asesoría según ruta y tipo de unidad\n\n" +
                "Para iniciar, envíanos:\n\n" +
                "🏢 Nombre de empresa\n" +
                "🚚 Número de unidades\n" +
                "📍 Ciudad/estado\n" +
                "📞 Nombre y teléfono de contacto\n\n" +
                "Ejemplo:\n" +
                "*Transportes del Norte, 35 unidades, Monterrey, Carlos 55...*";

            await SendTextMessage(to, text);
        }

        // =========================================
        // FRANQUICIAS / EXPANSIÓN
        // =========================================
        private async Task SendFranchiseMessage(string to)
        {
            var payload = new
            {
                messaging_product = "whatsapp",
                to,
                type = "interactive",
                interactive = new
                {
                    type = "list",
                    header = new
                    {
                        type = "text",
                        text = "Franquicias"
                    },
                    body = new
                    {
                        text =
                            "🏁 *Expansión Auto Tyre*\n\n" +
                            "Selecciona el modelo de franquicia que deseas conocer:"
                    },
                    footer = new
                    {
                        text = "Auto Tyre - Modelo de negocio"
                    },
                    action = new
                    {
                        button = "Ver modelos",
                        sections = new[]
                        {
                            new
                            {
                                title = "Modelos disponibles",
                                rows = new[]
                                {
                                    new
                                    {
                                        id = "franquicia_express",
                                        title = "Modelo Express",
                                        description = "Desde $1,800,000 + IVA"
                                    },
                                    new
                                    {
                                        id = "franquicia_premium",
                                        title = "Modelo Premium",
                                        description = "Desde $2,218,000 + IVA"
                                    },
                                    new
                                    {
                                        id = "franquicia_diamante",
                                        title = "Modelo Diamante",
                                        description = "Desde $3,013,000 + IVA"
                                    },
                                    new
                                    {
                                        id = "franquicia_movil",
                                        title = "Modelo móvil",
                                        description = "Desde $879,000 + IVA"
                                    },
                                    new
                                    {
                                        id = "contactar_asesor",
                                        title = "Contactar asesor",
                                        description = "Hablar con expansión"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            await SendPayload(payload, "Franchise menu response");
        }

        // =========================================
        // DETALLE DE MODELOS DE FRANQUICIA
        // =========================================
        private async Task SendFranchiseModelDetail(string to, string model)
        {
            string text = model switch
            {
                "franquicia_express" =>
                    "🏁 *Modelo Express Auto Tyre*\n\n" +
                    "💰 Inversión estimada: *$1,800,000 + IVA*\n" +
                    "📐 Espacio requerido: *80 a 120 m²*\n" +
                    "🛞 Inventario inicial: *150 llantas*\n" +
                    "🏗️ Ideal para zonas urbanas de alto flujo.\n\n" +
                    "Para iniciar el proceso escribe *Contactar asesor*.",

                "franquicia_premium" =>
                    "⭐ *Modelo Premium Auto Tyre*\n\n" +
                    "💰 Inversión estimada: *$2,218,000 + IVA*\n" +
                    "📐 Espacio requerido: *150 a 200 m²*\n" +
                    "🛞 Inventario inicial: *200 llantas*\n" +
                    "🛋️ Incluye sala de espera.\n\n" +
                    "Para iniciar el proceso escribe *Contactar asesor*.",

                "franquicia_diamante" =>
                    "💎 *Modelo Diamante Auto Tyre*\n\n" +
                    "💰 Inversión estimada: *$3,013,000 + IVA*\n" +
                    "📐 Espacio requerido: *200 a 350 m²*\n" +
                    "🛞 Inventario inicial: *250 llantas*\n" +
                    "🏗️ Mayor capacidad operativa y servicios especializados.\n\n" +
                    "Para iniciar el proceso escribe *Contactar asesor*.",

                "franquicia_movil" =>
                    "🚐 *Modelo Móvil Auto Tyre*\n\n" +
                    "💰 Inversión estimada: *$879,000 + IVA*\n" +
                    "✅ Puesta a punto de camioneta\n" +
                    "✅ Equipos\n" +
                    "✅ Imagen corporativa\n" +
                    "✅ Red de franquicias\n\n" +
                    "⚠️ No incluye la camioneta.\n\n" +
                    "Para iniciar el proceso escribe *Contactar asesor*.",

                _ =>
                    "Selecciona un modelo válido desde el menú de franquicias."
            };

            await SendTextMessage(to, text);
        }

        // =========================================
        // AUXILIO VIAL
        // =========================================
        private async Task SendRoadAssistanceMessage(string to)
        {
            var text =
                "🛟 *Auxilio vial Auto Tyre*\n\n" +
                "Para apoyarte necesitamos algunos datos:\n\n" +
                "1️⃣ Ubicación actual o referencia\n" +
                "2️⃣ Tipo de problema\n" +
                "3️⃣ Tipo de vehículo\n" +
                "4️⃣ Teléfono de contacto\n\n" +
                "Ejemplo:\n" +
                "*Estoy en Periférico Sur, llanta ponchada, camioneta SUV, 55...*\n\n" +
                "Un asesor revisará disponibilidad de apoyo en tu zona.";

            await SendTextMessage(to, text);
        }

        // =========================================
        // CONTACTAR ASESOR
        // =========================================
        private async Task SendAdvisorMessage(string to)
        {
            var text =
                "👨‍💼 *Contactar asesor Auto Tyre*\n\n" +
                "Con gusto te atenderemos.\n\n" +
                "Por favor envíanos:\n\n" +
                "👤 Nombre completo\n" +
                "📞 Teléfono\n" +
                "📍 Ciudad/estado\n" +
                "📝 Motivo de contacto\n\n" +
                "Ejemplo:\n" +
                "*Carlos Ramírez, 55..., CDMX, quiero cotizar 4 llantas.*\n\n" +
                "También puedes llamar al *55 7964 0165*.";

            await SendTextMessage(to, text);
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
        // Úsalo solo si tienes una URL pública válida.
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
        }
    }
}