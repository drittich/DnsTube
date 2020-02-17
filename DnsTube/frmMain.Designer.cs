namespace DnsTube
{
	partial class frmMain
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
			this.btnUpdateList = new System.Windows.Forms.Button();
			this.txtOutput = new System.Windows.Forms.TextBox();
			this.btnQuit = new System.Windows.Forms.Button();
			this.lblPublicIpv4Address = new System.Windows.Forms.Label();
			this.txtPublicIpv4 = new System.Windows.Forms.TextBox();
			this.listViewRecords = new System.Windows.Forms.ListView();
			this.colUpdate = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colAddress = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.colProxied = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.btnSettings = new System.Windows.Forms.Button();
			this.lblNextUpdate = new System.Windows.Forms.Label();
			this.txtNextUpdate = new System.Windows.Forms.TextBox();
			this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
			this.btnUpdate = new System.Windows.Forms.Button();
			this.txtPublicIpv6 = new System.Windows.Forms.TextBox();
			this.lblPublicIpv6Address = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// btnUpdateList
			// 
			this.btnUpdateList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnUpdateList.Location = new System.Drawing.Point(179, 597);
			this.btnUpdateList.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnUpdateList.Name = "btnUpdateList";
			this.btnUpdateList.Size = new System.Drawing.Size(134, 32);
			this.btnUpdateList.TabIndex = 0;
			this.btnUpdateList.Text = "Fetch List";
			this.btnUpdateList.UseVisualStyleBackColor = true;
			this.btnUpdateList.Click += new System.EventHandler(this.btnUpdateList_Click);
			// 
			// txtOutput
			// 
			this.txtOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtOutput.Location = new System.Drawing.Point(15, 334);
			this.txtOutput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.txtOutput.Multiline = true;
			this.txtOutput.Name = "txtOutput";
			this.txtOutput.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.txtOutput.Size = new System.Drawing.Size(772, 250);
			this.txtOutput.TabIndex = 1;
			// 
			// btnQuit
			// 
			this.btnQuit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnQuit.Location = new System.Drawing.Point(647, 597);
			this.btnQuit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnQuit.Name = "btnQuit";
			this.btnQuit.Size = new System.Drawing.Size(134, 32);
			this.btnQuit.TabIndex = 2;
			this.btnQuit.Text = "Quit";
			this.btnQuit.UseVisualStyleBackColor = true;
			this.btnQuit.Click += new System.EventHandler(this.btnQuit_Click);
			// 
			// lblPublicIpv4Address
			// 
			this.lblPublicIpv4Address.AutoSize = true;
			this.lblPublicIpv4Address.Location = new System.Drawing.Point(12, 17);
			this.lblPublicIpv4Address.Name = "lblPublicIpv4Address";
			this.lblPublicIpv4Address.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
			this.lblPublicIpv4Address.Size = new System.Drawing.Size(68, 15);
			this.lblPublicIpv4Address.TabIndex = 3;
			this.lblPublicIpv4Address.Text = "Public IPv4";
			// 
			// txtPublicIpv4
			// 
			this.txtPublicIpv4.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.txtPublicIpv4.Location = new System.Drawing.Point(86, 14);
			this.txtPublicIpv4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.txtPublicIpv4.Name = "txtPublicIpv4";
			this.txtPublicIpv4.ReadOnly = true;
			this.txtPublicIpv4.Size = new System.Drawing.Size(119, 23);
			this.txtPublicIpv4.TabIndex = 4;
			// 
			// listViewRecords
			// 
			this.listViewRecords.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.listViewRecords.CheckBoxes = true;
			this.listViewRecords.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colUpdate,
            this.colName,
            this.colAddress,
            this.colProxied});
			this.listViewRecords.HideSelection = false;
			this.listViewRecords.Location = new System.Drawing.Point(15, 49);
			this.listViewRecords.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.listViewRecords.Name = "listViewRecords";
			this.listViewRecords.Size = new System.Drawing.Size(772, 278);
			this.listViewRecords.TabIndex = 5;
			this.listViewRecords.UseCompatibleStateImageBehavior = false;
			this.listViewRecords.View = System.Windows.Forms.View.Details;
			this.listViewRecords.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.listViewRecords_ItemChecked);
			// 
			// colUpdate
			// 
			this.colUpdate.Text = "Update";
			// 
			// colName
			// 
			this.colName.Text = "Name";
			this.colName.Width = 127;
			// 
			// colAddress
			// 
			this.colAddress.Text = "Address";
			this.colAddress.Width = 300;
			// 
			// colProxied
			// 
			this.colProxied.Text = "Proxied";
			// 
			// btnSettings
			// 
			this.btnSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSettings.Location = new System.Drawing.Point(491, 597);
			this.btnSettings.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.Size = new System.Drawing.Size(134, 32);
			this.btnSettings.TabIndex = 6;
			this.btnSettings.Text = "Settings";
			this.btnSettings.UseVisualStyleBackColor = true;
			this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
			// 
			// lblNextUpdate
			// 
			this.lblNextUpdate.AutoSize = true;
			this.lblNextUpdate.Location = new System.Drawing.Point(589, 17);
			this.lblNextUpdate.Name = "lblNextUpdate";
			this.lblNextUpdate.Size = new System.Drawing.Size(73, 15);
			this.lblNextUpdate.TabIndex = 7;
			this.lblNextUpdate.Text = "Next Update";
			// 
			// txtNextUpdate
			// 
			this.txtNextUpdate.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.txtNextUpdate.Location = new System.Drawing.Point(668, 14);
			this.txtNextUpdate.Name = "txtNextUpdate";
			this.txtNextUpdate.ReadOnly = true;
			this.txtNextUpdate.Size = new System.Drawing.Size(119, 23);
			this.txtNextUpdate.TabIndex = 8;
			// 
			// notifyIcon1
			// 
			this.notifyIcon1.BalloonTipText = "Application will continue to work in the background";
			this.notifyIcon1.BalloonTipTitle = "DnsTube";
			this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
			this.notifyIcon1.Text = "DnsTube";
			this.notifyIcon1.Click += new System.EventHandler(this.notifyIcon1_Click);
			// 
			// btnUpdate
			// 
			this.btnUpdate.Location = new System.Drawing.Point(335, 597);
			this.btnUpdate.Name = "btnUpdate";
			this.btnUpdate.Size = new System.Drawing.Size(134, 32);
			this.btnUpdate.TabIndex = 9;
			this.btnUpdate.Text = "Manual Update";
			this.btnUpdate.UseVisualStyleBackColor = true;
			this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
			// 
			// txtPublicIpv6
			// 
			this.txtPublicIpv6.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.txtPublicIpv6.Location = new System.Drawing.Point(296, 14);
			this.txtPublicIpv6.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.txtPublicIpv6.Name = "txtPublicIpv6";
			this.txtPublicIpv6.ReadOnly = true;
			this.txtPublicIpv6.Size = new System.Drawing.Size(278, 23);
			this.txtPublicIpv6.TabIndex = 11;
			// 
			// lblPublicIpv6Address
			// 
			this.lblPublicIpv6Address.AutoSize = true;
			this.lblPublicIpv6Address.Location = new System.Drawing.Point(225, 17);
			this.lblPublicIpv6Address.Name = "lblPublicIpv6Address";
			this.lblPublicIpv6Address.Size = new System.Drawing.Size(65, 15);
			this.lblPublicIpv6Address.TabIndex = 10;
			this.lblPublicIpv6Address.Text = "Public IPv6";
			// 
			// frmMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 642);
			this.Controls.Add(this.txtPublicIpv6);
			this.Controls.Add(this.lblPublicIpv6Address);
			this.Controls.Add(this.btnUpdate);
			this.Controls.Add(this.txtNextUpdate);
			this.Controls.Add(this.lblNextUpdate);
			this.Controls.Add(this.btnSettings);
			this.Controls.Add(this.listViewRecords);
			this.Controls.Add(this.txtPublicIpv4);
			this.Controls.Add(this.lblPublicIpv4Address);
			this.Controls.Add(this.btnQuit);
			this.Controls.Add(this.txtOutput);
			this.Controls.Add(this.btnUpdateList);
			this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
			this.MinimumSize = new System.Drawing.Size(480, 600);
			this.Name = "frmMain";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "DnsTube v0.6b";
			this.Load += new System.EventHandler(this.frmMain_Load);
			this.Resize += new System.EventHandler(this.frmMain_Resize);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnUpdateList;
		private System.Windows.Forms.TextBox txtOutput;
		private System.Windows.Forms.Button btnQuit;
		private System.Windows.Forms.Label lblPublicIpv4Address;
		private System.Windows.Forms.TextBox txtPublicIpv4;
		private System.Windows.Forms.ListView listViewRecords;
		private System.Windows.Forms.ColumnHeader colUpdate;
		private System.Windows.Forms.ColumnHeader colName;
		private System.Windows.Forms.ColumnHeader colAddress;
		private System.Windows.Forms.Button btnSettings;
		private System.Windows.Forms.Label lblNextUpdate;
		private System.Windows.Forms.TextBox txtNextUpdate;
		private System.Windows.Forms.NotifyIcon notifyIcon1;
		private System.Windows.Forms.Button btnUpdate;
		private System.Windows.Forms.ColumnHeader colProxied;
		private System.Windows.Forms.TextBox txtPublicIpv6;
		private System.Windows.Forms.Label lblPublicIpv6Address;
	}
}

