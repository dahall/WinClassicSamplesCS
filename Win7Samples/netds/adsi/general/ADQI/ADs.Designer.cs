namespace ADQI;

partial class ADsDlg
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
		tableLayoutPanel1 = new TableLayoutPanel();
		label1 = new Label();
		adsPathText = new TextBox();
		closeBtn = new Button();
		label2 = new Label();
		label4 = new Label();
		label6 = new Label();
		label7 = new Label();
		label8 = new Label();
		nameText = new TextBox();
		parentText = new TextBox();
		classText = new TextBox();
		schemaText = new TextBox();
		guidText = new TextBox();
		parentBtn = new Button();
		schemaBtn = new Button();
		guidBtn = new Button();
		divider = new GroupBox();
		attrCombo = new ComboBox();
		valuesList = new ListBox();
		tableLayoutPanel2 = new TableLayoutPanel();
		getBtn = new Button();
		getExBtn = new Button();
		getInfoBtn = new Button();
		getInfoExBtn = new Button();
		putBtn = new Button();
		putExBtn = new Button();
		setInfoBtn = new Button();
		copyBtn = new Button();
		tableLayoutPanel1.SuspendLayout();
		tableLayoutPanel2.SuspendLayout();
		SuspendLayout();
		// 
		// tableLayoutPanel1
		// 
		tableLayoutPanel1.ColumnCount = 3;
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle());
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
		tableLayoutPanel1.Controls.Add(label1, 0, 0);
		tableLayoutPanel1.Controls.Add(adsPathText, 1, 0);
		tableLayoutPanel1.Controls.Add(closeBtn, 2, 0);
		tableLayoutPanel1.Controls.Add(label2, 0, 1);
		tableLayoutPanel1.Controls.Add(label4, 0, 3);
		tableLayoutPanel1.Controls.Add(label6, 0, 9);
		tableLayoutPanel1.Controls.Add(label7, 0, 7);
		tableLayoutPanel1.Controls.Add(label8, 1, 8);
		tableLayoutPanel1.Controls.Add(nameText, 1, 1);
		tableLayoutPanel1.Controls.Add(parentText, 1, 2);
		tableLayoutPanel1.Controls.Add(classText, 1, 3);
		tableLayoutPanel1.Controls.Add(schemaText, 1, 4);
		tableLayoutPanel1.Controls.Add(guidText, 1, 5);
		tableLayoutPanel1.Controls.Add(parentBtn, 0, 2);
		tableLayoutPanel1.Controls.Add(schemaBtn, 0, 4);
		tableLayoutPanel1.Controls.Add(guidBtn, 0, 5);
		tableLayoutPanel1.Controls.Add(divider, 0, 6);
		tableLayoutPanel1.Controls.Add(attrCombo, 1, 7);
		tableLayoutPanel1.Controls.Add(valuesList, 1, 9);
		tableLayoutPanel1.Controls.Add(tableLayoutPanel2, 2, 7);
		tableLayoutPanel1.Dock = DockStyle.Fill;
		tableLayoutPanel1.Location = new Point(0, 0);
		tableLayoutPanel1.Name = "tableLayoutPanel1";
		tableLayoutPanel1.Padding = new Padding(5, 5, 5, 10);
		tableLayoutPanel1.RowCount = 10;
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.RowStyles.Add(new RowStyle());
		tableLayoutPanel1.Size = new Size(474, 469);
		tableLayoutPanel1.TabIndex = 0;
		// 
		// label1
		// 
		label1.Anchor = AnchorStyles.Left;
		label1.AutoSize = true;
		label1.Location = new Point(8, 12);
		label1.Name = "label1";
		label1.Size = new Size(55, 15);
		label1.TabIndex = 0;
		label1.Text = "ADsPath:";
		// 
		// adsPathText
		// 
		adsPathText.Dock = DockStyle.Top;
		adsPathText.Location = new Point(89, 8);
		adsPathText.Name = "adsPathText";
		adsPathText.ReadOnly = true;
		adsPathText.Size = new Size(287, 23);
		adsPathText.TabIndex = 1;
		// 
		// closeBtn
		// 
		closeBtn.AutoSize = true;
		closeBtn.Dock = DockStyle.Top;
		closeBtn.Location = new Point(382, 8);
		closeBtn.Name = "closeBtn";
		tableLayoutPanel1.SetRowSpan(closeBtn, 2);
		closeBtn.Size = new Size(84, 25);
		closeBtn.TabIndex = 2;
		closeBtn.Text = "Close";
		closeBtn.UseVisualStyleBackColor = true;
		closeBtn.Click += closeBtn_Click;
		// 
		// label2
		// 
		label2.Anchor = AnchorStyles.Left;
		label2.AutoSize = true;
		label2.Location = new Point(8, 41);
		label2.Name = "label2";
		label2.Size = new Size(42, 15);
		label2.TabIndex = 0;
		label2.Text = "Name:";
		// 
		// label4
		// 
		label4.Anchor = AnchorStyles.Left;
		label4.AutoSize = true;
		label4.Location = new Point(8, 101);
		label4.Name = "label4";
		label4.Size = new Size(37, 15);
		label4.TabIndex = 0;
		label4.Text = "Class:";
		// 
		// label6
		// 
		label6.AutoSize = true;
		label6.Location = new Point(8, 248);
		label6.Margin = new Padding(3, 5, 3, 0);
		label6.Name = "label6";
		label6.Size = new Size(51, 15);
		label6.TabIndex = 0;
		label6.Text = "Value(s):";
		// 
		// label7
		// 
		label7.Anchor = AnchorStyles.Left;
		label7.AutoSize = true;
		label7.Location = new Point(8, 200);
		label7.Name = "label7";
		label7.Size = new Size(62, 15);
		label7.TabIndex = 0;
		label7.Text = "Attributes:";
		// 
		// label8
		// 
		label8.Anchor = AnchorStyles.Left;
		label8.AutoSize = true;
		label8.Location = new Point(89, 222);
		label8.Name = "label8";
		label8.Padding = new Padding(0, 3, 0, 3);
		label8.Size = new Size(255, 21);
		label8.TabIndex = 0;
		label8.Text = "(if an interface is shown, you may double click)";
		// 
		// nameText
		// 
		nameText.Dock = DockStyle.Top;
		nameText.Location = new Point(89, 37);
		nameText.Name = "nameText";
		nameText.ReadOnly = true;
		nameText.Size = new Size(287, 23);
		nameText.TabIndex = 1;
		// 
		// parentText
		// 
		parentText.Dock = DockStyle.Top;
		parentText.Location = new Point(89, 66);
		parentText.Name = "parentText";
		parentText.ReadOnly = true;
		parentText.Size = new Size(287, 23);
		parentText.TabIndex = 1;
		// 
		// classText
		// 
		classText.Dock = DockStyle.Top;
		classText.Location = new Point(89, 97);
		classText.Name = "classText";
		classText.ReadOnly = true;
		classText.Size = new Size(287, 23);
		classText.TabIndex = 1;
		// 
		// schemaText
		// 
		schemaText.Dock = DockStyle.Top;
		schemaText.Location = new Point(89, 126);
		schemaText.Name = "schemaText";
		schemaText.ReadOnly = true;
		schemaText.Size = new Size(287, 23);
		schemaText.TabIndex = 1;
		// 
		// guidText
		// 
		guidText.Dock = DockStyle.Top;
		guidText.Location = new Point(89, 157);
		guidText.Name = "guidText";
		guidText.ReadOnly = true;
		guidText.Size = new Size(287, 23);
		guidText.TabIndex = 1;
		// 
		// parentBtn
		// 
		parentBtn.AutoSize = true;
		parentBtn.Location = new Point(8, 66);
		parentBtn.Name = "parentBtn";
		parentBtn.Size = new Size(75, 25);
		parentBtn.TabIndex = 3;
		parentBtn.Text = "Parent...";
		parentBtn.UseVisualStyleBackColor = true;
		parentBtn.Click += parentBtn_Click;
		// 
		// schemaBtn
		// 
		schemaBtn.AutoSize = true;
		schemaBtn.Location = new Point(8, 126);
		schemaBtn.Name = "schemaBtn";
		schemaBtn.Size = new Size(75, 25);
		schemaBtn.TabIndex = 3;
		schemaBtn.Text = "Schema...";
		schemaBtn.UseVisualStyleBackColor = true;
		schemaBtn.Click += schemaBtn_Click;
		// 
		// guidBtn
		// 
		guidBtn.AutoSize = true;
		guidBtn.Location = new Point(8, 157);
		guidBtn.Name = "guidBtn";
		guidBtn.Size = new Size(75, 25);
		guidBtn.TabIndex = 3;
		guidBtn.Text = "GUID...";
		guidBtn.UseVisualStyleBackColor = true;
		guidBtn.Click += guidBtn_Click;
		// 
		// divider
		// 
		tableLayoutPanel1.SetColumnSpan(divider, 3);
		divider.Dock = DockStyle.Top;
		divider.Location = new Point(8, 188);
		divider.Name = "divider";
		divider.Size = new Size(458, 2);
		divider.TabIndex = 4;
		divider.TabStop = false;
		// 
		// attrCombo
		// 
		attrCombo.Dock = DockStyle.Top;
		attrCombo.DropDownStyle = ComboBoxStyle.DropDownList;
		attrCombo.FormattingEnabled = true;
		attrCombo.Location = new Point(89, 196);
		attrCombo.Name = "attrCombo";
		attrCombo.Size = new Size(287, 23);
		attrCombo.TabIndex = 5;
		attrCombo.SelectedIndexChanged += attrCombo_SelectedIndexChanged;
		// 
		// valuesList
		// 
		valuesList.Dock = DockStyle.Fill;
		valuesList.FormattingEnabled = true;
		valuesList.ItemHeight = 15;
		valuesList.Location = new Point(89, 246);
		valuesList.Name = "valuesList";
		valuesList.Size = new Size(287, 216);
		valuesList.TabIndex = 6;
		valuesList.DoubleClick += valuesList_DoubleClick;
		// 
		// tableLayoutPanel2
		// 
		tableLayoutPanel2.AutoSize = true;
		tableLayoutPanel2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		tableLayoutPanel2.ColumnCount = 1;
		tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle());
		tableLayoutPanel2.Controls.Add(getBtn, 0, 0);
		tableLayoutPanel2.Controls.Add(getExBtn, 0, 1);
		tableLayoutPanel2.Controls.Add(getInfoBtn, 0, 2);
		tableLayoutPanel2.Controls.Add(getInfoExBtn, 0, 3);
		tableLayoutPanel2.Controls.Add(putBtn, 0, 4);
		tableLayoutPanel2.Controls.Add(putExBtn, 0, 5);
		tableLayoutPanel2.Controls.Add(setInfoBtn, 0, 6);
		tableLayoutPanel2.Controls.Add(copyBtn, 0, 8);
		tableLayoutPanel2.Dock = DockStyle.Top;
		tableLayoutPanel2.Location = new Point(379, 193);
		tableLayoutPanel2.Margin = new Padding(0);
		tableLayoutPanel2.Name = "tableLayoutPanel2";
		tableLayoutPanel2.RowCount = 9;
		tableLayoutPanel1.SetRowSpan(tableLayoutPanel2, 3);
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
		tableLayoutPanel2.RowStyles.Add(new RowStyle());
		tableLayoutPanel2.Size = new Size(90, 266);
		tableLayoutPanel2.TabIndex = 7;
		// 
		// getBtn
		// 
		getBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		getBtn.Dock = DockStyle.Top;
		getBtn.Location = new Point(3, 3);
		getBtn.Name = "getBtn";
		getBtn.Size = new Size(84, 23);
		getBtn.TabIndex = 0;
		getBtn.Text = "Get";
		getBtn.UseVisualStyleBackColor = true;
		getBtn.Click += getBtn_Click;
		// 
		// getExBtn
		// 
		getExBtn.AutoSize = true;
		getExBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		getExBtn.Dock = DockStyle.Top;
		getExBtn.Location = new Point(3, 32);
		getExBtn.Name = "getExBtn";
		getExBtn.Size = new Size(84, 25);
		getExBtn.TabIndex = 0;
		getExBtn.Text = "GetEx";
		getExBtn.UseVisualStyleBackColor = true;
		getExBtn.Click += getExBtn_Click;
		// 
		// getInfoBtn
		// 
		getInfoBtn.AutoSize = true;
		getInfoBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		getInfoBtn.Dock = DockStyle.Top;
		getInfoBtn.Location = new Point(3, 63);
		getInfoBtn.Name = "getInfoBtn";
		getInfoBtn.Size = new Size(84, 25);
		getInfoBtn.TabIndex = 0;
		getInfoBtn.Text = "GetInfo";
		getInfoBtn.UseVisualStyleBackColor = true;
		getInfoBtn.Click += getInfoBtn_Click;
		// 
		// getInfoExBtn
		// 
		getInfoExBtn.AutoSize = true;
		getInfoExBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		getInfoExBtn.Dock = DockStyle.Top;
		getInfoExBtn.Location = new Point(3, 94);
		getInfoExBtn.Name = "getInfoExBtn";
		getInfoExBtn.Size = new Size(84, 25);
		getInfoExBtn.TabIndex = 0;
		getInfoExBtn.Text = "GetInfoEx...";
		getInfoExBtn.UseVisualStyleBackColor = true;
		getInfoExBtn.Click += getInfoExBtn_Click;
		// 
		// putBtn
		// 
		putBtn.AutoSize = true;
		putBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		putBtn.Dock = DockStyle.Top;
		putBtn.Enabled = false;
		putBtn.Location = new Point(3, 125);
		putBtn.Name = "putBtn";
		putBtn.Size = new Size(84, 25);
		putBtn.TabIndex = 0;
		putBtn.Text = "Put...";
		putBtn.UseVisualStyleBackColor = true;
		putBtn.Click += putBtn_Click;
		// 
		// putExBtn
		// 
		putExBtn.AutoSize = true;
		putExBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		putExBtn.Dock = DockStyle.Top;
		putExBtn.Enabled = false;
		putExBtn.Location = new Point(3, 156);
		putExBtn.Name = "putExBtn";
		putExBtn.Size = new Size(84, 25);
		putExBtn.TabIndex = 0;
		putExBtn.Text = "PutEx...";
		putExBtn.UseVisualStyleBackColor = true;
		putExBtn.Click += putExBtn_Click;
		// 
		// setInfoBtn
		// 
		setInfoBtn.AutoSize = true;
		setInfoBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		setInfoBtn.Dock = DockStyle.Top;
		setInfoBtn.Location = new Point(3, 187);
		setInfoBtn.Name = "setInfoBtn";
		setInfoBtn.Size = new Size(84, 25);
		setInfoBtn.TabIndex = 0;
		setInfoBtn.Text = "SetInfo";
		setInfoBtn.UseVisualStyleBackColor = true;
		setInfoBtn.Click += setInfoBtn_Click;
		// 
		// copyBtn
		// 
		copyBtn.AutoSize = true;
		copyBtn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		copyBtn.Dock = DockStyle.Top;
		copyBtn.Location = new Point(3, 238);
		copyBtn.Name = "copyBtn";
		copyBtn.Size = new Size(84, 25);
		copyBtn.TabIndex = 0;
		copyBtn.Text = "Copy";
		copyBtn.UseVisualStyleBackColor = true;
		copyBtn.Click += copyBtn_Click;
		// 
		// ADs
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(474, 469);
		Controls.Add(tableLayoutPanel1);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		Name = "ADs";
		StartPosition = FormStartPosition.CenterParent;
		Text = "ADs";
		Load += ADs_Load;
		tableLayoutPanel1.ResumeLayout(false);
		tableLayoutPanel1.PerformLayout();
		tableLayoutPanel2.ResumeLayout(false);
		tableLayoutPanel2.PerformLayout();
		ResumeLayout(false);
	}

	#endregion

	private TableLayoutPanel tableLayoutPanel1;
	private Label label1;
	private TextBox adsPathText;
	private Button closeBtn;
	private Label label2;
	private Label label4;
	private Label label6;
	private Label label7;
	private Label label8;
	private TextBox nameText;
	private TextBox parentText;
	private TextBox classText;
	private TextBox schemaText;
	private TextBox guidText;
	private Button parentBtn;
	private Button schemaBtn;
	private Button guidBtn;
	private GroupBox divider;
	private ComboBox attrCombo;
	private ListBox valuesList;
	private TableLayoutPanel tableLayoutPanel2;
	private Button getBtn;
	private Button getExBtn;
	private Button getInfoBtn;
	private Button getInfoExBtn;
	private Button putBtn;
	private Button putExBtn;
	private Button setInfoBtn;
	private Button copyBtn;
}