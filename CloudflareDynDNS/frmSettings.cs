using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CloudflareDynDNS
{
	public partial class frmSettings : Form
	{
		public frmSettings() 
		{
			InitializeComponent();
			foreach (Control c in Controls)
				c.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 9f, FontStyle.Regular, GraphicsUnit.Point);
		}

		void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		void btnSave_Click(object sender, EventArgs e)
		{
			Utility.SaveSetting("Email", txtEmail.Text);
			Utility.SaveSetting("ApiKey", txtApiKey.Text);
			Utility.SaveSetting("UpdateInterval", txtUpdateInterval.Text);
			Close();
		}

		void frmSettings_Load(object sender, EventArgs e)
		{
			txtEmail.Text = Utility.GetSetting("Email");
			txtApiKey.Text = Utility.GetSetting("ApiKey");
			txtUpdateInterval.Text = Utility.GetSetting("UpdateInterval");
		}
	}
}
