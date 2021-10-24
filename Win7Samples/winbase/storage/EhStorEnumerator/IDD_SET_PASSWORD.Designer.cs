
namespace EhStorEnumerator
{
    partial class IDD_SET_PASSWORD
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
            this.IDC_INDICATOR_ADMIN = new System.Windows.Forms.RadioButton();
            this.IDC_INDICATOR_USER = new System.Windows.Forms.RadioButton();
            this.IDC_ODL_PASSWORD = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.IDC_NEW_PASSWORD = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.IDC_PASSWORD_HINT = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.IDC_DEVICE_SID = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.IDOK = new System.Windows.Forms.Button();
            this.IDCANCEL = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // IDC_INDICATOR_ADMIN
            // 
            this.IDC_INDICATOR_ADMIN.AutoSize = true;
            this.IDC_INDICATOR_ADMIN.Checked = true;
            this.IDC_INDICATOR_ADMIN.Location = new System.Drawing.Point(10, 22);
            this.IDC_INDICATOR_ADMIN.Name = "IDC_INDICATOR_ADMIN";
            this.IDC_INDICATOR_ADMIN.Size = new System.Drawing.Size(98, 19);
            this.IDC_INDICATOR_ADMIN.TabIndex = 0;
            this.IDC_INDICATOR_ADMIN.TabStop = true;
            this.IDC_INDICATOR_ADMIN.Text = "&Administrator";
            this.IDC_INDICATOR_ADMIN.UseVisualStyleBackColor = true;
            this.IDC_INDICATOR_ADMIN.CheckedChanged += new System.EventHandler(this.OnPwdIndicatorCheckChanged);
            // 
            // IDC_INDICATOR_USER
            // 
            this.IDC_INDICATOR_USER.AutoSize = true;
            this.IDC_INDICATOR_USER.Location = new System.Drawing.Point(10, 47);
            this.IDC_INDICATOR_USER.Name = "IDC_INDICATOR_USER";
            this.IDC_INDICATOR_USER.Size = new System.Drawing.Size(48, 19);
            this.IDC_INDICATOR_USER.TabIndex = 0;
            this.IDC_INDICATOR_USER.TabStop = true;
            this.IDC_INDICATOR_USER.Text = "&User";
            this.IDC_INDICATOR_USER.UseVisualStyleBackColor = true;
            this.IDC_INDICATOR_USER.CheckedChanged += new System.EventHandler(this.OnPwdIndicatorCheckChanged);
            // 
            // IDC_ODL_PASSWORD
            // 
            this.IDC_ODL_PASSWORD.Location = new System.Drawing.Point(125, 12);
            this.IDC_ODL_PASSWORD.Name = "IDC_ODL_PASSWORD";
            this.IDC_ODL_PASSWORD.Size = new System.Drawing.Size(243, 23);
            this.IDC_ODL_PASSWORD.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Old Password:";
            // 
            // IDC_NEW_PASSWORD
            // 
            this.IDC_NEW_PASSWORD.Location = new System.Drawing.Point(125, 41);
            this.IDC_NEW_PASSWORD.Name = "IDC_NEW_PASSWORD";
            this.IDC_NEW_PASSWORD.Size = new System.Drawing.Size(243, 23);
            this.IDC_NEW_PASSWORD.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 44);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 15);
            this.label2.TabIndex = 2;
            this.label2.Text = "New Password:";
            // 
            // IDC_PASSWORD_HINT
            // 
            this.IDC_PASSWORD_HINT.Location = new System.Drawing.Point(125, 70);
            this.IDC_PASSWORD_HINT.Name = "IDC_PASSWORD_HINT";
            this.IDC_PASSWORD_HINT.Size = new System.Drawing.Size(243, 23);
            this.IDC_PASSWORD_HINT.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 15);
            this.label3.TabIndex = 2;
            this.label3.Text = "Password Hint:";
            // 
            // IDC_DEVICE_SID
            // 
            this.IDC_DEVICE_SID.Location = new System.Drawing.Point(125, 99);
            this.IDC_DEVICE_SID.Name = "IDC_DEVICE_SID";
            this.IDC_DEVICE_SID.Size = new System.Drawing.Size(243, 23);
            this.IDC_DEVICE_SID.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(11, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(102, 15);
            this.label4.TabIndex = 2;
            this.label4.Text = "Security Identifier:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.IDC_INDICATOR_ADMIN);
            this.groupBox1.Controls.Add(this.IDC_INDICATOR_USER);
            this.groupBox1.Location = new System.Drawing.Point(12, 138);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(175, 78);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Password Indicator";
            // 
            // IDOK
            // 
            this.IDOK.Location = new System.Drawing.Point(212, 193);
            this.IDOK.Name = "IDOK";
            this.IDOK.Size = new System.Drawing.Size(75, 23);
            this.IDOK.TabIndex = 4;
            this.IDOK.Text = "OK";
            this.IDOK.UseVisualStyleBackColor = true;
            this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
            // 
            // IDCANCEL
            // 
            this.IDCANCEL.Location = new System.Drawing.Point(293, 193);
            this.IDCANCEL.Name = "IDCANCEL";
            this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
            this.IDCANCEL.TabIndex = 4;
            this.IDCANCEL.Text = "Cancel";
            this.IDCANCEL.UseVisualStyleBackColor = true;
            // 
            // IDD_SET_PASSWORD
            // 
            this.AcceptButton = this.IDOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.IDCANCEL;
            this.ClientSize = new System.Drawing.Size(383, 227);
            this.Controls.Add(this.IDCANCEL);
            this.Controls.Add(this.IDOK);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IDC_DEVICE_SID);
            this.Controls.Add(this.IDC_PASSWORD_HINT);
            this.Controls.Add(this.IDC_NEW_PASSWORD);
            this.Controls.Add(this.IDC_ODL_PASSWORD);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "IDD_SET_PASSWORD";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Set/Change Password";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton IDC_INDICATOR_ADMIN;
        private System.Windows.Forms.RadioButton IDC_INDICATOR_USER;
        private System.Windows.Forms.TextBox IDC_ODL_PASSWORD;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox IDC_NEW_PASSWORD;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox IDC_PASSWORD_HINT;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox IDC_DEVICE_SID;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button IDOK;
        private System.Windows.Forms.Button IDCANCEL;
    }
}