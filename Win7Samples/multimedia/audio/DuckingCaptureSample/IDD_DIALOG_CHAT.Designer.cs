
namespace DuckingCaptureSample
{
	partial class IDD_DIALOG_CHAT
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IDD_DIALOG_CHAT));
			this.IDC_COMBO_CHAT_TRANSPORT = new System.Windows.Forms.ComboBox();
			this.IDC_RADIO_CAPTURE = new System.Windows.Forms.RadioButton();
			this.IDC_RADIO_RENDER = new System.Windows.Forms.RadioButton();
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER = new System.Windows.Forms.CheckBox();
			this.IDC_CHATSTART = new System.Windows.Forms.Button();
			this.IDC_CHATSTOP = new System.Windows.Forms.Button();
			this.IDOK = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// IDC_COMBO_CHAT_TRANSPORT
			// 
			this.IDC_COMBO_CHAT_TRANSPORT.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.IDC_COMBO_CHAT_TRANSPORT.FormattingEnabled = true;
			this.IDC_COMBO_CHAT_TRANSPORT.Location = new System.Drawing.Point(6, 27);
			this.IDC_COMBO_CHAT_TRANSPORT.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.IDC_COMBO_CHAT_TRANSPORT.Name = "IDC_COMBO_CHAT_TRANSPORT";
			this.IDC_COMBO_CHAT_TRANSPORT.Size = new System.Drawing.Size(259, 27);
			this.IDC_COMBO_CHAT_TRANSPORT.TabIndex = 0;
			this.IDC_COMBO_CHAT_TRANSPORT.SelectedIndexChanged += new System.EventHandler(this.IDC_COMBO_CHAT_TRANSPORT_SelectedIndexChanged);
			// 
			// IDC_RADIO_CAPTURE
			// 
			this.IDC_RADIO_CAPTURE.AutoSize = true;
			this.IDC_RADIO_CAPTURE.Checked = true;
			this.IDC_RADIO_CAPTURE.Location = new System.Drawing.Point(6, 27);
			this.IDC_RADIO_CAPTURE.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.IDC_RADIO_CAPTURE.Name = "IDC_RADIO_CAPTURE";
			this.IDC_RADIO_CAPTURE.Size = new System.Drawing.Size(177, 23);
			this.IDC_RADIO_CAPTURE.TabIndex = 1;
			this.IDC_RADIO_CAPTURE.TabStop = true;
			this.IDC_RADIO_CAPTURE.Text = "Use default input device";
			this.IDC_RADIO_CAPTURE.UseVisualStyleBackColor = true;
			this.IDC_RADIO_CAPTURE.CheckedChanged += new System.EventHandler(this.IDC_RADIO_CAPTURE_CheckedChanged);
			// 
			// IDC_RADIO_RENDER
			// 
			this.IDC_RADIO_RENDER.AutoSize = true;
			this.IDC_RADIO_RENDER.Location = new System.Drawing.Point(6, 58);
			this.IDC_RADIO_RENDER.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.IDC_RADIO_RENDER.Name = "IDC_RADIO_RENDER";
			this.IDC_RADIO_RENDER.Size = new System.Drawing.Size(187, 23);
			this.IDC_RADIO_RENDER.TabIndex = 2;
			this.IDC_RADIO_RENDER.Text = "Use default output device";
			this.IDC_RADIO_RENDER.UseVisualStyleBackColor = true;
			this.IDC_RADIO_RENDER.CheckedChanged += new System.EventHandler(this.IDC_RADIO_CAPTURE_CheckedChanged);
			// 
			// IDC_CHECK_HIDE_FROM_VOLUME_MIXER
			// 
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.AutoSize = true;
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Location = new System.Drawing.Point(27, 89);
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Name = "IDC_CHECK_HIDE_FROM_VOLUME_MIXER";
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Size = new System.Drawing.Size(208, 23);
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.TabIndex = 3;
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Text = "Hide chat from volume mixer";
			this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER.UseVisualStyleBackColor = true;
			// 
			// IDC_CHATSTART
			// 
			this.IDC_CHATSTART.Location = new System.Drawing.Point(12, 149);
			this.IDC_CHATSTART.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.IDC_CHATSTART.Name = "IDC_CHATSTART";
			this.IDC_CHATSTART.Size = new System.Drawing.Size(98, 27);
			this.IDC_CHATSTART.TabIndex = 4;
			this.IDC_CHATSTART.Text = "Start Chat";
			this.IDC_CHATSTART.UseVisualStyleBackColor = true;
			this.IDC_CHATSTART.Click += new System.EventHandler(this.IDC_CHATSTART_Click);
			// 
			// IDC_CHATSTOP
			// 
			this.IDC_CHATSTOP.Location = new System.Drawing.Point(116, 149);
			this.IDC_CHATSTOP.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.IDC_CHATSTOP.Name = "IDC_CHATSTOP";
			this.IDC_CHATSTOP.Size = new System.Drawing.Size(94, 27);
			this.IDC_CHATSTOP.TabIndex = 5;
			this.IDC_CHATSTOP.Text = "Stop Chat";
			this.IDC_CHATSTOP.UseVisualStyleBackColor = true;
			this.IDC_CHATSTOP.Click += new System.EventHandler(this.IDC_CHATSTOP_Click);
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(470, 149);
			this.IDOK.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(75, 27);
			this.IDOK.TabIndex = 6;
			this.IDOK.Text = "Exit";
			this.IDOK.UseVisualStyleBackColor = true;
			this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.IDC_COMBO_CHAT_TRANSPORT);
			this.groupBox1.Location = new System.Drawing.Point(12, 14);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.groupBox1.Size = new System.Drawing.Size(271, 81);
			this.groupBox1.TabIndex = 7;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "API";
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.IDC_RADIO_CAPTURE);
			this.groupBox2.Controls.Add(this.IDC_RADIO_RENDER);
			this.groupBox2.Controls.Add(this.IDC_CHECK_HIDE_FROM_VOLUME_MIXER);
			this.groupBox2.Location = new System.Drawing.Point(289, 14);
			this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.groupBox2.Size = new System.Drawing.Size(256, 127);
			this.groupBox2.TabIndex = 8;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Device options";
			// 
			// IDD_DIALOG_CHAT
			// 
			this.AcceptButton = this.IDOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(558, 187);
			this.Controls.Add(this.groupBox2);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.IDC_CHATSTOP);
			this.Controls.Add(this.IDC_CHATSTART);
			this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.Name = "IDD_DIALOG_CHAT";
			this.Text = "\"Chat\" Demo";
			this.Load += new System.EventHandler(this.IDD_DIALOG_CHAT_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox IDC_COMBO_CHAT_TRANSPORT;
		private System.Windows.Forms.RadioButton IDC_RADIO_CAPTURE;
		private System.Windows.Forms.RadioButton IDC_RADIO_RENDER;
		private System.Windows.Forms.CheckBox IDC_CHECK_HIDE_FROM_VOLUME_MIXER;
		private System.Windows.Forms.Button IDC_CHATSTART;
		private System.Windows.Forms.Button IDC_CHATSTOP;
		private System.Windows.Forms.Button IDOK;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
	}
}

