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
	public partial class frmBaseForm : Form
	{
		public frmBaseForm()
		{
			InitializeComponent();

			FixFonts();
		}

		void FixFonts()
		{
			foreach (Control c in Controls)
				c.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 32f, FontStyle.Italic, GraphicsUnit.Point);

			// Use a larger, bold version of the default dialog font for one control
			// this.label1.Font = new Font(SystemFonts.MessageBoxFont.Name, 12f, FontStyle.Bold, GraphicsUnit.Point);
		}
	}
}
