namespace custom_image_downloader
{
    partial class BulkImageDownloaderForm
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BulkImageDownloaderForm));
            txtUrls = new System.Windows.Forms.TextBox();
            txtCarpeta = new System.Windows.Forms.TextBox();
            btnSeleccionarCarpeta = new System.Windows.Forms.Button();
            txtNombreBase = new System.Windows.Forms.TextBox();
            btnDescargar = new System.Windows.Forms.Button();
            lblEstado = new System.Windows.Forms.Label();
            pbProgreso = new System.Windows.Forms.ProgressBar();
            btnCancelar = new System.Windows.Forms.Button();
            numConcurrencia = new System.Windows.Forms.NumericUpDown();
            label1 = new System.Windows.Forms.Label();
            btnPausar = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)numConcurrencia).BeginInit();
            SuspendLayout();
            // 
            // txtUrls
            // 
            txtUrls.Location = new System.Drawing.Point(22, 28);
            txtUrls.Multiline = true;
            txtUrls.Name = "txtUrls";
            txtUrls.PlaceholderText = "URLs (one URL per line)";
            txtUrls.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtUrls.Size = new System.Drawing.Size(370, 303);
            txtUrls.TabIndex = 0;
            // 
            // txtCarpeta
            // 
            txtCarpeta.Cursor = System.Windows.Forms.Cursors.Hand;
            txtCarpeta.Location = new System.Drawing.Point(398, 28);
            txtCarpeta.Name = "txtCarpeta";
            txtCarpeta.PlaceholderText = "Destination path";
            txtCarpeta.ReadOnly = true;
            txtCarpeta.Size = new System.Drawing.Size(272, 23);
            txtCarpeta.TabIndex = 1;
            txtCarpeta.Click += txtCarpeta_Click;
            // 
            // btnSeleccionarCarpeta
            // 
            btnSeleccionarCarpeta.Cursor = System.Windows.Forms.Cursors.Hand;
            btnSeleccionarCarpeta.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btnSeleccionarCarpeta.Location = new System.Drawing.Point(676, 28);
            btnSeleccionarCarpeta.Name = "btnSeleccionarCarpeta";
            btnSeleccionarCarpeta.Padding = new System.Windows.Forms.Padding(15, 0, 15, 0);
            btnSeleccionarCarpeta.Size = new System.Drawing.Size(112, 23);
            btnSeleccionarCarpeta.TabIndex = 2;
            btnSeleccionarCarpeta.Text = "Browse...";
            btnSeleccionarCarpeta.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            btnSeleccionarCarpeta.UseVisualStyleBackColor = true;
            btnSeleccionarCarpeta.Click += btnSeleccionarCarpeta_Click;
            // 
            // txtNombreBase
            // 
            txtNombreBase.Location = new System.Drawing.Point(398, 67);
            txtNombreBase.Name = "txtNombreBase";
            txtNombreBase.PlaceholderText = "Subfolder";
            txtNombreBase.Size = new System.Drawing.Size(272, 23);
            txtNombreBase.TabIndex = 3;
            // 
            // btnDescargar
            // 
            btnDescargar.Cursor = System.Windows.Forms.Cursors.Hand;
            btnDescargar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btnDescargar.Location = new System.Drawing.Point(22, 350);
            btnDescargar.Name = "btnDescargar";
            btnDescargar.Padding = new System.Windows.Forms.Padding(10, 0, 10, 0);
            btnDescargar.Size = new System.Drawing.Size(127, 33);
            btnDescargar.TabIndex = 4;
            btnDescargar.Text = "Download All";
            btnDescargar.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            btnDescargar.UseVisualStyleBackColor = true;
            btnDescargar.Click += btnDescargar_Click;
            // 
            // lblEstado
            // 
            lblEstado.AutoSize = true;
            lblEstado.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)0));
            lblEstado.ForeColor = System.Drawing.Color.RoyalBlue;
            lblEstado.Location = new System.Drawing.Point(398, 173);
            lblEstado.Name = "lblEstado";
            lblEstado.Size = new System.Drawing.Size(69, 30);
            lblEstado.TabIndex = 5;
            lblEstado.Text = "Ready";
            // 
            // pbProgreso
            // 
            pbProgreso.Location = new System.Drawing.Point(398, 216);
            pbProgreso.Name = "pbProgreso";
            pbProgreso.Size = new System.Drawing.Size(390, 23);
            pbProgreso.TabIndex = 6;
            // 
            // btnCancelar
            // 
            btnCancelar.Cursor = System.Windows.Forms.Cursors.Hand;
            btnCancelar.Enabled = false;
            btnCancelar.Image = global::custom_image_downloader.Properties.Resources.cancel;
            btnCancelar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btnCancelar.Location = new System.Drawing.Point(275, 350);
            btnCancelar.Name = "btnCancelar";
            btnCancelar.Padding = new System.Windows.Forms.Padding(19, 0, 0, 0);
            btnCancelar.Size = new System.Drawing.Size(108, 33);
            btnCancelar.TabIndex = 7;
            btnCancelar.Text = "Cancel";
            btnCancelar.UseVisualStyleBackColor = true;
            btnCancelar.Click += btnCancelar_Click;
            // 
            // numConcurrencia
            // 
            numConcurrencia.Location = new System.Drawing.Point(398, 123);
            numConcurrencia.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numConcurrencia.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numConcurrencia.Name = "numConcurrencia";
            numConcurrencia.Size = new System.Drawing.Size(272, 23);
            numConcurrencia.TabIndex = 8;
            numConcurrencia.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(398, 102);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(143, 15);
            label1.TabIndex = 9;
            label1.Text = "Simultaneous downloads:";
            // 
            // btnPausar
            // 
            btnPausar.Cursor = System.Windows.Forms.Cursors.Hand;
            btnPausar.Enabled = false;
            btnPausar.Image = global::custom_image_downloader.Properties.Resources.pause;
            btnPausar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btnPausar.Location = new System.Drawing.Point(155, 350);
            btnPausar.Name = "btnPausar";
            btnPausar.Padding = new System.Windows.Forms.Padding(20, 0, 0, 0);
            btnPausar.Size = new System.Drawing.Size(114, 33);
            btnPausar.TabIndex = 10;
            btnPausar.Text = "Pause";
            btnPausar.UseVisualStyleBackColor = true;
            btnPausar.Click += btnPausar_Click;
            // 
            // BulkImageDownloaderForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(813, 454);
            Controls.Add(btnPausar);
            Controls.Add(label1);
            Controls.Add(numConcurrencia);
            Controls.Add(btnCancelar);
            Controls.Add(pbProgreso);
            Controls.Add(lblEstado);
            Controls.Add(btnDescargar);
            Controls.Add(txtNombreBase);
            Controls.Add(btnSeleccionarCarpeta);
            Controls.Add(txtCarpeta);
            Controls.Add(txtUrls);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
            MaximizeBox = false;
            Text = "Bulk image downloader";
            ((System.ComponentModel.ISupportInitialize)numConcurrencia).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox txtUrls;
        private System.Windows.Forms.TextBox txtCarpeta;
        private System.Windows.Forms.Button btnSeleccionarCarpeta;
        private System.Windows.Forms.TextBox txtNombreBase;
        private System.Windows.Forms.Button btnDescargar;
        private System.Windows.Forms.Label lblEstado;
        private System.Windows.Forms.ProgressBar pbProgreso;
        private System.Windows.Forms.Button btnCancelar;
        private System.Windows.Forms.NumericUpDown numConcurrencia;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnPausar;
    }
}
