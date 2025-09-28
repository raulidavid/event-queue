using System;
using System.Linq;
using System.Text;
using StackExchange.Redis;
using System.Collections.Generic;
using Jiban.Domain;

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
            bool contieneMensaje = procesarMensaje.Values.Any(static x => x.Name.ToString().Equals(BaseConstants.REDIS_MENSAJE, StringComparison.Ordinal));
            
            return !string.IsNullOrEmpty(procesarMensaje.Id) && procesarMensaje.Values.Length > BaseConstants.VALOR_CERO && contieneMensaje;
        }
    }
}
