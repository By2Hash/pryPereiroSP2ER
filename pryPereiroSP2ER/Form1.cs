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
                            var nombreArchivo = Path.GetFileName(ruta);
                            // Mostrar sólo el nombre para claridad, la ruta se guarda internamente
                            lstMigracion.Items.Add("Añadido: " + nombreArchivo);
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
                    var nombreTabla = MakeSafeTableName(Path.GetFileNameWithoutExtension(ruta));

                    try
                    {
                        var lines = File.ReadAllLines(ruta, Encoding.Default);
                        if (lines.Length == 0)
                        {
                            lstMigracion.Items.Add("Error en " + nombreArchivo + ": archivo vacío");
                            continue;
                        }

                        // Detectar separador (priorizar ;, luego ,, luego tab)
                        char sep = DetectSeparator(lines[0]);

                        var headers = SplitLineRespectingQuotes(lines[0], sep);
                        var dt = new DataTable();

                        foreach (var raw in headers)
                        {
                            var colName = string.IsNullOrWhiteSpace(raw) ? "Columna" : raw.Trim();
                            // evitar columnas duplicadas
                            var unique = colName;
                            int counter = 1;
                            while (dt.Columns.Contains(unique))
                            {
                                unique = colName + "_" + counter;
                                counter++;
                            }
                            dt.Columns.Add(unique, typeof(string));
                        }

                        for (int i = 1; i < lines.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(lines[i]))
                                continue;

                            var parts = SplitLineRespectingQuotes(lines[i], sep);
                            var row = dt.NewRow();
                            for (int c = 0; c < dt.Columns.Count; c++)
                            {
                                if (c < parts.Length)
                                    row[c] = parts[c].Trim();
                                else
                                    row[c] = string.Empty;
                            }
                            dt.Rows.Add(row);
                        }

                        var ok = conexion.CreateTableFromDataTable(rutaDb, nombreTabla, dt);
                        if (ok)
                        {
                            lstMigracion.Items.Add("Migrado: " + nombreArchivo + " -> Tabla: " + nombreTabla);
                        }
                        else
                        {
                            lstMigracion.Items.Add("Error en " + nombreArchivo + ": " + conexion.ObtenerError());
                        }
                    }
                    catch (Exception ex)
                    {
                        lstMigracion.Items.Add("Error en " + nombreArchivo + ": " + ex.Message);
                    }
                } // foreach archivos

                lstMigracion.Items.Add("Migración finalizada.");
            } // using SaveFileDialog
        }

        // Helpers

        // Crea un nombre de tabla seguro (quita caracteres inválidos y limita longitud razonable)
        private string MakeSafeTableName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Tabla";

            var s = raw.Trim();
            var invalid = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '-', '.' }).ToArray();
            foreach (var ch in invalid)
                s = s.Replace(ch.ToString(), "_");

            if (s.Length > 60)
                s = s.Substring(0, 60);

            // Asegurar que no quede vacío
            if (string.IsNullOrWhiteSpace(s))
                s = "Tabla";

            return s;
        }

        // Detecta separador más probable en una línea (prioriza ;, luego ,, luego tab)
        private char DetectSeparator(string sample)
        {
            if (sample.Contains(";"))
                return ';';
            if (sample.Contains(","))
                return ',';
            if (sample.Contains("\t"))
                return '\t';
            return ','; // fallback
        }

        // Splits a line by separator respecting double quotes (simple parser).
        private string[] SplitLineRespectingQuotes(string line, char separator)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    // manejar comillas dobles escapadas ""
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // saltar la segunda comilla
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
