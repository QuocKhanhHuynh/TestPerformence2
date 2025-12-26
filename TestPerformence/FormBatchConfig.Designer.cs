namespace TestPerformence
{
    partial class FormBatchConfig
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.chkUseOpenVINO = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // chkUseOpenVINO
            // 
            this.chkUseOpenVINO.AutoSize = true;
            this.chkUseOpenVINO.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.chkUseOpenVINO.Location = new System.Drawing.Point(30, 60);
            this.chkUseOpenVINO.Name = "chkUseOpenVINO";
            this.chkUseOpenVINO.Size = new System.Drawing.Size(147, 23);
            this.chkUseOpenVINO.TabIndex = 0;
            this.chkUseOpenVINO.Text = "Sử dụng OpenVINO";
            this.chkUseOpenVINO.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.btnSave.Location = new Point(30, 100);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new Size(240, 45);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Lưu cấu hình";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.label1.Location = new System.Drawing.Point(25, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(193, 21);
            this.label1.TabIndex = 2;
            this.label1.Text = "Cấu hình chạy thống kê";
            // 
            // FormBatchConfig
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 180);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.chkUseOpenVINO);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormBatchConfig";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cấu hình suy luận";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.CheckBox chkUseOpenVINO;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
    }
}
