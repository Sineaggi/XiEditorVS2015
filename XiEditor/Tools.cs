using System.Text;

namespace XiEditor
{
	public class Tools
	{
		public static int getUTF16Cursor(string str, int cursor)
		{
			// Hacky method to find utf16 codepoint cursor
			// Encoding.UTF8.GetByteCount
			return Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(str), 0, cursor).Length;

		}

		public static int getUTF8Cursor(string str, int cursor)
		{
			// Hacky method to find utf8 byte offset cursor
			// Maybe run micro benchmarks? Just for fun.
			return Encoding.UTF8.GetByteCount(str.ToCharArray(0, cursor));
			//return Encoding.UTF8.GetBytes(str.Substring(0, cursor)).Length;
		}
	}
}
