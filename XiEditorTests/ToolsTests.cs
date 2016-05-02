using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace XiEditor.Tests
{
	[TestClass()]
	public class ToolTests
	{

	}

	[TestClass()]
	public class getUTF16CursorTests
	{
		[TestMethod()]
		public void getUTF16Cursor_Defaults()
		{
			Assert.AreEqual(Tools.getUTF16Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 0), 0);
			Assert.AreEqual(Tools.getUTF16Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 1), 1);
			Assert.AreEqual(Tools.getUTF16Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 2), 2);
			Assert.AreEqual(Tools.getUTF16Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 45), 35);
			Assert.AreEqual(Tools.getUTF16Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 69), 47);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void getUTF16Cursor_Overflow()
		{
			Tools.getUTF16Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 70);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void getUTF16Cursor_Underflow()
		{
			Tools.getUTF16Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", -1);
		}
	}

	[TestClass()]
	public class getUTF8CursorTests
	{
		[TestMethod()]
		public void getUTF8CursorTest_Defaults()
		{
			Assert.AreEqual(Tools.getUTF8Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 0), 0);
			Assert.AreEqual(Tools.getUTF8Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 1), 1);
			Assert.AreEqual(Tools.getUTF8Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 2), 2);
			Assert.AreEqual(Tools.getUTF8Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 35), 45);
			Assert.AreEqual(Tools.getUTF8Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 47), 69);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void getUTF8CursorTest_Overflow()
		{
			Tools.getUTF8Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", 70);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void getUTF8CursorTest_Underflow()
		{
			Tools.getUTF8Cursor("Descriptions on One Page: ❤ ☀ ☆ ☂ ☻ ♞ ☯ ☭ ☢ € →", -1);
		}
	}
}