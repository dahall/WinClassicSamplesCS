
namespace DuckingMediaPlayer
{
	partial class DuckingMediaPlayerSample
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DuckingMediaPlayerSample));
			this.label1 = new System.Windows.Forms.Label();
			this.IDC_EDIT_FILENAME = new System.Windows.Forms.TextBox();
			this.IDC_BUTTON_BROWSE = new System.Windows.Forms.Button();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.IDC_CHECK_MUTE = new System.Windows.Forms.CheckBox();
			this.label4 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.IDC_SLIDER_VOLUME = new System.Windows.Forms.TrackBar();
			this.label2 = new System.Windows.Forms.Label();
			this.IDC_CHECK_DUCKING_OPT_OUT = new System.Windows.Forms.CheckBox();
			this.IDC_CHECK_PAUSE_ON_DUCK = new System.Windows.Forms.CheckBox();
			this.IDC_SLIDER_PLAYBACKPOS = new System.Windows.Forms.TrackBar();
			this.IDC_BUTTON_STOP = new System.Windows.Forms.Button();
			this.IDC_BUTTON_PAUSE = new System.Windows.Forms.Button();
			this.IDC_BUTTON_PLAY = new System.Windows.Forms.Button();
			this.IDCANCEL = new System.Windows.Forms.Button();
			this.IDOK = new System.Windows.Forms.Button();
			this.progressTimer = new System.Windows.Forms.Timer(this.components);
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.IDC_SLIDER_VOLUME)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.IDC_SLIDER_PLAYBACKPOS)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(13, 10);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(76, 17);
			this.label1.TabIndex = 0;
			this.label1.Text = "File to play";
			// 
			// IDC_EDIT_FILENAME
			// 
			this.IDC_EDIT_FILENAME.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
			this.IDC_EDIT_FILENAME.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
			this.IDC_EDIT_FILENAME.Location = new System.Drawing.Point(13, 30);
			this.IDC_EDIT_FILENAME.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_EDIT_FILENAME.Name = "IDC_EDIT_FILENAME";
			this.IDC_EDIT_FILENAME.Size = new System.Drawing.Size(511, 22);
			this.IDC_EDIT_FILENAME.TabIndex = 1;
			this.IDC_EDIT_FILENAME.Leave += new System.EventHandler(this.IDC_EDIT_FILENAME_Leave);
			// 
			// IDC_BUTTON_BROWSE
			// 
			this.IDC_BUTTON_BROWSE.Location = new System.Drawing.Point(530, 28);
			this.IDC_BUTTON_BROWSE.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_BUTTON_BROWSE.Name = "IDC_BUTTON_BROWSE";
			this.IDC_BUTTON_BROWSE.Size = new System.Drawing.Size(94, 23);
			this.IDC_BUTTON_BROWSE.TabIndex = 2;
			this.IDC_BUTTON_BROWSE.Text = "Browse...";
			this.IDC_BUTTON_BROWSE.UseVisualStyleBackColor = true;
			this.IDC_BUTTON_BROWSE.Click += new System.EventHandler(this.IDC_BUTTON_BROWSE_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.IDC_CHECK_MUTE);
			this.groupBox1.Controls.Add(this.label4);
			this.groupBox1.Controls.Add(this.label3);
			this.groupBox1.Controls.Add(this.IDC_SLIDER_VOLUME);
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.IDC_CHECK_DUCKING_OPT_OUT);
			this.groupBox1.Controls.Add(this.IDC_CHECK_PAUSE_ON_DUCK);
			this.groupBox1.Controls.Add(this.IDC_SLIDER_PLAYBACKPOS);
			this.groupBox1.Controls.Add(this.IDC_BUTTON_STOP);
			this.groupBox1.Controls.Add(this.IDC_BUTTON_PAUSE);
			this.groupBox1.Controls.Add(this.IDC_BUTTON_PLAY);
			this.groupBox1.Location = new System.Drawing.Point(13, 56);
			this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Padding = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.groupBox1.Size = new System.Drawing.Size(611, 132);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "\"Media Player\" controls";
			// 
			// IDC_CHECK_MUTE
			// 
			this.IDC_CHECK_MUTE.AutoSize = true;
			this.IDC_CHECK_MUTE.Location = new System.Drawing.Point(537, 79);
			this.IDC_CHECK_MUTE.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_CHECK_MUTE.Name = "IDC_CHECK_MUTE";
			this.IDC_CHECK_MUTE.Size = new System.Drawing.Size(61, 21);
			this.IDC_CHECK_MUTE.TabIndex = 10;
			this.IDC_CHECK_MUTE.Text = "Mute";
			this.IDC_CHECK_MUTE.UseVisualStyleBackColor = true;
			this.IDC_CHECK_MUTE.CheckedChanged += new System.EventHandler(this.IDC_CHECK_MUTE_CheckedChanged);
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(493, 104);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(32, 17);
			this.label4.TabIndex = 9;
			this.label4.Text = "100";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(323, 104);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(16, 17);
			this.label3.TabIndex = 8;
			this.label3.Text = "0";
			// 
			// IDC_SLIDER_VOLUME
			// 
			this.IDC_SLIDER_VOLUME.AutoSize = false;
			this.IDC_SLIDER_VOLUME.Location = new System.Drawing.Point(318, 79);
			this.IDC_SLIDER_VOLUME.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_SLIDER_VOLUME.Maximum = 100;
			this.IDC_SLIDER_VOLUME.Name = "IDC_SLIDER_VOLUME";
			this.IDC_SLIDER_VOLUME.Size = new System.Drawing.Size(213, 19);
			this.IDC_SLIDER_VOLUME.TabIndex = 7;
			this.IDC_SLIDER_VOLUME.TickFrequency = 10;
			this.IDC_SLIDER_VOLUME.TickStyle = System.Windows.Forms.TickStyle.None;
			this.IDC_SLIDER_VOLUME.Scroll += new System.EventHandler(this.IDC_SLIDER_VOLUME_Scroll);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(318, 54);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(59, 17);
			this.label2.TabIndex = 6;
			this.label2.Text = "Volume:";
			// 
			// IDC_CHECK_DUCKING_OPT_OUT
			// 
			this.IDC_CHECK_DUCKING_OPT_OUT.AutoSize = true;
			this.IDC_CHECK_DUCKING_OPT_OUT.Location = new System.Drawing.Point(138, 79);
			this.IDC_CHECK_DUCKING_OPT_OUT.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_CHECK_DUCKING_OPT_OUT.Name = "IDC_CHECK_DUCKING_OPT_OUT";
			this.IDC_CHECK_DUCKING_OPT_OUT.Size = new System.Drawing.Size(148, 21);
			this.IDC_CHECK_DUCKING_OPT_OUT.TabIndex = 5;
			this.IDC_CHECK_DUCKING_OPT_OUT.Text = "Opt out of Ducking";
			this.IDC_CHECK_DUCKING_OPT_OUT.UseVisualStyleBackColor = true;
			this.IDC_CHECK_DUCKING_OPT_OUT.CheckedChanged += new System.EventHandler(this.IDC_CHECK_DUCKING_OPT_OUT_CheckedChanged);
			// 
			// IDC_CHECK_PAUSE_ON_DUCK
			// 
			this.IDC_CHECK_PAUSE_ON_DUCK.AutoSize = true;
			this.IDC_CHECK_PAUSE_ON_DUCK.Location = new System.Drawing.Point(6, 79);
			this.IDC_CHECK_PAUSE_ON_DUCK.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_CHECK_PAUSE_ON_DUCK.Name = "IDC_CHECK_PAUSE_ON_DUCK";
			this.IDC_CHECK_PAUSE_ON_DUCK.Size = new System.Drawing.Size(126, 21);
			this.IDC_CHECK_PAUSE_ON_DUCK.TabIndex = 4;
			this.IDC_CHECK_PAUSE_ON_DUCK.Text = "Pause on Duck";
			this.IDC_CHECK_PAUSE_ON_DUCK.UseVisualStyleBackColor = true;
			this.IDC_CHECK_PAUSE_ON_DUCK.CheckedChanged += new System.EventHandler(this.IDC_CHECK_PAUSE_ON_DUCK_CheckedChanged);
			// 
			// IDC_SLIDER_PLAYBACKPOS
			// 
			this.IDC_SLIDER_PLAYBACKPOS.AutoSize = false;
			this.IDC_SLIDER_PLAYBACKPOS.Location = new System.Drawing.Point(7, 22);
			this.IDC_SLIDER_PLAYBACKPOS.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_SLIDER_PLAYBACKPOS.Maximum = 1000;
			this.IDC_SLIDER_PLAYBACKPOS.Name = "IDC_SLIDER_PLAYBACKPOS";
			this.IDC_SLIDER_PLAYBACKPOS.Size = new System.Drawing.Size(595, 25);
			this.IDC_SLIDER_PLAYBACKPOS.TabIndex = 3;
			this.IDC_SLIDER_PLAYBACKPOS.TickFrequency = 10;
			this.IDC_SLIDER_PLAYBACKPOS.TickStyle = System.Windows.Forms.TickStyle.None;
			// 
			// IDC_BUTTON_STOP
			// 
			this.IDC_BUTTON_STOP.Enabled = false;
			this.IDC_BUTTON_STOP.Location = new System.Drawing.Point(206, 51);
			this.IDC_BUTTON_STOP.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_BUTTON_STOP.Name = "IDC_BUTTON_STOP";
			this.IDC_BUTTON_STOP.Size = new System.Drawing.Size(94, 23);
			this.IDC_BUTTON_STOP.TabIndex = 2;
			this.IDC_BUTTON_STOP.Text = "Stop";
			this.IDC_BUTTON_STOP.UseVisualStyleBackColor = true;
			this.IDC_BUTTON_STOP.Click += new System.EventHandler(this.IDC_BUTTON_STOP_Click);
			// 
			// IDC_BUTTON_PAUSE
			// 
			this.IDC_BUTTON_PAUSE.Enabled = false;
			this.IDC_BUTTON_PAUSE.Location = new System.Drawing.Point(106, 51);
			this.IDC_BUTTON_PAUSE.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_BUTTON_PAUSE.Name = "IDC_BUTTON_PAUSE";
			this.IDC_BUTTON_PAUSE.Size = new System.Drawing.Size(94, 23);
			this.IDC_BUTTON_PAUSE.TabIndex = 1;
			this.IDC_BUTTON_PAUSE.Text = "Pause";
			this.IDC_BUTTON_PAUSE.UseVisualStyleBackColor = true;
			this.IDC_BUTTON_PAUSE.Click += new System.EventHandler(this.IDC_BUTTON_PAUSE_Click);
			// 
			// IDC_BUTTON_PLAY
			// 
			this.IDC_BUTTON_PLAY.Enabled = false;
			this.IDC_BUTTON_PLAY.Location = new System.Drawing.Point(6, 51);
			this.IDC_BUTTON_PLAY.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDC_BUTTON_PLAY.Name = "IDC_BUTTON_PLAY";
			this.IDC_BUTTON_PLAY.Size = new System.Drawing.Size(94, 23);
			this.IDC_BUTTON_PLAY.TabIndex = 0;
			this.IDC_BUTTON_PLAY.Text = "Play";
			this.IDC_BUTTON_PLAY.UseVisualStyleBackColor = true;
			this.IDC_BUTTON_PLAY.Click += new System.EventHandler(this.IDC_BUTTON_PLAY_Click);
			// 
			// IDCANCEL
			// 
			this.IDCANCEL.Location = new System.Drawing.Point(530, 193);
			this.IDCANCEL.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDCANCEL.Name = "IDCANCEL";
			this.IDCANCEL.Size = new System.Drawing.Size(94, 23);
			this.IDCANCEL.TabIndex = 4;
			this.IDCANCEL.Text = "Cancel";
			this.IDCANCEL.UseVisualStyleBackColor = true;
			this.IDCANCEL.Click += new System.EventHandler(this.IDOK_Click);
			// 
			// IDOK
			// 
			this.IDOK.Location = new System.Drawing.Point(430, 193);
			this.IDOK.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.IDOK.Name = "IDOK";
			this.IDOK.Size = new System.Drawing.Size(94, 23);
			this.IDOK.TabIndex = 4;
			this.IDOK.Text = "OK";
			this.IDOK.UseVisualStyleBackColor = true;
			this.IDOK.Click += new System.EventHandler(this.IDOK_Click);
			// 
			// progressTimer
			// 
			this.progressTimer.Interval = 40;
			this.progressTimer.Tick += new System.EventHandler(this.progressTimer_Tick);
			// 
			// DuckingMediaPlayerSample
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(636, 226);
			this.Controls.Add(this.IDOK);
			this.Controls.Add(this.IDCANCEL);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.IDC_BUTTON_BROWSE);
			this.Controls.Add(this.IDC_EDIT_FILENAME);
			this.Controls.Add(this.label1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
			this.Name = "DuckingMediaPlayerSample";
			this.Text = "\"Media Player\"";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.IDC_SLIDER_VOLUME)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.IDC_SLIDER_PLAYBACKPOS)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox IDC_EDIT_FILENAME;
		private System.Windows.Forms.Button IDC_BUTTON_BROWSE;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox IDC_CHECK_MUTE;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TrackBar IDC_SLIDER_VOLUME;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox IDC_CHECK_DUCKING_OPT_OUT;
		private System.Windows.Forms.CheckBox IDC_CHECK_PAUSE_ON_DUCK;
		private System.Windows.Forms.TrackBar IDC_SLIDER_PLAYBACKPOS;
		private System.Windows.Forms.Button IDC_BUTTON_STOP;
		private System.Windows.Forms.Button IDC_BUTTON_PAUSE;
		private System.Windows.Forms.Button IDC_BUTTON_PLAY;
		private System.Windows.Forms.Button IDCANCEL;
		private System.Windows.Forms.Button IDOK;
		private System.Windows.Forms.Timer progressTimer;
	}
}

