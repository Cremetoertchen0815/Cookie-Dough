using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nez
{
	public class MessageBox
	{
		public static ShowResult Show(string text, ShowStyle style = ShowStyle.OkOnly, string title = "")
		{
			return ShowResult.OkOnly;
		}

		public enum ShowStyle
		{
			YesNo,OkOnly
		}

		public enum ShowResult
		{
			Yes, No, OkOnly
		}

	}
}
