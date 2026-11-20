using System;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Services;

namespace WebService1
{
    [WebService(Namespace = "http://i189sa.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    public class Service1 : WebService
    {
        private static readonly object LogLock = new object();
        private const string InitialStatus = "PENDIENTE";

        [WebMethod]
        public string HelloWorld()
        {
            return ExecuteLogged("HelloWorld", "", delegate
            {
                return "Servidor SOAP de incidencias";
            });
        }

        [WebMethod]
        public string ServerStatus()
        {
            return ExecuteLogged("ServerStatus", "", delegate
            {
                return "ACTIVO";
            });
        }

        [WebMethod]
        public string ServerDate()
        {
            return ExecuteLogged("ServerDate", "", delegate
            {
                return DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
            });
        }

        [WebMethod]
        public string InsertClient(decimal DNI, string Apellido, string Nombres, string Direccion, string Localidad)
        {
            string request = String.Format(CultureInfo.InvariantCulture,
                "DNI={0}; Apellido={1}; Nombres={2}; Direccion={3}; Localidad={4}",
                DNI, SafeLogValue(Apellido), SafeLogValue(Nombres), SafeLogValue(Direccion), SafeLogValue(Localidad));
            return ExecuteLogged("InsertClient", request, delegate
            {
                string validationError = ValidateClient(DNI, Apellido, Nombres, Direccion, Localidad);
                if (validationError != null)
                {
                    WriteLog("ERROR", "InsertClient", validationError);
                    return "ERROR";
                }

                try
                {
                    using (SqlConnection connection = new SqlConnection(ConnectionString))
                    {
                        connection.Open();
                        using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                        using (SqlCommand command = new SqlCommand(
                            "IF EXISTS (SELECT 1 FROM dbo.Clientes WITH (UPDLOCK, HOLDLOCK) WHERE DNI = @DNI) " +
                            "UPDATE dbo.Clientes SET Apellido = @Apellido, Nombres = @Nombres, " +
                            "Direccion = @Direccion, Localidad = @Localidad WHERE DNI = @DNI " +
                            "ELSE INSERT INTO dbo.Clientes (DNI, Apellido, Nombres, Direccion, Localidad) " +
                            "VALUES (@DNI, @Apellido, @Nombres, @Direccion, @Localidad);",
                            connection, transaction))
                        {
                            AddNumericParameter(command, "@DNI", DNI);
                            command.Parameters.Add("@Apellido", SqlDbType.VarChar, 30).Value = Apellido.Trim();
                            command.Parameters.Add("@Nombres", SqlDbType.VarChar, 50).Value = Nombres.Trim();
                            command.Parameters.Add("@Direccion", SqlDbType.VarChar, 100).Value = Direccion.Trim();
                            command.Parameters.Add("@Localidad", SqlDbType.VarChar, 50).Value = Localidad.Trim();
                            command.ExecuteNonQuery();
                            transaction.Commit();
                        }
                    }
                    return "OK";
                }
                catch (Exception exception)
                {
                    WriteLog("ERROR", "InsertClient", exception.ToString());
                    return "ERROR";
                }
            });
        }

        [WebMethod]
        public long IncidenceGenerate(decimal DNI, string Motivo, string Observaciones)
        {
            string request = String.Format(CultureInfo.InvariantCulture,
                "DNI={0}; Motivo={1}; Observaciones={2}",
                DNI, SafeLogValue(Motivo), SafeLogValue(Observaciones));
            return ExecuteLogged("IncidenceGenerate", request, delegate
            {
                string motive = NormalizeMotive(Motivo);
                if (motive == null)
                {
                    WriteLog("ERROR", "IncidenceGenerate", "Motivo inválido: " + SafeLogValue(Motivo));
                    return -2L;
                }
                if ((Observaciones ?? String.Empty).Length > 512)
                {
                    WriteLog("ERROR", "IncidenceGenerate", "Observaciones supera el límite de 512 caracteres.");
                    return -1L;
                }
                if (!IsValidDni(DNI))
                {
                    WriteLog("ERROR", "IncidenceGenerate", "DNI inválido: " +
                        DNI.ToString(CultureInfo.InvariantCulture));
                    return -1L;
                }

                try
                {
                    using (SqlConnection connection = new SqlConnection(ConnectionString))
                    {
                        connection.Open();
                        using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                        {
                            using (SqlCommand clientCommand = new SqlCommand(
                                "SELECT COUNT(1) FROM dbo.Clientes WITH (UPDLOCK, HOLDLOCK) WHERE DNI = @DNI",
                                connection, transaction))
                            {
                                AddNumericParameter(clientCommand, "@DNI", DNI);
                                if (Convert.ToInt32(clientCommand.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                                {
                                    transaction.Rollback();
                                    WriteLog("ERROR", "IncidenceGenerate", "No existe el cliente DNI=" +
                                        DNI.ToString(CultureInfo.InvariantCulture));
                                    return 0L;
                                }
                            }

                            long incidenceNumber;
                            using (SqlCommand incidenceCommand = new SqlCommand(
                                "INSERT INTO dbo.Incidencias " +
                                "(DNI, FechaHora, Motivo, Estado, Observaciones) " +
                                "VALUES (@DNI, GETDATE(), @Motivo, @Estado, @Observaciones); " +
                                "SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                                connection, transaction))
                            {
                                AddNumericParameter(incidenceCommand, "@DNI", DNI);
                                incidenceCommand.Parameters.Add("@Motivo", SqlDbType.VarChar, 50).Value = motive;
                                incidenceCommand.Parameters.Add("@Estado", SqlDbType.VarChar, 30).Value = InitialStatus;
                                incidenceCommand.Parameters.Add("@Observaciones", SqlDbType.VarChar, 512).Value =
                                    (object)(Observaciones ?? String.Empty);
                                incidenceNumber = Convert.ToInt64(incidenceCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
                            }
                            transaction.Commit();
                            return incidenceNumber;
                        }
                    }
                }
                catch (Exception exception)
                {
                    WriteLog("ERROR", "IncidenceGenerate", exception.ToString());
                    return -1L;
                }
            });
        }

        [WebMethod]
        public string ChangeStatus(long Nro_Incidencia, string EstadoNuevo)
        {
            string request = String.Format(CultureInfo.InvariantCulture,
                "Nro_Incidencia={0}; EstadoNuevo={1}", Nro_Incidencia, SafeLogValue(EstadoNuevo));
            return ExecuteLogged("ChangeStatus", request, delegate
            {
                string newStatus = (EstadoNuevo ?? String.Empty).Trim().ToUpperInvariant();
                if (!IsValidStatus(newStatus))
                {
                    string error = String.Format(CultureInfo.InvariantCulture,
                        "ERROR (Estado: {0} no contemplado por el servidor)", SafeLogValue(EstadoNuevo));
                    WriteLog("ERROR", "ChangeStatus", error);
                    return error;
                }
                if (Nro_Incidencia <= 0)
                {
                    WriteLog("ERROR", "ChangeStatus", "Número de incidencia inválido.");
                    return "ERROR";
                }

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                    {
                        string previousStatus;
                        using (SqlCommand selectCommand = new SqlCommand(
                            "SELECT Estado FROM dbo.Incidencias WITH (UPDLOCK, HOLDLOCK) " +
                            "WHERE Nro_Incidencia = @Nro_Incidencia",
                            connection, transaction))
                        {
                            AddNumericParameter(selectCommand, "@Nro_Incidencia", Nro_Incidencia);
                            object value = selectCommand.ExecuteScalar();
                            if (value == null || value == DBNull.Value)
                            {
                                transaction.Rollback();
                                WriteLog("ERROR", "ChangeStatus", "No existe la incidencia " +
                                    Nro_Incidencia.ToString(CultureInfo.InvariantCulture));
                                return "ERROR";
                            }
                            previousStatus = Convert.ToString(value, CultureInfo.InvariantCulture);
                        }

                        if (!String.Equals(previousStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                        {
                            using (SqlCommand updateCommand = new SqlCommand(
                                "UPDATE dbo.Incidencias SET Estado = @Nuevo WHERE Nro_Incidencia = @Nro_Incidencia; " +
                                "INSERT INTO dbo.AuditoriaIncidencias " +
                                "(Nro_Incidencia, EstadoAnterior, EstadoNuevo, FechaCambio) " +
                                "VALUES (@Nro_Incidencia, @Anterior, @Nuevo, GETDATE());",
                                connection, transaction))
                            {
                                AddNumericParameter(updateCommand, "@Nro_Incidencia", Nro_Incidencia);
                                updateCommand.Parameters.Add("@Anterior", SqlDbType.VarChar, 30).Value = previousStatus;
                                updateCommand.Parameters.Add("@Nuevo", SqlDbType.VarChar, 30).Value = newStatus;
                                updateCommand.ExecuteNonQuery();
                            }
                        }
                        transaction.Commit();
                    }
                }
                return "OK";
            });
        }

        [WebMethod]
        public string QueryStatus(long Nro_Incidencia)
        {
            return ExecuteLogged("QueryStatus",
                "Nro_Incidencia=" + Nro_Incidencia.ToString(CultureInfo.InvariantCulture), delegate
            {
                if (Nro_Incidencia <= 0)
                {
                    WriteLog("ERROR", "QueryStatus", "Número de incidencia inválido.");
                    return "ERROR";
                }

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                using (SqlCommand command = new SqlCommand(
                    "SELECT Estado FROM dbo.Incidencias WHERE Nro_Incidencia = @Nro_Incidencia", connection))
                {
                    AddNumericParameter(command, "@Nro_Incidencia", Nro_Incidencia);
                    connection.Open();
                    object value = command.ExecuteScalar();
                    if (value == null || value == DBNull.Value)
                    {
                        WriteLog("ERROR", "QueryStatus", "No existe la incidencia " +
                            Nro_Incidencia.ToString(CultureInfo.InvariantCulture));
                        return "ERROR";
                    }
                    return Convert.ToString(value, CultureInfo.InvariantCulture);
                }
            });
        }

        [WebMethod]
        public ClaseTipoIncidencia QueryIncidence(long Nro_Incidencia)
        {
            return ExecuteLogged("QueryIncidence",
                "Nro_Incidencia=" + Nro_Incidencia.ToString(CultureInfo.InvariantCulture), delegate
            {
                ClaseTipoIncidencia result = new ClaseTipoIncidencia();
                result.Nro_Incidencia = -1;
                if (Nro_Incidencia <= 0)
                {
                    WriteLog("ERROR", "QueryIncidence", "Número de incidencia inválido.");
                    return result;
                }

                using (SqlConnection connection = new SqlConnection(ConnectionString))
                using (SqlCommand command = new SqlCommand(
                    "SELECT i.Nro_Incidencia, i.DNI, i.FechaHora, i.Motivo, i.Estado, i.Observaciones, " +
                    "c.Apellido, c.Nombres, c.Direccion, c.Localidad " +
                    "FROM dbo.Incidencias i INNER JOIN dbo.Clientes c ON c.DNI = i.DNI " +
                    "WHERE i.Nro_Incidencia = @Nro_Incidencia", connection))
                {
                    AddNumericParameter(command, "@Nro_Incidencia", Nro_Incidencia);
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            WriteLog("ERROR", "QueryIncidence", "No existe la incidencia " +
                                Nro_Incidencia.ToString(CultureInfo.InvariantCulture));
                            return result;
                        }

                        result.Nro_Incidencia = Convert.ToInt64(reader["Nro_Incidencia"], CultureInfo.InvariantCulture);
                        result.DNI = Convert.ToDecimal(reader["DNI"], CultureInfo.InvariantCulture);
                        result.FechaHora = Convert.ToDateTime(reader["FechaHora"], CultureInfo.InvariantCulture);
                        result.Motivo = Convert.ToString(reader["Motivo"], CultureInfo.InvariantCulture);
                        result.Estado = Convert.ToString(reader["Estado"], CultureInfo.InvariantCulture);
                        result.Observaciones = Convert.ToString(reader["Observaciones"], CultureInfo.InvariantCulture);
                        result.Apellido = Convert.ToString(reader["Apellido"], CultureInfo.InvariantCulture);
                        result.Nombres = Convert.ToString(reader["Nombres"], CultureInfo.InvariantCulture);
                        result.Direccion = Convert.ToString(reader["Direccion"], CultureInfo.InvariantCulture);
                        result.Localidad = Convert.ToString(reader["Localidad"], CultureInfo.InvariantCulture);
                        return result;
                    }
                }
            });
        }

        [WebMethod]
        public DataTable QueryIncidences(string FechaDesde, string FechaHasta)
        {
            string request = String.Format(CultureInfo.InvariantCulture, "FechaDesde={0}; FechaHasta={1}",
                SafeLogValue(FechaDesde), SafeLogValue(FechaHasta));
            return ExecuteLogged("QueryIncidences", request, delegate
            {
                DateTime start;
                DateTime end;
                if (!TryParseDate(FechaDesde, out start) || !TryParseDate(FechaHasta, out end) || start.Date > end.Date)
                    throw new ArgumentException("Las fechas deben tener formato yyyy/MM/dd y FechaDesde no puede superar FechaHasta.");

                return QueryIncidences(
                    "WHERE i.FechaHora >= @Desde AND i.FechaHora < @Hasta",
                    delegate(SqlCommand command)
                    {
                        command.Parameters.Add("@Desde", SqlDbType.DateTime).Value = start.Date;
                        command.Parameters.Add("@Hasta", SqlDbType.DateTime).Value = end.Date.AddDays(1);
                    });
            });
        }

        [WebMethod]
        public DataTable QueryIncidencesByClient(decimal DNI)
        {
            return ExecuteLogged("QueryIncidencesByClient",
                "DNI=" + DNI.ToString(CultureInfo.InvariantCulture), delegate
            {
                if (!IsValidDni(DNI))
                    throw new ArgumentException("Debe indicar un DNI válido.");

                return QueryIncidences(
                    "WHERE i.DNI = @DNI",
                    delegate(SqlCommand command)
                    {
                        AddNumericParameter(command, "@DNI", DNI);
                    });
            });
        }

        private static string ConnectionString
        {
            get
            {
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["ConexionDB"];
                if (settings == null || String.IsNullOrEmpty(settings.ConnectionString))
                    throw new ConfigurationErrorsException("No está configurada la cadena de conexión ConexionDB.");
                return settings.ConnectionString;
            }
        }

        private static bool HasValue(string value)
        {
            return !String.IsNullOrEmpty(value) && !String.IsNullOrEmpty(value.Trim());
        }

        private static string ValidateClient(decimal dni, string apellido, string nombres,
            string direccion, string localidad)
        {
            if (!IsValidDni(dni))
                return "DNI debe ser un entero positivo con hasta 18 dígitos.";
            if (!HasValue(apellido) || !HasValue(nombres) || !HasValue(direccion) || !HasValue(localidad))
                return "Los datos del cliente son obligatorios.";
            if (apellido.Trim().Length > 30 || nombres.Trim().Length > 50 ||
                direccion.Trim().Length > 100 || localidad.Trim().Length > 50)
                return "La longitud de los datos del cliente supera el límite del esquema.";
            return null;
        }

        private static SqlParameter AddNumericParameter(SqlCommand command, string name, decimal value)
        {
            SqlParameter parameter = command.Parameters.Add(name, SqlDbType.Decimal);
            parameter.Precision = 18;
            parameter.Scale = 0;
            parameter.Value = value;
            return parameter;
        }

        private static bool IsValidDni(decimal dni)
        {
            return dni > 0 && dni <= 999999999999999999m && Decimal.Truncate(dni) == dni;
        }

        private static DataTable QueryIncidences(string whereClause, Action<SqlCommand> addParameters)
        {
            DataTable results = new DataTable("Incidencias");
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(
                "SELECT i.Nro_Incidencia, i.DNI, i.FechaHora, i.Motivo, i.Estado, i.Observaciones, " +
                "c.Apellido, c.Nombres, c.Direccion, c.Localidad " +
                "FROM dbo.Incidencias i INNER JOIN dbo.Clientes c ON c.DNI = i.DNI " +
                whereClause + " ORDER BY i.FechaHora DESC, i.Nro_Incidencia DESC", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                addParameters(command);
                adapter.Fill(results);
            }
            return results;
        }

        private static bool TryParseDate(string value, out DateTime date)
        {
            string[] formats = new string[] { "yyyy/MM/dd", "yyyy-MM-dd" };
            return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out date);
        }

        private static string NormalizeMotive(string value)
        {
            string motive = (value ?? String.Empty).Trim().ToUpperInvariant();
            if (motive == "RECLAMO" || motive == "PEDIDO DE INFO" ||
                motive == "DENUNCIA" || motive == "OTRO")
                return motive;
            return null;
        }

        private static bool IsValidStatus(string status)
        {
            return status == "DESPACHADO" || status == "SOLUCIONADO" ||
                status == "CERRADO" || status == "ANULADO";
        }

        private static string SafeLogValue(string value)
        {
            return (value ?? String.Empty).Replace("\r", " ").Replace("\n", " ");
        }

        private static T ExecuteLogged<T>(string operation, string request, Func<T> action)
        {
            WriteLog("REQUEST", operation, request);
            try
            {
                T result = action();
                WriteLog("RESPONSE", operation, DescribeResult(result));
                return result;
            }
            catch (Exception exception)
            {
                WriteLog("ERROR", operation, exception.ToString());
                throw;
            }
        }

        private static string DescribeResult<T>(T result)
        {
            DataTable table = result as DataTable;
            if (table != null)
            {
                try
                {
                    using (StringWriter writer = new StringWriter(CultureInfo.InvariantCulture))
                    {
                        table.WriteXml(writer, XmlWriteMode.WriteSchema);
                        return writer.ToString();
                    }
                }
                catch (Exception exception)
                {
                    return "No se pudo serializar la respuesta tabular para el log: " + exception;
                }
            }

            ClaseTipoIncidencia incidence = result as ClaseTipoIncidencia;
            if (incidence != null)
                return incidence.ToString();
            return SafeLogValue(Convert.ToString(result, CultureInfo.InvariantCulture));
        }

        private static void WriteLog(string kind, string operation, string detail)
        {
            string line = String.Format(CultureInfo.InvariantCulture,
                "{0:yyyy-MM-dd HH:mm:ss.fff} UTC [{1}] {2}: {3}{4}",
                DateTime.UtcNow, kind, operation, SafeLogValue(detail), Environment.NewLine);
            try
            {
                string path = HttpContext.Current == null
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data\\Service.log")
                    : HttpContext.Current.Server.MapPath("~/App_Data/Service.log");
                lock (LogLock)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    File.AppendAllText(path, line, Encoding.UTF8);
                }
            }
            catch (Exception loggingException)
            {
                Trace.WriteLine("No se pudo escribir el log del servicio: " + loggingException);
                Trace.WriteLine(line);
            }
        }
    }

    [Serializable]
    [System.Xml.Serialization.XmlType(Namespace = "http://i189sa.org/")]
    public class ClaseTipoIncidencia
    {
        public long Nro_Incidencia { get; set; }
        public decimal DNI { get; set; }
        public DateTime FechaHora { get; set; }
        public string Motivo { get; set; }
        public string Estado { get; set; }
        public string Observaciones { get; set; }
        public string Apellido { get; set; }
        public string Nombres { get; set; }
        public string Direccion { get; set; }
        public string Localidad { get; set; }

        public override string ToString()
        {
            return String.Format(CultureInfo.InvariantCulture,
                "Nro_Incidencia={0}; DNI={1}; FechaHora={2:yyyy-MM-dd HH:mm:ss}; Motivo={3}; Estado={4}; " +
                "Observaciones={5}; Apellido={6}; Nombres={7}; Direccion={8}; Localidad={9}",
                Nro_Incidencia, DNI, FechaHora, Safe(Motivo), Safe(Estado), Safe(Observaciones),
                Safe(Apellido), Safe(Nombres), Safe(Direccion), Safe(Localidad));
        }

        private static string Safe(string value)
        {
            return (value ?? String.Empty).Replace("\r", " ").Replace("\n", " ");
        }
    }
}
