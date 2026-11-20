# TP6 - Servidor de procesos remoto mediante WS

La solución contiene un servicio ASMX SOAP (`WebService1`) y un cliente WinForms WCF (`TestWS`). El cliente solo accede a los datos invocando el servicio; SQL Server se utiliza desde el servidor.

## Base de datos

Ejecutar `WebService1/Database/FALTAS.sql` en SQL Server. El script crea `FALTAS` (si no existe), `dbo.Clientes`, `dbo.Incidencias` y `dbo.AuditoriaIncidencias`, con sus claves, motivos/estados permitidos e índices. `Clientes` utiliza `DNI NUMERIC(18,0)`, `Apellido VARCHAR(30)`, `Nombres VARCHAR(50)`, `Direccion VARCHAR(100)` y `Localidad VARCHAR(50)`. `Incidencias` utiliza `Nro_Incidencia NUMERIC(18,0)` autogenerado, `DNI`, `FechaHora DATETIME`, `Motivo VARCHAR(50)`, `Estado VARCHAR(30)` y `Observaciones VARCHAR(512)`. Las incidencias nuevas quedan en estado `PENDIENTE`.

Configurar `ConexionDB` en `WebService1/WebService1/Web.config` para la instancia y base de datos de destino. La configuración inicial utiliza autenticación integrada de Windows, sin credenciales guardadas en el repositorio. Conceder al usuario de identidad del Application Pool permisos de lectura/escritura sobre las tres tablas y de escritura sobre `App_Data`.

## Métodos SOAP

El endpoint es `Service1.asmx`; la descripción WSDL se publica en `Service1.asmx?WSDL`.

| Método | Comportamiento |
| --- | --- |
| `ServerStatus` | Retorna exactamente `ACTIVO` cuando el WS responde; no depende de la conexión a SQL Server. |
| `ServerDate` | Fecha/hora del servidor en `yyyy/MM/dd HH:mm:ss`. |
| `InsertClient` | Inserta o actualiza por DNI entero positivo de hasta 18 dígitos; valida los límites de longitud del esquema. Retorna `OK` si guarda y exactamente `ERROR` ante datos inválidos o cualquier excepción SQL/otra. |
| `IncidenceGenerate(DNI, Motivo, Observaciones)` | Retorna el número autogenerado (`long`) si se creó, `0` si no existe el cliente, `-1` ante errores SQL/otros y `-2` si el motivo no es válido. Los motivos admitidos son `RECLAMO`, `PEDIDO DE INFO`, `DENUNCIA` y `OTRO`; el estado inicial es `PENDIENTE`. |
| `ChangeStatus(Nro_Incidencia, EstadoNuevo)` | Acepta `DESPACHADO`, `SOLUCIONADO`, `CERRADO` o `ANULADO`; retorna `OK` o `ERROR` si la incidencia no existe o el estado no está permitido. Para estado inválido devuelve `ERROR (Estado: <valor> no contemplado por el servidor)`. El cambio y la auditoría del estado previo se guardan en la misma transacción. |
| `QueryStatus` | Retorna el estado o `ERROR` si la incidencia no existe. |
| `QueryIncidence` | Retorna un `ClaseTipoIncidencia` con Nro, DNI, fecha/hora, motivo, estado, observaciones y datos del cliente. Si no existe, el DTO tiene `Nro_Incidencia = -1`. |
| `QueryIncidences` | Retorna una `DataTable` con los campos de incidencias/clientes para el rango inclusivo `yyyy/MM/dd`. |
| `QueryIncidencesByClient` | Retorna una `DataTable` con los campos de incidencias/clientes para el DNI numérico indicado. |

Cada operación registra solicitud, respuesta real y excepciones en `WebService1/App_Data/Service.log`. Las respuestas `DataTable` se incluyen completas en XML, no solo el total de filas. Por ello, el log contiene datos personales y debe limitarse por ACL, rotarse y retenerse conforme a las políticas del entorno. La identidad de IIS necesita permiso de modificación sobre `App_Data`. Si la escritura del archivo falla, los eventos de logging se envían a `System.Diagnostics.Trace`.

## Publicación y acceso desde la red

1. Compilar y publicar `WebService1.sln` con Visual Studio/MSBuild que tenga instalados ASP.NET y los targeting packs de .NET Framework 3.5.
2. En IIS, habilitar ASP.NET 3.5/ASP.NET 2.0, .NET Extensibility e ISAPI Extensions/Filters; crear una aplicación apuntando al contenido publicado y asignar un Application Pool CLR v2.0 (pipeline integrado o clásico).
3. Habilitar autenticación de Windows y deshabilitar acceso anónimo para que coincidan el `Web.config` del servicio y el `basicHttpBinding` del cliente. Configurar el acceso SQL para la identidad del Application Pool.
4. Asignar un binding DNS/IP y puerto al sitio, y habilitarlo en el firewall según la red permitida. Para WAN, publicar solo mediante HTTPS, cambiar el `basicHttpBinding` del cliente a `security mode="Transport"` con credenciales Windows y restringir el acceso por VPN/firewall; no exponer HTTP sin cifrar.
5. En `TestWS/app.config`, cambiar la dirección del endpoint `Service1Soap` por la URL del `Service1.asmx` publicado. Ejecutar el cliente con una cuenta Windows autorizada en IIS y SQL Server.

La URL de desarrollo `localhost:51760` y `Data Source=localhost` son valores iniciales de configuración; deben reemplazarse en el entorno de despliegue. Este repositorio no instala ni configura IIS, SQL Server, certificados, DNS o reglas de red en el host.
