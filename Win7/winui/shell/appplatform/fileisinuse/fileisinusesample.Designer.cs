namespace fileisinuse
{
	partial class CFileInUseApp
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
			this.IDC_INFO = new System.Windows.Forms.Label();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.IDM_FILE = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_FILE_OPENFILE = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_FILE_CLOSEFILE = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_FILE_EXIT = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// IDC_INFO
			// 
			this.IDC_INFO.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_INFO.Location = new System.Drawing.Point(13, 35);
			this.IDC_INFO.Name = "IDC_INFO";
			this.IDC_INFO.Size = new System.Drawing.Size(454, 100);
			this.IDC_INFO.TabIndex = 0;
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_FILE});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(479, 24);
			this.menuStrip1.TabIndex = 1;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// IDM_FILE
			// 
			this.IDM_FILE.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_FILE_OPENFILE,
            this.IDM_FILE_CLOSEFILE,
            this.IDM_FILE_EXIT});
			this.IDM_FILE.Name = "IDM_FILE";
			this.IDM_FILE.Size = new System.Drawing.Size(37, 20);
			this.IDM_FILE.Text = "File";
			this.IDM_FILE.DropDownOpening += new System.EventHandler(this._InitMenuPopup);
			// 
			// IDM_FILE_OPENFILE
			// 
			this.IDM_FILE_OPENFILE.Name = "IDM_FILE_OPENFILE";
			this.IDM_FILE_OPENFILE.Size = new System.Drawing.Size(133, 22);
			this.IDM_FILE_OPENFILE.Text = "&Open File...";
			this.IDM_FILE_OPENFILE.Click += new System.EventHandler(this._OnCommand);
			// 
			// IDM_FILE_CLOSEFILE
			// 
			this.IDM_FILE_CLOSEFILE.Enabled = false;
			this.IDM_FILE_CLOSEFILE.Name = "IDM_FILE_CLOSEFILE";
			this.IDM_FILE_CLOSEFILE.Size = new System.Drawing.Size(133, 22);
			this.IDM_FILE_CLOSEFILE.Text = "&Close File...";
			this.IDM_FILE_CLOSEFILE.Click += new System.EventHandler(this._OnCommand);
			// 
			// IDM_FILE_EXIT
			// 
			this.IDM_FILE_EXIT.Name = "IDM_FILE_EXIT";
			this.IDM_FILE_EXIT.Size = new System.Drawing.Size(133, 22);
			this.IDM_FILE_EXIT.Text = "E&xit";
			this.IDM_FILE_EXIT.Click += new System.EventHandler(this._OnCommand);
			// 
			// CFileInUseApp
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(479, 144);
			this.Controls.Add(this.IDC_INFO);
			this.Controls.Add(this.menuStrip1);
			this.Font = new System.Drawing.Font("Segoe UI", 9F);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "CFileInUseApp";
			this.Text = "IFileIsInUse Sample";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this._OnDestroy);
			this.Load += new System.EventHandler(this._OnInitDlg);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this._Drop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this._DragEnter);
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label IDC_INFO;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_OPENFILE;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_CLOSEFILE;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_EXIT;
	}
}

