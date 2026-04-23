using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using pryPereiroBaseDeDatos;

namespace pryPereiroSP2ER
{
    public partial class frmPrincipal : Form
    {
        // Definición de las tablas conocidas: nombre de archivo (sin extensión) -> columnas fijas
        private static readonly Dictionary<string, string[]> TablasConocidas = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Articulo",  new[] { "IdArticulo", "Nombre", "IdCategoria", "Precio" } },
            { "Categoria", new[] { "IdCategoria", "Nombre" } }
        };

        // Lista interna con rutas completas de los archivos seleccionados
        private readonly List<string> archivosSeleccionados = new List<string>();

        public frmPrincipal()
        {
            InitializeComponent();
        }

        private void btnExaminar_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                dlg.Title = "Seleccione archivo(s) de texto";
                dlg.Multiselect = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    foreach (var ruta in dlg.FileNames)
                    {
                        if (!File.Exists(ruta))
                            continue;

                        if (!archivosSeleccionados.Contains(ruta))
                        {
                            archivosSeleccionados.Add(ruta);
                            lstMigracion.Items.Add("Añadido: " + Path.GetFileName(ruta));
                        }
                        else
                        {
                            lstMigracion.Items.Add("Ya existe en la lista: " + Path.GetFileName(ruta));
                        }
                    }
                }
            }
        }

        private void btnIniciarMigracion_Click(object sender, EventArgs e)
        {
            if (archivosSeleccionados.Count == 0)
            {
                lstMigracion.Items.Add("No hay archivos seleccionados para migrar.");
                return;
            }

            // Pedir al usuario dónde guardar la base (mdb o accdb)
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "Guardar base de datos destino";
                dlg.Filter = "Access Database (*.accdb)|*.accdb|Access 2003 (*.mdb)|*.mdb";
                dlg.FileName = "Distribuidora.mdb";
                dlg.DefaultExt = "mdb";

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    lstMigracion.Items.Add("Migración cancelada por el usuario.");
                    return;
                }

                var rutaDb = dlg.FileName;
                var conexion = new CConexion();

                // Crear base si no existe
                if (!conexion.EnsureDatabaseExists(rutaDb))
                {
                    lstMigracion.Items.Add("Error: no se pudo crear/asegurar la base -> " + conexion.ObtenerError());
                    return;
                }

                // Procesar cada archivo seleccionado
                foreach (var ruta in archivosSeleccionados.ToList())
                {
                    var nombreArchivo = Path.GetFileName(ruta);
                    var nombreSinExt = Path.GetFileNameWithoutExtension(ruta); // "Articulo" o "Categoria"

                    // ── Detectar qué tabla corresponde al archivo ──────────────
                    string[] columnas;
                    if (!TablasConocidas.TryGetValue(nombreSinExt, out columnas))
                    {
                        lstMigracion.Items.Add("Omitido: " + nombreArchivo +
                            " — nombre no reconocido (se esperaba Articulo.txt o Categoria.txt)");
                        continue;
                    }

                    // El nombre de tabla en Access = nombre del archivo sin extensión
                    var nombreTabla = nombreSinExt;

                    try
                    {
                        var lines = File.ReadAllLines(ruta, Encoding.Default);
                        if (lines.Length == 0)
                        {
                            lstMigracion.Items.Add("Error en " + nombreArchivo + ": archivo vacío");
                            continue;
                        }

                        // Detectar separador usando la primera línea
                        char sep = DetectSeparator(lines[0]);

                        // Construir DataTable con las columnas fijas de esta tabla
                        var dt = new DataTable();
                        foreach (var col in columnas)
                            dt.Columns.Add(col, typeof(string));

                        // Todas las líneas son datos (el .txt NO tiene fila de encabezado)
                        foreach (var linea in lines)
                        {
                            if (string.IsNullOrWhiteSpace(linea))
                                continue;

                            var parts = SplitLineRespectingQuotes(linea, sep);
                            var row = dt.NewRow();
                            for (int c = 0; c < dt.Columns.Count; c++)
                                row[c] = c < parts.Length ? parts[c].Trim() : string.Empty;

                            dt.Rows.Add(row);
                        }

                        var ok = conexion.CreateTableFromDataTable(rutaDb, nombreTabla, dt);
                        if (ok)
                            lstMigracion.Items.Add("Migrado: " + nombreArchivo +
                                " -> Tabla [" + nombreTabla + "] (" + dt.Rows.Count + " registros)");
                        else
                            lstMigracion.Items.Add("Error en " + nombreArchivo + ": " + conexion.ObtenerError());
                    }
                    catch (Exception ex)
                    {
                        lstMigracion.Items.Add("Error en " + nombreArchivo + ": " + ex.Message);
                    }
                } // foreach archivos

                lstMigracion.Items.Add("Migración finalizada.");
            } // using SaveFileDialog
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        // Detecta separador más probable en una línea (prioriza ;, luego ,, luego tab)
        private char DetectSeparator(string sample)
        {
            if (sample.Contains(";")) return ';';
            if (sample.Contains(",")) return ',';
            if (sample.Contains("\t")) return '\t';
            return ',';
        }

        // Divide una línea por el separador respetando comillas dobles.
        private string[] SplitLineRespectingQuotes(string line, char separator)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == separator && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}