using System;
using System.Windows.Forms;

namespace CloudflareDynDNS
{
	public partial class frmSettings : Form
	{
		Settings settings;

		public frmSettings(Settings settings2) 
		{
			settings = settings2;
			InitializeComponent();
		}

		void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		void btnSave_Click(object sender, EventArgs e)
		{
			settings.EmailAddress = txtEmail.Text;
			settings.ApiKey = txtApiKey.Text;
			settings.UpdateIntervalMinutes = int.Parse(txtUpdateInterval.Text);
			settings.Save();
			Close();
		}

		void frmSettings_Load(object sender, EventArgs e)
		{
			txtEmail.Text = settings.EmailAddress;
			txtApiKey.Text = settings.ApiKey;
			txtUpdateInterval.Text = settings.UpdateIntervalMinutes.ToString();
		}

		void txtUpdateInterval_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) )
			{
				e.Handled = true;
			}
		}
	}
}
