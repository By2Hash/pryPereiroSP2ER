using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.IO;
using System.Windows.Forms;
using System.Data;

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
    }
}