using Jiban.BaseCode.PermissionsCode;
using StackExchange.Redis;
using System.Text;

namespace Jiban.Infrastructure
{
    public class BaseUtil
    {
        /// <summary>
        /// Método para validar que el mensaje del redis tenga la información completa
        /// </summary>
        /// <param name="procesarMensaje">Mensaje a validar</param>
        /// <returns>True o False si el mensaje esta correcto</returns>
        public static bool ValidarMensajeRedis(StreamEntry procesarMensaje)
        {
            bool contieneMensaje = procesarMensaje.Values.Any(static x => x.Name.ToString().Equals(JibanConstants.REDIS_MESSAGE, StringComparison.Ordinal));
            
            return !string.IsNullOrEmpty(procesarMensaje.Id) && procesarMensaje.Values.Length > JibanConstants.ZERO_VALUE && contieneMensaje;
        }

        /// <summary>
        /// Método para obtener información detallada de un mensaje de Redis para debugging
        /// </summary>
        /// <param name="procesarMensaje">Mensaje a inspeccionar</param>
        /// <returns>String con información del mensaje</returns>
        public static string GetMessageDebugInfo(StreamEntry procesarMensaje)
        {
            var info = new StringBuilder();
            info.AppendLine($"Message ID: {procesarMensaje.Id}");
            info.AppendLine($"Values Count: {procesarMensaje.Values.Length}");
            
            foreach (var value in procesarMensaje.Values)
            {
                info.AppendLine($"  {value.Name}: {value.Value}");
            }
            
            bool hasRedisMessage = procesarMensaje.Values.Any(x => x.Name.ToString().Equals(JibanConstants.REDIS_MESSAGE, StringComparison.Ordinal));
            info.AppendLine($"Contains REDIS_MESSAGE field: {hasRedisMessage}");
            info.AppendLine($"Is Valid: {ValidarMensajeRedis(procesarMensaje)}");
            
            return info.ToString();
        }
    }
}
