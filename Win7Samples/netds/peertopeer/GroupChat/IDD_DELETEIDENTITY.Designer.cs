namespace GroupChat
{
	partial class IDD_DELETEIDENTITY
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
			this.SuspendLayout();
			// 
			// IDC_CB_IDENTITY
			// 
			this.IDC_CB_IDENTITY.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.IDC_CB_IDENTITY.FormattingEnabled = true;
			this.IDC_CB_IDENTITY.Location = new System.Drawing.Point(63, 12);
			this.IDC_CB_IDENTITY.Name = "IDC_CB_IDENTITY";
			this.IDC_CB_IDENTITY.Size = new System.Drawing.Size(227, 21);
			this.IDC_CB_IDENTITY.Sorted = true;
			this.IDC_CB_IDENTITY.TabIndex = 16;
			// 
			// IDCANCEL
			// 
			this.IDCANCEL.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.IDCANCEL.Location = new System.Drawing.Point(158, 39);
			this.IDCANCEL.Name = "IDCANCEL";
			this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
			this.IDCANCEL.TabIndex = 15;
			this.IDCANCEL.Text = "&Cancel";
			this.IDCANCEL.UseVisualStyleBackColor = true;
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(77, 39);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(75, 23);
			this.IDOK.TabIndex = 14;
			this.IDOK.Text = "OK";
			this.IDOK.UseVisualStyleBackColor = true;
			this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 13);
			this.label1.TabIndex = 13;
			this.label1.Text = "&Identity:";
			// 
			// IDD_DELETEIDENTITY
			// 
			this.AcceptButton = this.IDOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.IDCANCEL;
			this.ClientSize = new System.Drawing.Size(308, 72);
			this.Controls.Add(this.IDC_CB_IDENTITY);
			this.Controls.Add(this.IDCANCEL);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "IDD_DELETEIDENTITY";
			this.Text = "Delete Identity";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox IDC_CB_IDENTITY;
		private System.Windows.Forms.Button IDCANCEL;
		private System.Windows.Forms.Button IDOK;
		private System.Windows.Forms.Label label1;
	}
}