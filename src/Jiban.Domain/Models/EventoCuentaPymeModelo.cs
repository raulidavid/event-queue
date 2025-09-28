using System;
using System.Collections.Generic;
using System.Text;

namespace Jiban.Domain.Models
{
    public class EventoCuentaPymeModelo
    {
        public int IdSolicitud { get; set; }
        public int IdSolicitudDetalle { get; set; }
        public int IdTipoSolicitud { get; set; }
        public string Identificacion { get; set; }
    }
}
