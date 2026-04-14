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

namespace pryPereiroSP2ER
{
    public partial class frmPrincipal : Form
    {
        public frmPrincipal()
        {
            InitializeComponent();
        }

        private void btnExaminar_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
                dlg.Title = "Seleccione un archivo de texto";
                dlg.Multiselect = false;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    var nombreArchivo = Path.GetFileName(dlg.FileName);
                    lstMigracion.Items.Add("- - - - - - - - - - - - - - - - - - - - - - - - - " + nombreArchivo + " - - - - - - - - - - - - - - - - - - - - - - - - - - - - ");
                    lstMigracion.Items.Add("- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - ");

                }
            }
        }
    }
}
