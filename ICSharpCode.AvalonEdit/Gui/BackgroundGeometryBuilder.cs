// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Gui
{
	/// <summary>
	/// Hel create a PathGeometry.
	/// </summary>
	public sealed class BackgroundGeometryBuilder
	{
		double cornerRadius = 3;
		
		/// <summary>
		/// Gets/sets the radius of the rounded corners.
		/// </summary>
		public double CornerRadius { 
			get { return cornerRadius; }
			set { cornerRadius = value; }
		}
		
		/// <summary>
		/// Creates a new BackgroundGeometryBuilder instance.
		/// </summary>
		public BackgroundGeometryBuilder()
		{
		}
		
		/// <summary>
		/// Adds the specified segments to the geometry.
		/// </summary>
		public void AddSegments(TextView textView, IEnumerable<ISegment> segments)
		{
			if (textView == null)
				throw new ArgumentNullException("textView");
			if (segments == null)
				throw new ArgumentNullException("segments");
			
			var scrollOffset = textView.ScrollOffset;
			foreach (ISegment segment in segments) {
				int segmentStart = segment.Offset;
				int segmentEnd = segment.Offset + segment.Length;
				
				foreach (VisualLine vl in textView.VisualLines) {
					int vlStartOffset = vl.FirstDocumentLine.Offset;
					if (vlStartOffset > segmentEnd)
						break;
					int vlEndOffset = vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length;
					if (vlEndOffset < segmentStart)
						continue;
					
					int segmentStartVC;
					if (segmentStart < vlStartOffset)
						segmentStartVC = 0;
					else
						segmentStartVC = vl.GetVisualColumn(segmentStart - vlStartOffset);
					
					int segmentEndVC;
					if (segmentEnd > vlEndOffset)
						segmentEndVC = vl.VisualLength;
					else
						segmentEndVC = vl.GetVisualColumn(segmentEnd - vlStartOffset);
					
					TextLine lastTextLine = vl.TextLines.Last();
					foreach (TextLine line in vl.TextLines) {
						double y = vl.GetTextLineVisualTop(line);
						int visualStartCol = vl.GetTextLineVisualStartColumn(line);
						int visualEndCol = visualStartCol + line.Length;
						if (line != lastTextLine)
							visualEndCol -= line.TrailingWhitespaceLength;
						
						if (segmentEndVC < visualStartCol)
							break;
						if (segmentStartVC > visualEndCol)
							continue;
						double left = line.GetDistanceFromCharacterHit(new CharacterHit(Math.Max(segmentStartVC, visualStartCol), 0));
						double right = line.GetDistanceFromCharacterHit(new CharacterHit(Math.Min(segmentEndVC, visualEndCol), 0));
						y -= scrollOffset.Y;
						left -= scrollOffset.X;
						right -= scrollOffset.X;
						AddRectangle(left, y, right, y + line.Height);
					}
				}
			}
		}
		
		PathFigureCollection figures = new PathFigureCollection();
		PathFigure figure;
		int insertionIndex;
		double lastTop, lastBottom;
		double lastLeft, lastRight;
		
		/// <summary>
		/// Adds a rectangle to the geometry.
		/// </summary>
		public void AddRectangle(double left, double top, double right, double bottom)
		{
			if (!top.IsClose(lastBottom)) {
				CloseFigure();
			}
			if (figure == null) {
				figure = new PathFigure();
				figure.StartPoint = new Point(left, top + cornerRadius);
				if (Math.Abs(left - right) > cornerRadius) {
					figure.Segments.Add(MakeArc(left + cornerRadius, top, SweepDirection.Clockwise));
					figure.Segments.Add(MakeLineSegment(right - cornerRadius, top));
					figure.Segments.Add(MakeArc(right, top + cornerRadius, SweepDirection.Clockwise));
				}
				figure.Segments.Add(MakeLineSegment(right, bottom - cornerRadius));
				insertionIndex = figure.Segments.Count;
				//figure.Segments.Add(MakeArc(left, bottom - cornerRadius, SweepDirection.Clockwise));
			} else {
				if (!lastRight.IsClose(right)) {
					double cr = right < lastRight ? -cornerRadius : cornerRadius;
					SweepDirection dir1 = right < lastRight ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
					SweepDirection dir2 = right < lastRight ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
					figure.Segments.Insert(insertionIndex++, MakeArc(lastRight + cr, lastBottom, dir1));
					figure.Segments.Insert(insertionIndex++, MakeLineSegment(right - cr, top));
					figure.Segments.Insert(insertionIndex++, MakeArc(right, top + cornerRadius, dir2));
				}
				figure.Segments.Insert(insertionIndex++, MakeLineSegment(right, bottom - cornerRadius));
				figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft, lastTop + cornerRadius));
				if (!lastLeft.IsClose(left)) {
					double cr = left < lastLeft ? cornerRadius : -cornerRadius;
					SweepDirection dir1 = left < lastLeft ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
					SweepDirection dir2 = left < lastLeft ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;
					figure.Segments.Insert(insertionIndex, MakeArc(lastLeft, lastBottom - cornerRadius, dir1));
					figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft - cr, lastBottom));
					figure.Segments.Insert(insertionIndex, MakeArc(left + cr, lastBottom, dir2));
				}
			}
			this.lastTop = top;
			this.lastBottom = bottom;
			this.lastLeft = left;
			this.lastRight = right;
		}
		
		ArcSegment MakeArc(double x, double y, SweepDirection dir)
		{
			ArcSegment arc = new ArcSegment(
				new Point(x, y),
				new Size(cornerRadius, cornerRadius),
				0, false, dir, true);
			arc.Freeze();
			return arc;
		}
		
		static LineSegment MakeLineSegment(double x, double y)
		{
			LineSegment ls = new LineSegment(new Point(x, y), true);
			ls.Freeze();
			return ls;
		}
		
		void CloseFigure()
		{
			if (figure != null) {
				figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft, lastTop + cornerRadius));
				if (Math.Abs(lastLeft - lastRight) > cornerRadius) {
					figure.Segments.Insert(insertionIndex, MakeArc(lastLeft, lastBottom - cornerRadius, SweepDirection.Clockwise));
					figure.Segments.Insert(insertionIndex, MakeLineSegment(lastLeft + cornerRadius, lastBottom));
					figure.Segments.Insert(insertionIndex, MakeArc(lastRight - cornerRadius, lastBottom, SweepDirection.Clockwise));
				}
				
				figure.IsClosed = true;
				figures.Add(figure);
				figure = null;
			}
		}
		
		/// <summary>
		/// Creates the geometry.
		/// Returns null when the geometry is empty!
		/// </summary>
		public PathGeometry CreateGeometry()
		{
			CloseFigure();
			if (figures.Count != 0) {
				PathGeometry g = new PathGeometry(figures);
				g.Freeze();
				return g;
			} else {
				return null;
			}
		}
	}
}
