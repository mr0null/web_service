using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml.Linq;

namespace WebService1
{
    /// <summary>
    /// Descripción breve de Service1
    /// </summary>
    [WebService(Namespace = "http://i189sa.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // Para permitir que se llame a este servicio web desde un script, usando ASP.NET AJAX, quite la marca de comentario de la línea siguiente. 
    // [System.Web.Script.Services.ScriptService]
    public class Service1 : System.Web.Services.WebService
    {

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        //---------------------------------------------------------------------------------
        // Retorna el estado del servidor 
        //---------------------------------------------------------------------------------
        [WebMethod]
        public string ServerStatus()
        {
            //return "ON-LINE";
            string s = ConfigurationManager.ConnectionStrings["ConexionDB"].ConnectionString;
            return s;
        }

        //---------------------------------------------------------------------------------
        // Retorna la Fecha del servidor 
        //---------------------------------------------------------------------------------
        [WebMethod]
        public string ServerDate()
        {
            DateTime ldt;
            ldt = System.DateTime.Now;
            return ldt.ToString("yyyy/MM/dd HH:mm:ss");
        }

        //---------------------------------------------------------------------------------
        // Genera una Incidencia y retorna el nuevo numero
        //---------------------------------------------------------------------------------
        [WebMethod]
        public string IncidenceGenerate (string DNI, string Apellido, string Nombre, string Direccion, string Localidad)
        {
            long li_incidencia;
            li_incidencia = 1;
            return li_incidencia.ToString();
        }

        //---------------------------------------------------------------------------------
        // Este metodo cambia el estado de una incidencia
        //---------------------------------------------------------------------------------
        [WebMethod]
        public string ChangeStatus(string NroIncidencia, string EstadoNuevo)
        {
            // Controlar si existe la incidencia
            // Controlar que el estado nuevo sea uno valido dentro de los valores posibles 
            // (“DESPACHADO”, “SOLUCIONADO”, “CERRADO” y “ANULADO”)
            // SI todo esta OK se cambia el estado en la Base de Datos y se retorna OK
            return "OK";
        }

        //---------------------------------------------------------------------------------
        // Retorna el estado de la incidencia
        //---------------------------------------------------------------------------------
        [WebMethod]
        public string QueryStatus (string NroIncidencia)
        {
            // Controlar si existe la incidencia
            // SI todo esta OK retorno el estado sino error
            return "ERROR";
        }

        //---------------------------------------------------------------------------------
        // Retorno la incidencia como un string separado por ;
        //---------------------------------------------------------------------------------
        [WebMethod]
        public string QueryIncidence (string NroIncidencia)
        {
            // Controlar si existe la incidencia
            // Arma un string separado por ";" con el registro de la base de datos
            return "retornar los datos de la incidencia separados por ;";
        }

        //---------------------------------------------------------------------------------
        // Retorno las incidencias entre 2 fechas. Retorna un datatable
        //---------------------------------------------------------------------------------
        [WebMethod]
        public System.Data.DataTable QueryIncidences (string FechaDesde, string FechaHasta)
        {
            // Controlar ambas fechas que sean correctas y que FechaDesde <= FechaHasta
            // Hacer la query entre ambas fechas en la base de datos
            // Retornar un DT
            System.Data.DataTable lo_DT = null;
            return null;
        }

    }
}
