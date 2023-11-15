namespace WindowsFormsApp1
{
    partial class FingerPrint
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
            this.components = new System.ComponentModel.Container();
            this.SystemTrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.lblNotifi = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // SystemTrayIcon
            // 
            this.SystemTrayIcon.Text = "Fingerprint";
            this.SystemTrayIcon.Visible = true;
            // 
            // lblNotifi
            // 
            this.lblNotifi.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.lblNotifi.AutoSize = true;
            this.lblNotifi.Font = new System.Drawing.Font("Arial", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNotifi.Location = new System.Drawing.Point(15, 9);
            this.lblNotifi.Name = "lblNotifi";
            this.lblNotifi.Size = new System.Drawing.Size(219, 17);
            this.lblNotifi.TabIndex = 0;
            this.lblNotifi.Text = "Please do not turn off application";
            // 
            // FingerPrint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(247, 35);
            this.Controls.Add(this.lblNotifi);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Name = "FingerPrint";
            this.Opacity = 0D;
            this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FingerPrint";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon SystemTrayIcon;
        public System.Windows.Forms.Label lblNotifi;
    }
}

