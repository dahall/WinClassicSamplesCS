namespace ADQI;

partial class ADQIDlg
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
		groupBox1 = new GroupBox();
		okBtn = new Button();
		adspathText = new TextBox();
		label1 = new Label();
		groupBox2 = new GroupBox();
		interfaceList = new ListBox();
		label2 = new Label();
		cancelBtn = new Button();
		groupBox1.SuspendLayout();
		groupBox2.SuspendLayout();
		SuspendLayout();
		// 
		// groupBox1
		// 
		groupBox1.Controls.Add(okBtn);
		groupBox1.Controls.Add(adspathText);
		groupBox1.Controls.Add(label1);
		groupBox1.Location = new Point(12, 12);
		groupBox1.Name = "groupBox1";
		groupBox1.Size = new Size(499, 83);
		groupBox1.TabIndex = 0;
		groupBox1.TabStop = false;
		groupBox1.Text = "ADs Path";
		// 
		// okBtn
		// 
		okBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		okBtn.Location = new Point(415, 48);
		okBtn.Name = "okBtn";
		okBtn.Size = new Size(75, 23);
		okBtn.TabIndex = 2;
		okBtn.Text = "OK";
		okBtn.UseVisualStyleBackColor = true;
		okBtn.Click += okBtn_Click;
		// 
		// adspathText
		// 
		adspathText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		adspathText.Location = new Point(10, 48);
		adspathText.Name = "adspathText";
		adspathText.Size = new Size(389, 23);
		adspathText.TabIndex = 1;
		adspathText.TextChanged += adspathText_TextChanged;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Location = new Point(8, 21);
		label1.Name = "label1";
		label1.Size = new Size(465, 15);
		label1.TabIndex = 0;
		label1.Text = "e.g.: LDAP://DC=ArcadiaBay, DC=COM - You can use ADSVW.EXE to copy the ADsPath";
		// 
		// groupBox2
		// 
		groupBox2.Controls.Add(interfaceList);
		groupBox2.Controls.Add(label2);
		groupBox2.Location = new Point(12, 104);
		groupBox2.Name = "groupBox2";
		groupBox2.Size = new Size(499, 242);
		groupBox2.TabIndex = 1;
		groupBox2.TabStop = false;
		groupBox2.Text = "Supported Interfaces";
		// 
		// interfaceList
		// 
		interfaceList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		interfaceList.FormattingEnabled = true;
		interfaceList.ItemHeight = 15;
		interfaceList.Location = new Point(10, 47);
		interfaceList.Name = "interfaceList";
		interfaceList.Size = new Size(480, 184);
		interfaceList.TabIndex = 1;
		interfaceList.DoubleClick += interfaceList_DoubleClick;
		// 
		// label2
		// 
		label2.AutoSize = true;
		label2.Location = new Point(10, 25);
		label2.Name = "label2";
		label2.Size = new Size(427, 15);
		label2.TabIndex = 0;
		label2.Text = "Double click to view. For alternate credentials use IADsOpenDsObject, e.g. LDAP";
		// 
		// cancelBtn
		// 
		cancelBtn.Location = new Point(230, 352);
		cancelBtn.Name = "cancelBtn";
		cancelBtn.Size = new Size(75, 23);
		cancelBtn.TabIndex = 2;
		cancelBtn.Text = "Close";
		cancelBtn.UseVisualStyleBackColor = true;
		cancelBtn.Click += cancelBtn_Click;
		// 
		// ADQIDlg
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(523, 386);
		Controls.Add(cancelBtn);
		Controls.Add(groupBox2);
		Controls.Add(groupBox1);
		Name = "ADQIDlg";
		Text = "Active Directory Service Interfaces - Quest for Interfaces";
		FormClosing += ADQIDlg_FormClosing;
		Load += ADQIDlg_Load;
		groupBox1.ResumeLayout(false);
		groupBox1.PerformLayout();
		groupBox2.ResumeLayout(false);
		groupBox2.PerformLayout();
		ResumeLayout(false);
	}

	#endregion

	private GroupBox groupBox1;
	private Button okBtn;
	private TextBox adspathText;
	private Label label1;
	private GroupBox groupBox2;
	private ListBox interfaceList;
	private Label label2;
	private Button cancelBtn;
}
