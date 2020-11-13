namespace GroupChat
{
	partial class IDD_JOINGROUP
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
			this.IDC_EDIT_PASSWORD = new System.Windows.Forms.TextBox();
			this.IDC_STATIC_PASSWORD = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.IDC_CHECK_PASSWORD = new System.Windows.Forms.CheckBox();
			this.IDC_EDT_LOCATION = new System.Windows.Forms.TextBox();
			this.IDC_BTN_BROWSE = new System.Windows.Forms.Button();
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
			this.IDC_CB_IDENTITY.TabIndex = 20;
			// 
			// IDCANCEL
			// 
			this.IDCANCEL.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.IDCANCEL.Location = new System.Drawing.Point(159, 129);
			this.IDCANCEL.Name = "IDCANCEL";
			this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
			this.IDCANCEL.TabIndex = 19;
			this.IDCANCEL.Text = "&Cancel";
			this.IDCANCEL.UseVisualStyleBackColor = true;
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(78, 129);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(75, 23);
			this.IDOK.TabIndex = 18;
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
			this.label1.TabIndex = 17;
			this.label1.Text = "&Identity:";
			// 
			// IDC_EDIT_PASSWORD
			// 
			this.IDC_EDIT_PASSWORD.Enabled = false;
			this.IDC_EDIT_PASSWORD.Location = new System.Drawing.Point(74, 100);
			this.IDC_EDIT_PASSWORD.Name = "IDC_EDIT_PASSWORD";
			this.IDC_EDIT_PASSWORD.Size = new System.Drawing.Size(216, 20);
			this.IDC_EDIT_PASSWORD.TabIndex = 22;
			this.IDC_EDIT_PASSWORD.UseSystemPasswordChar = true;
			// 
			// IDC_STATIC_PASSWORD
			// 
			this.IDC_STATIC_PASSWORD.AutoSize = true;
			this.IDC_STATIC_PASSWORD.Enabled = false;
			this.IDC_STATIC_PASSWORD.Location = new System.Drawing.Point(12, 103);
			this.IDC_STATIC_PASSWORD.Name = "IDC_STATIC_PASSWORD";
			this.IDC_STATIC_PASSWORD.Size = new System.Drawing.Size(56, 13);
			this.IDC_STATIC_PASSWORD.TabIndex = 21;
			this.IDC_STATIC_PASSWORD.Text = "Password:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 43);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 13);
			this.label2.TabIndex = 23;
			this.label2.Text = "Invitation:";
			// 
			// IDC_CHECK_PASSWORD
			// 
			this.IDC_CHECK_PASSWORD.AutoSize = true;
			this.IDC_CHECK_PASSWORD.Location = new System.Drawing.Point(15, 71);
			this.IDC_CHECK_PASSWORD.Name = "IDC_CHECK_PASSWORD";
			this.IDC_CHECK_PASSWORD.Size = new System.Drawing.Size(105, 17);
			this.IDC_CHECK_PASSWORD.TabIndex = 24;
			this.IDC_CHECK_PASSWORD.Text = "Password-Based";
			this.IDC_CHECK_PASSWORD.UseVisualStyleBackColor = true;
			this.IDC_CHECK_PASSWORD.CheckedChanged += new System.EventHandler(this.IDC_CHECK_PASSWORD_CheckedChanged);
			// 
			// IDC_EDT_LOCATION
			// 
			this.IDC_EDT_LOCATION.Location = new System.Drawing.Point(71, 40);
			this.IDC_EDT_LOCATION.Name = "IDC_EDT_LOCATION";
			this.IDC_EDT_LOCATION.Size = new System.Drawing.Size(138, 20);
			this.IDC_EDT_LOCATION.TabIndex = 25;
			// 
			// IDC_BTN_BROWSE
			// 
			this.IDC_BTN_BROWSE.Location = new System.Drawing.Point(215, 38);
			this.IDC_BTN_BROWSE.Name = "IDC_BTN_BROWSE";
			this.IDC_BTN_BROWSE.Size = new System.Drawing.Size(75, 23);
			this.IDC_BTN_BROWSE.TabIndex = 26;
			this.IDC_BTN_BROWSE.Text = "&Browse...";
			this.IDC_BTN_BROWSE.UseVisualStyleBackColor = true;
			this.IDC_BTN_BROWSE.Click += new System.EventHandler(this.IDC_BTN_BROWSE_Click);
			// 
			// IDD_JOINGROUP
			// 
			this.AcceptButton = this.IDOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.IDCANCEL;
			this.ClientSize = new System.Drawing.Size(307, 160);
			this.Controls.Add(this.IDC_BTN_BROWSE);
			this.Controls.Add(this.IDC_EDT_LOCATION);
			this.Controls.Add(this.IDC_CHECK_PASSWORD);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.IDC_EDIT_PASSWORD);
			this.Controls.Add(this.IDC_STATIC_PASSWORD);
			this.Controls.Add(this.IDC_CB_IDENTITY);
			this.Controls.Add(this.IDCANCEL);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "IDD_JOINGROUP";
			this.Text = "Join Group";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox IDC_CB_IDENTITY;
		private System.Windows.Forms.Button IDCANCEL;
		private System.Windows.Forms.Button IDOK;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox IDC_EDIT_PASSWORD;
		private System.Windows.Forms.Label IDC_STATIC_PASSWORD;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox IDC_CHECK_PASSWORD;
		private System.Windows.Forms.TextBox IDC_EDT_LOCATION;
		private System.Windows.Forms.Button IDC_BTN_BROWSE;
	}
}