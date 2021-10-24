
namespace EhStorEnumerator
{
    partial class EhStorEnumerator2
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EhStorEnumerator2));
            this.IDC_DEVLIST = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.IDC_REFRESH = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.IDR_POPUPMENU_CERT = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.IDR_POPUPMENU_PASSWORD = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.certificateSILOToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.ID_CERTIFICATE_QUERYINFORMATION = new System.Windows.Forms.ToolStripMenuItem();
            this.ID_CERTIFICATE_HOSTAUTHENTICATION = new System.Windows.Forms.ToolStripMenuItem();
            this.ID_CERTIFICATE_DEVICEAUTHENTICATION = new System.Windows.Forms.ToolStripMenuItem();
            this.ID_CERTIFICATE_UNAUTHENTICATION = new System.Windows.Forms.ToolStripMenuItem();
            this.ID_CERTIFICATE_CERTIFICATES = new System.Windows.Forms.ToolStripMenuItem();
            this.ID_CERTIFICATE_INITTOMANUFACTURERSTATE = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ID_PASSWORD_QUERYINFORMATION = new System.Windows.Forms.ToolStripMenuItem();
            this.ID_PASSWORD_SET = new System.Windows.Forms.ToolStripMenuItem();
            this.ID_PASSWORD_INITTOMANUFACTURERSTATE = new System.Windows.Forms.ToolStripMenuItem();
            this.IDR_POPUPMENU_CERT.SuspendLayout();
            this.IDR_POPUPMENU_PASSWORD.SuspendLayout();
            this.SuspendLayout();
            // 
            // IDC_DEVLIST
            // 
            this.IDC_DEVLIST.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IDC_DEVLIST.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3});
            this.IDC_DEVLIST.FullRowSelect = true;
            this.IDC_DEVLIST.GridLines = true;
            this.IDC_DEVLIST.HideSelection = false;
            this.IDC_DEVLIST.Location = new System.Drawing.Point(13, 13);
            this.IDC_DEVLIST.MultiSelect = false;
            this.IDC_DEVLIST.Name = "IDC_DEVLIST";
            this.IDC_DEVLIST.Size = new System.Drawing.Size(571, 269);
            this.IDC_DEVLIST.TabIndex = 0;
            this.IDC_DEVLIST.UseCompatibleStateImageBehavior = false;
            this.IDC_DEVLIST.View = System.Windows.Forms.View.Details;
            this.IDC_DEVLIST.MouseClick += new System.Windows.Forms.MouseEventHandler(this.IDC_DEVLIST_MouseClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Description";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Manufacturer";
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "PNP ID";
            // 
            // IDC_REFRESH
            // 
            this.IDC_REFRESH.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.IDC_REFRESH.Location = new System.Drawing.Point(509, 294);
            this.IDC_REFRESH.Name = "IDC_REFRESH";
            this.IDC_REFRESH.Size = new System.Drawing.Size(75, 23);
            this.IDC_REFRESH.TabIndex = 1;
            this.IDC_REFRESH.Text = "Refresh";
            this.IDC_REFRESH.UseVisualStyleBackColor = true;
            this.IDC_REFRESH.Click += new System.EventHandler(this.IDC_REFRESH_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 298);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(276, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Right click on the portable device for actions menu";
            // 
            // IDR_POPUPMENU_CERT
            // 
            this.IDR_POPUPMENU_CERT.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.certificateSILOToolStripMenuItem,
            this.toolStripMenuItem2,
            this.ID_CERTIFICATE_QUERYINFORMATION,
            this.ID_CERTIFICATE_HOSTAUTHENTICATION,
            this.ID_CERTIFICATE_DEVICEAUTHENTICATION,
            this.ID_CERTIFICATE_UNAUTHENTICATION,
            this.ID_CERTIFICATE_CERTIFICATES,
            this.ID_CERTIFICATE_INITTOMANUFACTURERSTATE});
            this.IDR_POPUPMENU_CERT.Name = "IDR_POPUPMENU_CERT";
            this.IDR_POPUPMENU_CERT.Size = new System.Drawing.Size(210, 186);
            // 
            // IDR_POPUPMENU_PASSWORD
            // 
            this.IDR_POPUPMENU_PASSWORD.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripSeparator1,
            this.ID_PASSWORD_QUERYINFORMATION,
            this.ID_PASSWORD_SET,
            this.ID_PASSWORD_INITTOMANUFACTURERSTATE});
            this.IDR_POPUPMENU_PASSWORD.Name = "IDR_POPUPMENU";
            this.IDR_POPUPMENU_PASSWORD.Size = new System.Drawing.Size(210, 98);
            // 
            // certificateSILOToolStripMenuItem
            // 
            this.certificateSILOToolStripMenuItem.Enabled = false;
            this.certificateSILOToolStripMenuItem.Name = "certificateSILOToolStripMenuItem";
            this.certificateSILOToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.certificateSILOToolStripMenuItem.Text = "Certificate SILO";
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(206, 6);
            // 
            // ID_CERTIFICATE_QUERYINFORMATION
            // 
            this.ID_CERTIFICATE_QUERYINFORMATION.Name = "ID_CERTIFICATE_QUERYINFORMATION";
            this.ID_CERTIFICATE_QUERYINFORMATION.Size = new System.Drawing.Size(209, 22);
            this.ID_CERTIFICATE_QUERYINFORMATION.Text = "Query Information";
            this.ID_CERTIFICATE_QUERYINFORMATION.Click += new System.EventHandler(this.OnCertificateQueryinformation);
            // 
            // ID_CERTIFICATE_HOSTAUTHENTICATION
            // 
            this.ID_CERTIFICATE_HOSTAUTHENTICATION.Name = "ID_CERTIFICATE_HOSTAUTHENTICATION";
            this.ID_CERTIFICATE_HOSTAUTHENTICATION.Size = new System.Drawing.Size(209, 22);
            this.ID_CERTIFICATE_HOSTAUTHENTICATION.Text = "Host Authentication";
            this.ID_CERTIFICATE_HOSTAUTHENTICATION.Click += new System.EventHandler(this.OnCertificateHostauthentication);
            // 
            // ID_CERTIFICATE_DEVICEAUTHENTICATION
            // 
            this.ID_CERTIFICATE_DEVICEAUTHENTICATION.Name = "ID_CERTIFICATE_DEVICEAUTHENTICATION";
            this.ID_CERTIFICATE_DEVICEAUTHENTICATION.Size = new System.Drawing.Size(209, 22);
            this.ID_CERTIFICATE_DEVICEAUTHENTICATION.Text = "Device Authentication";
            this.ID_CERTIFICATE_DEVICEAUTHENTICATION.Click += new System.EventHandler(this.OnCertificateDeviceauthentication);
            // 
            // ID_CERTIFICATE_UNAUTHENTICATION
            // 
            this.ID_CERTIFICATE_UNAUTHENTICATION.Name = "ID_CERTIFICATE_UNAUTHENTICATION";
            this.ID_CERTIFICATE_UNAUTHENTICATION.Size = new System.Drawing.Size(209, 22);
            this.ID_CERTIFICATE_UNAUTHENTICATION.Text = "UnAuthentication";
            this.ID_CERTIFICATE_UNAUTHENTICATION.Click += new System.EventHandler(this.OnCertificateUnauthentication);
            // 
            // ID_CERTIFICATE_CERTIFICATES
            // 
            this.ID_CERTIFICATE_CERTIFICATES.Name = "ID_CERTIFICATE_CERTIFICATES";
            this.ID_CERTIFICATE_CERTIFICATES.Size = new System.Drawing.Size(209, 22);
            this.ID_CERTIFICATE_CERTIFICATES.Text = "Certificates";
            this.ID_CERTIFICATE_CERTIFICATES.Click += new System.EventHandler(this.OnCertificateCertificates);
            // 
            // ID_CERTIFICATE_INITTOMANUFACTURERSTATE
            // 
            this.ID_CERTIFICATE_INITTOMANUFACTURERSTATE.Name = "ID_CERTIFICATE_INITTOMANUFACTURERSTATE";
            this.ID_CERTIFICATE_INITTOMANUFACTURERSTATE.Size = new System.Drawing.Size(209, 22);
            this.ID_CERTIFICATE_INITTOMANUFACTURERSTATE.Text = "Init to Manufacturer State";
            this.ID_CERTIFICATE_INITTOMANUFACTURERSTATE.Click += new System.EventHandler(this.OnCertificateInittomanufacturerstate);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Enabled = false;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(209, 22);
            this.toolStripMenuItem1.Text = "Password SILO";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(206, 6);
            // 
            // ID_PASSWORD_QUERYINFORMATION
            // 
            this.ID_PASSWORD_QUERYINFORMATION.Name = "ID_PASSWORD_QUERYINFORMATION";
            this.ID_PASSWORD_QUERYINFORMATION.Size = new System.Drawing.Size(209, 22);
            this.ID_PASSWORD_QUERYINFORMATION.Text = "Query Information";
            this.ID_PASSWORD_QUERYINFORMATION.Click += new System.EventHandler(this.OnPasswordQueryInformation);
            // 
            // ID_PASSWORD_SET
            // 
            this.ID_PASSWORD_SET.Name = "ID_PASSWORD_SET";
            this.ID_PASSWORD_SET.Size = new System.Drawing.Size(209, 22);
            this.ID_PASSWORD_SET.Text = "Set/Change Password";
            this.ID_PASSWORD_SET.Click += new System.EventHandler(this.OnPasswordSet);
            // 
            // ID_PASSWORD_INITTOMANUFACTURERSTATE
            // 
            this.ID_PASSWORD_INITTOMANUFACTURERSTATE.Name = "ID_PASSWORD_INITTOMANUFACTURERSTATE";
            this.ID_PASSWORD_INITTOMANUFACTURERSTATE.Size = new System.Drawing.Size(209, 22);
            this.ID_PASSWORD_INITTOMANUFACTURERSTATE.Text = "Init to Manufacturer State";
            this.ID_PASSWORD_INITTOMANUFACTURERSTATE.Click += new System.EventHandler(this.OnPasswordInittomanufacturerstate);
            // 
            // EhStorEnumerator2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(596, 329);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.IDC_REFRESH);
            this.Controls.Add(this.IDC_DEVLIST);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "EhStorEnumerator2";
            this.Text = "Enhanced Storage Sample";
            this.Load += new System.EventHandler(this.EhStorEnumerator2_Load);
            this.IDR_POPUPMENU_CERT.ResumeLayout(false);
            this.IDR_POPUPMENU_PASSWORD.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView IDC_DEVLIST;
        private System.Windows.Forms.Button IDC_REFRESH;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ContextMenuStrip IDR_POPUPMENU_CERT;
        private System.Windows.Forms.ContextMenuStrip IDR_POPUPMENU_PASSWORD;
        private System.Windows.Forms.ToolStripMenuItem certificateSILOToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem ID_CERTIFICATE_QUERYINFORMATION;
        private System.Windows.Forms.ToolStripMenuItem ID_CERTIFICATE_HOSTAUTHENTICATION;
        private System.Windows.Forms.ToolStripMenuItem ID_CERTIFICATE_DEVICEAUTHENTICATION;
        private System.Windows.Forms.ToolStripMenuItem ID_CERTIFICATE_UNAUTHENTICATION;
        private System.Windows.Forms.ToolStripMenuItem ID_CERTIFICATE_CERTIFICATES;
        private System.Windows.Forms.ToolStripMenuItem ID_CERTIFICATE_INITTOMANUFACTURERSTATE;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem ID_PASSWORD_QUERYINFORMATION;
        private System.Windows.Forms.ToolStripMenuItem ID_PASSWORD_SET;
        private System.Windows.Forms.ToolStripMenuItem ID_PASSWORD_INITTOMANUFACTURERSTATE;
    }
}

