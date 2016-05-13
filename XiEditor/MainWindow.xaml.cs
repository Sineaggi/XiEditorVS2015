using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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

		string tabName;

		private int firstLine = 0;
		private int lastLine = 0;

		private double ascent;
		private double desent;
		private double leading;
		private double linespace;

		//TextBox textBox;

		public MainWindow()
		{
			InitializeComponent();


            string filename = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\xieditor\", "path", null);
            if (String.IsNullOrWhiteSpace(filename) )
            {

                OpenFileDialog xiEnginePicker = new OpenFileDialog();

                xiEnginePicker.InitialDirectory = "c:\\";
                xiEnginePicker.Filter = "xi-editor core (*.exe)|*.exe|All files (*.*)|*.*";
                xiEnginePicker.Title = "Please select the xi-editor.exe in your rust > target directory.";

                if (xiEnginePicker.ShowDialog() == true )
                {
                    filename = xiEnginePicker.FileName;
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\xieditor\", "path", filename );
                } else
                {
                    System.Windows.MessageBox.Show("You must select a xi-editor core engine.");
                    Environment.Exit(1);
                }
           }

            // filename = @"C:\Users\Clayton\Source\xi-editor\rust\target\debug\xicore.exe";
			coreConnection = new CoreConnection(filename, delegate (object data) {
				handleCoreCmd(data);
			});

			//textBox = new TextBox();

			// Text ediitor using a text box
			textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
			textBox.PreviewTextInput += TextBox_PreviewTextInput;
			textBox.PreviewMouseUp += TextBox_PreviewMouseUp;
			textBox.DragEnter += TextBox_DragEnter;

			//canvasScroller.
			FormattedText someText = new FormattedText("A", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Consolas"), 12, Brushes.Red);
			linespace = someText.Height;

			scrollBar.Scroll += ScrollBar_Scroll;

			// Text editor using a custom canvas
			textCanvas.SizeChanged += TextCanvas_SizeChanged;

			// Windows has a differnt method for handling pasting than the rest of the inputs
			DataObject.AddPastingHandler(textBox, TextBox_OnPaste);

			tabName = (coreConnection.sendRpc("new_tab", new { }) as JValue).Value as string;
		}

		private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
		{
			textCanvas.ScrollTo = e.NewValue;
		}

		private void handleCoreCmd(object json)
		{
			dynamic obj = json;
			if (obj["method"] != null)
			{
				var method = (string)obj["method"];
				var parameters = obj["params"];
				handleRpc(method, parameters);
			} else
			{
				Console.WriteLine("unknown json from core: " + json);
			}
		}

		private void handleRpc(string method, object parameters)
		{
			if (method.Equals("update"))
			{
				dynamic obj = parameters;
				var update = obj["update"];
				updateSafe(update);
			}
		}

		private void TextCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			double origin_y = 0;
			var first = (int)Math.Ceiling(origin_y / linespace);
			var last = (int)Math.Ceiling(e.NewSize.Height / linespace);
			
			if (last != lastLine || first != firstLine)
			{
				lastLine = last;
				var json = new object[] { firstLine, lastLine };
				sendRpcAsync("scroll", json);
			}
		}

		private void TextBox_DragEnter(object sender, DragEventArgs e)
		{
			var isText = e.Data.GetDataPresent(DataFormats.UnicodeText, true);
			if (!isText)
				return;

			var text = e.Data.GetData(DataFormats.UnicodeText) as string;

			var json = new { keycode = 0, chars = text, flags = 0 };
			sendRpcAsync("key", json);
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

			var json = new object[] { row, col, flags, num_clicks };
			sendRpcAsync("click", json);
		}

		private void TextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
		{
			var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
			if (!isText)
				return;

			var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;

			var json = new { keycode = 0, chars = text, flags = 0 };
			sendRpcAsync("key", json);
		}

		private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			var text = e.Text;
			var json = new { keycode = 0, chars = text, flags = 0 };
			sendRpcAsync("key", json);
		}

		public void sendRpcAsync(string in_method, object in_params)
		{
			var req = new Dictionary<string, dynamic> { { "method", in_method }, { "params", in_params }, { "tab", tabName } };
			// dispatch stuff
			coreConnection.sendRpcAsync("edit", req);
		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			
			int ech = KeyInterop.VirtualKeyFromKey(e.Key);

			Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

			if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
			{

			}

			if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{

			}

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
			
			var json = new { keycode = 0, chars = x, flags = flags };
			sendRpcAsync("key", json);
		}

		void updateSafe(object data)
		{
			// This is not a pretty function, but it works.
			// It should be prettier with a better api

			bool found_cursor = false;
			int cursor = 0;
			bool found_sel = false;
			int sel_x = 0, sel_y = 0;
			lineText.Clear();
			sb.Clear();

			var listLines = new List<Line>();

			dynamic text = data;
			var first_line = (int)text["first_line"];
			var height = (int)text["height"];
			var scrollto_x = (int)text["scrollto"][0];
			var scrollto_y = (int)text["scrollto"][1];
			var lines = (JArray)text["lines"];
			for (int i = 0; i < lines.Count; i++)
			{
				var lark = new Line();
				var lobe = (JArray)lines[i];
				var textline = (string)lobe[0];
				lark.line = (string)lobe[0];
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
						lark.sel = new int[] { (int)extra[1], (int)extra[2]};
					}
					else
					{
						if (!found_cursor)
						{
							if (typee.Equals("cursor"))
							{
								found_cursor = true;
								var cur_loc = (int)extra[1];
								var new_cur_loc = Tools.getUTF16Cursor(textline, cur_loc);
								lark.cursor = new_cur_loc;
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
				listLines.Add(lark);
			}
			
			var stringText = sb.ToString();

			Dispatcher.Invoke(() =>
			{
				// Update test canvas
				textCanvas.Lines = listLines;

				// This logic works like so
				// (viewPort / (max - min + viewPort) * track)
				// By default, with one line, scrolling should be 1-1, meaning unable to scroll.
				// With two lines, scrolling all the way down should cause only 1 line to be seen

				// Update scroll bar
				scrollBar.Minimum = 0;
				scrollBar.Maximum = height;
				//scrollBar.Value = first_line;
				scrollBar.ViewportSize = textCanvas.ActualHeight/linespace;
				//scrollBar.Track.Value = 0;

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
					coreConnection.sendJson(json);
				} else
				{
					return;
				}
			} else
			{
				var json = new { filename = filepath };
				sendRpcAsync("save", json);
			}
		}

		private void openButton_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			if (openFileDialog.ShowDialog() == true)
			{
				filepath = openFileDialog.FileName;
				sendRpcAsync("open", new { filename = filepath });
			}
		}

		private void newtabButton_Click(object sender, RoutedEventArgs e)
		{
			tabName = (coreConnection.sendRpc("new_tab", new { }) as JValue).Value as string;
		}
	}
}
