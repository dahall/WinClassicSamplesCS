namespace ADQI;

partial class ADsContainerDlg
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
		groupBox1 = new GroupBox();
		filterText = new TextBox();
		setBtn = new Button();
		label1 = new Label();
		closeBtn = new Button();
		label2 = new Label();
		childList = new ListBox();
		viewBtn = new Button();
		deleteBtn = new Button();
		renameBtn = new Button();
		moveBtn = new Button();
		createBtn = new Button();
		groupBox1.SuspendLayout();
		SuspendLayout();
		// 
		// groupBox1
		// 
		groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		groupBox1.Controls.Add(filterText);
		groupBox1.Controls.Add(setBtn);
		groupBox1.Controls.Add(label1);
		groupBox1.Location = new Point(12, 8);
		groupBox1.Name = "groupBox1";
		groupBox1.Size = new Size(293, 76);
		groupBox1.TabIndex = 0;
		groupBox1.TabStop = false;
		groupBox1.Text = "Class Filter";
		// 
		// filterText
		// 
		filterText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		filterText.Location = new Point(8, 38);
		filterText.Name = "filterText";
		filterText.Size = new Size(205, 23);
		filterText.TabIndex = 2;
		// 
		// setBtn
		// 
		setBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		setBtn.Location = new Point(219, 37);
		setBtn.Name = "setBtn";
		setBtn.Size = new Size(68, 23);
		setBtn.TabIndex = 1;
		setBtn.Text = "Set";
		setBtn.UseVisualStyleBackColor = true;
		setBtn.Click += setBtn_Click;
		// 
		// label1
		// 
		label1.AutoSize = true;
		label1.Location = new Point(8, 19);
		label1.Name = "label1";
		label1.Size = new Size(234, 15);
		label1.TabIndex = 0;
		label1.Text = "(type object class(es), separated by comma";
		// 
		// closeBtn
		// 
		closeBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		closeBtn.Location = new Point(311, 12);
		closeBtn.Name = "closeBtn";
		closeBtn.Size = new Size(75, 23);
		closeBtn.TabIndex = 1;
		closeBtn.Text = "Close";
		closeBtn.UseVisualStyleBackColor = true;
		closeBtn.Click += closeBtn_Click;
		// 
		// label2
		// 
		label2.AutoSize = true;
		label2.Location = new Point(12, 87);
		label2.Name = "label2";
		label2.Size = new Size(263, 15);
		label2.TabIndex = 2;
		label2.Text = "Children Objects: (double click to view the child)";
		// 
		// childList
		// 
		childList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
		childList.FormattingEnabled = true;
		childList.ItemHeight = 15;
		childList.Location = new Point(12, 105);
		childList.Name = "childList";
		childList.Size = new Size(293, 229);
		childList.TabIndex = 3;
		childList.SelectedIndexChanged += childList_SelectedIndexChanged;
		childList.DoubleClick += childList_DoubleClick;
		// 
		// viewBtn
		// 
		viewBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
		viewBtn.Location = new Point(43, 340);
		viewBtn.Name = "viewBtn";
		viewBtn.Size = new Size(75, 23);
		viewBtn.TabIndex = 4;
		viewBtn.Text = "View";
		viewBtn.UseVisualStyleBackColor = true;
		viewBtn.Click += viewBtn_Click;
		// 
		// deleteBtn
		// 
		deleteBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		deleteBtn.Location = new Point(311, 83);
		deleteBtn.Name = "deleteBtn";
		deleteBtn.Size = new Size(75, 23);
		deleteBtn.TabIndex = 1;
		deleteBtn.Text = "Delete";
		deleteBtn.UseVisualStyleBackColor = true;
		deleteBtn.Click += deleteBtn_Click;
		// 
		// renameBtn
		// 
		renameBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		renameBtn.Location = new Point(311, 112);
		renameBtn.Name = "renameBtn";
		renameBtn.Size = new Size(75, 23);
		renameBtn.TabIndex = 1;
		renameBtn.Text = "Rename...";
		renameBtn.UseVisualStyleBackColor = true;
		renameBtn.Click += renameBtn_Click;
		// 
		// moveBtn
		// 
		moveBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		moveBtn.Location = new Point(311, 141);
		moveBtn.Name = "moveBtn";
		moveBtn.Size = new Size(75, 23);
		moveBtn.TabIndex = 1;
		moveBtn.Text = "Move";
		moveBtn.UseVisualStyleBackColor = true;
		moveBtn.Click += moveBtn_Click;
		// 
		// createBtn
		// 
		createBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
		createBtn.Enabled = false;
		createBtn.Location = new Point(311, 170);
		createBtn.Name = "createBtn";
		createBtn.Size = new Size(75, 23);
		createBtn.TabIndex = 1;
		createBtn.Text = "Create...";
		createBtn.UseVisualStyleBackColor = true;
		// 
		// ADsContainerDlg
		// 
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(398, 369);
		Controls.Add(viewBtn);
		Controls.Add(childList);
		Controls.Add(label2);
		Controls.Add(createBtn);
		Controls.Add(moveBtn);
		Controls.Add(renameBtn);
		Controls.Add(deleteBtn);
		Controls.Add(closeBtn);
		Controls.Add(groupBox1);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		Name = "ADsContainerDlg";
		Text = "ADsContainer";
		groupBox1.ResumeLayout(false);
		groupBox1.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private GroupBox groupBox1;
	private TextBox filterText;
	private Button setBtn;
	private Label label1;
	private Button closeBtn;
	private Label label2;
	private ListBox childList;
	private Button viewBtn;
	private Button deleteBtn;
	private Button renameBtn;
	private Button moveBtn;
	private Button createBtn;
}