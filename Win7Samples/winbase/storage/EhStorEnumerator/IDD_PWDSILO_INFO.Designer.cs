
namespace EhStorEnumerator
{
    partial class IDD_PWDSILO_INFO
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
            this.IDOK = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.IDC_FRIENDLY_NAME = new System.Windows.Forms.TextBox();
            this.IDC_ADMIN_HINT = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.IDC_USER_NAME = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.IDC_USER_HINT = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.IDC_AUTHN_STATE = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // IDOK
            // 
            this.IDOK.Location = new System.Drawing.Point(367, 163);
            this.IDOK.Name = "IDOK";
            this.IDOK.Size = new System.Drawing.Size(75, 23);
            this.IDOK.TabIndex = 0;
            this.IDOK.Text = "OK";
            this.IDOK.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(109, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Silo Friendly Name:";
            // 
            // IDC_FRIENDLY_NAME
            // 
            this.IDC_FRIENDLY_NAME.Location = new System.Drawing.Point(137, 13);
            this.IDC_FRIENDLY_NAME.Name = "IDC_FRIENDLY_NAME";
            this.IDC_FRIENDLY_NAME.ReadOnly = true;
            this.IDC_FRIENDLY_NAME.Size = new System.Drawing.Size(305, 23);
            this.IDC_FRIENDLY_NAME.TabIndex = 2;
            // 
            // IDC_ADMIN_HINT
            // 
            this.IDC_ADMIN_HINT.Location = new System.Drawing.Point(137, 42);
            this.IDC_ADMIN_HINT.Name = "IDC_ADMIN_HINT";
            this.IDC_ADMIN_HINT.ReadOnly = true;
            this.IDC_ADMIN_HINT.Size = new System.Drawing.Size(305, 23);
            this.IDC_ADMIN_HINT.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 45);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "Admin Hint:";
            // 
            // IDC_USER_NAME
            // 
            this.IDC_USER_NAME.Location = new System.Drawing.Point(137, 71);
            this.IDC_USER_NAME.Name = "IDC_USER_NAME";
            this.IDC_USER_NAME.ReadOnly = true;
            this.IDC_USER_NAME.Size = new System.Drawing.Size(305, 23);
            this.IDC_USER_NAME.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 74);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 15);
            this.label3.TabIndex = 5;
            this.label3.Text = "User Name:";
            // 
            // IDC_USER_HINT
            // 
            this.IDC_USER_HINT.Location = new System.Drawing.Point(137, 102);
            this.IDC_USER_HINT.Name = "IDC_USER_HINT";
            this.IDC_USER_HINT.ReadOnly = true;
            this.IDC_USER_HINT.Size = new System.Drawing.Size(305, 23);
            this.IDC_USER_HINT.TabIndex = 8;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 105);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(112, 15);
            this.label4.TabIndex = 7;
            this.label4.Text = "User Password Hint:";
            // 
            // IDC_AUTHN_STATE
            // 
            this.IDC_AUTHN_STATE.Location = new System.Drawing.Point(137, 131);
            this.IDC_AUTHN_STATE.Name = "IDC_AUTHN_STATE";
            this.IDC_AUTHN_STATE.ReadOnly = true;
            this.IDC_AUTHN_STATE.Size = new System.Drawing.Size(305, 23);
            this.IDC_AUTHN_STATE.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 134);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(118, 15);
            this.label5.TabIndex = 9;
            this.label5.Text = "Authentication State:";
            // 
            // IDD_PWDSILO_INFO
            // 
            this.AcceptButton = this.IDOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(459, 198);
            this.Controls.Add(this.IDC_AUTHN_STATE);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.IDC_USER_HINT);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.IDC_USER_NAME);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.IDC_ADMIN_HINT);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.IDC_FRIENDLY_NAME);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IDOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "IDD_PWDSILO_INFO";
            this.Text = "Password silo information";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button IDOK;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox IDC_FRIENDLY_NAME;
        private System.Windows.Forms.TextBox IDC_ADMIN_HINT;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox IDC_USER_NAME;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox IDC_USER_HINT;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox IDC_AUTHN_STATE;
        private System.Windows.Forms.Label label5;
    }
}