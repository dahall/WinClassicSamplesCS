namespace DesktopToastsSample
{
	partial class DesktopToastsApp
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
			this.ShowToastButton = new System.Windows.Forms.Button();
			this.Output = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// ShowToastButton
			// 
			this.ShowToastButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.ShowToastButton.Location = new System.Drawing.Point(149, 114);
			this.ShowToastButton.Name = "ShowToastButton";
			this.ShowToastButton.Size = new System.Drawing.Size(103, 23);
			this.ShowToastButton.TabIndex = 0;
			this.ShowToastButton.Text = "View Text Toast";
			this.ShowToastButton.UseVisualStyleBackColor = true;
			this.ShowToastButton.Click += new System.EventHandler(this.DisplayToast);
			// 
			// Output
			// 
			this.Output.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.Output.Location = new System.Drawing.Point(12, 12);
			this.Output.Multiline = true;
			this.Output.Name = "Output";
			this.Output.Size = new System.Drawing.Size(239, 96);
			this.Output.TabIndex = 1;
			this.Output.Text = "Whatever action you take on the displayed toast will be shown here.";
			// 
			// DesktopToastsApp
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(264, 148);
			this.Controls.Add(this.Output);
			this.Controls.Add(this.ShowToastButton);
			this.Name = "DesktopToastsApp";
			this.Text = "Desktop Toasts Demo App";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button ShowToastButton;
		private System.Windows.Forms.TextBox Output;
	}
}

