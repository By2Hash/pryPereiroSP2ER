namespace pryPereiroSP2ER
{
    partial class frmPrincipal
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmPrincipal));
            this.lblSeleccionarArch = new System.Windows.Forms.Label();
            this.btnExaminar = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.lstMigracion = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // lblSeleccionarArch
            // 
            resources.ApplyResources(this.lblSeleccionarArch, "lblSeleccionarArch");
            this.lblSeleccionarArch.Name = "lblSeleccionarArch";
            // 
            // btnExaminar
            // 
            resources.ApplyResources(this.btnExaminar, "btnExaminar");
            this.btnExaminar.Name = "btnExaminar";
            this.btnExaminar.UseVisualStyleBackColor = true;
            this.btnExaminar.Click += new System.EventHandler(this.btnExaminar_Click);
            // 
            // button2
            // 
            resources.ApplyResources(this.button2, "button2");
            this.button2.Name = "button2";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // lstMigracion
            // 
            this.lstMigracion.FormattingEnabled = true;
            this.lstMigracion.Items.AddRange(new object[] {
            resources.GetString("lstMigracion.Items")});
            resources.ApplyResources(this.lstMigracion, "lstMigracion");
            this.lstMigracion.Name = "lstMigracion";
            // 
            // frmPrincipal
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lstMigracion);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.btnExaminar);
            this.Controls.Add(this.lblSeleccionarArch);
            this.Name = "frmPrincipal";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSeleccionarArch;
        private System.Windows.Forms.Button btnExaminar;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.ListBox lstMigracion;
    }
}

