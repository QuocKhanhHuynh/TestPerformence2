namespace TestPerformence
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnSelectImage = new Button();
            picOriginal = new PictureBox();
            picProcessed = new PictureBox();
            lblQRCode = new Label();
            txtQRCode = new TextBox();
            lblProductTotal = new Label();
            txtProductTotal = new TextBox();
            lblProductCode = new Label();
            txtProductCode = new TextBox();
            lblSize = new Label();
            txtSize = new TextBox();
            lblColor = new Label();
            txtColor = new TextBox();
            ((System.ComponentModel.ISupportInitialize)picOriginal).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picProcessed).BeginInit();
            SuspendLayout();
            // 
            // btnSelectImage
            // 
            btnSelectImage.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSelectImage.Location = new Point(12, 12);
            btnSelectImage.Name = "btnSelectImage";
            btnSelectImage.Size = new Size(150, 40);
            btnSelectImage.TabIndex = 0;
            btnSelectImage.Text = "Chọn ảnh";
            btnSelectImage.UseVisualStyleBackColor = true;
            btnSelectImage.Click += btnSelectImage_Click;
            // 
            // picOriginal
            // 
            picOriginal.BorderStyle = BorderStyle.FixedSingle;
            picOriginal.Location = new Point(12, 60);
            picOriginal.Name = "picOriginal";
            picOriginal.Size = new Size(640, 480);
            picOriginal.SizeMode = PictureBoxSizeMode.Zoom;
            picOriginal.TabIndex = 1;
            picOriginal.TabStop = false;
            // 
            // picProcessed
            // 
            picProcessed.BorderStyle = BorderStyle.FixedSingle;
            picProcessed.Location = new Point(670, 60);
            picProcessed.Name = "picProcessed";
            picProcessed.Size = new Size(400, 300);
            picProcessed.SizeMode = PictureBoxSizeMode.Zoom;
            picProcessed.TabIndex = 2;
            picProcessed.TabStop = false;
            // 
            // lblQRCode
            // 
            lblQRCode.AutoSize = true;
            lblQRCode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblQRCode.Location = new Point(670, 380);
            lblQRCode.Name = "lblQRCode";
            lblQRCode.Size = new Size(65, 15);
            lblQRCode.TabIndex = 3;
            lblQRCode.Text = "QR Code:";
            // 
            // txtQRCode
            // 
            txtQRCode.Font = new Font("Segoe UI", 9F);
            txtQRCode.Location = new Point(800, 377);
            txtQRCode.Name = "txtQRCode";
            txtQRCode.ReadOnly = true;
            txtQRCode.Size = new Size(270, 23);
            txtQRCode.TabIndex = 4;
            // 
            // lblProductTotal
            // 
            lblProductTotal.AutoSize = true;
            lblProductTotal.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblProductTotal.Location = new Point(670, 415);
            lblProductTotal.Name = "lblProductTotal";
            lblProductTotal.Size = new Size(123, 15);
            lblProductTotal.TabIndex = 5;
            lblProductTotal.Text = "Tổng số sản phẩm:";
            // 
            // txtProductTotal
            // 
            txtProductTotal.Font = new Font("Segoe UI", 9F);
            txtProductTotal.Location = new Point(800, 412);
            txtProductTotal.Name = "txtProductTotal";
            txtProductTotal.ReadOnly = true;
            txtProductTotal.Size = new Size(270, 23);
            txtProductTotal.TabIndex = 6;
            // 
            // lblProductCode
            // 
            lblProductCode.AutoSize = true;
            lblProductCode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblProductCode.Location = new Point(670, 450);
            lblProductCode.Name = "lblProductCode";
            lblProductCode.Size = new Size(96, 15);
            lblProductCode.TabIndex = 7;
            lblProductCode.Text = "Mã sản phẩm:";
            // 
            // txtProductCode
            // 
            txtProductCode.Font = new Font("Segoe UI", 9F);
            txtProductCode.Location = new Point(800, 447);
            txtProductCode.Name = "txtProductCode";
            txtProductCode.ReadOnly = true;
            txtProductCode.Size = new Size(270, 23);
            txtProductCode.TabIndex = 8;
            // 
            // lblSize
            // 
            lblSize.AutoSize = true;
            lblSize.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblSize.Location = new Point(670, 485);
            lblSize.Name = "lblSize";
            lblSize.Size = new Size(33, 15);
            lblSize.TabIndex = 9;
            lblSize.Text = "Size:";
            // 
            // txtSize
            // 
            txtSize.Font = new Font("Segoe UI", 9F);
            txtSize.Location = new Point(800, 482);
            txtSize.Name = "txtSize";
            txtSize.ReadOnly = true;
            txtSize.Size = new Size(270, 23);
            txtSize.TabIndex = 10;
            // 
            // lblColor
            // 
            lblColor.AutoSize = true;
            lblColor.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblColor.Location = new Point(670, 520);
            lblColor.Name = "lblColor";
            lblColor.Size = new Size(66, 15);
            lblColor.TabIndex = 11;
            lblColor.Text = "Màu sắc:";
            // 
            // txtColor
            // 
            txtColor.Font = new Font("Segoe UI", 9F);
            txtColor.Location = new Point(800, 517);
            txtColor.Name = "txtColor";
            txtColor.ReadOnly = true;
            txtColor.Size = new Size(270, 23);
            txtColor.TabIndex = 12;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1084, 561);
            Controls.Add(txtColor);
            Controls.Add(lblColor);
            Controls.Add(txtSize);
            Controls.Add(lblSize);
            Controls.Add(txtProductCode);
            Controls.Add(lblProductCode);
            Controls.Add(txtProductTotal);
            Controls.Add(lblProductTotal);
            Controls.Add(txtQRCode);
            Controls.Add(lblQRCode);
            Controls.Add(picProcessed);
            Controls.Add(picOriginal);
            Controls.Add(btnSelectImage);
            Name = "Form1";
            Text = "Label Detection - YOLO & OCR";
            ((System.ComponentModel.ISupportInitialize)picOriginal).EndInit();
            ((System.ComponentModel.ISupportInitialize)picProcessed).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSelectImage;
        private PictureBox picOriginal;
        private PictureBox picProcessed;
        private Label lblQRCode;
        private TextBox txtQRCode;
        private Label lblProductTotal;
        private TextBox txtProductTotal;
        private Label lblProductCode;
        private TextBox txtProductCode;
        private Label lblSize;
        private TextBox txtSize;
        private Label lblColor;
        private TextBox txtColor;

    }
}
