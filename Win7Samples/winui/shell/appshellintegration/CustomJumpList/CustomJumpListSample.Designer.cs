namespace CustomJumpList
{
	partial class CustomJumpListSample
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomJumpListSample));
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_FILE_CREATECUSTOMJUMPLIST = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_FILE_DEREGISTERFILETYPES = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_EXIT = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_FILE_DELETECUSTOMJUMPLIST = new System.Windows.Forms.ToolStripMenuItem();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(410, 24);
			this.menuStrip1.TabIndex = 0;
			this.menuStrip1.Text = "menuStrip1";
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_FILE_CREATECUSTOMJUMPLIST,
            this.IDM_FILE_DELETECUSTOMJUMPLIST,
            this.toolStripSeparator1,
            this.IDM_FILE_DEREGISTERFILETYPES,
            this.toolStripMenuItem2,
            this.IDM_EXIT});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// IDM_FILE_CREATECUSTOMJUMPLIST
			// 
			this.IDM_FILE_CREATECUSTOMJUMPLIST.Name = "IDM_FILE_CREATECUSTOMJUMPLIST";
			this.IDM_FILE_CREATECUSTOMJUMPLIST.Size = new System.Drawing.Size(206, 22);
			this.IDM_FILE_CREATECUSTOMJUMPLIST.Text = "&Create Custom Jump List";
			this.IDM_FILE_CREATECUSTOMJUMPLIST.Click += new System.EventHandler(this.IDM_FILE_CREATECUSTOMJUMPLIST_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(203, 6);
			// 
			// IDM_FILE_DEREGISTERFILETYPES
			// 
			this.IDM_FILE_DEREGISTERFILETYPES.Name = "IDM_FILE_DEREGISTERFILETYPES";
			this.IDM_FILE_DEREGISTERFILETYPES.Size = new System.Drawing.Size(206, 22);
			this.IDM_FILE_DEREGISTERFILETYPES.Text = "Clean&up Files and Types";
			this.IDM_FILE_DEREGISTERFILETYPES.Click += new System.EventHandler(this.IDM_FILE_DEREGISTERFILETYPES_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(203, 6);
			// 
			// IDM_EXIT
			// 
			this.IDM_EXIT.Name = "IDM_EXIT";
			this.IDM_EXIT.Size = new System.Drawing.Size(206, 22);
			this.IDM_EXIT.Text = "E&xit";
			this.IDM_EXIT.Click += new System.EventHandler(this.IDM_EXIT_Click);
			// 
			// IDM_FILE_DELETECUSTOMJUMPLIST
			// 
			this.IDM_FILE_DELETECUSTOMJUMPLIST.Name = "IDM_FILE_DELETECUSTOMJUMPLIST";
			this.IDM_FILE_DELETECUSTOMJUMPLIST.Size = new System.Drawing.Size(206, 22);
			this.IDM_FILE_DELETECUSTOMJUMPLIST.Text = "&Delete Custom Jump List";
			this.IDM_FILE_DELETECUSTOMJUMPLIST.Click += new System.EventHandler(this.IDM_FILE_DELETECUSTOMJUMPLIST_Click);
			// 
			// CustomJumpListSample
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(410, 252);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "CustomJumpListSample";
			this.Text = "CustomJumpListSample";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_CREATECUSTOMJUMPLIST;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_DEREGISTERFILETYPES;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem IDM_EXIT;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_DELETECUSTOMJUMPLIST;
	}
}

