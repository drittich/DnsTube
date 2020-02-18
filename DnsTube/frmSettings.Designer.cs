namespace DnsTube
{
	partial class frmSettings
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmSettings));
			this.lblEmail = new System.Windows.Forms.Label();
			this.txtEmail = new System.Windows.Forms.TextBox();
			this.lblApiKey = new System.Windows.Forms.Label();
			this.txtApiKey = new System.Windows.Forms.TextBox();
			this.btnSave = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.lblUpdateInterval = new System.Windows.Forms.Label();
			this.txtUpdateInterval = new System.Windows.Forms.TextBox();
			this.chkStartMinimized = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.txtApiToken = new System.Windows.Forms.TextBox();
			this.lblApiToken = new System.Windows.Forms.Label();
			this.rbUseApiKey = new System.Windows.Forms.RadioButton();
			this.rbUseApiToken = new System.Windows.Forms.RadioButton();
			this.label3 = new System.Windows.Forms.Label();
			this.lnkCloudflare = new System.Windows.Forms.LinkLabel();
			this.lblProtocol = new System.Windows.Forms.Label();
			this.rbProtocolIPv4 = new System.Windows.Forms.RadioButton();
			this.rbProtocolIPv6 = new System.Windows.Forms.RadioButton();
			this.rbProtocolIPv4AndIPv6 = new System.Windows.Forms.RadioButton();
			this.panAuth = new System.Windows.Forms.Panel();
			this.panProtocol = new System.Windows.Forms.Panel();
			this.chkNotifyOfUpdates = new System.Windows.Forms.CheckBox();
			this.panAuth.SuspendLayout();
			this.panProtocol.SuspendLayout();
			this.SuspendLayout();
			// 
			// lblEmail
			// 
			this.lblEmail.AutoSize = true;
			this.lblEmail.Location = new System.Drawing.Point(13, 14);
			this.lblEmail.Name = "lblEmail";
			this.lblEmail.Size = new System.Drawing.Size(36, 15);
			this.lblEmail.TabIndex = 0;
			this.lblEmail.Text = "Email";
			// 
			// txtEmail
			// 
			this.txtEmail.Location = new System.Drawing.Point(107, 11);
			this.txtEmail.Name = "txtEmail";
			this.txtEmail.Size = new System.Drawing.Size(340, 23);
			this.txtEmail.TabIndex = 1;
			// 
			// lblApiKey
			// 
			this.lblApiKey.AutoSize = true;
			this.lblApiKey.Location = new System.Drawing.Point(12, 73);
			this.lblApiKey.Name = "lblApiKey";
			this.lblApiKey.Size = new System.Drawing.Size(47, 15);
			this.lblApiKey.TabIndex = 2;
			this.lblApiKey.Text = "API Key";
			// 
			// txtApiKey
			// 
			this.txtApiKey.Location = new System.Drawing.Point(106, 70);
			this.txtApiKey.Name = "txtApiKey";
			this.txtApiKey.Size = new System.Drawing.Size(341, 23);
			this.txtApiKey.TabIndex = 5;
			// 
			// btnSave
			// 
			this.btnSave.Location = new System.Drawing.Point(278, 223);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(75, 24);
			this.btnSave.TabIndex = 8;
			this.btnSave.Text = "Save";
			this.btnSave.UseVisualStyleBackColor = true;
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Location = new System.Drawing.Point(372, 223);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 24);
			this.btnCancel.TabIndex = 9;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// lblUpdateInterval
			// 
			this.lblUpdateInterval.AutoSize = true;
			this.lblUpdateInterval.Location = new System.Drawing.Point(13, 136);
			this.lblUpdateInterval.Name = "lblUpdateInterval";
			this.lblUpdateInterval.Size = new System.Drawing.Size(87, 15);
			this.lblUpdateInterval.TabIndex = 6;
			this.lblUpdateInterval.Text = "Update Interval";
			// 
			// txtUpdateInterval
			// 
			this.txtUpdateInterval.Location = new System.Drawing.Point(106, 133);
			this.txtUpdateInterval.MaxLength = 4;
			this.txtUpdateInterval.Name = "txtUpdateInterval";
			this.txtUpdateInterval.Size = new System.Drawing.Size(109, 23);
			this.txtUpdateInterval.TabIndex = 6;
			this.txtUpdateInterval.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtUpdateInterval_KeyPress);
			// 
			// chkStartMinimized
			// 
			this.chkStartMinimized.AutoSize = true;
			this.chkStartMinimized.Location = new System.Drawing.Point(106, 165);
			this.chkStartMinimized.Name = "chkStartMinimized";
			this.chkStartMinimized.Size = new System.Drawing.Size(109, 19);
			this.chkStartMinimized.TabIndex = 7;
			this.chkStartMinimized.Text = "Start minimized";
			this.chkStartMinimized.UseVisualStyleBackColor = true;
			this.chkStartMinimized.CheckedChanged += new System.EventHandler(this.chkStartMinimized_CheckedChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(221, 136);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(58, 15);
			this.label1.TabIndex = 10;
			this.label1.Text = "(minutes)";
			// 
			// txtApiToken
			// 
			this.txtApiToken.Location = new System.Drawing.Point(106, 70);
			this.txtApiToken.Name = "txtApiToken";
			this.txtApiToken.Size = new System.Drawing.Size(341, 23);
			this.txtApiToken.TabIndex = 4;
			// 
			// lblApiToken
			// 
			this.lblApiToken.AutoSize = true;
			this.lblApiToken.Location = new System.Drawing.Point(12, 73);
			this.lblApiToken.Name = "lblApiToken";
			this.lblApiToken.Size = new System.Drawing.Size(59, 15);
			this.lblApiToken.TabIndex = 11;
			this.lblApiToken.Text = "API Token";
			// 
			// rbUseApiKey
			// 
			this.rbUseApiKey.AutoSize = true;
			this.rbUseApiKey.Location = new System.Drawing.Point(107, 9);
			this.rbUseApiKey.Name = "rbUseApiKey";
			this.rbUseApiKey.Size = new System.Drawing.Size(65, 19);
			this.rbUseApiKey.TabIndex = 2;
			this.rbUseApiKey.TabStop = true;
			this.rbUseApiKey.Text = "Use key";
			this.rbUseApiKey.UseVisualStyleBackColor = true;
			this.rbUseApiKey.CheckedChanged += new System.EventHandler(this.rbUseApiKeyOrToken_CheckedChanged);
			// 
			// rbUseApiToken
			// 
			this.rbUseApiToken.AutoSize = true;
			this.rbUseApiToken.Location = new System.Drawing.Point(188, 9);
			this.rbUseApiToken.Name = "rbUseApiToken";
			this.rbUseApiToken.Size = new System.Drawing.Size(77, 19);
			this.rbUseApiToken.TabIndex = 3;
			this.rbUseApiToken.TabStop = true;
			this.rbUseApiToken.Text = "Use token";
			this.rbUseApiToken.UseVisualStyleBackColor = true;
			this.rbUseApiToken.CheckedChanged += new System.EventHandler(this.rbUseApiKeyOrToken_CheckedChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(11, 11);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(79, 15);
			this.label3.TabIndex = 15;
			this.label3.Text = "Authorization";
			// 
			// lnkCloudflare
			// 
			this.lnkCloudflare.AutoSize = true;
			this.lnkCloudflare.Location = new System.Drawing.Point(291, 11);
			this.lnkCloudflare.Name = "lnkCloudflare";
			this.lnkCloudflare.Size = new System.Drawing.Size(126, 15);
			this.lnkCloudflare.TabIndex = 16;
			this.lnkCloudflare.TabStop = true;
			this.lnkCloudflare.Text = "Cloudflare auth config";
			this.lnkCloudflare.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// lblProtocol
			// 
			this.lblProtocol.AutoSize = true;
			this.lblProtocol.Location = new System.Drawing.Point(12, 12);
			this.lblProtocol.Name = "lblProtocol";
			this.lblProtocol.Size = new System.Drawing.Size(52, 15);
			this.lblProtocol.TabIndex = 17;
			this.lblProtocol.Text = "Protocol";
			// 
			// rbProtocolIPv4
			// 
			this.rbProtocolIPv4.AutoSize = true;
			this.rbProtocolIPv4.Location = new System.Drawing.Point(107, 10);
			this.rbProtocolIPv4.Name = "rbProtocolIPv4";
			this.rbProtocolIPv4.Size = new System.Drawing.Size(47, 19);
			this.rbProtocolIPv4.TabIndex = 18;
			this.rbProtocolIPv4.TabStop = true;
			this.rbProtocolIPv4.Tag = "0";
			this.rbProtocolIPv4.Text = "IPv4";
			this.rbProtocolIPv4.UseVisualStyleBackColor = true;
			// 
			// rbProtocolIPv6
			// 
			this.rbProtocolIPv6.AutoSize = true;
			this.rbProtocolIPv6.Location = new System.Drawing.Point(188, 10);
			this.rbProtocolIPv6.Name = "rbProtocolIPv6";
			this.rbProtocolIPv6.Size = new System.Drawing.Size(47, 19);
			this.rbProtocolIPv6.TabIndex = 19;
			this.rbProtocolIPv6.TabStop = true;
			this.rbProtocolIPv6.Tag = "1";
			this.rbProtocolIPv6.Text = "IPv6";
			this.rbProtocolIPv6.UseVisualStyleBackColor = true;
			// 
			// rbProtocolIPv4AndIPv6
			// 
			this.rbProtocolIPv4AndIPv6.AutoSize = true;
			this.rbProtocolIPv4AndIPv6.Location = new System.Drawing.Point(277, 10);
			this.rbProtocolIPv4AndIPv6.Name = "rbProtocolIPv4AndIPv6";
			this.rbProtocolIPv4AndIPv6.Size = new System.Drawing.Size(50, 19);
			this.rbProtocolIPv4AndIPv6.TabIndex = 20;
			this.rbProtocolIPv4AndIPv6.TabStop = true;
			this.rbProtocolIPv4AndIPv6.Tag = "2";
			this.rbProtocolIPv4AndIPv6.Text = "Both";
			this.rbProtocolIPv4AndIPv6.UseVisualStyleBackColor = true;
			// 
			// panAuth
			// 
			this.panAuth.Controls.Add(this.lnkCloudflare);
			this.panAuth.Controls.Add(this.rbUseApiKey);
			this.panAuth.Controls.Add(this.rbUseApiToken);
			this.panAuth.Controls.Add(this.label3);
			this.panAuth.Location = new System.Drawing.Point(1, 32);
			this.panAuth.Name = "panAuth";
			this.panAuth.Size = new System.Drawing.Size(468, 42);
			this.panAuth.TabIndex = 21;
			// 
			// panProtocol
			// 
			this.panProtocol.Controls.Add(this.rbProtocolIPv4AndIPv6);
			this.panProtocol.Controls.Add(this.lblProtocol);
			this.panProtocol.Controls.Add(this.rbProtocolIPv4);
			this.panProtocol.Controls.Add(this.rbProtocolIPv6);
			this.panProtocol.Location = new System.Drawing.Point(1, 91);
			this.panProtocol.Name = "panProtocol";
			this.panProtocol.Size = new System.Drawing.Size(468, 42);
			this.panProtocol.TabIndex = 22;
			// 
			// chkNotifyOfUpdates
			// 
			this.chkNotifyOfUpdates.AutoSize = true;
			this.chkNotifyOfUpdates.Location = new System.Drawing.Point(106, 190);
			this.chkNotifyOfUpdates.Name = "chkNotifyOfUpdates";
			this.chkNotifyOfUpdates.Size = new System.Drawing.Size(138, 19);
			this.chkNotifyOfUpdates.TabIndex = 23;
			this.chkNotifyOfUpdates.Text = "Notify about updates";
			this.chkNotifyOfUpdates.UseVisualStyleBackColor = true;
			// 
			// frmSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(467, 266);
			this.Controls.Add(this.chkNotifyOfUpdates);
			this.Controls.Add(this.txtApiToken);
			this.Controls.Add(this.lblApiToken);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.chkStartMinimized);
			this.Controls.Add(this.txtUpdateInterval);
			this.Controls.Add(this.lblUpdateInterval);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnSave);
			this.Controls.Add(this.txtApiKey);
			this.Controls.Add(this.lblApiKey);
			this.Controls.Add(this.txtEmail);
			this.Controls.Add(this.lblEmail);
			this.Controls.Add(this.panAuth);
			this.Controls.Add(this.panProtocol);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "frmSettings";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Settings";
			this.Load += new System.EventHandler(this.frmSettings_Load);
			this.panAuth.ResumeLayout(false);
			this.panAuth.PerformLayout();
			this.panProtocol.ResumeLayout(false);
			this.panProtocol.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label lblEmail;
		private System.Windows.Forms.TextBox txtEmail;
		private System.Windows.Forms.Label lblApiKey;
		private System.Windows.Forms.TextBox txtApiKey;
		private System.Windows.Forms.Button btnSave;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label lblUpdateInterval;
		private System.Windows.Forms.TextBox txtUpdateInterval;
		private System.Windows.Forms.CheckBox chkStartMinimized;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtApiToken;
		private System.Windows.Forms.Label lblApiToken;
		private System.Windows.Forms.RadioButton rbUseApiKey;
		private System.Windows.Forms.RadioButton rbUseApiToken;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.LinkLabel lnkCloudflare;
		private System.Windows.Forms.Label lblProtocol;
		private System.Windows.Forms.RadioButton rbProtocolIPv4;
		private System.Windows.Forms.RadioButton rbProtocolIPv6;
		private System.Windows.Forms.RadioButton rbProtocolIPv4AndIPv6;
		private System.Windows.Forms.Panel panAuth;
		private System.Windows.Forms.Panel panProtocol;
		private System.Windows.Forms.CheckBox chkNotifyOfUpdates;
	}
}