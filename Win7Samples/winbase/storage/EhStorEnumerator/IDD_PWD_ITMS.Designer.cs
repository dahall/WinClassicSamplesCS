
namespace EhStorEnumerator
{
    partial class IDD_PWD_ITMS
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
            this.IDC_DEVICE_SID = new System.Windows.Forms.TextBox();
            this.IDOK = new System.Windows.Forms.Button();
            this.IDCANCEL = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 15);
            this.label1.TabIndex = 0;
            this.label1.Text = "Security Identifier:";
            // 
            // IDC_DEVICE_SID
            // 
            this.IDC_DEVICE_SID.Location = new System.Drawing.Point(121, 12);
            this.IDC_DEVICE_SID.Name = "IDC_DEVICE_SID";
            this.IDC_DEVICE_SID.Size = new System.Drawing.Size(331, 23);
            this.IDC_DEVICE_SID.TabIndex = 1;
            // 
            // IDOK
            // 
            this.IDOK.Location = new System.Drawing.Point(296, 45);
            this.IDOK.Name = "IDOK";
            this.IDOK.Size = new System.Drawing.Size(75, 23);
            this.IDOK.TabIndex = 2;
            this.IDOK.Text = "OK";
            this.IDOK.UseVisualStyleBackColor = true;
            // 
            // IDCANCEL
            // 
            this.IDCANCEL.Location = new System.Drawing.Point(377, 45);
            this.IDCANCEL.Name = "IDCANCEL";
            this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
            this.IDCANCEL.TabIndex = 3;
            this.IDCANCEL.Text = "Cancel";
            this.IDCANCEL.UseVisualStyleBackColor = true;
            // 
            // IDD_PWD_ITMS
            // 
            this.AcceptButton = this.IDOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.IDCANCEL;
            this.ClientSize = new System.Drawing.Size(464, 79);
            this.Controls.Add(this.IDCANCEL);
            this.Controls.Add(this.IDOK);
            this.Controls.Add(this.IDC_DEVICE_SID);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "IDD_PWD_ITMS";
            this.Text = "Init to Manufacturer State";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox IDC_DEVICE_SID;
        private System.Windows.Forms.Button IDOK;
        private System.Windows.Forms.Button IDCANCEL;
    }
}