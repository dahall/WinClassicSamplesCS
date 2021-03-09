namespace GroupChat
{
	partial class IDD_NEWIDENTITY
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
			this.label1 = new System.Windows.Forms.Label();
			this.IDC_EDT_FRIENDLYNAME = new System.Windows.Forms.TextBox();
			this.button2 = new System.Windows.Forms.Button();
			this.IDOK = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(38, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "&Name:";
			// 
			// IDC_EDT_FRIENDLYNAME
			// 
			this.IDC_EDT_FRIENDLYNAME.Location = new System.Drawing.Point(56, 12);
			this.IDC_EDT_FRIENDLYNAME.MaxLength = 257;
			this.IDC_EDT_FRIENDLYNAME.Name = "IDC_EDT_FRIENDLYNAME";
			this.IDC_EDT_FRIENDLYNAME.Size = new System.Drawing.Size(203, 20);
			this.IDC_EDT_FRIENDLYNAME.TabIndex = 1;
			// 
			// button2
			// 
			this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button2.Location = new System.Drawing.Point(144, 46);
			this.button2.Name = "button2";
			this.button2.Size = new System.Drawing.Size(75, 23);
			this.button2.TabIndex = 3;
			this.button2.Text = "&Cancel";
			this.button2.UseVisualStyleBackColor = true;
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(63, 46);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(75, 23);
			this.IDOK.TabIndex = 4;
			this.IDOK.Text = "OK";
			this.IDOK.UseVisualStyleBackColor = true;
			this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
			// 
			// IDD_NEWIDENTITY
			// 
			this.AcceptButton = this.IDOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.button2;
			this.ClientSize = new System.Drawing.Size(279, 81);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.button2);
			this.Controls.Add(this.IDC_EDT_FRIENDLYNAME);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "IDD_NEWIDENTITY";
			this.Text = "Create New Identity";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox IDC_EDT_FRIENDLYNAME;
		private System.Windows.Forms.Button button2;
		private System.Windows.Forms.Button IDOK;
	}
}