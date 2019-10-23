namespace ExplorerBrowserSearch
{
	partial class CExplorerBrowserSearchApp
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
			this.components = new System.ComponentModel.Container();
			this.IDC_SEARCHBOXBGND = new System.Windows.Forms.TableLayoutPanel();
			this.IDC_SEARCHBOX = new System.Windows.Forms.TextBox();
			this.IDC_SEARCHING = new System.Windows.Forms.Label();
			this.IDC_NAME = new System.Windows.Forms.Label();
			this.IDC_OPEN_ITEM = new System.Windows.Forms.Button();
			this.timer = new System.Windows.Forms.Timer(this.components);
			this.IDC_EXPLORER_BROWSER = new System.Windows.Forms.Panel();
			this.IDC_SEARCHBOXBGND.SuspendLayout();
			this.SuspendLayout();
			// 
			// IDC_SEARCHBOXBGND
			// 
			this.IDC_SEARCHBOXBGND.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_SEARCHBOXBGND.AutoSize = true;
			this.IDC_SEARCHBOXBGND.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.IDC_SEARCHBOXBGND.BackColor = System.Drawing.SystemColors.Window;
			this.IDC_SEARCHBOXBGND.ColumnCount = 2;
			this.IDC_SEARCHBOXBGND.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.IDC_SEARCHBOXBGND.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.IDC_SEARCHBOXBGND.Controls.Add(this.IDC_SEARCHBOX, 0, 0);
			this.IDC_SEARCHBOXBGND.Controls.Add(this.IDC_SEARCHING, 1, 0);
			this.IDC_SEARCHBOXBGND.Location = new System.Drawing.Point(266, 15);
			this.IDC_SEARCHBOXBGND.Name = "IDC_SEARCHBOXBGND";
			this.IDC_SEARCHBOXBGND.RowCount = 1;
			this.IDC_SEARCHBOXBGND.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.IDC_SEARCHBOXBGND.Size = new System.Drawing.Size(227, 22);
			this.IDC_SEARCHBOXBGND.TabIndex = 4;
			// 
			// IDC_SEARCHBOX
			// 
			this.IDC_SEARCHBOX.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.IDC_SEARCHBOX.Dock = System.Windows.Forms.DockStyle.Top;
			this.IDC_SEARCHBOX.Location = new System.Drawing.Point(3, 3);
			this.IDC_SEARCHBOX.Name = "IDC_SEARCHBOX";
			this.IDC_SEARCHBOX.Size = new System.Drawing.Size(200, 16);
			this.IDC_SEARCHBOX.TabIndex = 0;
			this.IDC_SEARCHBOX.TextChanged += new System.EventHandler(this.IDC_SEARCHBOX_TextChanged);
			// 
			// IDC_SEARCHING
			// 
			this.IDC_SEARCHING.Image = global::ExplorerBrowserSearch.Properties.Resources.searchwh;
			this.IDC_SEARCHING.Location = new System.Drawing.Point(209, 3);
			this.IDC_SEARCHING.Margin = new System.Windows.Forms.Padding(3);
			this.IDC_SEARCHING.Name = "IDC_SEARCHING";
			this.IDC_SEARCHING.Size = new System.Drawing.Size(15, 15);
			this.IDC_SEARCHING.TabIndex = 1;
			this.IDC_SEARCHING.Click += new System.EventHandler(this.IDC_SEARCHING_Click);
			this.IDC_SEARCHING.MouseLeave += new System.EventHandler(this.IDC_SEARCHING_MouseLeave);
			this.IDC_SEARCHING.MouseMove += new System.Windows.Forms.MouseEventHandler(this.IDC_SEARCHING_MouseMove);
			// 
			// IDC_NAME
			// 
			this.IDC_NAME.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.IDC_NAME.AutoSize = true;
			this.IDC_NAME.Location = new System.Drawing.Point(110, 405);
			this.IDC_NAME.Name = "IDC_NAME";
			this.IDC_NAME.Size = new System.Drawing.Size(0, 15);
			this.IDC_NAME.TabIndex = 6;
			// 
			// IDC_OPEN_ITEM
			// 
			this.IDC_OPEN_ITEM.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.IDC_OPEN_ITEM.Location = new System.Drawing.Point(15, 399);
			this.IDC_OPEN_ITEM.Name = "IDC_OPEN_ITEM";
			this.IDC_OPEN_ITEM.Size = new System.Drawing.Size(87, 27);
			this.IDC_OPEN_ITEM.TabIndex = 5;
			this.IDC_OPEN_ITEM.Text = "Open";
			this.IDC_OPEN_ITEM.UseVisualStyleBackColor = true;
			this.IDC_OPEN_ITEM.Click += new System.EventHandler(this._OnOpenItem);
			// 
			// timer
			// 
			this.timer.Interval = 250;
			this.timer.Tick += new System.EventHandler(this.Timer_Tick);
			// 
			// IDC_EXPLORER_BROWSER
			// 
			this.IDC_EXPLORER_BROWSER.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_EXPLORER_BROWSER.Location = new System.Drawing.Point(15, 43);
			this.IDC_EXPLORER_BROWSER.Name = "IDC_EXPLORER_BROWSER";
			this.IDC_EXPLORER_BROWSER.Size = new System.Drawing.Size(478, 350);
			this.IDC_EXPLORER_BROWSER.TabIndex = 7;
			this.IDC_EXPLORER_BROWSER.Visible = false;
			this.IDC_EXPLORER_BROWSER.Resize += new System.EventHandler(this.IDC_EXPLORER_BROWSER_Resize);
			// 
			// CExplorerBrowserSearchApp
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(508, 441);
			this.Controls.Add(this.IDC_EXPLORER_BROWSER);
			this.Controls.Add(this.IDC_SEARCHBOXBGND);
			this.Controls.Add(this.IDC_NAME);
			this.Controls.Add(this.IDC_OPEN_ITEM);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "CExplorerBrowserSearchApp";
			this.Text = "Simple Explorer Browser Search Example";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this._OnDestroyDialog);
			this.Load += new System.EventHandler(this._OnInitializeDialog);
			this.IDC_SEARCHBOXBGND.ResumeLayout(false);
			this.IDC_SEARCHBOXBGND.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel IDC_SEARCHBOXBGND;
		private System.Windows.Forms.TextBox IDC_SEARCHBOX;
		private System.Windows.Forms.Label IDC_SEARCHING;
		private System.Windows.Forms.Label IDC_NAME;
		private System.Windows.Forms.Button IDC_OPEN_ITEM;
		private System.Windows.Forms.Timer timer;
		private System.Windows.Forms.Panel IDC_EXPLORER_BROWSER;
	}
}
