namespace ChangeNotifyWatcher
{
	partial class CChangeNotifyApp
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
				_watcher.Dispose();
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Event", System.Windows.Forms.HorizontalAlignment.Left);
			this.IDC_LISTVIEW = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.IDC_PICK = new System.Windows.Forms.Button();
			this.IDC_RECURSIVE = new System.Windows.Forms.CheckBox();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// IDC_LISTVIEW
			// 
			this.IDC_LISTVIEW.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_LISTVIEW.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
			this.IDC_LISTVIEW.ContextMenuStrip = this.contextMenuStrip1;
			this.IDC_LISTVIEW.FullRowSelect = true;
			listViewGroup1.Header = "Event";
			listViewGroup1.Name = "GROUPID_NAMES";
			this.IDC_LISTVIEW.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1});
			this.IDC_LISTVIEW.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.IDC_LISTVIEW.HideSelection = false;
			this.IDC_LISTVIEW.Location = new System.Drawing.Point(15, 15);
			this.IDC_LISTVIEW.Name = "IDC_LISTVIEW";
			this.IDC_LISTVIEW.Size = new System.Drawing.Size(363, 279);
			this.IDC_LISTVIEW.TabIndex = 0;
			this.IDC_LISTVIEW.UseCompatibleStateImageBehavior = false;
			this.IDC_LISTVIEW.View = System.Windows.Forms.View.Details;
			// 
			// columnHeader1
			// 
			this.columnHeader1.Text = "Name";
			// 
			// columnHeader2
			// 
			this.columnHeader2.Text = "Value";
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.copyAllToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(120, 48);
			// 
			// copyToolStripMenuItem
			// 
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(119, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.Copy_Click);
			// 
			// copyAllToolStripMenuItem
			// 
			this.copyAllToolStripMenuItem.Name = "copyAllToolStripMenuItem";
			this.copyAllToolStripMenuItem.Size = new System.Drawing.Size(119, 22);
			this.copyAllToolStripMenuItem.Text = "Copy All";
			this.copyAllToolStripMenuItem.Click += new System.EventHandler(this.CopyAll_Click);
			// 
			// IDC_PICK
			// 
			this.IDC_PICK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.IDC_PICK.Location = new System.Drawing.Point(15, 302);
			this.IDC_PICK.Name = "IDC_PICK";
			this.IDC_PICK.Size = new System.Drawing.Size(87, 27);
			this.IDC_PICK.TabIndex = 1;
			this.IDC_PICK.Text = "Pick...";
			this.IDC_PICK.UseVisualStyleBackColor = true;
			this.IDC_PICK.Click += new System.EventHandler(this.PickItem);
			// 
			// IDC_RECURSIVE
			// 
			this.IDC_RECURSIVE.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_RECURSIVE.AutoSize = true;
			this.IDC_RECURSIVE.Location = new System.Drawing.Point(303, 308);
			this.IDC_RECURSIVE.Name = "IDC_RECURSIVE";
			this.IDC_RECURSIVE.Size = new System.Drawing.Size(76, 19);
			this.IDC_RECURSIVE.TabIndex = 2;
			this.IDC_RECURSIVE.Text = "Recursive";
			this.IDC_RECURSIVE.UseVisualStyleBackColor = true;
			// 
			// CChangeNotifyApp
			// 
			this.AllowDrop = true;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(393, 339);
			this.Controls.Add(this.IDC_RECURSIVE);
			this.Controls.Add(this.IDC_PICK);
			this.Controls.Add(this.IDC_LISTVIEW);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "CChangeNotifyApp";
			this.Text = "Shell Change Notify Watcher";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnDestroyDlg);
			this.Load += new System.EventHandler(this.OnInitDlg);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.OnDrop);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.OnDragEnter);
			this.DragLeave += new System.EventHandler(this.OnDragLeave);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListView IDC_LISTVIEW;
		private System.Windows.Forms.Button IDC_PICK;
		private System.Windows.Forms.CheckBox IDC_RECURSIVE;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem copyAllToolStripMenuItem;
	}
}
