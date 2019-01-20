namespace AutomaticJumpList
{
	partial class AutomaticJumpListSample
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutomaticJumpListSample));
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_FILE_OPEN = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.knownCategoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_FILE_CLEARHISTORY = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_FILE_DEREGISTERFILETYPES = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_EXIT = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_CATEGORY_RECENT = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_CATEGORY_FREQUENT = new System.Windows.Forms.ToolStripMenuItem();
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
			this.IDM_FILE_OPEN,
			this.toolStripSeparator1,
			this.knownCategoryToolStripMenuItem,
			this.IDM_FILE_CLEARHISTORY,
			this.toolStripMenuItem1,
			this.IDM_FILE_DEREGISTERFILETYPES,
			this.toolStripMenuItem2,
			this.IDM_EXIT});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// IDM_FILE_OPEN
			// 
			this.IDM_FILE_OPEN.Name = "IDM_FILE_OPEN";
			this.IDM_FILE_OPEN.Size = new System.Drawing.Size(200, 22);
			this.IDM_FILE_OPEN.Text = "&Open...";
			this.IDM_FILE_OPEN.Click += new System.EventHandler(this.IDM_FILE_OPEN_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(197, 6);
			// 
			// knownCategoryToolStripMenuItem
			// 
			this.knownCategoryToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.IDM_CATEGORY_RECENT,
			this.IDM_CATEGORY_FREQUENT});
			this.knownCategoryToolStripMenuItem.Name = "knownCategoryToolStripMenuItem";
			this.knownCategoryToolStripMenuItem.Size = new System.Drawing.Size(200, 22);
			this.knownCategoryToolStripMenuItem.Text = "&Known Category";
			// 
			// IDM_FILE_CLEARHISTORY
			// 
			this.IDM_FILE_CLEARHISTORY.Name = "IDM_FILE_CLEARHISTORY";
			this.IDM_FILE_CLEARHISTORY.Size = new System.Drawing.Size(200, 22);
			this.IDM_FILE_CLEARHISTORY.Text = "&Clear History";
			this.IDM_FILE_CLEARHISTORY.Click += new System.EventHandler(this.IDM_FILE_CLEARHISTORY_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(197, 6);
			// 
			// IDM_FILE_DEREGISTERFILETYPES
			// 
			this.IDM_FILE_DEREGISTERFILETYPES.Name = "IDM_FILE_DEREGISTERFILETYPES";
			this.IDM_FILE_DEREGISTERFILETYPES.Size = new System.Drawing.Size(200, 22);
			this.IDM_FILE_DEREGISTERFILETYPES.Text = "Clean&up Files and Types";
			this.IDM_FILE_DEREGISTERFILETYPES.Click += new System.EventHandler(this.IDM_FILE_DEREGISTERFILETYPES_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(197, 6);
			// 
			// IDM_EXIT
			// 
			this.IDM_EXIT.Name = "IDM_EXIT";
			this.IDM_EXIT.Size = new System.Drawing.Size(200, 22);
			this.IDM_EXIT.Text = "E&xit";
			this.IDM_EXIT.Click += new System.EventHandler(this.IDM_EXIT_Click);
			// 
			// IDM_CATEGORY_RECENT
			// 
			this.IDM_CATEGORY_RECENT.Name = "IDM_CATEGORY_RECENT";
			this.IDM_CATEGORY_RECENT.Size = new System.Drawing.Size(180, 22);
			this.IDM_CATEGORY_RECENT.Text = "&Recent";
			this.IDM_CATEGORY_RECENT.Click += new System.EventHandler(this.IDM_CATEGORY_RECENT_Click);
			// 
			// IDM_CATEGORY_FREQUENT
			// 
			this.IDM_CATEGORY_FREQUENT.Name = "IDM_CATEGORY_FREQUENT";
			this.IDM_CATEGORY_FREQUENT.Size = new System.Drawing.Size(180, 22);
			this.IDM_CATEGORY_FREQUENT.Text = "&Frequent";
			this.IDM_CATEGORY_FREQUENT.Click += new System.EventHandler(this.IDM_CATEGORY_FREQUENT_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Window;
			this.ClientSize = new System.Drawing.Size(410, 252);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "Form1";
			this.Text = "AutomaticJumpListSample";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_OPEN;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem knownCategoryToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem IDM_CATEGORY_RECENT;
		private System.Windows.Forms.ToolStripMenuItem IDM_CATEGORY_FREQUENT;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_CLEARHISTORY;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem IDM_FILE_DEREGISTERFILETYPES;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem IDM_EXIT;
	}
}

