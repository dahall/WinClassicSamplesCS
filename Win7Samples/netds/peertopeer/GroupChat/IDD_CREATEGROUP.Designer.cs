namespace GroupChat
{
	partial class IDD_CREATEGROUP
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
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.IDC_STATIC_PASSWORD = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.IDC_BTN_NEW_IDENTITY = new System.Windows.Forms.Button();
			this.IDOK = new System.Windows.Forms.Button();
			this.IDCANCEL = new System.Windows.Forms.Button();
			this.IDC_CB_IDENTITY = new System.Windows.Forms.ComboBox();
			this.IDC_EDT_GROUPNAME = new System.Windows.Forms.TextBox();
			this.IDC_RADIO_AUTH_INVITE = new System.Windows.Forms.RadioButton();
			this.IDC_RADIO_AUTH_PASSW = new System.Windows.Forms.RadioButton();
			this.IDC_EDIT_PASSWORD = new System.Windows.Forms.TextBox();
			this.IDC_RADIO_GLOBAL_SCOPE = new System.Windows.Forms.RadioButton();
			this.IDC_RADIO_LOCAL_SCOPE = new System.Windows.Forms.RadioButton();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(44, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "&Identity:";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(13, 44);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(39, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "&Group:";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 73);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(78, 13);
			this.label3.TabIndex = 2;
			this.label3.Text = "Authentication:";
			// 
			// IDC_STATIC_PASSWORD
			// 
			this.IDC_STATIC_PASSWORD.AutoSize = true;
			this.IDC_STATIC_PASSWORD.Enabled = false;
			this.IDC_STATIC_PASSWORD.Location = new System.Drawing.Point(13, 104);
			this.IDC_STATIC_PASSWORD.Name = "IDC_STATIC_PASSWORD";
			this.IDC_STATIC_PASSWORD.Size = new System.Drawing.Size(56, 13);
			this.IDC_STATIC_PASSWORD.TabIndex = 3;
			this.IDC_STATIC_PASSWORD.Text = "Password:";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(13, 134);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(41, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "Scope:";
			// 
			// IDC_BTN_NEW_IDENTITY
			// 
			this.IDC_BTN_NEW_IDENTITY.Location = new System.Drawing.Point(296, 10);
			this.IDC_BTN_NEW_IDENTITY.Name = "IDC_BTN_NEW_IDENTITY";
			this.IDC_BTN_NEW_IDENTITY.Size = new System.Drawing.Size(75, 23);
			this.IDC_BTN_NEW_IDENTITY.TabIndex = 5;
			this.IDC_BTN_NEW_IDENTITY.Text = "&New...";
			this.IDC_BTN_NEW_IDENTITY.UseVisualStyleBackColor = true;
			this.IDC_BTN_NEW_IDENTITY.Click += new System.EventHandler(this.IDC_BTN_NEW_IDENTITY_Click);
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(113, 168);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(75, 23);
			this.IDOK.TabIndex = 6;
			this.IDOK.Text = "OK";
			this.IDOK.UseVisualStyleBackColor = true;
			this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
			// 
			// IDCANCEL
			// 
			this.IDCANCEL.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.IDCANCEL.Location = new System.Drawing.Point(194, 168);
			this.IDCANCEL.Name = "IDCANCEL";
			this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
			this.IDCANCEL.TabIndex = 7;
			this.IDCANCEL.Text = "&Cancel";
			this.IDCANCEL.UseVisualStyleBackColor = true;
			// 
			// IDC_CB_IDENTITY
			// 
			this.IDC_CB_IDENTITY.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.IDC_CB_IDENTITY.FormattingEnabled = true;
			this.IDC_CB_IDENTITY.Location = new System.Drawing.Point(63, 10);
			this.IDC_CB_IDENTITY.Name = "IDC_CB_IDENTITY";
			this.IDC_CB_IDENTITY.Size = new System.Drawing.Size(227, 21);
			this.IDC_CB_IDENTITY.Sorted = true;
			this.IDC_CB_IDENTITY.TabIndex = 8;
			// 
			// IDC_EDT_GROUPNAME
			// 
			this.IDC_EDT_GROUPNAME.Location = new System.Drawing.Point(63, 41);
			this.IDC_EDT_GROUPNAME.MaxLength = 257;
			this.IDC_EDT_GROUPNAME.Name = "IDC_EDT_GROUPNAME";
			this.IDC_EDT_GROUPNAME.Size = new System.Drawing.Size(227, 20);
			this.IDC_EDT_GROUPNAME.TabIndex = 9;
			// 
			// IDC_RADIO_AUTH_INVITE
			// 
			this.IDC_RADIO_AUTH_INVITE.AutoSize = true;
			this.IDC_RADIO_AUTH_INVITE.Location = new System.Drawing.Point(96, 71);
			this.IDC_RADIO_AUTH_INVITE.Name = "IDC_RADIO_AUTH_INVITE";
			this.IDC_RADIO_AUTH_INVITE.Size = new System.Drawing.Size(68, 17);
			this.IDC_RADIO_AUTH_INVITE.TabIndex = 10;
			this.IDC_RADIO_AUTH_INVITE.Text = "Invitation";
			this.IDC_RADIO_AUTH_INVITE.UseVisualStyleBackColor = true;
			this.IDC_RADIO_AUTH_INVITE.CheckedChanged += new System.EventHandler(this.AuthRadioCheckChanged);
			// 
			// IDC_RADIO_AUTH_PASSW
			// 
			this.IDC_RADIO_AUTH_PASSW.AutoSize = true;
			this.IDC_RADIO_AUTH_PASSW.Location = new System.Drawing.Point(180, 71);
			this.IDC_RADIO_AUTH_PASSW.Name = "IDC_RADIO_AUTH_PASSW";
			this.IDC_RADIO_AUTH_PASSW.Size = new System.Drawing.Size(71, 17);
			this.IDC_RADIO_AUTH_PASSW.TabIndex = 11;
			this.IDC_RADIO_AUTH_PASSW.Text = "Password";
			this.IDC_RADIO_AUTH_PASSW.UseVisualStyleBackColor = true;
			this.IDC_RADIO_AUTH_PASSW.CheckedChanged += new System.EventHandler(this.AuthRadioCheckChanged);
			// 
			// IDC_EDIT_PASSWORD
			// 
			this.IDC_EDIT_PASSWORD.Enabled = false;
			this.IDC_EDIT_PASSWORD.Location = new System.Drawing.Point(75, 101);
			this.IDC_EDIT_PASSWORD.Name = "IDC_EDIT_PASSWORD";
			this.IDC_EDIT_PASSWORD.Size = new System.Drawing.Size(215, 20);
			this.IDC_EDIT_PASSWORD.TabIndex = 12;
			this.IDC_EDIT_PASSWORD.UseSystemPasswordChar = true;
			// 
			// IDC_RADIO_GLOBAL_SCOPE
			// 
			this.IDC_RADIO_GLOBAL_SCOPE.AutoSize = true;
			this.IDC_RADIO_GLOBAL_SCOPE.Checked = true;
			this.IDC_RADIO_GLOBAL_SCOPE.Location = new System.Drawing.Point(96, 132);
			this.IDC_RADIO_GLOBAL_SCOPE.Name = "IDC_RADIO_GLOBAL_SCOPE";
			this.IDC_RADIO_GLOBAL_SCOPE.Size = new System.Drawing.Size(55, 17);
			this.IDC_RADIO_GLOBAL_SCOPE.TabIndex = 10;
			this.IDC_RADIO_GLOBAL_SCOPE.TabStop = true;
			this.IDC_RADIO_GLOBAL_SCOPE.Text = "Global";
			this.IDC_RADIO_GLOBAL_SCOPE.UseVisualStyleBackColor = true;
			// 
			// IDC_RADIO_LOCAL_SCOPE
			// 
			this.IDC_RADIO_LOCAL_SCOPE.AutoSize = true;
			this.IDC_RADIO_LOCAL_SCOPE.Location = new System.Drawing.Point(180, 132);
			this.IDC_RADIO_LOCAL_SCOPE.Name = "IDC_RADIO_LOCAL_SCOPE";
			this.IDC_RADIO_LOCAL_SCOPE.Size = new System.Drawing.Size(51, 17);
			this.IDC_RADIO_LOCAL_SCOPE.TabIndex = 11;
			this.IDC_RADIO_LOCAL_SCOPE.Text = "Local";
			this.IDC_RADIO_LOCAL_SCOPE.UseVisualStyleBackColor = true;
			// 
			// IDD_CREATEGROUP
			// 
			this.AcceptButton = this.IDOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.IDCANCEL;
			this.ClientSize = new System.Drawing.Size(387, 203);
			this.Controls.Add(this.IDC_EDIT_PASSWORD);
			this.Controls.Add(this.IDC_RADIO_LOCAL_SCOPE);
			this.Controls.Add(this.IDC_RADIO_AUTH_PASSW);
			this.Controls.Add(this.IDC_RADIO_GLOBAL_SCOPE);
			this.Controls.Add(this.IDC_RADIO_AUTH_INVITE);
			this.Controls.Add(this.IDC_EDT_GROUPNAME);
			this.Controls.Add(this.IDC_CB_IDENTITY);
			this.Controls.Add(this.IDCANCEL);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.IDC_BTN_NEW_IDENTITY);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.IDC_STATIC_PASSWORD);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "IDD_CREATEGROUP";
			this.Text = "Create New Group";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label IDC_STATIC_PASSWORD;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button IDC_BTN_NEW_IDENTITY;
		private System.Windows.Forms.Button IDOK;
		private System.Windows.Forms.Button IDCANCEL;
		private System.Windows.Forms.ComboBox IDC_CB_IDENTITY;
		private System.Windows.Forms.TextBox IDC_EDT_GROUPNAME;
		private System.Windows.Forms.RadioButton IDC_RADIO_AUTH_INVITE;
		private System.Windows.Forms.RadioButton IDC_RADIO_AUTH_PASSW;
		private System.Windows.Forms.TextBox IDC_EDIT_PASSWORD;
		private System.Windows.Forms.RadioButton IDC_RADIO_GLOBAL_SCOPE;
		private System.Windows.Forms.RadioButton IDC_RADIO_LOCAL_SCOPE;
	}
}