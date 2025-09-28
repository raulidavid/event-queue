using System.ComponentModel;

namespace Jiban.Domain
{
    public class BaseConstants
    {
        public const int VALOR_CERO = 0;
        public const string PROCESAR = "Procesar";
        public const string REDIS_MENSAJE = "Mensaje";
        public const string MOVER = "MENSAJE MOVIDO A";
        public const string DETENIENDOSE = "Deteniendose...";
        public const string FORMATO_FECHA = "MM/dd/yyyy HH:mm";
        public const string FECHA_EJECUCION = "Fecha Ejecucion";
        public const string ESPERANDO_MENSAJE = "Esperando Mensaje...";
        public const string REDIS_GRUPO_EJECUCION = "evento.ventadigital";
        public const string HOSTED_1_MENSAJE = "{NOMBRE_HOSTED} {1_MENSAJE}";
        public const string HOSTED_2_MENSAJE = "{NOMBRE_HOSTED} {2_MENSAJE}";
        public const string MENSAJES_NUEVOS = "Numero Mensajes Nuevos a Procesar:";
        public const string CUENTA_PYME_SEGUNDOS_ESPERA = "CuentaPyme:SegundosEspera";
        public const string ERROR_MENSAJE = "Error en el mensaje Id: {0}, Mensaje:{1}";
        public const string FIN_PROCESANDO_MENSAJE = "Fin Ejecución de los Mensaje...";
        public const string MENSAJES_PENDIENTES = "Numero Mensaje Pendientes a Procesar:";
        public const string INICIO_PROCESANDO_MENSAJE = "Inicio Ejecución de los Mensaje...";
        public const string CUENTA_PYME_NUMERO_INTENTOS = "CuentaPyme:NumeroIntentos";
        public const string HOSTED_SERVICES_CUENTA_PYME_FINAL = "HostedServices_Cuenta_Pyme_Final";
        public const string CUENTA_PYME_NUMERO_MENSAJES_NUEVOS = "CuentaPyme:NumeroMensajesNuevos";
        public const string CUENTA_PYME_NUMERO_MENSAJES_PENDIENTES = "CuentaPyme:NumeroMensajesPendientes";
        public const string EVENTO_VENTADIGITAL_COLA_FINAL_CUENTA_PYME = "evento_ventadigital_cola_cuenta_pyme_final";
        public const string EVENTO_VENTADIGITAL_COLA_CUENTA_PYME_CARTA_MUERTA = "evento_ventadigital_cola_cuenta_pyme_carta_muerta";
    }
}
