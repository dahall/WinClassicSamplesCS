namespace ExplorerBrowserCustomContents
{
	partial class CExplorerBrowserHostDialog
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
			this.IDC_BROWSER = new System.Windows.Forms.Panel();
			this.IDC_STATIC = new System.Windows.Forms.GroupBox();
			this.IDC_LBLPATH = new System.Windows.Forms.Label();
			this.IDC_FOLDERPATH = new System.Windows.Forms.Label();
			this.IDC_FOLDERNAME = new System.Windows.Forms.Label();
			this.IDC_LBLFOLDER = new System.Windows.Forms.Label();
			this.IDC_EXPLORE = new System.Windows.Forms.Button();
			this.IDC_REFRESH = new System.Windows.Forms.Button();
			this.IDC_CANCEL = new System.Windows.Forms.Button();
			this.IDC_STATUS = new System.Windows.Forms.Label();
			this.IDC_STATIC.SuspendLayout();
			this.SuspendLayout();
			// 
			// IDC_BROWSER
			// 
			this.IDC_BROWSER.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_BROWSER.Location = new System.Drawing.Point(7, 6);
			this.IDC_BROWSER.Name = "IDC_BROWSER";
			this.IDC_BROWSER.Size = new System.Drawing.Size(407, 361);
			this.IDC_BROWSER.TabIndex = 0;
			this.IDC_BROWSER.Visible = false;
			this.IDC_BROWSER.Resize += new System.EventHandler(this.IDC_BROWSER_Resize);
			// 
			// IDC_STATIC
			// 
			this.IDC_STATIC.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_STATIC.Controls.Add(this.IDC_LBLPATH);
			this.IDC_STATIC.Controls.Add(this.IDC_FOLDERPATH);
			this.IDC_STATIC.Controls.Add(this.IDC_FOLDERNAME);
			this.IDC_STATIC.Controls.Add(this.IDC_LBLFOLDER);
			this.IDC_STATIC.Location = new System.Drawing.Point(7, 372);
			this.IDC_STATIC.Name = "IDC_STATIC";
			this.IDC_STATIC.Size = new System.Drawing.Size(407, 67);
			this.IDC_STATIC.TabIndex = 2;
			this.IDC_STATIC.TabStop = false;
			this.IDC_STATIC.Text = "Folder Information";
			// 
			// IDC_LBLPATH
			// 
			this.IDC_LBLPATH.AutoSize = true;
			this.IDC_LBLPATH.Location = new System.Drawing.Point(6, 34);
			this.IDC_LBLPATH.Name = "IDC_LBLPATH";
			this.IDC_LBLPATH.Size = new System.Drawing.Size(32, 13);
			this.IDC_LBLPATH.TabIndex = 3;
			this.IDC_LBLPATH.Text = "Path:";
			this.IDC_LBLPATH.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// IDC_FOLDERPATH
			// 
			this.IDC_FOLDERPATH.AutoEllipsis = true;
			this.IDC_FOLDERPATH.AutoSize = true;
			this.IDC_FOLDERPATH.Location = new System.Drawing.Point(51, 34);
			this.IDC_FOLDERPATH.Name = "IDC_FOLDERPATH";
			this.IDC_FOLDERPATH.Size = new System.Drawing.Size(10, 13);
			this.IDC_FOLDERPATH.TabIndex = 1;
			this.IDC_FOLDERPATH.Text = ":";
			// 
			// IDC_FOLDERNAME
			// 
			this.IDC_FOLDERNAME.AutoSize = true;
			this.IDC_FOLDERNAME.Location = new System.Drawing.Point(51, 16);
			this.IDC_FOLDERNAME.Name = "IDC_FOLDERNAME";
			this.IDC_FOLDERNAME.Size = new System.Drawing.Size(10, 13);
			this.IDC_FOLDERNAME.TabIndex = 1;
			this.IDC_FOLDERNAME.Text = ":";
			// 
			// IDC_LBLFOLDER
			// 
			this.IDC_LBLFOLDER.AutoSize = true;
			this.IDC_LBLFOLDER.Location = new System.Drawing.Point(6, 16);
			this.IDC_LBLFOLDER.Name = "IDC_LBLFOLDER";
			this.IDC_LBLFOLDER.Size = new System.Drawing.Size(39, 13);
			this.IDC_LBLFOLDER.TabIndex = 3;
			this.IDC_LBLFOLDER.Text = "Folder:";
			this.IDC_LBLFOLDER.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// IDC_EXPLORE
			// 
			this.IDC_EXPLORE.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.IDC_EXPLORE.Location = new System.Drawing.Point(7, 445);
			this.IDC_EXPLORE.Name = "IDC_EXPLORE";
			this.IDC_EXPLORE.Size = new System.Drawing.Size(75, 23);
			this.IDC_EXPLORE.TabIndex = 4;
			this.IDC_EXPLORE.Text = "&Open";
			this.IDC_EXPLORE.UseVisualStyleBackColor = true;
			this.IDC_EXPLORE.Click += new System.EventHandler(this._OnExplore);
			// 
			// IDC_REFRESH
			// 
			this.IDC_REFRESH.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.IDC_REFRESH.Location = new System.Drawing.Point(88, 445);
			this.IDC_REFRESH.Name = "IDC_REFRESH";
			this.IDC_REFRESH.Size = new System.Drawing.Size(75, 23);
			this.IDC_REFRESH.TabIndex = 4;
			this.IDC_REFRESH.Text = "&Refresh";
			this.IDC_REFRESH.UseVisualStyleBackColor = true;
			this.IDC_REFRESH.Click += new System.EventHandler(this._OnRefresh);
			// 
			// IDC_CANCEL
			// 
			this.IDC_CANCEL.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_CANCEL.Location = new System.Drawing.Point(339, 445);
			this.IDC_CANCEL.Name = "IDC_CANCEL";
			this.IDC_CANCEL.Size = new System.Drawing.Size(75, 23);
			this.IDC_CANCEL.TabIndex = 4;
			this.IDC_CANCEL.Text = "Close";
			this.IDC_CANCEL.UseVisualStyleBackColor = true;
			this.IDC_CANCEL.Click += new System.EventHandler(this.IDC_CANCEL_Click);
			// 
			// IDC_STATUS
			// 
			this.IDC_STATUS.AutoSize = true;
			this.IDC_STATUS.Location = new System.Drawing.Point(4, 372);
			this.IDC_STATUS.Name = "IDC_STATUS";
			this.IDC_STATUS.Size = new System.Drawing.Size(84, 13);
			this.IDC_STATUS.TabIndex = 1;
			this.IDC_STATUS.Text = "Finding folders...";
			// 
			// CExplorerBrowserHostDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(422, 475);
			this.Controls.Add(this.IDC_STATUS);
			this.Controls.Add(this.IDC_CANCEL);
			this.Controls.Add(this.IDC_REFRESH);
			this.Controls.Add(this.IDC_EXPLORE);
			this.Controls.Add(this.IDC_STATIC);
			this.Controls.Add(this.IDC_BROWSER);
			this.Name = "CExplorerBrowserHostDialog";
			this.Text = "Known Folder Browser";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this._OnDestroyDlg);
			this.Load += new System.EventHandler(this._OnInitDlg);
			this.IDC_STATIC.ResumeLayout(false);
			this.IDC_STATIC.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel IDC_BROWSER;
		private System.Windows.Forms.GroupBox IDC_STATIC;
		private System.Windows.Forms.Label IDC_LBLPATH;
		private System.Windows.Forms.Label IDC_FOLDERPATH;
		private System.Windows.Forms.Label IDC_FOLDERNAME;
		private System.Windows.Forms.Label IDC_LBLFOLDER;
		private System.Windows.Forms.Button IDC_EXPLORE;
		private System.Windows.Forms.Button IDC_REFRESH;
		private System.Windows.Forms.Button IDC_CANCEL;
		private System.Windows.Forms.Label IDC_STATUS;
	}
}

