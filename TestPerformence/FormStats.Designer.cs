namespace TestPerformence
{
    partial class FormStats
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
            this.lblSuccessAvg = new System.Windows.Forms.Label();
            this.lblOCRFailAvg = new System.Windows.Forms.Label();
            this.lblQRFailAvg = new System.Windows.Forms.Label();
            this.lstOCRFailFiles = new System.Windows.Forms.ListBox();
            this.lstQRFailFiles = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblSuccessAvg
            // 
            this.lblSuccessAvg.AutoSize = true;
            this.lblSuccessAvg.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblSuccessAvg.Location = new System.Drawing.Point(20, 20);
            this.lblSuccessAvg.Name = "lblSuccessAvg";
            this.lblSuccessAvg.Size = new System.Drawing.Size(220, 19);
            this.lblSuccessAvg.TabIndex = 0;
            this.lblSuccessAvg.Text = "Thời gian TB thành công: 0 ms";
            // 
            // lblOCRFailAvg
            // 
            this.lblOCRFailAvg.AutoSize = true;
            this.lblOCRFailAvg.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblOCRFailAvg.Location = new System.Drawing.Point(20, 50);
            this.lblOCRFailAvg.Name = "lblOCRFailAvg";
            this.lblOCRFailAvg.Size = new System.Drawing.Size(200, 19);
            this.lblOCRFailAvg.TabIndex = 1;
            this.lblOCRFailAvg.Text = "Thời gian TB lỗi OCR: 0 ms";
            // 
            // lblQRFailAvg
            // 
            this.lblQRFailAvg.AutoSize = true;
            this.lblQRFailAvg.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblQRFailAvg.Location = new System.Drawing.Point(20, 80);
            this.lblQRFailAvg.Name = "lblQRFailAvg";
            this.lblQRFailAvg.Size = new System.Drawing.Size(193, 19);
            this.lblQRFailAvg.TabIndex = 2;
            this.lblQRFailAvg.Text = "Thời gian TB lỗi QR: 0 ms";
            // 
            // lstOCRFailFiles
            // 
            this.lstOCRFailFiles.FormattingEnabled = true;
            this.lstOCRFailFiles.ItemHeight = 15;
            this.lstOCRFailFiles.Location = new System.Drawing.Point(20, 140);
            this.lstOCRFailFiles.Name = "lstOCRFailFiles";
            this.lstOCRFailFiles.Size = new System.Drawing.Size(350, 150);
            this.lstOCRFailFiles.TabIndex = 3;
            // 
            // lstQRFailFiles
            // 
            this.lstQRFailFiles.FormattingEnabled = true;
            this.lstQRFailFiles.ItemHeight = 15;
            this.lstQRFailFiles.Location = new System.Drawing.Point(400, 140);
            this.lstQRFailFiles.Name = "lstQRFailFiles";
            this.lstQRFailFiles.Size = new System.Drawing.Size(350, 150);
            this.lstQRFailFiles.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.label1.Location = new System.Drawing.Point(20, 120);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(126, 15);
            this.label1.TabIndex = 5;
            this.label1.Text = "Danh sách file lỗi OCR:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
            this.label2.Location = new System.Drawing.Point(400, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(119, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "Danh sách file lỗi QR:";
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(675, 300);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 7;
            this.btnClose.Text = "Đóng";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // 
            // label3
            // 
            this.label3 = new System.Windows.Forms.Label();
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.Color.DimGray;
            this.label3.Location = new System.Drawing.Point(20, 305);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(250, 15);
            this.label3.TabIndex = 8;
            this.label3.Text = "* Nhấn vào tên file trong danh sách để Copy";
            // 
            // FormStats
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(780, 340);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lstQRFailFiles);
            this.Controls.Add(this.lstOCRFailFiles);
            this.Controls.Add(this.lblQRFailAvg);
            this.Controls.Add(this.lblOCRFailAvg);
            this.Controls.Add(this.lblSuccessAvg);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "FormStats";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Thống kế hiệu năng chi tiết";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblSuccessAvg;
        private System.Windows.Forms.Label lblOCRFailAvg;
        private System.Windows.Forms.Label lblQRFailAvg;
        private System.Windows.Forms.ListBox lstOCRFailFiles;
        private System.Windows.Forms.ListBox lstQRFailFiles;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnClose;
    }
}
