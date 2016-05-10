using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Windows;
using System.Collections.Generic;

namespace XiEditor
{
	class CustomCanvas : Canvas
	{
		private SolidColorBrush HightlightBackground = new SolidColorBrush(Color.FromRgb(173, 214, 255));
		private SolidColorBrush HighlightForeground = new SolidColorBrush(Color.FromRgb(20, 61, 102));
		private Typeface typeFace = new Typeface("Consolas");
		private int fontSize = 12;
		private double fontHeight;

		private double _Crop;
		public double ScrollTo
		{
			get { return _Crop; }
			set
			{
				_Crop = value;
				InvalidateVisual();
			}
		}

		private List<Line> _Lines = new List<Line>();
		public List<Line> Lines
		{
			get { return _Lines; }
			set
			{
				if (value == null)
					_Lines = new List<Line>();
				else
					_Lines = value;
				InvalidateVisual();
			}
		}

		protected override void OnRender(DrawingContext dc)
		{
			fontHeight = new FormattedText("A", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, 12, Brushes.Black).Height;
			var running_height =  -(ScrollTo * fontHeight);

			for (int i = 0; i < Lines.Count; i++)
			{
				// loop through all lines
				var line = Lines[i];
				var text = line.line;

				var formattedLine = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, 12, Brushes.Black);
				var height = formattedLine.Height;

				if (line.sel != null)
				{
					// draw selection
					var start_x = 0.0;
					var end_x = 0.0;
					if (line.sel[0] == 0)
					{
						// we start at the beginning of the line
						start_x = 0;
					} else
					{
						// we start somewhere inside the string
						var sub = text.Substring(0, line.sel[0]);
						var startText = new FormattedText(sub, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black);
						start_x = startText.WidthIncludingTrailingWhitespace;
					}

					if (line.sel[1] == text.Length)
					{
						// we end at the end of the string
						end_x = formattedLine.WidthIncludingTrailingWhitespace;
					} else
					{
						// we end somewhere inside the string
						var sub1 = text.Substring(0, line.sel[1]);
						var startText = new FormattedText(sub1, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black);
						end_x = startText.WidthIncludingTrailingWhitespace;
					}
					// FIXME: Small lines visible inbetween highlight blocks
					dc.DrawRectangle(HightlightBackground, null, new Rect(new Point(start_x, running_height), new Point(end_x, running_height + height)));
					formattedLine.SetForegroundBrush(HighlightForeground, line.sel[0], line.sel[1] - line.sel[0]);
				}
				else if (line.cursor.HasValue == true)
				{
					// draw cursor
					int cursor = line.cursor.Value;

					var cursor_x = 0.0;
					if (cursor == 0)
					{
						// cursor is at the beginning of the line
					}
					else
					{
						// cursor is somewhere in the text
						var sub = text.Substring(0, cursor);
						var startText = new FormattedText(sub, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black);
						cursor_x = startText.WidthIncludingTrailingWhitespace;
					}
					// TODO: Keep consistant pixel width
					// TODO: Add blink animation
					dc.DrawLine(new Pen(Brushes.Black, 0.25), new Point(cursor_x, running_height), new Point(cursor_x, running_height + height));
				}
				dc.DrawText(formattedLine, new Point(0, running_height));
				running_height += height;
			}
		}
	}
}
