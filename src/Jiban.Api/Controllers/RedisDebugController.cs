using Jiban.BaseCode.PermissionsCode;
using JibanPermissions.Services;
using Microsoft.AspNetCore.Mvc;
using Jiban.Infrastructure;

namespace Jiban.Api.Controllers
{
    /// <summary>
    /// Controlador para debugging y monitoreo de colas Redis de notificaciones
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class RedisDebugController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RedisDebugController> _logger;

        public RedisDebugController(IEventService eventService, IConfiguration configuration, ILogger<RedisDebugController> logger)
        {
            _eventService = eventService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Verifica que el controlador esté funcionando correctamente
        /// </summary>
        /// <returns>Estado del controlador de debugging</returns>
        /// <response code="200">Controlador funcionando correctamente</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult Health()
        {
            return Ok(new
            {
                Status = "FUNCIONANDO",
                Controller = "RedisDebugController",
                Timestamp = DateTime.UtcNow,
                Message = "El controlador de debugging de Redis está operativo"
            });
        }

        /// <summary>
        /// Obtiene la configuración actual de Redis y las colas de notificaciones
        /// </summary>
        /// <returns>Información de configuración del sistema</returns>
        /// <response code="200">Configuración obtenida exitosamente</response>
        [HttpGet("config")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetRedisConfig()
        {
            var config = new
            {
                RedisHostname = _configuration["Redis:Hostname"],
                RedisPassword = !string.IsNullOrEmpty(_configuration["Redis:Password"]) ? "***CONFIGURADA***" : "***NO CONFIGURADA***",
                StartLetterQueue = _configuration[JibanConstants.NOTIFICATION_START_LETTER_QUEUE],
                FinishLetterQueue = _configuration[JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE],
                DeadLetterQueue = _configuration[JibanConstants.NOTIFICATION_DEAD_LETTER_QUEUE],
                ExecutionGroup = JibanConstants.REDIS_EXECUTION_GROUP,
                NewMessagesCount = _configuration[JibanConstants.NOTIFICATION_NEW_MESSAGES_COUNT],
                PendingMessagesCount = _configuration[JibanConstants.NOTIFICATION_PENDING_MESSAGES_COUNT],
                WaitSeconds = _configuration[JibanConstants.NOTIFICATION_WAIT_SECONDS],
                RetryAttempts = _configuration[JibanConstants.NOTIFICATION_RETRY_ATTEMPTS],
                ConfigurationTimestamp = DateTime.UtcNow
            };

            return Ok(config);
        }

        /// <summary>
        /// Prueba la conexión a Redis y verifica que las colas estén accesibles
        /// </summary>
        /// <returns>Resultado de la prueba de conexión</returns>
        /// <response code="200">Conexión exitosa</response>
        /// <response code="400">Error de configuración</response>
        /// <response code="500">Error de conexión a Redis</response>
        [HttpGet("test-connection")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> TestRedisConnection()
        {
            try
            {
                string queueName = _configuration[JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE];
                
                if (string.IsNullOrEmpty(queueName))
                {
                    return BadRequest(new
                    {
                        Error = "El nombre de la cola no está configurado correctamente",
                        ConfigurationKey = JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE,
                        Timestamp = DateTime.UtcNow
                    });
                }

                // Intentar crear grupo de consumidor (fallará si ya existe, lo cual está bien)
                try
                {
                    await _eventService.CreateConsumerGroupAsync(queueName, JibanConstants.REDIS_EXECUTION_GROUP);
                    _logger.LogInformation($"Grupo de consumidor creado exitosamente: {JibanConstants.REDIS_EXECUTION_GROUP}");
                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Resultado de creación de grupo de consumidor: {ex.Message}");
                }

                // Intentar leer mensajes
                var newMessages = await _eventService.ReadNewMessagesAsync(queueName, JibanConstants.REDIS_EXECUTION_GROUP, 1);
                var pendingMessages = await _eventService.ReadPendingMessagesAsync(queueName, JibanConstants.REDIS_EXECUTION_GROUP, 1);

                return Ok(new
                {
                    ConnectionStatus = "EXITOSA",
                    QueueName = queueName,
                    ExecutionGroup = JibanConstants.REDIS_EXECUTION_GROUP,
                    NewMessagesFound = newMessages.Length,
                    PendingMessagesFound = pendingMessages.Length,
                    Message = "Conexión a Redis establecida correctamente",
                    TestTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la prueba de conexión a Redis");
                return StatusCode(500, new
                {
                    ConnectionStatus = "FALLIDA",
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Message = "No se pudo conectar a Redis. Verifique la configuración de conexión.",
                    TestTimestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Inspecciona los mensajes en la cola de notificaciones finales
        /// </summary>
        /// <param name="maxMessages">Número máximo de mensajes a inspeccionar (por defecto 5)</param>
        /// <returns>Información detallada de los mensajes en la cola</returns>
        /// <response code="200">Mensajes inspeccionados exitosamente</response>
        /// <response code="400">Error de configuración</response>
        /// <response code="500">Error al inspeccionar mensajes</response>
        [HttpGet("inspect-messages")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> InspectMessages([FromQuery] int maxMessages = 5)
        {
            try
            {
                string queueName = _configuration[JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE];
                
                if (string.IsNullOrEmpty(queueName))
                {
                    return BadRequest(new
                    {
                        Error = "El nombre de la cola no está configurado correctamente",
                        ConfigurationKey = JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE,
                        Timestamp = DateTime.UtcNow
                    });
                }

                var newMessages = await _eventService.ReadNewMessagesAsync(queueName, JibanConstants.REDIS_EXECUTION_GROUP, maxMessages);
                var pendingMessages = await _eventService.ReadPendingMessagesAsync(queueName, JibanConstants.REDIS_EXECUTION_GROUP, maxMessages);

                var result = new
                {
                    QueueName = queueName,
                    ExecutionGroup = JibanConstants.REDIS_EXECUTION_GROUP,
                    MaxMessagesRequested = maxMessages,
                    NewMessages = newMessages.Select(msg => new
                    {
                        Id = msg.Id,
                        DebugInfo = BaseUtil.GetMessageDebugInfo(msg),
                        IsValid = BaseUtil.ValidarMensajeRedis(msg)
                    }).ToArray(),
                    PendingMessages = pendingMessages.Select(msg => new
                    {
                        Id = msg.Id,
                        DebugInfo = BaseUtil.GetMessageDebugInfo(msg),
                        IsValid = BaseUtil.ValidarMensajeRedis(msg)
                    }).ToArray(),
                    Summary = new
                    {
                        TotalNewMessages = newMessages.Length,
                        TotalPendingMessages = pendingMessages.Length,
                        TotalMessages = newMessages.Length + pendingMessages.Length
                    },
                    InspectionTimestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló la inspección de mensajes");
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    InnerError = ex.InnerException?.Message,
                    Message = "No se pudieron inspeccionar los mensajes. Verifique la conexión a Redis.",
                    InspectionTimestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Obtiene estadísticas básicas de todas las colas configuradas
        /// </summary>
        /// <returns>Estadísticas de las colas de notificaciones</returns>
        /// <response code="200">Estadísticas obtenidas exitosamente</response>
        /// <response code="500">Error al obtener estadísticas</response>
        [HttpGet("queue-stats")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetQueueStats()
        {
            try
            {
                var stats = new List<object>();
                
                var queues = new[]
                {
                    new { Name = "StartLetterQueue", ConfigKey = JibanConstants.NOTIFICATION_START_LETTER_QUEUE },
                    new { Name = "FinishLetterQueue", ConfigKey = JibanConstants.NOTIFICATION_FINISH_LETTER_QUEUE },
                    new { Name = "DeadLetterQueue", ConfigKey = JibanConstants.NOTIFICATION_DEAD_LETTER_QUEUE }
                };

                foreach (var queue in queues)
                {
                    string queueName = _configuration[queue.ConfigKey];
                    
                    if (!string.IsNullOrEmpty(queueName))
                    {
                        try
                        {
                            var newMessages = await _eventService.ReadNewMessagesAsync(queueName, JibanConstants.REDIS_EXECUTION_GROUP, 0);
                            var pendingMessages = await _eventService.ReadPendingMessagesAsync(queueName, JibanConstants.REDIS_EXECUTION_GROUP, 0);
                            
                            stats.Add(new
                            {
                                QueueType = queue.Name,
                                QueueName = queueName,
                                NewMessagesCount = newMessages.Length,
                                PendingMessagesCount = pendingMessages.Length,
                                Status = "ACCESIBLE"
                            });
                        }
                        catch (Exception ex)
                        {
                            stats.Add(new
                            {
                                QueueType = queue.Name,
                                QueueName = queueName,
                                NewMessagesCount = -1,
                                PendingMessagesCount = -1,
                                Status = "ERROR",
                                Error = ex.Message
                            });
                        }
                    }
                    else
                    {
                        stats.Add(new
                        {
                            QueueType = queue.Name,
                            QueueName = "NO_CONFIGURADA",
                            NewMessagesCount = -1,
                            PendingMessagesCount = -1,
                            Status = "NO_CONFIGURADA"
                        });
                    }
                }

                return Ok(new
                {
                    ExecutionGroup = JibanConstants.REDIS_EXECUTION_GROUP,
                    Queues = stats,
                    StatsTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de colas");
                return StatusCode(500, new
                {
                    Error = ex.Message,
                    Message = "No se pudieron obtener las estadísticas de las colas",
                    StatsTimestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Verificar información del stream
        /// </summary>
        [HttpGet("stream-info")]
        public async Task<IActionResult> GetStreamInfo()
        {
            try
            {
                string queueName = "Notification:FinishLetterQueue";
                string executionGroup = "jiban-processors";

                var result = new
                {
                    QueueName = queueName,
                    ExecutionGroup = executionGroup,
                    Timestamp = DateTime.Now,
                    
                    // Intentar leer mensajes nuevos
                    NewMessages = await TryReadNewMessages(queueName, executionGroup),
                    
                    // Intentar leer mensajes pendientes
                    PendingMessages = await TryReadPendingMessages(queueName, executionGroup),
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo información del stream");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Crear consumer group
        /// </summary>
        [HttpPost("create-consumer-group")]
        public async Task<IActionResult> CreateConsumerGroup()
        {
            try
            {
                string queueName = "Notification:FinishLetterQueue";
                string executionGroup = "jiban-processors";

                await _eventService.CreateConsumerGroupAsync(queueName, executionGroup);
                
                return Ok(new { 
                    message = "Consumer group created successfully",
                    queueName,
                    executionGroup 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando consumer group");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Probar lectura de mensajes nuevos
        /// </summary>
        [HttpGet("test-new-messages")]
        public async Task<IActionResult> TestNewMessages()
        {
            try
            {
                string queueName = "Notification:FinishLetterQueue";
                string executionGroup = "jiban-processors";

                var messages = await _eventService.ReadNewMessagesAsync(queueName, executionGroup, 5);
                
                var result = messages.Select(msg => new
                {
                    Id = msg.Id,
                    Values = msg.Values.Select(v => new { v.Name, Value = v.Value.ToString() }).ToArray()
                }).ToArray();

                return Ok(new
                {
                    Count = messages.Length,
                    Messages = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leyendo mensajes nuevos");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Probar lectura de mensajes pendientes
        /// </summary>
        [HttpGet("test-pending-messages")]
        public async Task<IActionResult> TestPendingMessages()
        {
            try
            {
                string queueName = "Notification:FinishLetterQueue";
                string executionGroup = "jiban-processors";

                var messages = await _eventService.ReadPendingMessagesAsync(queueName, executionGroup, 5);
                
                var result = messages.Select(msg => new
                {
                    Id = msg.Id,
                    Values = msg.Values.Select(v => new { v.Name, Value = v.Value.ToString() }).ToArray()
                }).ToArray();

                return Ok(new
                {
                    Count = messages.Length,
                    Messages = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leyendo mensajes pendientes");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        /// <summary>
        /// Probar deserialización de mensaje
        /// </summary>
        [HttpGet("test-deserialization")]
        public async Task<IActionResult> TestDeserialization()
        {
            try
            {
                string queueName = "Notification:FinishLetterQueue";
                string executionGroup = "jiban-processors";

                // Intentar leer un mensaje
                var messages = await _eventService.ReadNewMessagesAsync(queueName, executionGroup, 1);
                
                if (messages.Length == 0)
                {
                    return Ok(new { message = "No hay mensajes para deserializar" });
                }

                var message = messages[0];
                
                try
                {
                    // Intentar deserializar
                    var eventModel = _eventService.GetMessage<Domain.Models.EventAuthorizeDocumentModel>(message);
                    
                    return Ok(new
                    {
                        MessageId = message.Id,
                        DeserializationSuccess = true,
                        EventModel = new
                        {
                            eventModel.IdSolicitud,
                            eventModel.IdSolicitudDetalle,
                            eventModel.IdTipoSolicitud,
                            eventModel.Identificacion
                        }
                    });
                }
                catch (Exception deserializeEx)
                {
                    return Ok(new
                    {
                        MessageId = message.Id,
                        DeserializationSuccess = false,
                        DeserializationError = deserializeEx.Message,
                        RawValues = message.Values.Select(v => new { v.Name, Value = v.Value.ToString() }).ToArray()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error probando deserialización");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private async Task<object> TryReadNewMessages(string queueName, string executionGroup)
        {
            try
            {
                var messages = await _eventService.ReadNewMessagesAsync(queueName, executionGroup, 1);
                return new
                {
                    Success = true,
                    Count = messages.Length,
                    HasMessages = messages.Length > 0
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<object> TryReadPendingMessages(string queueName, string executionGroup)
        {
            try
            {
                var messages = await _eventService.ReadPendingMessagesAsync(queueName, executionGroup, 1);
                return new
                {
                    Success = true,
                    Count = messages.Length,
                    HasMessages = messages.Length > 0
                };
            }
            catch (Exception ex)
            {
                return new
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }
    }
}