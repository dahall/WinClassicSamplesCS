namespace MagnificationFullscreen
{
	partial class FullscreenMagnifierSample
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
			this.IDC_STATIC = new System.Windows.Forms.Label();
			this.IDC_RADIO_100 = new System.Windows.Forms.RadioButton();
			this.IDC_RADIO_200 = new System.Windows.Forms.RadioButton();
			this.IDC_RADIO_300 = new System.Windows.Forms.RadioButton();
			this.IDC_RADIO_400 = new System.Windows.Forms.RadioButton();
			this.IDC_CHECK_SETGRAYSCALE = new System.Windows.Forms.CheckBox();
			this.IDC_CHECK_SETINPUTTRANSFORM = new System.Windows.Forms.CheckBox();
			this.IDC_BUTTON_GETSETTINGS = new System.Windows.Forms.Button();
			this.IDC_CLOSE = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// IDC_STATIC
			// 
			this.IDC_STATIC.AutoSize = true;
			this.IDC_STATIC.Location = new System.Drawing.Point(12, 14);
			this.IDC_STATIC.Name = "IDC_STATIC";
			this.IDC_STATIC.Size = new System.Drawing.Size(198, 15);
			this.IDC_STATIC.TabIndex = 0;
			this.IDC_STATIC.Text = "Set the current magnification factor:";
			// 
			// IDC_RADIO_100
			// 
			this.IDC_RADIO_100.AutoSize = true;
			this.IDC_RADIO_100.Checked = true;
			this.IDC_RADIO_100.Location = new System.Drawing.Point(13, 32);
			this.IDC_RADIO_100.Name = "IDC_RADIO_100";
			this.IDC_RADIO_100.Size = new System.Drawing.Size(53, 19);
			this.IDC_RADIO_100.TabIndex = 1;
			this.IDC_RADIO_100.TabStop = true;
			this.IDC_RADIO_100.Text = "&100%";
			this.IDC_RADIO_100.UseVisualStyleBackColor = true;
			this.IDC_RADIO_100.Click += new System.EventHandler(this.HandleCommand);
			// 
			// IDC_RADIO_200
			// 
			this.IDC_RADIO_200.AutoSize = true;
			this.IDC_RADIO_200.Location = new System.Drawing.Point(72, 32);
			this.IDC_RADIO_200.Name = "IDC_RADIO_200";
			this.IDC_RADIO_200.Size = new System.Drawing.Size(53, 19);
			this.IDC_RADIO_200.TabIndex = 1;
			this.IDC_RADIO_200.Text = "&200%";
			this.IDC_RADIO_200.UseVisualStyleBackColor = true;
			this.IDC_RADIO_200.Click += new System.EventHandler(this.HandleCommand);
			// 
			// IDC_RADIO_300
			// 
			this.IDC_RADIO_300.AutoSize = true;
			this.IDC_RADIO_300.Location = new System.Drawing.Point(131, 32);
			this.IDC_RADIO_300.Name = "IDC_RADIO_300";
			this.IDC_RADIO_300.Size = new System.Drawing.Size(53, 19);
			this.IDC_RADIO_300.TabIndex = 1;
			this.IDC_RADIO_300.Text = "&300%";
			this.IDC_RADIO_300.UseVisualStyleBackColor = true;
			this.IDC_RADIO_300.Click += new System.EventHandler(this.HandleCommand);
			// 
			// IDC_RADIO_400
			// 
			this.IDC_RADIO_400.AutoSize = true;
			this.IDC_RADIO_400.Location = new System.Drawing.Point(190, 32);
			this.IDC_RADIO_400.Name = "IDC_RADIO_400";
			this.IDC_RADIO_400.Size = new System.Drawing.Size(53, 19);
			this.IDC_RADIO_400.TabIndex = 1;
			this.IDC_RADIO_400.Text = "&400%";
			this.IDC_RADIO_400.UseVisualStyleBackColor = true;
			this.IDC_RADIO_400.Click += new System.EventHandler(this.HandleCommand);
			// 
			// IDC_CHECK_SETGRAYSCALE
			// 
			this.IDC_CHECK_SETGRAYSCALE.AutoSize = true;
			this.IDC_CHECK_SETGRAYSCALE.Location = new System.Drawing.Point(12, 57);
			this.IDC_CHECK_SETGRAYSCALE.Name = "IDC_CHECK_SETGRAYSCALE";
			this.IDC_CHECK_SETGRAYSCALE.Size = new System.Drawing.Size(109, 19);
			this.IDC_CHECK_SETGRAYSCALE.TabIndex = 2;
			this.IDC_CHECK_SETGRAYSCALE.Text = "&Apply grayscale";
			this.IDC_CHECK_SETGRAYSCALE.UseVisualStyleBackColor = true;
			this.IDC_CHECK_SETGRAYSCALE.Click += new System.EventHandler(this.HandleCommand);
			// 
			// IDC_CHECK_SETINPUTTRANSFORM
			// 
			this.IDC_CHECK_SETINPUTTRANSFORM.AutoSize = true;
			this.IDC_CHECK_SETINPUTTRANSFORM.Location = new System.Drawing.Point(12, 82);
			this.IDC_CHECK_SETINPUTTRANSFORM.Name = "IDC_CHECK_SETINPUTTRANSFORM";
			this.IDC_CHECK_SETINPUTTRANSFORM.Size = new System.Drawing.Size(128, 19);
			this.IDC_CHECK_SETINPUTTRANSFORM.TabIndex = 2;
			this.IDC_CHECK_SETINPUTTRANSFORM.Text = "&Set input transform";
			this.IDC_CHECK_SETINPUTTRANSFORM.UseVisualStyleBackColor = true;
			this.IDC_CHECK_SETINPUTTRANSFORM.Click += new System.EventHandler(this.HandleCommand);
			// 
			// IDC_BUTTON_GETSETTINGS
			// 
			this.IDC_BUTTON_GETSETTINGS.Location = new System.Drawing.Point(12, 107);
			this.IDC_BUTTON_GETSETTINGS.Name = "IDC_BUTTON_GETSETTINGS";
			this.IDC_BUTTON_GETSETTINGS.Size = new System.Drawing.Size(152, 23);
			this.IDC_BUTTON_GETSETTINGS.TabIndex = 3;
			this.IDC_BUTTON_GETSETTINGS.Text = "&Get the current settings";
			this.IDC_BUTTON_GETSETTINGS.UseVisualStyleBackColor = true;
			this.IDC_BUTTON_GETSETTINGS.Click += new System.EventHandler(this.HandleCommand);
			// 
			// IDC_CLOSE
			// 
			this.IDC_CLOSE.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.IDC_CLOSE.Location = new System.Drawing.Point(190, 147);
			this.IDC_CLOSE.Name = "IDC_CLOSE";
			this.IDC_CLOSE.Size = new System.Drawing.Size(75, 23);
			this.IDC_CLOSE.TabIndex = 4;
			this.IDC_CLOSE.Text = "&Close";
			this.IDC_CLOSE.UseVisualStyleBackColor = true;
			this.IDC_CLOSE.Click += new System.EventHandler(this.HandleCommand);
			// 
			// FullscreenMagnifierSample
			// 
			this.AcceptButton = this.IDC_CLOSE;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(277, 182);
			this.Controls.Add(this.IDC_CLOSE);
			this.Controls.Add(this.IDC_BUTTON_GETSETTINGS);
			this.Controls.Add(this.IDC_CHECK_SETINPUTTRANSFORM);
			this.Controls.Add(this.IDC_CHECK_SETGRAYSCALE);
			this.Controls.Add(this.IDC_RADIO_400);
			this.Controls.Add(this.IDC_RADIO_300);
			this.Controls.Add(this.IDC_RADIO_200);
			this.Controls.Add(this.IDC_RADIO_100);
			this.Controls.Add(this.IDC_STATIC);
			this.Name = "FullscreenMagnifierSample";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Fullscreen Magnifier Sample";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label IDC_STATIC;
		private System.Windows.Forms.RadioButton IDC_RADIO_100;
		private System.Windows.Forms.RadioButton IDC_RADIO_200;
		private System.Windows.Forms.RadioButton IDC_RADIO_300;
		private System.Windows.Forms.RadioButton IDC_RADIO_400;
		private System.Windows.Forms.CheckBox IDC_CHECK_SETGRAYSCALE;
		private System.Windows.Forms.CheckBox IDC_CHECK_SETINPUTTRANSFORM;
		private System.Windows.Forms.Button IDC_BUTTON_GETSETTINGS;
		private System.Windows.Forms.Button IDC_CLOSE;
	}
}