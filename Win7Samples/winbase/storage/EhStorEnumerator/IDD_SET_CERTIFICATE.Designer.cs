
namespace EhStorEnumerator
{
    partial class IDD_SET_CERTIFICATE
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
            this.IDCANCEL = new System.Windows.Forms.Button();
            this.IDOK = new System.Windows.Forms.Button();
            this.IDC_DEVICE_ID = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.IDC_CERT_SUBJECT = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.IDC_CERT_TYPE = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.IDC_VALIDATION_POLICY = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.IDC_CERT_INDEX = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.IDC_CERT_SIGNER_INDEX = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // IDCANCEL
            // 
            this.IDCANCEL.Location = new System.Drawing.Point(448, 172);
            this.IDCANCEL.Name = "IDCANCEL";
            this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
            this.IDCANCEL.TabIndex = 5;
            this.IDCANCEL.Text = "Cancel";
            this.IDCANCEL.UseVisualStyleBackColor = true;
            // 
            // IDOK
            // 
            this.IDOK.Location = new System.Drawing.Point(367, 172);
            this.IDOK.Name = "IDOK";
            this.IDOK.Size = new System.Drawing.Size(75, 23);
            this.IDOK.TabIndex = 6;
            this.IDOK.Text = "OK";
            this.IDOK.UseVisualStyleBackColor = true;
            this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
            // 
            // IDC_DEVICE_ID
            // 
            this.IDC_DEVICE_ID.Location = new System.Drawing.Point(121, 12);
            this.IDC_DEVICE_ID.Name = "IDC_DEVICE_ID";
            this.IDC_DEVICE_ID.ReadOnly = true;
            this.IDC_DEVICE_ID.Size = new System.Drawing.Size(402, 23);
            this.IDC_DEVICE_ID.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "Device:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 125);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(106, 15);
            this.label2.TabIndex = 7;
            this.label2.Text = "Certificate Subject:";
            // 
            // IDC_CERT_SUBJECT
            // 
            this.IDC_CERT_SUBJECT.Location = new System.Drawing.Point(120, 122);
            this.IDC_CERT_SUBJECT.Name = "IDC_CERT_SUBJECT";
            this.IDC_CERT_SUBJECT.ReadOnly = true;
            this.IDC_CERT_SUBJECT.Size = new System.Drawing.Size(403, 23);
            this.IDC_CERT_SUBJECT.TabIndex = 8;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 44);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(90, 15);
            this.label3.TabIndex = 7;
            this.label3.Text = "Certificate type:";
            // 
            // IDC_CERT_TYPE
            // 
            this.IDC_CERT_TYPE.FormattingEnabled = true;
            this.IDC_CERT_TYPE.Location = new System.Drawing.Point(121, 41);
            this.IDC_CERT_TYPE.Name = "IDC_CERT_TYPE";
            this.IDC_CERT_TYPE.Size = new System.Drawing.Size(179, 23);
            this.IDC_CERT_TYPE.TabIndex = 9;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 73);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(97, 15);
            this.label4.TabIndex = 7;
            this.label4.Text = "Validation Policy:";
            // 
            // IDC_VALIDATION_POLICY
            // 
            this.IDC_VALIDATION_POLICY.FormattingEnabled = true;
            this.IDC_VALIDATION_POLICY.Location = new System.Drawing.Point(121, 70);
            this.IDC_VALIDATION_POLICY.Name = "IDC_VALIDATION_POLICY";
            this.IDC_VALIDATION_POLICY.Size = new System.Drawing.Size(179, 23);
            this.IDC_VALIDATION_POLICY.TabIndex = 9;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(333, 44);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(39, 15);
            this.label5.TabIndex = 7;
            this.label5.Text = "Index:";
            // 
            // IDC_CERT_INDEX
            // 
            this.IDC_CERT_INDEX.FormattingEnabled = true;
            this.IDC_CERT_INDEX.Location = new System.Drawing.Point(414, 41);
            this.IDC_CERT_INDEX.Name = "IDC_CERT_INDEX";
            this.IDC_CERT_INDEX.Size = new System.Drawing.Size(109, 23);
            this.IDC_CERT_INDEX.TabIndex = 9;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(333, 73);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(75, 15);
            this.label6.TabIndex = 7;
            this.label6.Text = "Signer Index:";
            // 
            // IDC_CERT_SIGNER_INDEX
            // 
            this.IDC_CERT_SIGNER_INDEX.FormattingEnabled = true;
            this.IDC_CERT_SIGNER_INDEX.Location = new System.Drawing.Point(414, 70);
            this.IDC_CERT_SIGNER_INDEX.Name = "IDC_CERT_SIGNER_INDEX";
            this.IDC_CERT_SIGNER_INDEX.Size = new System.Drawing.Size(109, 23);
            this.IDC_CERT_SIGNER_INDEX.TabIndex = 9;
            // 
            // IDD_SET_CERTIFICATE
            // 
            this.AcceptButton = this.IDOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.IDCANCEL;
            this.ClientSize = new System.Drawing.Size(544, 207);
            this.Controls.Add(this.IDC_CERT_SIGNER_INDEX);
            this.Controls.Add(this.IDC_CERT_INDEX);
            this.Controls.Add(this.IDC_VALIDATION_POLICY);
            this.Controls.Add(this.IDC_CERT_TYPE);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.IDC_CERT_SUBJECT);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.IDC_DEVICE_ID);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IDCANCEL);
            this.Controls.Add(this.IDOK);
            this.Name = "IDD_SET_CERTIFICATE";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Set Certificate";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button IDCANCEL;
        private System.Windows.Forms.Button IDOK;
        private System.Windows.Forms.TextBox IDC_DEVICE_ID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox IDC_CERT_SUBJECT;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox IDC_CERT_TYPE;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox IDC_VALIDATION_POLICY;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox IDC_CERT_INDEX;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox IDC_CERT_SIGNER_INDEX;
    }
}