namespace custom_image_downloader
{
    partial class BulkImageDownloader
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BulkImageDownloader));
            txtUrls = new TextBox();
            txtCarpeta = new TextBox();
            btnSeleccionarCarpeta = new Button();
            txtNombreBase = new TextBox();
            btnDescargar = new Button();
            lblEstado = new Label();
            pbProgreso = new ProgressBar();
            btnCancelar = new Button();
            SuspendLayout();
            // 
            // txtUrls
            // 
            txtUrls.Location = new Point(22, 28);
            txtUrls.Multiline = true;
            txtUrls.Name = "txtUrls";
            txtUrls.PlaceholderText = "URLs (one URL per line)";
            txtUrls.ScrollBars = ScrollBars.Vertical;
            txtUrls.Size = new Size(370, 303);
            txtUrls.TabIndex = 0;
            txtUrls.TextChanged += textBox1_TextChanged;
            // 
            // txtCarpeta
            // 
            txtCarpeta.Location = new Point(398, 28);
            txtCarpeta.Name = "txtCarpeta";
            txtCarpeta.PlaceholderText = "Destination path";
            txtCarpeta.ReadOnly = true;
            txtCarpeta.Size = new Size(272, 23);
            txtCarpeta.TabIndex = 1;
            txtCarpeta.Click += txtCarpeta_Click;
            // 
            // btnSeleccionarCarpeta
            // 
            btnSeleccionarCarpeta.Location = new Point(676, 28);
            btnSeleccionarCarpeta.Name = "btnSeleccionarCarpeta";
            btnSeleccionarCarpeta.Size = new Size(112, 23);
            btnSeleccionarCarpeta.TabIndex = 2;
            btnSeleccionarCarpeta.Text = "Browse...";
            btnSeleccionarCarpeta.UseVisualStyleBackColor = true;
            btnSeleccionarCarpeta.Click += btnSeleccionarCarpeta_Click;
            // 
            // txtNombreBase
            // 
            txtNombreBase.Location = new Point(398, 67);
            txtNombreBase.Name = "txtNombreBase";
            txtNombreBase.PlaceholderText = "Subfolder";
            txtNombreBase.Size = new Size(272, 23);
            txtNombreBase.TabIndex = 3;
            // 
            // btnDescargar
            // 
            btnDescargar.Location = new Point(22, 350);
            btnDescargar.Name = "btnDescargar";
            btnDescargar.Size = new Size(123, 23);
            btnDescargar.TabIndex = 4;
            btnDescargar.Text = "Download All";
            btnDescargar.UseVisualStyleBackColor = true;
            btnDescargar.Click += btnDescargar_Click;
            // 
            // lblEstado
            // 
            lblEstado.AutoSize = true;
            lblEstado.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblEstado.ForeColor = Color.RoyalBlue;
            lblEstado.Location = new Point(398, 107);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new Size(99, 30);
            lblEstado.TabIndex = 5;
            lblEstado.Text = "Waiting...";
            // 
            // pbProgreso
            // 
            pbProgreso.Location = new Point(398, 158);
            pbProgreso.Name = "pbProgreso";
            pbProgreso.Size = new Size(390, 23);
            pbProgreso.TabIndex = 6;
            // 
            // btnCancelar
            // 
            btnCancelar.Location = new Point(151, 350);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Size = new Size(91, 23);
            btnCancelar.TabIndex = 7;
            btnCancelar.Text = "Cancel";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // BulkImageDownloader
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnCancelar);
            Controls.Add(pbProgreso);
            Controls.Add(lblEstado);
            Controls.Add(btnDescargar);
            Controls.Add(txtNombreBase);
            Controls.Add(btnSeleccionarCarpeta);
            Controls.Add(txtCarpeta);
            Controls.Add(txtUrls);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "BulkImageDownloader";
            Text = "Bulk image downloader";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox txtUrls;
        private TextBox txtCarpeta;
        private Button btnSeleccionarCarpeta;
        private TextBox txtNombreBase;
        private Button btnDescargar;
        private Label lblEstado;
        private ProgressBar pbProgreso;
        private Button btnCancelar;
    }
}
