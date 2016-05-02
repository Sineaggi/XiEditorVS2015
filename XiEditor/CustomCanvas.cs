using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Windows;

namespace XiEditor
{
	class CustomCanvas : Canvas
	{
		Typeface tf;
		Point renderPoint;
		private string internalText;

		private string strong;
		private int cor;

		public string Text
		{
			get
			{
				return internalText;
			}
			set
			{
				if (value == null)
				{
					internalText = string.Empty;
				} else
				{
					internalText = value;
				}
				InvalidateVisual();
			}
		}

		private void calculateVisible()
		{
			FormattedText someText = new FormattedText("A", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 12, Brushes.Black);
			double width = someText.Width;
			double height = someText.Height;
			int columns = (int)Math.Ceiling(ActualWidth / width);
			int rows = (int)Math.Ceiling(ActualHeight / height);
		}

		public CustomCanvas() : base()
		{
			tf = new Typeface("Consolas");
			internalText = "";
			renderPoint = new Point(0, 0);
			calculateVisible();
			SizeChanged += CustomCanvas_SizeChanged;
		}

		private void CustomCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			calculateVisible();
		}

		public void Update(string text, int cur)
		{
			strong = text;
			cor = cur;

			InvalidateVisual();
		}

		protected override void OnRender(DrawingContext dc)
		{
			var s = internalText;
			FormattedText someText = new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 12, Brushes.Black);
			dc.DrawText(someText, renderPoint);
		}
	}
}
