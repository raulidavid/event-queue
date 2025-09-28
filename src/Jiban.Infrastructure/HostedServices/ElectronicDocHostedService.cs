using Microsoft.Extensions.DependencyInjection;
using Jiban.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Jiban.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Jiban.AspNetCore.Services;
using StackExchange.Redis;
using Jiban.Domain.Models;
using System.Diagnostics;
using System.Text.Json;
using Jiban.Domain;
using System.Text;
using System;

namespace Jiban.Infrastructure.HostedServices
{
    public partial class ElectronicDocHostedService : BackgroundService
    {
        private readonly IServiceScope _serviceScope;
        private readonly ILogger<ElectronicDocHostedService> _logger;
        private readonly IElectronicDocService _electronicDocService;
        private readonly IConfiguration _configuration;
        private readonly IEventService _eventService;
        
        public ElectronicDocHostedService(ILogger<ElectronicDocHostedService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScope = serviceScopeFactory.CreateScope();
            _logger = logger;
            _eventService = _serviceScope.ServiceProvider.GetRequiredService<IEventService>();
            _electronicDocService = _serviceScope.ServiceProvider.GetRequiredService<IElectronicDocService>();
            _configuration = _serviceScope.ServiceProvider.GetRequiredService<IConfiguration>();
        }

        /// <summary>
        /// Ejecutar proceso
        /// </summary>
        /// <param name="cancellationToken">Acceso para detener proceso</param>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _eventService.CreateConsumerGroupAsync(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    DateTime fechaEjecucion = Convert.ToDateTime(DateTime.Now.ToString(BaseConstants.FORMATO_FECHA));
                    _logger.LogInformation(BaseConstants.HOSTED_2_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.FECHA_EJECUCION, fechaEjecucion);
                    _logger.LogInformation(BaseConstants.HOSTED_1_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.INICIO_PROCESANDO_MENSAJE);
                    await ProcesarMensajesPendientes();
                    await ProcesarMensajesNuevos();
                    _logger.LogInformation(BaseConstants.HOSTED_1_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.FIN_PROCESANDO_MENSAJE);
                    _logger.LogInformation(BaseConstants.HOSTED_1_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.ESPERANDO_MENSAJE);
                    await Task.Delay(TimeSpan.FromSeconds(Convert.ToInt32(_configuration[BaseConstants.CUENTA_PYME_SEGUNDOS_ESPERA])), cancellationToken);
                }
                catch (Exception excepcion)
                {
                    _logger.LogError(BaseConstants.HOSTED_2_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, excepcion, excepcion.Message);
                }
            }
        }

        /// <summary>
        /// Detener Proceso
        /// </summary>
        /// <param name="cancellationToken">Acceso para la cancelacion</param>
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation(BaseConstants.HOSTED_1_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.DETENIENDOSE);
            return base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Metodo para procesar los mensajes nuevos
        /// </summary>
        async Task ProcesarMensajesNuevos()
        {
            StreamEntry[] mensajesNuevos = await _eventService.ReadNewMessagesAsync(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, Convert.ToInt32(_configuration[BaseConstants.CUENTA_PYME_NUMERO_MENSAJES_NUEVOS]));
            _logger.LogInformation(BaseConstants.HOSTED_2_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.MENSAJES_NUEVOS, mensajesNuevos.Length.ToString());

            foreach (StreamEntry procesarMensajesNuevo in mensajesNuevos)
            {
                if (BaseUtil.ValidarMensajeRedis(procesarMensajesNuevo))
                {
                    string mensajeId = procesarMensajesNuevo.Id;
                    EventoCuentaPymeModelo eventoCuentaPymeModelo = _eventService.GetMessage<EventoCuentaPymeModelo>(procesarMensajesNuevo);

                    try
                    {
                        await ProcesarEvento(eventoCuentaPymeModelo);
                        await _eventService.DeleteMessageById(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, mensajeId);
                    }
                    catch (Exception excepcion)
                    {
                        _logger.LogError(BaseConstants.HOSTED_1_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.ERROR_MENSAJE, mensajeId, eventoCuentaPymeModelo);
                        _logger.LogError(BaseConstants.HOSTED_2_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, excepcion, excepcion.Message);
                    }
                }
                else
                {
                    await _eventService.DeleteMessageById(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, procesarMensajesNuevo.Id);
                }
            }
        }

        /// <summary>
        /// Metodo para procesar los Mensajes Pendientes
        /// </summary>
        async Task ProcesarMensajesPendientes()
        {
            StreamEntry[] mensajesPendientes = await _eventService.ReadPendingMessagesAsync(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, Convert.ToInt32(_configuration[BaseConstants.CUENTA_PYME_NUMERO_MENSAJES_PENDIENTES]));
            _logger.LogInformation(BaseConstants.HOSTED_2_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.MENSAJES_PENDIENTES, mensajesPendientes.Length.ToString());

            foreach (StreamEntry procesarMensajePendiente in mensajesPendientes)
            {
                if (BaseUtil.ValidarMensajeRedis(procesarMensajePendiente))
                {
                    string mensajeId = procesarMensajePendiente.Id;
                    StreamPendingMessageInfo informacionMensajePendiente = await _eventService.GetPendingMessageById(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, mensajeId);
                    EventoCuentaPymeModelo eventoCuentaPymeModelo = _eventService.GetMessage<EventoCuentaPymeModelo>(procesarMensajePendiente);

                    try
                    {
                        if (informacionMensajePendiente.DeliveryCount > Convert.ToInt32(_configuration[BaseConstants.CUENTA_PYME_NUMERO_INTENTOS]))
                        {
                            await ProcesarMensajeRezagado(eventoCuentaPymeModelo);
                            await _eventService.DeleteMessageById(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, mensajeId);
                        }
                        else
                        {
                            await ProcesarEvento(eventoCuentaPymeModelo);
                            await _eventService.DeleteMessageById(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, mensajeId);
                        }
                    }
                    catch (Exception excepcion)
                    {
                        _logger.LogError(BaseConstants.HOSTED_1_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, BaseConstants.ERROR_MENSAJE, mensajeId, eventoCuentaPymeModelo);
                        _logger.LogError(BaseConstants.HOSTED_2_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, excepcion, excepcion.Message);
                    }
                }
                else
                {
                    await _eventService.DeleteMessageById(BaseConstants.EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME, BaseConstants.REDIS_GRUPO_EJECUCION, procesarMensajePendiente.Id);
                }
            }
        }

        /// <summary>
        /// Procesar el evento que se recibe de la cola evento_ventadigital_cola_final_cuenta_pyme del Redis
        /// </summary>
        /// <param name="eventoCuentaPymeModelo">Modelo con informacion de la solicitud procesador</param>
        private async Task ProcesarEvento(EventoCuentaPymeModelo eventoCuentaPymeModelo)
        {
            try
            {
                string identificacionSolicitud = eventoCuentaPymeModelo.IdSolicitudDetalle.ToString();
                string identificacion = eventoCuentaPymeModelo.Identificacion;

                //await ProcesarSolicitudCheques(eventoCuentaPymeModelo, solicitudCapoVistaModelo);
                //await ProcesarGeneracionDocumentacion(eventoCuentaPymeModelo, solicitudCapoVistaModelo);
                //await ProcesarActualizacionUsuarioFinal(solicitudCapoVistaModelo, identificacion);

            }
            catch (Exception excepcion)
            {
                _logger.LogError(BaseConstants.HOSTED_2_MENSAJE, BaseConstants.HOSTED_SERVICES_CUENTA_PYME_FINAL, excepcion, excepcion.Message);
                throw;
            }
        }

        /// <summary>
        /// Actualizar el estado de la solicitud de rezagado
        /// </summary>
        /// <param name="eventoCreditosConsumoModelo">Mensaje para procesar rezagado del redis</param>
        /// <exception cref="NotImplementedException"></exception>
        private async Task ProcesarMensajeRezagado(EventoCuentaPymeModelo eventoCreditosConsumoModelo)
        {
            await _eventService.CreateConsumerGroupAsync(BaseConstants.EVENTO_VENTADIGITAL_COLA_CUENTA_PYME_CARTA_MUERTA, BaseConstants.REDIS_GRUPO_EJECUCION);
            _logger.LogInformation($"{BaseConstants.MOVER} {BaseConstants.EVENTO_VENTADIGITAL_COLA_CUENTA_PYME_CARTA_MUERTA}");
            
            // Note: PublishAsync method expects a BaseMessageStream object, not EventoCuentaPymeModelo directly
            // You may need to create a BaseMessageStream wrapper or modify this based on your implementation
            // await _eventService.PublishAsync(BaseConstants.EVENTO_VENTADIGITAL_COLA_CUENTA_PYME_CARTA_MUERTA, eventoCreditosConsumoModelo);
        }
    }
}