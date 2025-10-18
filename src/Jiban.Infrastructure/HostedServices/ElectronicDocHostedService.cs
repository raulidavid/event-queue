using Jiban.BaseCode.PermissionsCode;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Jiban.Nswag;

namespace Jiban.Infrastructure.HostedServices
{
    public partial class ElectronicDocHostedService
    {
        /// <summary>
        /// Ejecutar proceso principal del servicio
        /// </summary>
        /// <param name="cancellationToken">Token para detener el proceso</param>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Log de configuración inicial para debugging
                _logger.LogInformation(JibanConstants.CONFIG_DEBUG_HEADER);
                
                // Obtener configuraciones usando las constantes
                string finishLetterQueue = _configuration[JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE] ?? JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE;
                string redisExecutionGroup = JibanConstants.REDIS_EXECUTION_GROUP;
                string newMessagesCount = _configuration[JibanConstants.NOTIFICATION_NEW_MESSAGES_COUNT] ?? "10";
                string pendingMessagesCount = _configuration[JibanConstants.NOTIFICATION_PENDING_MESSAGES_COUNT] ?? "10";
                string waitSeconds = _configuration[JibanConstants.NOTIFICATION_WAIT_SECONDS] ?? "30";
                string retryAttempts = _configuration[JibanConstants.NOTIFICATION_RETRY_ATTEMPTS] ?? "3";
                string deadLetterQueue = _configuration[JibanConstants.NOTIFICATION_DEAD_LETTER_QUEUE] ?? "dead-letter-queue";

                _logger.LogInformation("📋 Configuración cargada:");
                _logger.LogInformation("   • NOTIFICATION_FINISH_LETTER_QUEUE: {FinishLetterQueue}", finishLetterQueue);
                _logger.LogInformation("   • REDIS_EXECUTION_GROUP: {RedisExecutionGroup}", redisExecutionGroup);
                _logger.LogInformation("   • NOTIFICATION_NEW_MESSAGES_COUNT: {NewMessagesCount}", newMessagesCount);
                _logger.LogInformation("   • NOTIFICATION_PENDING_MESSAGES_COUNT: {PendingMessagesCount}", pendingMessagesCount);
                _logger.LogInformation("   • NOTIFICATION_WAIT_SECONDS: {WaitSeconds}", waitSeconds);
                _logger.LogInformation("   • NOTIFICATION_RETRY_ATTEMPTS: {RetryAttempts}", retryAttempts);
                _logger.LogInformation("   • NOTIFICATION_DEAD_LETTER_QUEUE: {DeadLetterQueue}", deadLetterQueue);
                _logger.LogInformation("=====================================");

                // Crear el grupo de consumidores
                _logger.LogInformation("🔗 Creando grupo de consumidores para cola: {QueueName}", finishLetterQueue);
                
                try
                {
                    await _eventService.CreateConsumerGroupAsync(finishLetterQueue, redisExecutionGroup);
                    _logger.LogInformation("{SuccessEmoji} Grupo de consumidores creado exitosamente", JibanConstants.SUCCESS_EMOJI);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "{WarningEmoji} El grupo de consumidores ya existe o hubo un error menor: {Message}", 
                        JibanConstants.WARNING_EMOJI, ex.Message);
                }

                // Verificar que la cola tenga mensajes disponibles
                await VerificarEstadoCola(finishLetterQueue, redisExecutionGroup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ErrorEmoji} Error durante la inicialización del servicio: {Message}", 
                    JibanConstants.ERROR_EMOJI, ex.Message);
                throw;
            }

            // Almacenar configuraciones para uso en el ciclo principal
            string queueName = _configuration[JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE] ?? JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE;
            string executionGroup = JibanConstants.REDIS_EXECUTION_GROUP;

            // 🔄 ESTRATEGIA DE PROCESAMIENTO: Pendientes primero, luego nuevos
            // ================================================================
            // ¿Por qué procesamos pendientes primero?
            // 1. Los pendientes son mensajes que fallaron antes → prioridad alta
            // 2. Evita acumulación de mensajes no confirmados
            // 3. Garantiza que no perdemos mensajes por errores temporales
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    DateTime fechaEjecucion = Convert.ToDateTime(DateTime.Now.ToString(JibanConstants.DATE_FORMAT));
                    _logger.LogInformation(JibanConstants.HOSTED_2_MESSAGE, 
                        JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                        JibanConstants.EXECUTION_DATE, 
                        fechaEjecucion);
                    
                    _logger.LogInformation(JibanConstants.HOSTED_1_MESSAGE, 
                        JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                        JibanConstants.START_PROCESSING_MESSAGE);
                    
                    // Procesar mensajes pendientes primero (prioridad alta)
                    await ProcesarMensajesPendientes(queueName, executionGroup);
                    
                    // Luego procesar mensajes nuevos
                    await ProcesarMensajesNuevos(queueName, executionGroup);
                    
                    _logger.LogInformation(JibanConstants.HOSTED_1_MESSAGE, 
                        JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                        JibanConstants.END_PROCESSING_MESSAGE);
                    
                    _logger.LogInformation(JibanConstants.HOSTED_1_MESSAGE, 
                        JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                        JibanConstants.WAITING_MESSAGE);
                    
                    // Esperar antes del siguiente ciclo
                    int waitSeconds = Convert.ToInt32(_configuration[JibanConstants.NOTIFICATION_WAIT_SECONDS] ?? "30");
                    _logger.LogInformation("{WaitingEmoji} Esperando {WaitSeconds} segundos antes del próximo ciclo", 
                        JibanConstants.WAITING_EMOJI, waitSeconds);
                    
                    await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("{StopEmoji} Servicio cancelado por token de cancelación", JibanConstants.STOP_EMOJI);
                    break;
                }
                catch (Exception excepcion)
                {
                    _logger.LogError(JibanConstants.HOSTED_2_MESSAGE, 
                        JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                        excepcion, 
                        excepcion.Message);
                    
                    // Esperar un poco antes de reintentar para evitar loops de error
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }
        }

        /// <summary>
        /// Verificar el estado actual de la cola Redis
        /// </summary>
        /// <param name="queueName">Nombre de la cola</param>
        /// <param name="executionGroup">Grupo de ejecución</param>
        private async Task VerificarEstadoCola(string queueName, string executionGroup)
        {
            try
            {
                _logger.LogInformation("🔍 Verificando estado de la cola: {QueueName} con grupo: {ExecutionGroup}", queueName, executionGroup);
                
                // Intentar leer algunos mensajes para verificar conectividad
                try
                {
                    // NOTA: ReadNewMessagesAsync internamente hace:
                    // XREADGROUP GROUP {executionGroup} {consumer} COUNT 1 STREAMS {queueName} >
                    // El ">" significa "dame mensajes más nuevos que el último entregado al grupo"
                    var testMessages = await _eventService.ReadNewMessagesAsync(queueName, executionGroup, 1);
                    _logger.LogInformation("{SuccessEmoji} Verificación de cola completada - Conectividad OK", JibanConstants.SUCCESS_EMOJI);
                    
                    if (testMessages.Length > JibanConstants.ZERO_VALUE)
                    {
                        _logger.LogInformation("📊 Se encontraron mensajes NUEVOS en la cola para procesar");
                    }
                    else
                    {
                        _logger.LogInformation("📭 No hay mensajes NUEVOS en la cola actualmente");
                    }
                    
                    // También verificar si hay mensajes pendientes
                    var pendingMessages = await _eventService.ReadPendingMessagesAsync(queueName, executionGroup, 1);
                    if (pendingMessages.Length > JibanConstants.ZERO_VALUE)
                    {
                        _logger.LogInformation("📤 Se encontraron {Count} mensajes PENDIENTES", pendingMessages.Length);
                    }
                    else
                    {
                        _logger.LogInformation("{SuccessEmoji} No hay mensajes pendientes - estado limpio", JibanConstants.SUCCESS_EMOJI);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ErrorEmoji} Error verificando mensajes: {Message}", JibanConstants.ERROR_EMOJI, ex.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{WarningEmoji} Error al verificar estado de la cola: {Message}", JibanConstants.WARNING_EMOJI, ex.Message);
            }
        }

        /// <summary>
        /// Detener el servicio
        /// </summary>
        /// <param name="cancellationToken">Token de cancelación</param>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(JibanConstants.HOSTED_1_MESSAGE, 
                JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                JibanConstants.STOPPING);
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Procesar mensajes nuevos desde la cola Redis
        /// 
        /// COMANDO REDIS EQUIVALENTE:
        /// XREADGROUP GROUP {executionGroup} {consumer} COUNT {maxCount} STREAMS {queueName} >
        /// 
        /// El ">" significa: "dame mensajes con ID mayor al último entregado a este grupo"
        /// </summary>
        private async Task ProcesarMensajesNuevos(string queueName, string executionGroup)
        {
            try
            {
                int maxCount = Convert.ToInt32(_configuration[JibanConstants.NOTIFICATION_NEW_MESSAGES_COUNT] ?? "10");
                
                _logger.LogInformation("📥 Buscando mensajes NUEVOS en cola: {QueueName}, grupo: {Group}, max: {MaxCount}", 
                    queueName, executionGroup, maxCount);
                
                // 📥 AQUÍ REDIS DETERMINA QUE SON "NUEVOS":
                // 1. Redis mantiene un "last-delivered-id" para cada Consumer Group
                // 2. Solo devuelve mensajes con ID > last-delivered-id
                // 3. Al leer un mensaje, automáticamente actualiza last-delivered-id
                // 4. El mensaje pasa a la PEL (Pending Entries List) del consumidor
                StreamEntry[] mensajesNuevos = await _eventService.ReadNewMessagesAsync(queueName, executionGroup, maxCount);
                
                _logger.LogInformation(JibanConstants.HOSTED_2_MESSAGE, 
                    JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                    JibanConstants.NEW_MESSAGES, 
                    mensajesNuevos.Length.ToString());

                if (mensajesNuevos.Length == JibanConstants.ZERO_VALUE)
                {
                    _logger.LogDebug("{InfoEmoji} No hay mensajes nuevos en la cola: {QueueName}", 
                        JibanConstants.INFO_EMOJI, queueName);
                    return;
                }

                foreach (StreamEntry mensaje in mensajesNuevos)
                {
                    // IMPORTANTE: En este punto el mensaje YA está en PEL como "pendiente"
                    // Si falla el procesamiento, quedará pendiente para el próximo ciclo
                    await ProcesarMensajeIndividual(mensaje, queueName, executionGroup, "NUEVO");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ErrorEmoji} Error procesando mensajes nuevos: {Message}", 
                    JibanConstants.ERROR_EMOJI, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Procesar mensajes pendientes desde la cola Redis
        /// 
        /// COMANDOS REDIS EQUIVALENTES:
        /// 1. XPENDING {queueName} {executionGroup} - obtener lista de mensajes pendientes
        /// 2. XCLAIM {queueName} {executionGroup} {consumer} {min-idle-time} {message-id}
        /// 
        /// Los mensajes pendientes son aquellos que fueron leídos pero no confirmados con XACK
        /// </summary>
        private async Task ProcesarMensajesPendientes(string queueName, string executionGroup)
        {
            try
            {
                int maxCount = Convert.ToInt32(_configuration[JibanConstants.NOTIFICATION_PENDING_MESSAGES_COUNT] ?? "10");
                
                _logger.LogInformation("📤 Buscando mensajes PENDIENTES en cola: {QueueName}, grupo: {Group}, max: {MaxCount}", 
                    queueName, executionGroup, maxCount);
                
                // 📤 AQUÍ REDIS DETERMINA QUE SON "PENDIENTES":
                // 1. Redis mantiene una PEL (Pending Entries List) por consumidor
                // 2. Cuando un mensaje se lee con XREADGROUP, va automáticamente a PEL
                // 3. Solo se elimina de PEL cuando se hace XACK (acknowledgment)
                // 4. ReadPendingMessagesAsync() recupera mensajes de PEL que no han sido XACK
                StreamEntry[] mensajesPendientes = await _eventService.ReadPendingMessagesAsync(queueName, executionGroup, maxCount);
                
                _logger.LogInformation(JibanConstants.HOSTED_2_MESSAGE, 
                    JibanConstants.HOSTED_SERVICES_NOTIFICATION_START, 
                    JibanConstants.PENDING_MESSAGES, 
                    mensajesPendientes.Length.ToString());

                if (mensajesPendientes.Length == JibanConstants.ZERO_VALUE)
                {
                    _logger.LogDebug("{InfoEmoji} No hay mensajes pendientes en la cola: {QueueName}", 
                        JibanConstants.INFO_EMOJI, queueName);
                    return;
                }

                foreach (StreamEntry mensaje in mensajesPendientes)
                {
                    await ProcesarMensajePendienteIndividual(mensaje, queueName, executionGroup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ErrorEmoji} Error procesando mensajes pendientes: {Message}", 
                    JibanConstants.ERROR_EMOJI, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Procesar un mensaje individual (nuevo)
        /// </summary>
        /// <param name="mensaje">Mensaje a procesar</param>
        /// <param name="queueName">Nombre de la cola</param>
        /// <param name="executionGroup">Grupo de ejecución</param>
        /// <param name="tipoMensaje">Tipo de mensaje para logging</param>
        private async Task ProcesarMensajeIndividual(StreamEntry mensaje, string queueName, string executionGroup, string tipoMensaje)
        {
            string mensajeId = mensaje.Id;
            _logger.LogInformation("{ProcessingEmoji} Procesando mensaje {TipoMensaje} ID: {MessageId}", 
                JibanConstants.PROCESSING_EMOJI, tipoMensaje, mensajeId);
            
            try
            {
                // Log del contenido del mensaje para debugging
                _logger.LogDebug("📋 Contenido del mensaje {MessageId}: {MessageContent}", mensajeId, 
                    string.Join(", ", mensaje.Values.Select(v => $"{v.Name}={v.Value}")));
                
                // Validar el mensaje
                if (!BaseUtil.ValidarMensajeRedis(mensaje))
                {
                    _logger.LogWarning("{WarningEmoji} Mensaje inválido encontrado ID: {MessageId}, eliminando...", 
                        JibanConstants.WARNING_EMOJI, mensajeId);
                    // XACK - confirma el mensaje y lo elimina de PEL
                    await _eventService.DeleteMessageById(queueName, executionGroup, mensajeId);
                    return;
                }

                // Deserializar el mensaje
                DocumentEmailRequest documentEmailRequest;
                try
                {
                    documentEmailRequest = _eventService.GetMessage<DocumentEmailRequest>(mensaje);
                    _logger.LogInformation("{SuccessEmoji} Mensaje deserializado correctamente: SriDocumentId={SriDocumentId}, Email={Email}, DocumentSriType={DocumentSriType}", 
                        JibanConstants.SUCCESS_EMOJI, documentEmailRequest.SriDocumentId, documentEmailRequest.Email, documentEmailRequest.DocumentSriType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{ErrorEmoji} Error deserializando mensaje {MessageId}: {Message}", 
                        JibanConstants.ERROR_EMOJI, mensajeId, ex.Message);
                    // Eliminar mensaje que no se puede deserializar
                    await _eventService.DeleteMessageById(queueName, executionGroup, mensajeId);
                    return;
                }

                // Procesar el evento
                await ProcesarEvento(documentEmailRequest);
                
                // 🎯 CRÍTICO: Solo si llegamos aquí sin excepciones, confirmamos el mensaje
                // DeleteMessageById() internamente hace XACK para eliminar de PEL
                await _eventService.DeleteMessageById(queueName, executionGroup, mensajeId);
                _logger.LogInformation("{SuccessEmoji} Mensaje {TipoMensaje} {MessageId} procesado y eliminado exitosamente", 
                    JibanConstants.SUCCESS_EMOJI, tipoMensaje, mensajeId);
            }
            catch (Exception excepcion)
            {
                _logger.LogError(JibanConstants.HOSTED_1_MESSAGE, 
                    JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE, 
                    JibanConstants.ERROR_MESSAGE, 
                    mensajeId, 
                    tipoMensaje);
                _logger.LogError(JibanConstants.HOSTED_2_MESSAGE, 
                    JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE, 
                    excepcion, 
                    excepcion.Message);
                // ❌ NO eliminamos el mensaje en caso de error
                // El mensaje quedará como PENDIENTE para el próximo ciclo
                // Esto es lo que permite el mecanismo de reintento automático
            }
        }

        /// <summary>
        /// Procesar un mensaje pendiente individual
        /// </summary>
        /// <param name="mensaje">Mensaje pendiente a procesar</param>
        /// <param name="queueName">Nombre de la cola</param>
        /// <param name="executionGroup">Grupo de ejecución</param>
        private async Task ProcesarMensajePendienteIndividual(StreamEntry mensaje, string queueName, string executionGroup)
        {
            string mensajeId = mensaje.Id;
            
            try
            {
                // Validar el mensaje
                if (!BaseUtil.ValidarMensajeRedis(mensaje))
                {
                    _logger.LogWarning("{WarningEmoji} Mensaje pendiente inválido encontrado ID: {MessageId}, eliminando...", 
                        JibanConstants.WARNING_EMOJI, mensajeId);
                    await _eventService.DeleteMessageById(queueName, executionGroup, mensajeId);
                    return;
                }

                // 📊 OBTENER INFORMACIÓN DE ENTREGA:
                // GetPendingMessageById devuelve información sobre:
                // - DeliveryCount: cuántas veces se ha intentado procesar
                // - LastDeliveryTime: cuándo fue la última entrega
                // - ConsumerName: qué consumidor lo tiene
                StreamPendingMessageInfo informacionMensajePendiente = await _eventService.GetPendingMessageById(queueName, executionGroup, mensajeId);
                DocumentEmailRequest eventAuthorizeDocument = _eventService.GetMessage<DocumentEmailRequest>(mensaje);

                // Verificar intentos de reintento
                int maxRetryAttempts = Convert.ToInt32(_configuration[JibanConstants.NOTIFICATION_RETRY_ATTEMPTS] ?? "3");
                
                _logger.LogInformation("{ProcessingEmoji} Procesando mensaje pendiente ID: {MessageId}, Intentos: {DeliveryCount}/{MaxRetryAttempts}", 
                    JibanConstants.PROCESSING_EMOJI, mensajeId, informacionMensajePendiente.DeliveryCount, maxRetryAttempts);

                if (informacionMensajePendiente.DeliveryCount > maxRetryAttempts)
                {
                    _logger.LogWarning("{WarningEmoji} Mensaje {MessageId} excedió intentos de reintento ({DeliveryCount} > {MaxRetryAttempts}), moviendo a cola de mensajes muertos", 
                        JibanConstants.WARNING_EMOJI, mensajeId, informacionMensajePendiente.DeliveryCount, maxRetryAttempts);
                    
                    await ProcesarMensajeRezagado(eventAuthorizeDocument);
                    await _eventService.DeleteMessageById(queueName, executionGroup, mensajeId);
                }
                else
                {
                    // Intentar procesar el mensaje
                    await ProcesarEvento(eventAuthorizeDocument);
                    await _eventService.DeleteMessageById(queueName, executionGroup, mensajeId);
                    _logger.LogInformation("{SuccessEmoji} Mensaje pendiente {MessageId} procesado y eliminado exitosamente", 
                        JibanConstants.SUCCESS_EMOJI, mensajeId);
                }
            }
            catch (Exception excepcion)
            {
                _logger.LogError(JibanConstants.HOSTED_1_MESSAGE, 
                    JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                    JibanConstants.ERROR_MESSAGE, 
                    mensajeId, 
                    excepcion.Message);
                _logger.LogError(JibanConstants.HOSTED_2_MESSAGE, 
                    JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                    excepcion, 
                    excepcion.Message);
                // No eliminamos el mensaje en caso de error - seguirá siendo pendiente
            }
        }

        /// <summary>
        /// Procesar el evento que se recibe de la cola Redis
        /// </summary>
        /// <param name="documentEmailRequest">Modelo con información de la solicitud a procesar</param>
        private async Task ProcesarEvento(DocumentEmailRequest documentEmailRequest)
        {
            try
            {

                _logger.LogInformation("{ProcessingEmoji} Procesando evento - Autorizar Documento", 
                    JibanConstants.PROCESSING_EMOJI);

                // PROCESAMIENTO REAL DEL EVENTO
                _logger.LogInformation("📄 Iniciando procesamiento de documentos electrónicos para: {Identificacion}", documentEmailRequest.Email);
                documentEmailRequest.ProcessingType = ProcessingType.Direct;
                var tokenAndRefreshToken = await _tokenBuilder.GenerateTokenAndRefreshTokenAsync(documentEmailRequest.UserId);
                _tokenAccessor.SetToken(tokenAndRefreshToken.Token);
                await SendEmailDocumentsAsync(documentEmailRequest);
                _tokenAccessor.ClearToken();

                _logger.LogInformation("{SuccessEmoji} Evento procesado exitosamente - Autorizar Documento", 
                    JibanConstants.SUCCESS_EMOJI);
            }
            catch (Exception excepcion)
            {
                _logger.LogError(JibanConstants.HOSTED_2_MESSAGE, 
                    JibanConstants.HOSTED_SERVICES_NOTIFICATION_END, 
                    excepcion, 
                    excepcion.Message);
                throw;
            }
        }

        /// <summary>
        /// Procesar mensaje que ha excedido los intentos de reintento (dead letter queue)
        /// </summary>
        /// <param name="eventAuthorizeDocument">Mensaje a mover a cola de mensajes muertos</param>
        private async Task ProcesarMensajeRezagado(DocumentEmailRequest eventAuthorizeDocument)
        {
            try
            {
                string deadLetterQueue = _configuration[JibanConstants.NOTIFICATION_DEAD_LETTER_QUEUE];
                
                if (string.IsNullOrEmpty(deadLetterQueue))
                {
                    _logger.LogWarning("{WarningEmoji} No se configuró cola de mensajes muertos, mensaje se perderá", 
                        JibanConstants.WARNING_EMOJI);
                    return;
                }

                _logger.LogInformation("📤 {Move} {DeadLetterQueue}", 
                    JibanConstants.MOVE, deadLetterQueue);
                
                await _eventService.CreateConsumerGroupAsync(deadLetterQueue, JibanConstants.REDIS_EXECUTION_GROUP);
                
                // TODO: Implementar PublishAsync o el método apropiado para mover a dead letter queue
                // await _eventService.PublishAsync(deadLetterQueue, eventAuthorizeDocument);
                
                _logger.LogInformation("{SuccessEmoji} Mensaje movido a cola de mensajes muertos: {DeadLetterQueue}", 
                    JibanConstants.SUCCESS_EMOJI, deadLetterQueue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{ErrorEmoji} Error moviendo mensaje a cola de mensajes muertos: {Message}", 
                    JibanConstants.ERROR_EMOJI, ex.Message);
            }
        }

        /// <summary>
        /// Liberar recursos del servicio
        /// </summary>
        public override void Dispose()
        {
            _logger.LogInformation("🧹 Liberando recursos de ElectronicDocHostedService");
            _serviceScope?.Dispose();
            base.Dispose();
        }
    }
}

/*
📊 RESUMEN DE CÓMO REDIS DISTINGUE MENSAJES NUEVOS VS PENDIENTES:
=================================================================

🏗️ ESTRUCTURA INTERNA DE REDIS:

Stream: "Notification:FinishLetterQueue"
├── Entry: "1760134131299-0" → {data}
├── Entry: "1760134131300-1" → {data}
└── Entry: "1760134131301-2" → {data}

Consumer Group: "jiban-processors"
├── last-delivered-id: "1760134131300-1"  ← Solo entregar mensajes > este ID
├── Consumer: "consumer-1"
│   └── PEL (Pending Entries List):
│       ├── "1760134131299-0" (delivery-count: 2, last-delivery: 10min ago)
│       └── "1760134131300-1" (delivery-count: 1, last-delivery: 5min ago)
└── Consumer: "consumer-2" 
    └── PEL: (vacía)

🔄 PROCESO:

1. ReadNewMessagesAsync() → XREADGROUP ... > 
   - Solo devuelve mensajes con ID > "1760134131300-1"
   - Si encuentra "1760134131301-2", lo entrega y actualiza last-delivered-id
   - El mensaje va automáticamente a PEL del consumidor

2. ReadPendingMessagesAsync() → XPENDING + XCLAIM
   - Busca mensajes en PEL que no han sido XACK
   - Puede "reclamar" mensajes de otros consumidores inactivos
   - Incrementa delivery-count

3. DeleteMessageById() → XACK
   - Elimina el mensaje de PEL
   - El mensaje se considera "procesado exitosamente"

❌ Si falla el procesamiento:
   - NO se hace XACK
   - Mensaje permanece en PEL
   - Próximo ReadPendingMessagesAsync() lo recupera
   - Se incrementa delivery-count

✅ Si el procesamiento es exitoso:
   - Se hace XACK 
   - Mensaje se elimina de PEL
   - No se volverá a procesar

🎯 TU PROBLEMA PROBABLE:
- Verifica que el nombre de la cola sea exactamente "Notification:FinishLetterQueue"
- Verifica que el grupo de consumidores "jiban-processors" exista
- Verifica que haya conexión a Redis
- Mira los logs para ver si hay errores de deserialización
*/