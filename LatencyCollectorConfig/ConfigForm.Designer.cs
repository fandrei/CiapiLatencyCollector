namespace LatencyCollectorConfig
{
	partial class ConfigForm
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
			this.OkButton = new System.Windows.Forms.Button();
			this.StreamingUrlEdit = new System.Windows.Forms.TextBox();
			this.label4 = new System.Windows.Forms.Label();
			this.ServerUrlEdit = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.PasswordEdit = new System.Windows.Forms.TextBox();
			this.UserNameEdit = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.DataPollingRateEdit = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// OkButton
			// 
			this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.OkButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.OkButton.Location = new System.Drawing.Point(440, 159);
			this.OkButton.Name = "OkButton";
			this.OkButton.Size = new System.Drawing.Size(75, 23);
			this.OkButton.TabIndex = 4;
			this.OkButton.Text = "OK";
			this.OkButton.UseVisualStyleBackColor = true;
			this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
			// 
			// StreamingUrlEdit
			// 
			this.StreamingUrlEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.StreamingUrlEdit.Location = new System.Drawing.Point(98, 33);
			this.StreamingUrlEdit.Name = "StreamingUrlEdit";
			this.StreamingUrlEdit.Size = new System.Drawing.Size(414, 20);
			this.StreamingUrlEdit.TabIndex = 1;
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(12, 35);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(70, 13);
			this.label4.TabIndex = 26;
			this.label4.Text = "Streaming Url";
			// 
			// ServerUrlEdit
			// 
			this.ServerUrlEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.ServerUrlEdit.Location = new System.Drawing.Point(98, 7);
			this.ServerUrlEdit.Name = "ServerUrlEdit";
			this.ServerUrlEdit.Size = new System.Drawing.Size(414, 20);
			this.ServerUrlEdit.TabIndex = 0;
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(12, 9);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(54, 13);
			this.label3.TabIndex = 25;
			this.label3.Text = "Server Url";
			// 
			// PasswordEdit
			// 
			this.PasswordEdit.Location = new System.Drawing.Point(100, 93);
			this.PasswordEdit.Name = "PasswordEdit";
			this.PasswordEdit.PasswordChar = '*';
			this.PasswordEdit.Size = new System.Drawing.Size(249, 20);
			this.PasswordEdit.TabIndex = 3;
			// 
			// UserNameEdit
			// 
			this.UserNameEdit.Location = new System.Drawing.Point(100, 67);
			this.UserNameEdit.Name = "UserNameEdit";
			this.UserNameEdit.Size = new System.Drawing.Size(249, 20);
			this.UserNameEdit.TabIndex = 2;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(14, 93);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(53, 13);
			this.label2.TabIndex = 30;
			this.label2.Text = "Password";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 67);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(58, 13);
			this.label1.TabIndex = 29;
			this.label1.Text = "User name";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(12, 128);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(70, 34);
			this.label5.TabIndex = 32;
			this.label5.Text = "Data polling rate (per min)";
			// 
			// DataPollingRateEdit
			// 
			this.DataPollingRateEdit.Location = new System.Drawing.Point(100, 128);
			this.DataPollingRateEdit.Name = "DataPollingRateEdit";
			this.DataPollingRateEdit.Size = new System.Drawing.Size(249, 20);
			this.DataPollingRateEdit.TabIndex = 33;
			// 
			// ConfigForm
			// 
			this.AcceptButton = this.OkButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(527, 194);
			this.Controls.Add(this.DataPollingRateEdit);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.PasswordEdit);
			this.Controls.Add(this.UserNameEdit);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.OkButton);
			this.Controls.Add(this.StreamingUrlEdit);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.ServerUrlEdit);
			this.Controls.Add(this.label3);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ConfigForm";
			this.ShowIcon = false;
			this.Text = "ConfigForm";
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button OkButton;
		private System.Windows.Forms.TextBox StreamingUrlEdit;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.TextBox ServerUrlEdit;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox PasswordEdit;
		private System.Windows.Forms.TextBox UserNameEdit;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox DataPollingRateEdit;
	}
}