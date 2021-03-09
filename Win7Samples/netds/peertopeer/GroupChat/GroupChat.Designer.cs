namespace GroupChat
{
	partial class GroupChat
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GroupChat));
			this.IDC_MENU = new System.Windows.Forms.MenuStrip();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_CLEARTEXT = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_EXIT = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_CREATEIDENTITY = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_SAVEIDENTITYINFO = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_DELETEIDENTITY = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_CREATEGROUP = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_JOINGROUP = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_OPENGROUP = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_CLOSEGROUP = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_DELETEGROUP = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.IDM_CREATEINVITATION = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
			this.IDM_ABOUT = new System.Windows.Forms.ToolStripMenuItem();
			this.IDC_STATUS = new System.Windows.Forms.StatusStrip();
			this.SB_PART_STATUS = new System.Windows.Forms.ToolStripStatusLabel();
			this.SB_PART_MESSAGE = new System.Windows.Forms.ToolStripStatusLabel();
			this.IDC_SEND = new System.Windows.Forms.Button();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.IDC_TEXTBOX = new System.Windows.Forms.TextBox();
			this.IDC_STATIC_MEMBERS = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.IDC_MESSAGES = new System.Windows.Forms.TextBox();
			this.IDC_MEMBERS = new System.Windows.Forms.ListBox();
			this.IDC_MENU.SuspendLayout();
			this.IDC_STATUS.SuspendLayout();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// IDC_MENU
			// 
			this.IDC_MENU.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4});
			this.IDC_MENU.Location = new System.Drawing.Point(0, 0);
			this.IDC_MENU.Name = "IDC_MENU";
			this.IDC_MENU.Padding = new System.Windows.Forms.Padding(5, 2, 0, 2);
			this.IDC_MENU.Size = new System.Drawing.Size(537, 24);
			this.IDC_MENU.TabIndex = 0;
			this.IDC_MENU.Text = "menuStrip1";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_CLEARTEXT,
            this.toolStripSeparator1,
            this.IDM_EXIT});
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(37, 20);
			this.toolStripMenuItem1.Text = "&File";
			// 
			// IDM_CLEARTEXT
			// 
			this.IDM_CLEARTEXT.Name = "IDM_CLEARTEXT";
			this.IDM_CLEARTEXT.Size = new System.Drawing.Size(125, 22);
			this.IDM_CLEARTEXT.Text = "&Clear Text";
			this.IDM_CLEARTEXT.Click += new System.EventHandler(this.IDM_CLEARTEXT_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(122, 6);
			// 
			// IDM_EXIT
			// 
			this.IDM_EXIT.Name = "IDM_EXIT";
			this.IDM_EXIT.Size = new System.Drawing.Size(125, 22);
			this.IDM_EXIT.Text = "E&xit";
			this.IDM_EXIT.Click += new System.EventHandler(this.IDM_EXIT_Click);
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_CREATEIDENTITY,
            this.IDM_SAVEIDENTITYINFO,
            this.IDM_DELETEIDENTITY});
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(59, 20);
			this.toolStripMenuItem2.Text = "&Identity";
			// 
			// IDM_CREATEIDENTITY
			// 
			this.IDM_CREATEIDENTITY.Name = "IDM_CREATEIDENTITY";
			this.IDM_CREATEIDENTITY.Size = new System.Drawing.Size(174, 22);
			this.IDM_CREATEIDENTITY.Text = "&Create...";
			this.IDM_CREATEIDENTITY.Click += new System.EventHandler(this.IDM_CREATEIDENTITY_Click);
			// 
			// IDM_SAVEIDENTITYINFO
			// 
			this.IDM_SAVEIDENTITYINFO.Name = "IDM_SAVEIDENTITYINFO";
			this.IDM_SAVEIDENTITYINFO.Size = new System.Drawing.Size(174, 22);
			this.IDM_SAVEIDENTITYINFO.Text = "&Save Identity Info...";
			this.IDM_SAVEIDENTITYINFO.Click += new System.EventHandler(this.IDM_SAVEIDENTITYINFO_Click);
			// 
			// IDM_DELETEIDENTITY
			// 
			this.IDM_DELETEIDENTITY.Name = "IDM_DELETEIDENTITY";
			this.IDM_DELETEIDENTITY.Size = new System.Drawing.Size(174, 22);
			this.IDM_DELETEIDENTITY.Text = "&Delete...";
			this.IDM_DELETEIDENTITY.Click += new System.EventHandler(this.IDM_DELETEIDENTITY_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_CREATEGROUP,
            this.IDM_JOINGROUP,
            this.IDM_OPENGROUP,
            this.IDM_CLOSEGROUP,
            this.IDM_DELETEGROUP,
            this.toolStripSeparator2,
            this.IDM_CREATEINVITATION});
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(52, 20);
			this.toolStripMenuItem3.Text = "&Group";
			// 
			// IDM_CREATEGROUP
			// 
			this.IDM_CREATEGROUP.Name = "IDM_CREATEGROUP";
			this.IDM_CREATEGROUP.Size = new System.Drawing.Size(161, 22);
			this.IDM_CREATEGROUP.Text = "&Create...";
			this.IDM_CREATEGROUP.Click += new System.EventHandler(this.IDM_CREATEGROUP_Click);
			// 
			// IDM_JOINGROUP
			// 
			this.IDM_JOINGROUP.Name = "IDM_JOINGROUP";
			this.IDM_JOINGROUP.Size = new System.Drawing.Size(161, 22);
			this.IDM_JOINGROUP.Text = "&Join...";
			this.IDM_JOINGROUP.Click += new System.EventHandler(this.IDM_JOINGROUP_Click);
			// 
			// IDM_OPENGROUP
			// 
			this.IDM_OPENGROUP.Name = "IDM_OPENGROUP";
			this.IDM_OPENGROUP.Size = new System.Drawing.Size(161, 22);
			this.IDM_OPENGROUP.Text = "&Open...";
			this.IDM_OPENGROUP.Click += new System.EventHandler(this.IDM_OPENGROUP_Click);
			// 
			// IDM_CLOSEGROUP
			// 
			this.IDM_CLOSEGROUP.Name = "IDM_CLOSEGROUP";
			this.IDM_CLOSEGROUP.Size = new System.Drawing.Size(161, 22);
			this.IDM_CLOSEGROUP.Text = "C&lose";
			this.IDM_CLOSEGROUP.Click += new System.EventHandler(this.IDM_CLOSEGROUP_Click);
			// 
			// IDM_DELETEGROUP
			// 
			this.IDM_DELETEGROUP.Name = "IDM_DELETEGROUP";
			this.IDM_DELETEGROUP.Size = new System.Drawing.Size(161, 22);
			this.IDM_DELETEGROUP.Text = "&Delete...";
			this.IDM_DELETEGROUP.Click += new System.EventHandler(this.IDM_DELETEGROUP_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(158, 6);
			// 
			// IDM_CREATEINVITATION
			// 
			this.IDM_CREATEINVITATION.Enabled = false;
			this.IDM_CREATEINVITATION.Name = "IDM_CREATEINVITATION";
			this.IDM_CREATEINVITATION.Size = new System.Drawing.Size(161, 22);
			this.IDM_CREATEINVITATION.Text = "Create In&vitation";
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.IDM_ABOUT});
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(44, 20);
			this.toolStripMenuItem4.Text = "&Help";
			// 
			// IDM_ABOUT
			// 
			this.IDM_ABOUT.Name = "IDM_ABOUT";
			this.IDM_ABOUT.Size = new System.Drawing.Size(116, 22);
			this.IDM_ABOUT.Text = "&About...";
			this.IDM_ABOUT.Click += new System.EventHandler(this.IDM_ABOUT_Click);
			// 
			// IDC_STATUS
			// 
			this.IDC_STATUS.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.SB_PART_STATUS,
            this.SB_PART_MESSAGE});
			this.IDC_STATUS.Location = new System.Drawing.Point(0, 313);
			this.IDC_STATUS.Name = "IDC_STATUS";
			this.IDC_STATUS.Padding = new System.Windows.Forms.Padding(1, 0, 12, 0);
			this.IDC_STATUS.Size = new System.Drawing.Size(537, 22);
			this.IDC_STATUS.TabIndex = 1;
			this.IDC_STATUS.Text = "statusStrip1";
			// 
			// SB_PART_STATUS
			// 
			this.SB_PART_STATUS.AutoSize = false;
			this.SB_PART_STATUS.Name = "SB_PART_STATUS";
			this.SB_PART_STATUS.Size = new System.Drawing.Size(100, 17);
			// 
			// SB_PART_MESSAGE
			// 
			this.SB_PART_MESSAGE.Name = "SB_PART_MESSAGE";
			this.SB_PART_MESSAGE.Size = new System.Drawing.Size(0, 17);
			// 
			// IDC_SEND
			// 
			this.IDC_SEND.Location = new System.Drawing.Point(357, 204);
			this.IDC_SEND.Name = "IDC_SEND";
			this.IDC_SEND.Size = new System.Drawing.Size(64, 35);
			this.IDC_SEND.TabIndex = 2;
			this.IDC_SEND.Text = "Send";
			this.IDC_SEND.UseVisualStyleBackColor = true;
			this.IDC_SEND.Click += new System.EventHandler(this.IDC_SEND_Click);
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 17F));
			this.tableLayoutPanel1.Controls.Add(this.IDC_TEXTBOX, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.IDC_STATIC_MEMBERS, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.label2, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.IDC_MESSAGES, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.IDC_MEMBERS, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.IDC_SEND, 1, 2);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 24);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(537, 289);
			this.tableLayoutPanel1.TabIndex = 3;
			// 
			// IDC_TEXTBOX
			// 
			this.IDC_TEXTBOX.BackColor = System.Drawing.SystemColors.Window;
			this.IDC_TEXTBOX.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IDC_TEXTBOX.Location = new System.Drawing.Point(3, 204);
			this.IDC_TEXTBOX.Multiline = true;
			this.IDC_TEXTBOX.Name = "IDC_TEXTBOX";
			this.IDC_TEXTBOX.Size = new System.Drawing.Size(348, 82);
			this.IDC_TEXTBOX.TabIndex = 7;
			// 
			// IDC_STATIC_MEMBERS
			// 
			this.IDC_STATIC_MEMBERS.AutoSize = true;
			this.IDC_STATIC_MEMBERS.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IDC_STATIC_MEMBERS.Location = new System.Drawing.Point(3, 0);
			this.IDC_STATIC_MEMBERS.Name = "IDC_STATIC_MEMBERS";
			this.IDC_STATIC_MEMBERS.Size = new System.Drawing.Size(348, 28);
			this.IDC_STATIC_MEMBERS.TabIndex = 3;
			this.IDC_STATIC_MEMBERS.Text = "Offline";
			this.IDC_STATIC_MEMBERS.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.label2.Location = new System.Drawing.Point(357, 0);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(177, 28);
			this.label2.TabIndex = 4;
			this.label2.Text = "Chat Members";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// IDC_MESSAGES
			// 
			this.IDC_MESSAGES.BackColor = System.Drawing.SystemColors.Window;
			this.IDC_MESSAGES.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IDC_MESSAGES.Location = new System.Drawing.Point(3, 31);
			this.IDC_MESSAGES.Multiline = true;
			this.IDC_MESSAGES.Name = "IDC_MESSAGES";
			this.IDC_MESSAGES.ReadOnly = true;
			this.IDC_MESSAGES.Size = new System.Drawing.Size(348, 167);
			this.IDC_MESSAGES.TabIndex = 5;
			// 
			// IDC_MEMBERS
			// 
			this.IDC_MEMBERS.Dock = System.Windows.Forms.DockStyle.Fill;
			this.IDC_MEMBERS.FormattingEnabled = true;
			this.IDC_MEMBERS.Location = new System.Drawing.Point(357, 31);
			this.IDC_MEMBERS.Name = "IDC_MEMBERS";
			this.IDC_MEMBERS.Size = new System.Drawing.Size(177, 167);
			this.IDC_MEMBERS.TabIndex = 6;
			this.IDC_MEMBERS.DoubleClick += new System.EventHandler(this.IDC_MEMBERS_DoubleClick);
			// 
			// GroupChat
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(537, 335);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.IDC_STATUS);
			this.Controls.Add(this.IDC_MENU);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.IDC_MENU;
			this.Name = "GroupChat";
			this.Text = "Group Chat Sample Application";
			this.IDC_MENU.ResumeLayout(false);
			this.IDC_MENU.PerformLayout();
			this.IDC_STATUS.ResumeLayout(false);
			this.IDC_STATUS.PerformLayout();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip IDC_MENU;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem IDM_CLEARTEXT;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem IDM_EXIT;
		private System.Windows.Forms.ToolStripMenuItem IDM_CREATEIDENTITY;
		private System.Windows.Forms.ToolStripMenuItem IDM_SAVEIDENTITYINFO;
		private System.Windows.Forms.ToolStripMenuItem IDM_DELETEIDENTITY;
		private System.Windows.Forms.ToolStripMenuItem IDM_CREATEGROUP;
		private System.Windows.Forms.ToolStripMenuItem IDM_JOINGROUP;
		private System.Windows.Forms.ToolStripMenuItem IDM_OPENGROUP;
		private System.Windows.Forms.ToolStripMenuItem IDM_CLOSEGROUP;
		private System.Windows.Forms.ToolStripMenuItem IDM_DELETEGROUP;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem IDM_CREATEINVITATION;
		private System.Windows.Forms.ToolStripMenuItem IDM_ABOUT;
		private System.Windows.Forms.StatusStrip IDC_STATUS;
		private System.Windows.Forms.ToolStripStatusLabel SB_PART_STATUS;
		private System.Windows.Forms.ToolStripStatusLabel SB_PART_MESSAGE;
		private System.Windows.Forms.Button IDC_SEND;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label IDC_STATIC_MEMBERS;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox IDC_MESSAGES;
		private System.Windows.Forms.ListBox IDC_MEMBERS;
		private System.Windows.Forms.TextBox IDC_TEXTBOX;
	}
}

