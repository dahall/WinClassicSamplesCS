namespace Sampler;

partial class NuGetPackages
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
		listBox1 = new System.Windows.Forms.ListBox();
		listBox2 = new System.Windows.Forms.ListBox();
		splitContainer1 = new System.Windows.Forms.SplitContainer();
		splitContainer2 = new System.Windows.Forms.SplitContainer();
		listBox3 = new System.Windows.Forms.ListBox();
		((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
		splitContainer1.Panel1.SuspendLayout();
		splitContainer1.Panel2.SuspendLayout();
		splitContainer1.SuspendLayout();
		((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
		splitContainer2.Panel1.SuspendLayout();
		splitContainer2.Panel2.SuspendLayout();
		splitContainer2.SuspendLayout();
		SuspendLayout();
		// 
		// listBox1
		// 
		listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
		listBox1.FormattingEnabled = true;
		listBox1.ItemHeight = 25;
		listBox1.Items.AddRange(new object[] { "Loading..." });
		listBox1.Location = new System.Drawing.Point(0, 0);
		listBox1.Name = "listBox1";
		listBox1.Size = new System.Drawing.Size(282, 654);
		listBox1.TabIndex = 0;
		listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;
		// 
		// listBox2
		// 
		listBox2.Dock = System.Windows.Forms.DockStyle.Fill;
		listBox2.FormattingEnabled = true;
		listBox2.ItemHeight = 25;
		listBox2.Location = new System.Drawing.Point(0, 0);
		listBox2.Name = "listBox2";
		listBox2.Size = new System.Drawing.Size(187, 654);
		listBox2.TabIndex = 1;
		listBox2.SelectedIndexChanged += listBox2_SelectedIndexChanged;
		// 
		// splitContainer1
		// 
		splitContainer1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		splitContainer1.Location = new System.Drawing.Point(12, 12);
		splitContainer1.Name = "splitContainer1";
		// 
		// splitContainer1.Panel1
		// 
		splitContainer1.Panel1.Controls.Add(listBox1);
		// 
		// splitContainer1.Panel2
		// 
		splitContainer1.Panel2.Controls.Add(splitContainer2);
		splitContainer1.Size = new System.Drawing.Size(848, 654);
		splitContainer1.SplitterDistance = 282;
		splitContainer1.TabIndex = 2;
		// 
		// splitContainer2
		// 
		splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
		splitContainer2.Location = new System.Drawing.Point(0, 0);
		splitContainer2.Name = "splitContainer2";
		// 
		// splitContainer2.Panel1
		// 
		splitContainer2.Panel1.Controls.Add(listBox2);
		// 
		// splitContainer2.Panel2
		// 
		splitContainer2.Panel2.Controls.Add(listBox3);
		splitContainer2.Size = new System.Drawing.Size(562, 654);
		splitContainer2.SplitterDistance = 187;
		splitContainer2.TabIndex = 0;
		// 
		// listBox3
		// 
		listBox3.Dock = System.Windows.Forms.DockStyle.Fill;
		listBox3.FormattingEnabled = true;
		listBox3.ItemHeight = 25;
		listBox3.Location = new System.Drawing.Point(0, 0);
		listBox3.Name = "listBox3";
		listBox3.Size = new System.Drawing.Size(371, 654);
		listBox3.TabIndex = 2;
		// 
		// NuGetPackages
		// 
		AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		ClientSize = new System.Drawing.Size(872, 678);
		Controls.Add(splitContainer1);
		FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		Name = "NuGetPackages";
		Text = "NuGetPackages";
		splitContainer1.Panel1.ResumeLayout(false);
		splitContainer1.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
		splitContainer1.ResumeLayout(false);
		splitContainer2.Panel1.ResumeLayout(false);
		splitContainer2.Panel2.ResumeLayout(false);
		((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
		splitContainer2.ResumeLayout(false);
		ResumeLayout(false);
	}

	#endregion

	private System.Windows.Forms.ListBox listBox1;
	private System.Windows.Forms.ListBox listBox2;
	private System.Windows.Forms.SplitContainer splitContainer1;
	private System.Windows.Forms.SplitContainer splitContainer2;
	private System.Windows.Forms.ListBox listBox3;
}