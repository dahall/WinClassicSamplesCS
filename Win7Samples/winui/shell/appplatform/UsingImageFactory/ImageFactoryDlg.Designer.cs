namespace ImageFactorySample;

partial class ImageFactoryDlg
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
			this.IDCANCEL = new System.Windows.Forms.Button();
			this.IDOK = new System.Windows.Forms.Button();
			this.IDC_STATIC1 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.IDC_STATIC1)).BeginInit();
			this.SuspendLayout();
			// 
			// IDCANCEL
			// 
			this.IDCANCEL.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.IDCANCEL.Location = new System.Drawing.Point(209, 162);
			this.IDCANCEL.Name = "IDCANCEL";
			this.IDCANCEL.Size = new System.Drawing.Size(75, 23);
			this.IDCANCEL.TabIndex = 0;
			this.IDCANCEL.Text = "Cancel";
			this.IDCANCEL.UseVisualStyleBackColor = true;
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(128, 162);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(75, 23);
			this.IDOK.TabIndex = 0;
			this.IDOK.Text = "OK";
			this.IDOK.UseVisualStyleBackColor = true;
			// 
			// IDC_STATIC1
			// 
			this.IDC_STATIC1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.IDC_STATIC1.Location = new System.Drawing.Point(13, 13);
			this.IDC_STATIC1.Name = "IDC_STATIC1";
			this.IDC_STATIC1.Size = new System.Drawing.Size(271, 143);
			this.IDC_STATIC1.TabIndex = 1;
			this.IDC_STATIC1.TabStop = false;
			// 
			// ImageFactoryDlg
			// 
			this.AcceptButton = this.IDOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.IDCANCEL;
			this.ClientSize = new System.Drawing.Size(296, 197);
			this.Controls.Add(this.IDC_STATIC1);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.IDCANCEL);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			this.Name = "ImageFactoryDlg";
			this.Text = "IShellItemImageFactory Sample";
			((System.ComponentModel.ISupportInitialize)(this.IDC_STATIC1)).EndInit();
			this.ResumeLayout(false);

	}

	#endregion

	private System.Windows.Forms.Button IDCANCEL;
	private System.Windows.Forms.Button IDOK;
	public System.Windows.Forms.PictureBox IDC_STATIC1;
}