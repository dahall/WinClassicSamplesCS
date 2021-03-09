namespace GroupChat
{
	partial class IDD_OPENGROUP
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
			this.IDC_CB_IDENTITY = new System.Windows.Forms.ComboBox();
			this.IDCANCEL = new System.Windows.Forms.Button();
			this.IDOK = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.IDC_CB_GROUP = new System.Windows.Forms.ComboBox();
			this.label2 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// IDC_CB_IDENTITY
			// 
			this.IDC_CB_IDENTITY.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.IDC_CB_IDENTITY.FormattingEnabled = true;
			this.IDC_CB_IDENTITY.Location = new System.Drawing.Point(62, 12);
			this.IDC_CB_IDENTITY.Name = "IDC_CB_IDENTITY";
			this.IDC_CB_IDENTITY.Size = new System.Drawing.Size(227, 21);
			this.IDC_CB_IDENTITY.Sorted = true;
			this.IDC_CB_IDENTITY.TabIndex = 12;
			this.IDC_CB_IDENTITY.SelectedIndexChanged += new System.EventHandler(this.IDC_CB_IDENTITY_SelectedIndexChanged);
			// 
			// IDCANCEL
			// 
			this.IDCANCEL.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.IDCANCEL.Location = new System.Drawing.Point(158, 66);
			this.IDCANCEL.Name = "IDCANCEL";
			this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
			this.IDCANCEL.TabIndex = 11;
			this.IDCANCEL.Text = "&Cancel";
			this.IDCANCEL.UseVisualStyleBackColor = true;
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(77, 66);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(75, 23);
			this.IDOK.TabIndex = 10;
			this.IDOK.Text = "OK";
			this.IDOK.UseVisualStyleBackColor = true;
			this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 13);
			this.label1.TabIndex = 9;
			this.label1.Text = "&Identity:";
			// 
			// IDC_CB_GROUP
			// 
			this.IDC_CB_GROUP.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.IDC_CB_GROUP.FormattingEnabled = true;
			this.IDC_CB_GROUP.Location = new System.Drawing.Point(62, 39);
			this.IDC_CB_GROUP.Name = "IDC_CB_GROUP";
			this.IDC_CB_GROUP.Size = new System.Drawing.Size(227, 21);
			this.IDC_CB_GROUP.Sorted = true;
			this.IDC_CB_GROUP.TabIndex = 14;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 42);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(39, 13);
			this.label2.TabIndex = 13;
			this.label2.Text = "&Group:";
			// 
			// IDD_OPENGROUP
			// 
			this.AcceptButton = this.IDOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.IDCANCEL;
			this.ClientSize = new System.Drawing.Size(302, 101);
			this.Controls.Add(this.IDC_CB_GROUP);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.IDC_CB_IDENTITY);
			this.Controls.Add(this.IDCANCEL);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "IDD_OPENGROUP";
			this.Text = "Open Group";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox IDC_CB_IDENTITY;
		private System.Windows.Forms.Button IDCANCEL;
		private System.Windows.Forms.Button IDOK;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.ComboBox IDC_CB_GROUP;
		private System.Windows.Forms.Label label2;
	}
}