
namespace EhStorEnumerator
{
    partial class IDD_CERTIFICATES
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
            this.IDC_DELETE = new System.Windows.Forms.Button();
            this.IDC_ADD_TO_DEVICE = new System.Windows.Forms.Button();
            this.IDC_CERT_LIST = new System.Windows.Forms.ListView();
            this.IDC_CERTTAB = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.IDC_CERTTAB.SuspendLayout();
            this.SuspendLayout();
            // 
            // IDOK
            // 
            this.IDOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.IDOK.Location = new System.Drawing.Point(555, 344);
            this.IDOK.Name = "IDOK";
            this.IDOK.Size = new System.Drawing.Size(75, 23);
            this.IDOK.TabIndex = 0;
            this.IDOK.Text = "OK";
            this.IDOK.UseVisualStyleBackColor = true;
            // 
            // IDC_DELETE
            // 
            this.IDC_DELETE.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.IDC_DELETE.Location = new System.Drawing.Point(141, 344);
            this.IDC_DELETE.Name = "IDC_DELETE";
            this.IDC_DELETE.Size = new System.Drawing.Size(75, 23);
            this.IDC_DELETE.TabIndex = 1;
            this.IDC_DELETE.Text = "Delete";
            this.IDC_DELETE.UseVisualStyleBackColor = true;
            this.IDC_DELETE.Click += new System.EventHandler(this.IDC_DELETE_Click);
            // 
            // IDC_ADD_TO_DEVICE
            // 
            this.IDC_ADD_TO_DEVICE.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.IDC_ADD_TO_DEVICE.Location = new System.Drawing.Point(13, 344);
            this.IDC_ADD_TO_DEVICE.Name = "IDC_ADD_TO_DEVICE";
            this.IDC_ADD_TO_DEVICE.Size = new System.Drawing.Size(110, 23);
            this.IDC_ADD_TO_DEVICE.TabIndex = 2;
            this.IDC_ADD_TO_DEVICE.Text = "Add to Device";
            this.IDC_ADD_TO_DEVICE.UseVisualStyleBackColor = true;
            this.IDC_ADD_TO_DEVICE.Click += new System.EventHandler(this.IDC_ADD_TO_DEVICE_Click);
            // 
            // IDC_CERT_LIST
            // 
            this.IDC_CERT_LIST.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IDC_CERT_LIST.FullRowSelect = true;
            this.IDC_CERT_LIST.GridLines = true;
            this.IDC_CERT_LIST.HideSelection = false;
            this.IDC_CERT_LIST.Location = new System.Drawing.Point(12, 40);
            this.IDC_CERT_LIST.MultiSelect = false;
            this.IDC_CERT_LIST.Name = "IDC_CERT_LIST";
            this.IDC_CERT_LIST.Size = new System.Drawing.Size(622, 293);
            this.IDC_CERT_LIST.TabIndex = 3;
            this.IDC_CERT_LIST.UseCompatibleStateImageBehavior = false;
            this.IDC_CERT_LIST.View = System.Windows.Forms.View.Details;
            this.IDC_CERT_LIST.SelectedIndexChanged += new System.EventHandler(this.IDC_CERT_LIST_SelectedIndexChanged);
            // 
            // IDC_CERTTAB
            // 
            this.IDC_CERTTAB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IDC_CERTTAB.Controls.Add(this.tabPage1);
            this.IDC_CERTTAB.Controls.Add(this.tabPage2);
            this.IDC_CERTTAB.Controls.Add(this.tabPage3);
            this.IDC_CERTTAB.Controls.Add(this.tabPage4);
            this.IDC_CERTTAB.Controls.Add(this.tabPage5);
            this.IDC_CERTTAB.Location = new System.Drawing.Point(13, 13);
            this.IDC_CERTTAB.Name = "IDC_CERTTAB";
            this.IDC_CERTTAB.SelectedIndex = 0;
            this.IDC_CERTTAB.Size = new System.Drawing.Size(621, 25);
            this.IDC_CERTTAB.TabIndex = 4;
            this.IDC_CERTTAB.SelectedIndexChanged += new System.EventHandler(this.IDC_CERTTAB_SelectedIndexChanged);
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 24);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(613, 0);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Device";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 24);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(613, 0);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Store CA";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 24);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(613, 0);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Store MY";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 24);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Size = new System.Drawing.Size(613, 0);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Store ROOT";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            this.tabPage5.Location = new System.Drawing.Point(4, 24);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Size = new System.Drawing.Size(613, 0);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Store SPC";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // IDD_CERTIFICATES
            // 
            this.AcceptButton = this.IDOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(646, 379);
            this.Controls.Add(this.IDC_CERTTAB);
            this.Controls.Add(this.IDC_CERT_LIST);
            this.Controls.Add(this.IDC_ADD_TO_DEVICE);
            this.Controls.Add(this.IDC_DELETE);
            this.Controls.Add(this.IDOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "IDD_CERTIFICATES";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Certificates";
            this.IDC_CERTTAB.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button IDOK;
        private System.Windows.Forms.Button IDC_DELETE;
        private System.Windows.Forms.Button IDC_ADD_TO_DEVICE;
        private System.Windows.Forms.ListView IDC_CERT_LIST;
        private System.Windows.Forms.TabControl IDC_CERTTAB;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.TabPage tabPage5;
    }
}