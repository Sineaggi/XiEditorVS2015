using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace XiEditor
{
	public partial class MainWindow : Window
	{
		CoreConnection coreConnection;

		string filepath;
		ulong MODIFIER_SHIFT = 2;
		
		// Cache text lines for things like cursor lookup
		List<string> lineText = new List<string>();
		StringBuilder sb = new StringBuilder();

		public MainWindow()
		{
			InitializeComponent();

			coreConnection = new CoreConnection();
			coreConnection.DataReceived += HandleData;
			coreConnection.ProcessExited += Process_Exited;

			// Text ediitor using a text box
			textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			textBox.PreviewTextInput += TextBox_PreviewTextInput;
			textBox.PreviewMouseUp += TextBox_PreviewMouseUp;
			textBox.DragEnter += TextBox_DragEnter;

			// Text editor using a custom canvas
			textCanvas.SizeChanged += TextCanvas_SizeChanged;

			// Windows has a differnt method for handling pasting than the rest of the inputs
			DataObject.AddPastingHandler(textBox, TextBox_OnPaste);
		}

		private void TextCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Typeface tf = new Typeface("Consolas");
			FormattedText someText = new FormattedText("A", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 12, Brushes.Red);
			double width = someText.Width;
			double height = someText.Height;
			int columns = (int)Math.Ceiling(e.NewSize.Width / width);
			int rows = (int)Math.Ceiling(e.NewSize.Height / height);

			var json = new object[] { "scroll", new object[] { 0, rows } };
			coreConnection.SendJson(json);
		}

		private void HandleData(object sender, string e)
		{
			processData(e);
		}

		private void TextBox_DragEnter(object sender, DragEventArgs e)
		{
			var isText = e.Data.GetDataPresent(DataFormats.UnicodeText, true);
			if (!isText)
				return;

			var text = e.Data.GetData(DataFormats.UnicodeText) as string;

			var json = new object[] { "key", new { keycode = 0, chars = text, flags = 0 } };
			coreConnection.SendJson(json);
		}

		private void TextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			// This ugly code finds the utf8 offset based on the caret index of the text box
			var index = textBox.CaretIndex;
			if (lineText.Count == 0)
				return;

			var num_clicks = 1;
			ulong flags = 0;

			if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
			{
				flags ^= MODIFIER_SHIFT;
			}

			var row = 0;
			var str = lineText[0];

			while (index > str.Length)
			{
				index -= str.Length;
				row++;
				str = lineText[row];
			}

			var col = Tools.getUTF8Cursor(str, index);

			var json = new object[] { "click", new object[] { row, col, flags, num_clicks } };
			coreConnection.SendJson(json);
		}

		private void TextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
		{
			var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
			if (!isText)
				return;

			var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;

			var json = new object[] { "key", new { keycode = 0, chars = text, flags = 0 } };
			coreConnection.SendJson(json);
		}

		private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			string x;
			x = e.Text;

			var json = new object[] { "key", new { keycode = 0, chars = x, flags = 0 } };
			coreConnection.SendJson(json);
		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			
			int ech = KeyInterop.VirtualKeyFromKey(e.Key);

			// She's a real beauty
			char x;
			switch (e.Key)
			{
				case Key.Up:
					x = '\uF700';
					break;
				case Key.Down:
					x = '\uF701';
					break;
				case Key.Left:
					x = '\uF702';
					break;
				case Key.Right:
					x = '\uF703';
					break;
				case Key.PageUp:
					x = '\uF72C';
					break;
				case Key.PageDown:
					x = '\uF72D';
					break;
				case Key.F1:
					x = '\uF704';
					break;
				case Key.F2:
					x = '\uF705';
					break;
				case Key.Back:
					x = '\x7F';
					break;
				case Key.Delete:
					// Note, there shouldn't be a character in this string. It won't do anything.
					// I don't know what the delete key character is.
					x = ' ';
					break;
				case Key.Space:
					x = ' ';
					break;
				default:
					return;
			}

			ulong flags = 0;
			
			if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift))
			{
				flags ^= MODIFIER_SHIFT;
			}

			var json = new object[] { "key", new { keycode = 0, chars = x, flags = flags } };
			coreConnection.SendJson(json);
		}
		
		private void Process_Exited(object sender, EventArgs e)
		{
			Console.WriteLine("xi-editor exited");
			// We managed to crash the core.
			Dispatcher.Invoke(() =>
			{
				textBox.IsEnabled = false;
			});
		}

		private void MyProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			// Console.WriteLine(e.Data);
		}

		private void processData(string json)
		{
			// This is not a pretty function, but it works.
			// It should be prettier with a better api

			bool found_cursor = false;
			int cursor = 0;
			bool found_sel = false;
			int sel_x = 0, sel_y = 0;
			lineText.Clear();
			sb.Clear();

			JArray arr = (JArray)JsonConvert.DeserializeObject(json);

			foreach (var s in arr)
			{
				switch (s.Type)
				{
					case JTokenType.String:
						var text = (string)s;
						break;
					case JTokenType.Object:
						var jobj = (JObject)s;
						var first_line = (int)jobj["first_line"];
						var height = (int)jobj["height"];
						var scrolltop = (JArray)jobj["scrollto"];
						int[] scrollto = new int[2];
						for (int i = 0; i < scrolltop.Count; i++)
						{
							scrollto[i] = (int)scrolltop[i];
						}
						var lines = (JArray)jobj["lines"];
						for (int i = 0; i < lines.Count; i++)
						{
							var lobe = (JArray)lines[i];
							var textline = (string)lobe[0];
							sb.Append(textline);
							lineText.Add(textline);
							for (int j = 1; j < lobe.Count; j++)
							{
								var extra = (JArray)lobe[j];
								var typee = (string)extra[0];
								if (typee.Equals("sel"))
								{
									found_sel = true;
									sel_x += Tools.getUTF16Cursor(textline, (int)extra[1]);
									sel_y += Tools.getUTF16Cursor(textline, (int)extra[2]);
								} else
								{
									if (!found_cursor)
									{
										if (typee.Equals("cursor"))
										{
											found_cursor = true;
											var cur_loc = (int)extra[1];
											var new_cur_loc = Tools.getUTF16Cursor(textline, cur_loc);
											cursor += new_cur_loc;
										}
									}
								}
								
							}
							if (!found_cursor)
							{
								cursor += textline.Length;
							}
							if (!found_sel)
							{
								sel_x += textline.Length;
								sel_y += textline.Length;
							}
						}
						break;
					default:
						// Garbage
						break;
				}
			}
			var stringText = sb.ToString();

			Dispatcher.Invoke(() =>
			{
				// Update test canvas
				textCanvas.Text = stringText;
				textCanvas.Update(stringText, cursor);

				// Update text box
				textBox.Text = stringText;
				textBox.CaretIndex = cursor;
				if (found_sel)
				{
					textBox.Select(sel_x, sel_y - sel_x);
				}
				textBox.Focus();
			});
		}

		private void saveButton_Click(object sender, RoutedEventArgs e)
		{
			if (filepath == null)
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				if (saveFileDialog.ShowDialog() == true)
				{
					filepath = saveFileDialog.FileName;
					var json = new[] { "save", filepath };
					coreConnection.SendJson(json);
				} else
				{
					return;
				}
			} else
			{
				var json = new[] { "save", filepath };
				coreConnection.SendJson(json);
			}
		}

		private void openButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
			{
				filepath = openFileDialog.FileName;
				var json = new[] { "open", filepath };
				coreConnection.SendJson(json);
			}
		}
	}
}
