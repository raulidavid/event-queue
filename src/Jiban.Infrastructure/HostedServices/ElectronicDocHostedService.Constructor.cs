using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Jiban.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using JibanPermissions.Services;

namespace Jiban.Infrastructure.HostedServices
{
    /// <summary>
    /// Servicio en segundo plano para procesar documentos electrónicos desde colas Redis
    /// 
    /// EXPLICACIÓN DE MENSAJES NUEVOS VS PENDIENTES EN REDIS STREAMS:
    /// ================================================================
    /// 
    /// 📥 MENSAJES NUEVOS:
    /// - Son mensajes que NUNCA han sido leídos por NINGÚN consumidor del grupo
    /// - Redis mantiene un "last delivered ID" por grupo de consumidores
    /// - ReadNewMessagesAsync() usa XREADGROUP con ID ">" que significa "dame todo lo que sea mayor al último ID entregado"
    /// - Cuando un mensaje se lee por primera vez, automáticamente pasa a "pendiente"
    /// 
    /// 📤 MENSAJES PENDIENTES:
    /// - Son mensajes que YA fueron leídos por algún consumidor del grupo
    /// - Pero NO han sido confirmados con XACK (acknowledged)
    /// - Redis mantiene una "Pending Entries List (PEL)" por consumidor
    /// - ReadPendingMessagesAsync() usa XPENDING + XCLAIM para recuperar mensajes abandonados
    /// 
    /// 🔄 FLUJO COMPLETO:
    /// 1. Mensaje llega al stream → Redis lo asigna un ID único (timestamp-sequence)
    /// 2. ReadNewMessagesAsync() lee el mensaje → pasa a "pendiente" automáticamente
    /// 3. Si procesamiento OK → DeleteMessageById() hace XACK → mensaje se elimina de PEL
    /// 4. Si procesamiento FALLA → mensaje permanece "pendiente"
    /// 5. ReadPendingMessagesAsync() recupera mensajes pendientes para reintento
    /// 
    /// 📊 EN TU IMAGEN REDIS:
    /// - "Notification:FinishLetterQueue" tiene 2 entradas
    /// - ID: "1760134131299-0" es un timestamp (ms desde epoch) + secuencia
    /// - Si hay Consumer Groups, algunos mensajes pueden estar "pendientes"
    /// </summary>
    public partial class ElectronicDocHostedService : BackgroundService
    {
        private readonly IServiceScope _serviceScope;
        private readonly ILogger<ElectronicDocHostedService> _logger;
        private readonly IElectronicDocService _electronicDocService;
        private readonly ITokenAccessor _tokenAccessor;
        private readonly ITokenBuilder _tokenBuilder;
        private readonly IConfiguration _configuration;
        private readonly IEventService _eventService;
        
        public ElectronicDocHostedService(ILogger<ElectronicDocHostedService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScope = serviceScopeFactory.CreateScope();
            _logger = logger;
            _eventService = _serviceScope.ServiceProvider.GetRequiredService<IEventService>();
            _electronicDocService = _serviceScope.ServiceProvider.GetRequiredService<IElectronicDocService>();
            _tokenAccessor = _serviceScope.ServiceProvider.GetRequiredService<ITokenAccessor>();
            _tokenBuilder = _serviceScope.ServiceProvider.GetRequiredService<ITokenBuilder>();
            _configuration = _serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
        }
    }
}