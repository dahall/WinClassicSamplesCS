
namespace tsfapp
{
	partial class CTSFMainWnd
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CTSFMainWnd));
			this.IDR_MAIN_MENU = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_LOAD = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_SAVE = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_EXIT = new System.Windows.Forms.ToolStripMenuItem();
			this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_GETPRESERVEDKEY = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_GETDISPATTR = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_GET_TEXTOWNER = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_GET_READING = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_GET_COMPOSING = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_TERMINATE_COMPOSITION = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_RECONVERT = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_PLAYBACK = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_TEST = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_ABOUT = new System.Windows.Forms.ToolStripMenuItem();
			this.IDC_STATUSBAR = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.toolStripStatusLabel2 = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_pTSFEditWnd = new CTSFEditWnd();
			this.IDR_MAIN_MENU.SuspendLayout();
			this.IDC_STATUSBAR.SuspendLayout();
			this.SuspendLayout();
			// 
			// IDR_MAIN_MENU
			// 
			this.IDR_MAIN_MENU.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.testToolStripMenuItem,
            this.helpToolStripMenuItem});
			this.IDR_MAIN_MENU.Location = new System.Drawing.Point(0, 0);
			this.IDR_MAIN_MENU.Name = "IDR_MAIN_MENU";
			this.IDR_MAIN_MENU.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
			this.IDR_MAIN_MENU.Size = new System.Drawing.Size(686, 24);
			this.IDR_MAIN_MENU.TabIndex = 0;
			this.IDR_MAIN_MENU.Text = "menuStrip1";
			this.IDR_MAIN_MENU.MenuActivate += new System.EventHandler(this.IDR_MAIN_MENU_MenuActivate);
			// 
			// fileToolStripMenuItem
			// 
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_LOAD,
            this.IDM_SAVE,
            this.toolStripSeparator,
            this.IDM_EXIT});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this.fileToolStripMenuItem.Text = "&File";
			// 
			// IDM_LOAD
			// 
			this.IDM_LOAD.Image = ((System.Drawing.Image)(resources.GetObject("IDM_LOAD.Image")));
			this.IDM_LOAD.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.IDM_LOAD.Name = "IDM_LOAD";
			this.IDM_LOAD.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.IDM_LOAD.Size = new System.Drawing.Size(180, 22);
			this.IDM_LOAD.Text = "&Open";
			// 
			// IDM_SAVE
			// 
			this.IDM_SAVE.Image = ((System.Drawing.Image)(resources.GetObject("IDM_SAVE.Image")));
			this.IDM_SAVE.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.IDM_SAVE.Name = "IDM_SAVE";
			this.IDM_SAVE.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
			this.IDM_SAVE.Size = new System.Drawing.Size(180, 22);
			this.IDM_SAVE.Text = "&Save";
			// 
			// toolStripSeparator
			// 
			this.toolStripSeparator.Name = "toolStripSeparator";
			this.toolStripSeparator.Size = new System.Drawing.Size(177, 6);
			// 
			// IDM_EXIT
			// 
			this.IDM_EXIT.Name = "IDM_EXIT";
			this.IDM_EXIT.Size = new System.Drawing.Size(180, 22);
			this.IDM_EXIT.Text = "E&xit";
			// 
			// testToolStripMenuItem
			// 
			this.testToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_GETPRESERVEDKEY,
            this.IDM_GETDISPATTR,
            this.IDM_GET_TEXTOWNER,
            this.IDM_GET_READING,
            this.IDM_GET_COMPOSING,
            this.IDM_TERMINATE_COMPOSITION,
            this.IDM_RECONVERT,
            this.IDM_PLAYBACK,
            this.toolStripSeparator1,
            this.IDM_TEST});
			this.testToolStripMenuItem.Name = "testToolStripMenuItem";
			this.testToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
			this.testToolStripMenuItem.Text = "&Tools";
			// 
			// IDM_GETPRESERVEDKEY
			// 
			this.IDM_GETPRESERVEDKEY.Name = "IDM_GETPRESERVEDKEY";
			this.IDM_GETPRESERVEDKEY.Size = new System.Drawing.Size(220, 22);
			this.IDM_GETPRESERVEDKEY.Text = "&Get Preserved Key";
			// 
			// IDM_GETDISPATTR
			// 
			this.IDM_GETDISPATTR.Name = "IDM_GETDISPATTR";
			this.IDM_GETDISPATTR.Size = new System.Drawing.Size(220, 22);
			this.IDM_GETDISPATTR.Text = "Get &Display Attributes";
			// 
			// IDM_GET_TEXTOWNER
			// 
			this.IDM_GET_TEXTOWNER.Name = "IDM_GET_TEXTOWNER";
			this.IDM_GET_TEXTOWNER.Size = new System.Drawing.Size(220, 22);
			this.IDM_GET_TEXTOWNER.Text = "Get Text &Owner";
			// 
			// IDM_GET_READING
			// 
			this.IDM_GET_READING.Name = "IDM_GET_READING";
			this.IDM_GET_READING.Size = new System.Drawing.Size(220, 22);
			this.IDM_GET_READING.Text = "Get Rea&ding Text";
			// 
			// IDM_GET_COMPOSING
			// 
			this.IDM_GET_COMPOSING.Name = "IDM_GET_COMPOSING";
			this.IDM_GET_COMPOSING.Size = new System.Drawing.Size(220, 22);
			this.IDM_GET_COMPOSING.Text = "Get &Composing";
			// 
			// IDM_TERMINATE_COMPOSITION
			// 
			this.IDM_TERMINATE_COMPOSITION.Enabled = false;
			this.IDM_TERMINATE_COMPOSITION.Name = "IDM_TERMINATE_COMPOSITION";
			this.IDM_TERMINATE_COMPOSITION.Size = new System.Drawing.Size(220, 22);
			this.IDM_TERMINATE_COMPOSITION.Text = "&Terminate All Compositions";
			// 
			// IDM_RECONVERT
			// 
			this.IDM_RECONVERT.Name = "IDM_RECONVERT";
			this.IDM_RECONVERT.Size = new System.Drawing.Size(220, 22);
			this.IDM_RECONVERT.Text = "&Reconvert (Correct) Text";
			// 
			// IDM_PLAYBACK
			// 
			this.IDM_PLAYBACK.Name = "IDM_PLAYBACK";
			this.IDM_PLAYBACK.Size = new System.Drawing.Size(220, 22);
			this.IDM_PLAYBACK.Text = "&Playback Text";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(217, 6);
			// 
			// IDM_TEST
			// 
			this.IDM_TEST.Name = "IDM_TEST";
			this.IDM_TEST.Size = new System.Drawing.Size(220, 22);
			this.IDM_TEST.Text = "&Test";
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_ABOUT});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this.helpToolStripMenuItem.Text = "&Help";
			// 
			// IDM_ABOUT
			// 
			this.IDM_ABOUT.Name = "IDM_ABOUT";
			this.IDM_ABOUT.Size = new System.Drawing.Size(180, 22);
			this.IDM_ABOUT.Text = "&About...";
			// 
			// IDC_STATUSBAR
			// 
			this.IDC_STATUSBAR.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripStatusLabel2});
			this.IDC_STATUSBAR.Location = new System.Drawing.Point(0, 368);
			this.IDC_STATUSBAR.Name = "IDC_STATUSBAR";
			this.IDC_STATUSBAR.Padding = new System.Windows.Forms.Padding(1, 0, 12, 0);
			this.IDC_STATUSBAR.Size = new System.Drawing.Size(686, 22);
			this.IDC_STATUSBAR.TabIndex = 1;
			this.IDC_STATUSBAR.Text = "statusStrip1";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
			// 
			// toolStripStatusLabel2
			// 
			this.toolStripStatusLabel2.Name = "toolStripStatusLabel2";
			this.toolStripStatusLabel2.Size = new System.Drawing.Size(0, 17);
			// 
			// m_pTSFEditWnd
			// 
			this.m_pTSFEditWnd.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_pTSFEditWnd.Location = new System.Drawing.Point(0, 24);
			this.m_pTSFEditWnd.Multiline = true;
			this.m_pTSFEditWnd.Name = "textBox1";
			this.m_pTSFEditWnd.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.m_pTSFEditWnd.Size = new System.Drawing.Size(686, 344);
			this.m_pTSFEditWnd.TabIndex = 3;
			// 
			// CTSFMainWnd
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(686, 390);
			this.Controls.Add(this.m_pTSFEditWnd);
			this.Controls.Add(this.IDC_STATUSBAR);
			this.Controls.Add(this.IDR_MAIN_MENU);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.IDR_MAIN_MENU;
			this.Name = "CTSFMainWnd";
			this.Text = "TSF Test Application";
			this.IDR_MAIN_MENU.ResumeLayout(false);
			this.IDR_MAIN_MENU.PerformLayout();
			this.IDC_STATUSBAR.ResumeLayout(false);
			this.IDC_STATUSBAR.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip IDR_MAIN_MENU;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		internal System.Windows.Forms.ToolStripMenuItem IDM_LOAD;
		private System.Windows.Forms.ToolStripMenuItem IDM_SAVE;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator;
		private System.Windows.Forms.ToolStripMenuItem IDM_EXIT;
		private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
		internal System.Windows.Forms.ToolStripMenuItem IDM_GETPRESERVEDKEY;
		internal System.Windows.Forms.ToolStripMenuItem IDM_GETDISPATTR;
		internal System.Windows.Forms.ToolStripMenuItem IDM_GET_TEXTOWNER;
		internal System.Windows.Forms.ToolStripMenuItem IDM_GET_READING;
		internal System.Windows.Forms.ToolStripMenuItem IDM_GET_COMPOSING;
		internal System.Windows.Forms.ToolStripMenuItem IDM_TERMINATE_COMPOSITION;
		internal System.Windows.Forms.ToolStripMenuItem IDM_RECONVERT;
		internal System.Windows.Forms.ToolStripMenuItem IDM_PLAYBACK;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem IDM_TEST;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem IDM_ABOUT;
		internal System.Windows.Forms.StatusStrip IDC_STATUSBAR;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel2;
		private CTSFEditWnd m_pTSFEditWnd;
	}
}

