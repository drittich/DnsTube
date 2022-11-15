using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnsTube.Core.Models
{
	internal class LogEntryDTO
	{
		public int Id { get; set; }
		public int Created { get; set; }
		public string Text { get; set; } = "";
		public int LogLevel { get; set; }
	}
}
