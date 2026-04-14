using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;

namespace pryPereiroBaseDeDatos
{
    internal class CConexion
    {
        private OleDbConnection CNN;
        private DataSet DS;
        private string ERROR = "";

        public CConexion()
        {
            CNN = new OleDbConnection();
            DS = new DataSet();
        }

        public bool MostrarEnGrilla(string rutaArchivo, string nombreTabla, DataGridView grilla)
        {
            bool resultado = false;
            string extension = Path.GetExtension(rutaArchivo).ToLower();
            string cadenaConexion = "";

            // Proveedores a intentar según la extensión
            var providersToTry = new List<string>();
            if (extension == ".mdb")
            {
                providersToTry.Add("Microsoft.Jet.OLEDB.4.0");
                providersToTry.Add("Microsoft.ACE.OLEDB.16.0");
                providersToTry.Add("Microsoft.ACE.OLEDB.12.0");
            }
            else if (extension == ".accdb")
            {
                providersToTry.Add("Microsoft.ACE.OLEDB.16.0");
                providersToTry.Add("Microsoft.ACE.OLEDB.12.0");
            }

            // Buscar un proveedor que funcione
            foreach (var provider in providersToTry)
            {
                var csTry = $"Provider={provider};Data Source={rutaArchivo};";
                try
                {
                    using (var cnnTry = new OleDbConnection(csTry))
                    {
                        cnnTry.Open();
                        cnnTry.Close();
                    }
                    cadenaConexion = csTry;
                    break;
                }
                catch
                {
                    // Ignorar y probar siguiente proveedor
                }
            }

            if (string.IsNullOrEmpty(cadenaConexion))
            {
                ERROR = "No se encontró un proveedor OLEDB disponible para abrir el archivo. Instale Microsoft Access Database Engine (ACE) o use la plataforma correcta (x86/x64).";
                return false;
            }

            try
            {
                // Usar SELECT para mayor compatibilidad
                var safeTable = nombreTabla;
                if (!safeTable.StartsWith("[") && !safeTable.EndsWith("]"))
                {
                    safeTable = "[" + safeTable + "]";
                }

                var query = "SELECT * FROM " + safeTable;

                using (var conn = new OleDbConnection(cadenaConexion))
                {
                    using (var cmd = new OleDbCommand(query, conn))
                    using (var da = new OleDbDataAdapter(cmd))
                    {
                        var table = new DataTable();
                        da.Fill(table);
                        grilla.DataSource = table;
                        resultado = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ERROR = ex.Message;
                resultado = false;
            }

            return resultado;
        }

        public string ObtenerError()
        {
            return ERROR;
        }

        // -------------------------------------------------------
        // Nuevos métodos para crear BD, tablas e insertar datos
        // -------------------------------------------------------

        // Asegura que exista el archivo Access; si no existe lo crea usando ADOX (COM) probando varios proveedores.
        public bool EnsureDatabaseExists(string rutaDb)
        {
            try
            {
                if (File.Exists(rutaDb))
                    return true;

                var providers = new List<string>
                {
                    "Microsoft.ACE.OLEDB.16.0",
                    "Microsoft.ACE.OLEDB.12.0",
                    "Microsoft.Jet.OLEDB.4.0"
                };

                foreach (var provider in providers)
                {
                    try
                    {
                        var catalogType = Type.GetTypeFromProgID("ADOX.Catalog");
                        if (catalogType == null)
                            continue;

                        dynamic catalog = Activator.CreateInstance(catalogType);
                        string connStr = $"Provider={provider};Data Source={rutaDb};";
                        catalog.Create(connStr);
                        // Liberar COM
                        Marshal.FinalReleaseComObject(catalog);
                        return true;
                    }
                    catch
                    {
                        // probar siguiente proveedor
                    }
                }

                ERROR = "No se pudo crear la base de datos: ningún proveedor ADOX/ACE/Jet disponible.";
                return false;
            }
            catch (Exception ex)
            {
                ERROR = $"Error al crear la base de datos: {ex.Message}";
                return false;
            }
        }

        // Crea (o reemplaza) una tabla en la base y vuelca los datos del DataTable.
        // Todas las columnas se crean como TEXT.
        public bool CreateTableFromDataTable(string rutaDb, string nombreTabla, DataTable dt)
        {
            try
            {
                if (!File.Exists(rutaDb))
                {
                    ERROR = "La base de datos no existe: " + rutaDb;
                    return false;
                }

                // Determinar proveedor usable para el .mdb
                string provider = DetectProviderForFile(rutaDb);
                if (string.IsNullOrEmpty(provider))
                {
                    ERROR = "No se encontró proveedor OLEDB para la base de datos.";
                    return false;
                }

                string connStr = $"Provider={provider};Data Source={rutaDb};";

                using (var conn = new OleDbConnection(connStr))
                {
                    conn.Open();

                    // Nombre de tabla seguro entre corchetes
                    var safeTable = nombreTabla;
                    if (!safeTable.StartsWith("[") && !safeTable.EndsWith("]"))
                        safeTable = "[" + safeTable + "]";

                    // Intentar eliminar si existe
                    try
                    {
                        using (var cmdDrop = new OleDbCommand($"DROP TABLE {safeTable}", conn))
                        {
                            cmdDrop.ExecuteNonQuery();
                        }
                    }
                    catch
                    {
                        // ignorar si no existe
                    }

                    // Construir CREATE TABLE
                    var columnsDefs = new List<string>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        var colName = SanitizeColumnName(col.ColumnName);
                        columnsDefs.Add($"[{colName}] TEXT");
                    }

                    var createSql = $"CREATE TABLE {safeTable} ({string.Join(", ", columnsDefs)})";
                    using (var cmdCreate = new OleDbCommand(createSql, conn))
                    {
                        cmdCreate.ExecuteNonQuery();
                    }

                    // Insertar filas con parámetros
                    if (dt.Rows.Count > 0)
                    {
                        var columnNames = dt.Columns.Cast<DataColumn>().Select(c => $"[{SanitizeColumnName(c.ColumnName)}]").ToArray();
                        var paramPlaceholders = string.Join(", ", Enumerable.Range(0, dt.Columns.Count).Select(i => "?"));
                        var insertSql = $"INSERT INTO {safeTable} ({string.Join(", ", columnNames)}) VALUES ({paramPlaceholders})";

                        using (var tran = conn.BeginTransaction())
                        {
                            using (var cmdInsert = new OleDbCommand(insertSql, conn, tran))
                            {
                                // preparar parámetros por posición
                                for (int i = 0; i < dt.Columns.Count; i++)
                                {
                                    cmdInsert.Parameters.Add(new OleDbParameter());
                                }

                                foreach (DataRow row in dt.Rows)
                                {
                                    for (int i = 0; i < dt.Columns.Count; i++)
                                    {
                                        var val = row[i] == DBNull.Value ? null : row[i].ToString();
                                        cmdInsert.Parameters[i].Value = val ?? (object)DBNull.Value;
                                    }
                                    cmdInsert.ExecuteNonQuery();
                                }
                            }
                            tran.Commit();
                        }
                    }

                    conn.Close();
                }

                return true;
            }
            catch (Exception ex)
            {
                ERROR = "Error al crear tabla: " + ex.Message;
                return false;
            }
        }

        // Detecta qué proveedor funciona para un archivo .mdb/.accdb devolviendo el nombre del proveedor.
        private string DetectProviderForFile(string rutaArchivo)
        {
            var providersToTry = new List<string>
            {
                "Microsoft.ACE.OLEDB.16.0",
                "Microsoft.ACE.OLEDB.12.0",
                "Microsoft.Jet.OLEDB.4.0"
            };

            foreach (var provider in providersToTry)
            {
                var csTry = $"Provider={provider};Data Source={rutaArchivo};";
                try
                {
                    using (var cnnTry = new OleDbConnection(csTry))
                    {
                        cnnTry.Open();
                        cnnTry.Close();
                    }
                    return provider;
                }
                catch
                {
                    // ignorar
                }
            }

            return null;
        }

        // Sanitiza nombres de columna para SQL (elimina corchetes internos)
        private string SanitizeColumnName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Columna";

            var s = raw.Trim();
            s = s.Replace("]", "").Replace("[", "");
            s = s.Replace(" ", "_");
            return s;
        }
    }
}