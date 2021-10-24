
namespace EhStorEnumerator
{
    partial class IDD_CERT_SILO_INFO
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
            this.label1 = new System.Windows.Forms.Label();
            this.IDC_FRIENDLY_NAME = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.IDC_SILO_GUID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.IDC_CERT_COUNT = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.IDC_AUTHN_STATE = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.IDC_HASH_ALGS = new System.Windows.Forms.ListBox();
            this.label6 = new System.Windows.Forms.Label();
            this.IDC_SIGNING_ALGS = new System.Windows.Forms.ListBox();
            this.label7 = new System.Windows.Forms.Label();
            this.IDC_ASYMM_KEY = new System.Windows.Forms.ListBox();
            this.IDOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Silo Friendly Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // IDC_FRIENDLY_NAME
            // 
            this.IDC_FRIENDLY_NAME.Location = new System.Drawing.Point(141, 12);
            this.IDC_FRIENDLY_NAME.Name = "IDC_FRIENDLY_NAME";
            this.IDC_FRIENDLY_NAME.ReadOnly = true;
            this.IDC_FRIENDLY_NAME.Size = new System.Drawing.Size(299, 23);
            this.IDC_FRIENDLY_NAME.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(59, 15);
            this.label2.TabIndex = 0;
            this.label2.Text = "Silo GUID:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // IDC_SILO_GUID
            // 
            this.IDC_SILO_GUID.Location = new System.Drawing.Point(141, 41);
            this.IDC_SILO_GUID.Name = "IDC_SILO_GUID";
            this.IDC_SILO_GUID.ReadOnly = true;
            this.IDC_SILO_GUID.Size = new System.Drawing.Size(299, 23);
            this.IDC_SILO_GUID.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(103, 15);
            this.label3.TabIndex = 0;
            this.label3.Text = "Certificates count:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // IDC_CERT_COUNT
            // 
            this.IDC_CERT_COUNT.Location = new System.Drawing.Point(141, 70);
            this.IDC_CERT_COUNT.Name = "IDC_CERT_COUNT";
            this.IDC_CERT_COUNT.ReadOnly = true;
            this.IDC_CERT_COUNT.Size = new System.Drawing.Size(299, 23);
            this.IDC_CERT_COUNT.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(117, 15);
            this.label4.TabIndex = 0;
            this.label4.Text = "Authentication state:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // IDC_AUTHN_STATE
            // 
            this.IDC_AUTHN_STATE.Location = new System.Drawing.Point(141, 99);
            this.IDC_AUTHN_STATE.Name = "IDC_AUTHN_STATE";
            this.IDC_AUTHN_STATE.ReadOnly = true;
            this.IDC_AUTHN_STATE.Size = new System.Drawing.Size(299, 23);
            this.IDC_AUTHN_STATE.TabIndex = 1;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 131);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(99, 15);
            this.label5.TabIndex = 0;
            this.label5.Text = "Hash Algorithms:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // IDC_HASH_ALGS
            // 
            this.IDC_HASH_ALGS.Location = new System.Drawing.Point(141, 128);
            this.IDC_HASH_ALGS.Name = "IDC_HASH_ALGS";
            this.IDC_HASH_ALGS.Size = new System.Drawing.Size(299, 89);
            this.IDC_HASH_ALGS.TabIndex = 1;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 226);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(112, 15);
            this.label6.TabIndex = 0;
            this.label6.Text = "Signing Algorithms:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // IDC_SIGNING_ALGS
            // 
            this.IDC_SIGNING_ALGS.Location = new System.Drawing.Point(141, 223);
            this.IDC_SIGNING_ALGS.Name = "IDC_SIGNING_ALGS";
            this.IDC_SIGNING_ALGS.Size = new System.Drawing.Size(299, 89);
            this.IDC_SIGNING_ALGS.TabIndex = 1;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 321);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(96, 15);
            this.label7.TabIndex = 0;
            this.label7.Text = "Asymmetric Key:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // IDC_ASYMM_KEY
            // 
            this.IDC_ASYMM_KEY.Location = new System.Drawing.Point(141, 318);
            this.IDC_ASYMM_KEY.Name = "IDC_ASYMM_KEY";
            this.IDC_ASYMM_KEY.Size = new System.Drawing.Size(299, 89);
            this.IDC_ASYMM_KEY.TabIndex = 1;
            // 
            // IDOK
            // 
            this.IDOK.Location = new System.Drawing.Point(365, 426);
            this.IDOK.Name = "IDOK";
            this.IDOK.Size = new System.Drawing.Size(75, 23);
            this.IDOK.TabIndex = 2;
            this.IDOK.Text = "OK";
            this.IDOK.UseVisualStyleBackColor = true;
            // 
            // IDD_CERT_SOLO_INFO
            // 
            this.AcceptButton = this.IDOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(452, 463);
            this.Controls.Add(this.IDOK);
            this.Controls.Add(this.IDC_ASYMM_KEY);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.IDC_SIGNING_ALGS);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.IDC_HASH_ALGS);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.IDC_AUTHN_STATE);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.IDC_CERT_COUNT);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.IDC_SILO_GUID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.IDC_FRIENDLY_NAME);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "IDD_CERT_SOLO_INFO";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Certificate Silo Information";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox IDC_FRIENDLY_NAME;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox IDC_SILO_GUID;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox IDC_CERT_COUNT;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox IDC_AUTHN_STATE;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ListBox IDC_HASH_ALGS;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ListBox IDC_SIGNING_ALGS;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ListBox IDC_ASYMM_KEY;
        private System.Windows.Forms.Button IDOK;
    }
}