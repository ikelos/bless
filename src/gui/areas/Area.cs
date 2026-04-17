/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *
 *   This file is part of Bless.
 *
 *   Bless is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *   Bless is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with Bless; if not, write to the Free Software
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using Bless.Gui.Drawers;
using Bless.Util;
using Bless.Buffers;
using Bless.Tools.Find;
using System.Collections.Generic;
using System.Xml;
using Cairo;

namespace Bless.Gui.Areas {

/// <summary>
/// An area on the screen that displays data in a specific way.
/// </summary>
public abstract class Area
{
	protected AreaGroup areaGroup;
	protected Drawer drawer;
	protected Drawer.Information drawerInformation;
	protected string type;

	// display
	protected int x;
	protected int y;
	protected int width;
	protected int height;
	protected int bpr;
	protected int dpb; // digits per byte
	protected int fixedBpr;
	// Cairo context used for all drawing (set before each render pass)
	protected Cairo.Context backCr;
	protected bool isAreaRealized;

	// Cursor colours
	protected Gdk.RGBA activeCursorColor;
	protected Gdk.RGBA inactiveCursorColor;
	protected bool isActive;

	protected int cursorDigit;
	protected bool cursorFocus;
	protected bool canFocus;

	public enum RenderMergeFlags {None = 0, Left = 1, Right = 2}

	// Abstract methods
	abstract protected void RenderRowNormal(int i, int p, int n, bool blank);
	abstract protected void RenderRowHighlight(int i, int p, int n, bool blank, Drawer.HighlightType ht);
	abstract public void GetDisplayInfoByOffset(long off, out int orow, out int obyte, out int ox, out int oy);

	public enum GetOffsetFlags {
		Eof   = 1,
		Abyss = 2
	}

	abstract public long GetOffsetByDisplayInfo(int x, int y, out int digit, out GetOffsetFlags rflags);

	virtual public bool HandleKey(Gdk.Key key, bool overwrite)
	{
		return false;
	}

	abstract public int CalcWidth(int n, bool force);

	public delegate Area AreaCreatorFunc(AreaGroup ag);

	static private Dictionary<string, AreaCreatorFunc> pluginTable;

	static public void AddFactoryItem(string name, AreaCreatorFunc createArea)
	{
		if (pluginTable == null)
			pluginTable = new Dictionary<string, AreaCreatorFunc>();
		pluginTable.Add(name, createArea);
	}

	static public Area Factory(string name, AreaGroup ag)
	{
		try {
			AreaCreatorFunc acf = pluginTable[name];
			return acf(ag);
		}
		catch (KeyNotFoundException e) {
			System.Console.WriteLine(e.Message);
		}
		return null;
	}

	public Area(AreaGroup areaGroup)
	{
		this.areaGroup = areaGroup;
		drawerInformation = new Drawer.Information();
		canFocus  = false;
		dpb       = 0;
		fixedBpr  = -1;
		isAreaRealized = false;
	}

	public virtual void Configure(XmlNode parentNode)
	{
		XmlNodeList childNodes = parentNode.ChildNodes;
		foreach (XmlNode node in childNodes) {
			if (node.Name == "bpr")
				this.FixedBytesPerRow = System.Convert.ToInt32(node.InnerText);
			if (node.Name == "display")
				ParseDisplay(node, drawerInformation);
		}
	}

	void ParseDisplay(XmlNode parentNode, Drawer.Information info)
	{
		XmlNodeList childNodes = parentNode.ChildNodes;
		foreach (XmlNode node in childNodes) {
			if (node.Name == "evenrow")
				ParseDisplayRow(node, info, Drawer.RowType.Even);
			else if (node.Name == "oddrow")
				ParseDisplayRow(node, info, Drawer.RowType.Odd);
			else if (node.Name == "font")
				info.FontName = node.InnerText;
		}
	}

	void ParseDisplayRow(XmlNode parentNode, Drawer.Information info, Drawer.RowType rowType)
	{
		Drawer.Color fg, bg;
		XmlNodeList childNodes = parentNode.ChildNodes;
		foreach (XmlNode node in childNodes) {
			ParseDisplayType(node, out fg, out bg);
			if (node.Name == "evencolumn") {
				if (bg != null)
					info.bgNormal[(int)rowType, (int)Drawer.ColumnType.Even] = bg;
				if (fg != null)
					info.fgNormal[(int)rowType, (int)Drawer.ColumnType.Even] = fg;
			}
			else if (node.Name == "oddcolumn") {
				if (bg != null)
					info.bgNormal[(int)rowType, (int)Drawer.ColumnType.Odd] = bg;
				if (fg != null)
					info.fgNormal[(int)rowType, (int)Drawer.ColumnType.Odd] = fg;
			}
			else if (node.Name == "selectedcolumn") {
				if (bg != null)
					info.bgHighlight[(int)rowType, (int)Drawer.HighlightType.Selection] = bg;
				if (fg != null)
					info.fgHighlight[(int)rowType, (int)Drawer.HighlightType.Selection] = fg;
			}
			else if (node.Name == "patternmatchcolumn") {
				if (bg != null)
					info.bgHighlight[(int)rowType, (int)Drawer.HighlightType.PatternMatch] = bg;
				if (fg != null)
					info.fgHighlight[(int)rowType, (int)Drawer.HighlightType.PatternMatch] = fg;
			}
		}
	}

	void ParseDisplayType(XmlNode parentNode, out Drawer.Color fg, out Drawer.Color bg)
	{
		fg = null;
		bg = null;
		XmlNodeList childNodes = parentNode.ChildNodes;
		foreach (XmlNode node in childNodes) {
			if (node.Name == "foreground")
				fg = new Drawer.Color(Drawer.Information.ParseRGBA(node.InnerText));
			if (node.Name == "background")
				bg = new Drawer.Color(Drawer.Information.ParseRGBA(node.InnerText));
		}
	}

	/// <summary>Set the Cairo context used for all drawing in this area.</summary>
	internal void SetCairoContext(Cairo.Context cr)
	{
		backCr = cr;
	}

	/// <summary>Realize the area (called once the DrawingArea is mapped).</summary>
	public virtual void Realize()
	{
		activeCursorColor   = Drawer.Information.ParseRGBA("red");
		inactiveCursorColor = Drawer.Information.ParseRGBA("gray");
		isActive       = true;
		isAreaRealized = true;
	}

	// Properties
	public int X      { set { x = value; }     get { return x; } }
	public int Y      { set { y = value; }     get { return y; } }
	public int Width  { set { width = value; } get { return width; } }
	public int Height { set { height = value; } get { return height; } }

	public int BytesPerRow   { set { bpr = value; }      get { return bpr; } }
	public int FixedBytesPerRow { set { fixedBpr = value; } get { return fixedBpr; } }
	public int DigitsPerByte { get { return dpb; } }

	public int CursorDigit {
		get { return cursorDigit; }
		set { cursorDigit = (value >= dpb) ? dpb - 1 : value; }
	}

	public bool HasCursorFocus {
		set { cursorFocus = value; }
		get { return cursorFocus; }
	}

	public bool CanFocus { get { return canFocus; } }

	public Drawer.Information DrawerInformation {
		get { return drawerInformation; }
		set { drawerInformation = value; }
	}

	public virtual Drawer Drawer { get { return drawer; } }

	public string Type { get { return type; } }

	public bool IsActive {
		set { isActive = value; }
		get { return isActive; }
	}

	/// <summary>
	/// Called to draw extra (data-independent) content during a full redraw.
	/// </summary>
	protected internal virtual void RenderExtra()
	{
	}

	void RenderRangeHelper(Drawer.HighlightType ht, int rstart, int bstart, int len)
	{
		if (ht != Drawer.HighlightType.Normal)
			RenderRowHighlight(rstart, bstart, len, false, ht);
		else
			RenderRowNormal(rstart, bstart, len, false);
	}

	///<summary>Fill a rectangle with the given color using Cairo</summary>
	protected void FillRect(Gdk.RGBA color, int rx, int ry, int rw, int rh)
	{
		backCr.SetSourceRGBA(color.Red, color.Green, color.Blue, color.Alpha);
		backCr.Rectangle(rx, ry, rw, rh);
		backCr.Fill();
	}

	protected internal virtual void RenderHighlight(Highlight h, Drawer.HighlightType left, Drawer.HighlightType right)
	{
		if (isAreaRealized == false)
			return;

		int rstart, bstart, xstart, ystart;
		int rend, bend, xend, yend;
		bool odd;
		Gdk.RGBA gc;
		Gdk.RGBA oddColor;
		Gdk.RGBA evenColor;
		Gdk.RGBA leftColor;
		Gdk.RGBA rightColor;

		oddColor  = drawer.GetBackgroundColor(Drawer.RowType.Odd,  h.Type);
		evenColor = drawer.GetBackgroundColor(Drawer.RowType.Even, h.Type);

		GetDisplayInfoByOffset(h.Start, out rstart, out bstart, out xstart, out ystart);
		GetDisplayInfoByOffset(h.End,   out rend,   out bend,   out xend,   out yend);

		bool drawLeft = false;
		int dxstart = xstart;

		if (bstart > 0) {
			int digit;
			GetOffsetFlags gof;
			GetOffsetByDisplayInfo(xstart - 1, ystart, out digit, out gof);
			if ((gof & GetOffsetFlags.Abyss) != 0) {
				dxstart -= drawer.Width;
				drawLeft = true;
			}
		}

		bool drawRight = false;
		int dxend = xend;

		if (bend < bpr - 1) {
			int digit;
			GetOffsetFlags gof;
			GetOffsetByDisplayInfo(xend + dpb * drawer.Width, yend, out digit, out gof);
			if ((gof & GetOffsetFlags.Abyss) != 0) {
				dxend += drawer.Width;
				drawRight = true;
			}
		}

		// Single row
		if (rstart == rend) {
			odd = (((h.Start / bpr) % 2) == 1);
			if (odd) {
				gc         = oddColor;
				leftColor  = drawer.GetBackgroundColor(Drawer.RowType.Odd, left);
				rightColor = drawer.GetBackgroundColor(Drawer.RowType.Odd, right);
			} else {
				gc         = evenColor;
				leftColor  = drawer.GetBackgroundColor(Drawer.RowType.Even, left);
				rightColor = drawer.GetBackgroundColor(Drawer.RowType.Even, right);
			}

			if (drawLeft)
				FillRect(leftColor, x + dxstart, y + ystart, drawer.Width, drawer.Height);
			if (drawRight)
				FillRect(rightColor, x + xend + dpb * drawer.Width, y + yend, drawer.Width, drawer.Height);

			FillRect(gc, x + xstart, y + ystart, xend - xstart + dpb * drawer.Width, drawer.Height);
			RenderRangeHelper(h.Type, rstart, bstart, bend - bstart + 1);
		}
		else {
			// Multi-row range

			// Render first row
			odd = (((h.Start / bpr) % 2) == 1);
			if (odd) {
				gc         = oddColor;
				leftColor  = drawer.GetBackgroundColor(Drawer.RowType.Odd, left);
				rightColor = drawer.GetBackgroundColor(Drawer.RowType.Odd, right);
			} else {
				gc         = evenColor;
				leftColor  = drawer.GetBackgroundColor(Drawer.RowType.Even, left);
				rightColor = drawer.GetBackgroundColor(Drawer.RowType.Even, right);
			}

			if (drawLeft)
				FillRect(leftColor, x + dxstart, y + ystart, drawer.Width, drawer.Height);
			FillRect(gc, x + xstart, y + ystart, width - xstart, drawer.Height);
			RenderRangeHelper(h.Type, rstart, bstart, bpr - bstart);

			long curOffset = h.Start + bpr - bstart;

			// Middle rows
			for (int i = rstart + 1; i < rend; i++) {
				odd = (((curOffset / bpr) % 2) == 1);
				gc  = odd ? oddColor : evenColor;
				FillRect(gc, x, y + i * drawer.Height, width, drawer.Height);
				RenderRangeHelper(h.Type, i, 0, bpr);
				curOffset += bpr;
			}

			// Last row
			odd = (((h.End / bpr) % 2) == 1);
			if (odd) {
				gc         = oddColor;
				leftColor  = drawer.GetBackgroundColor(Drawer.RowType.Odd, left);
				rightColor = drawer.GetBackgroundColor(Drawer.RowType.Odd, right);
			} else {
				gc         = evenColor;
				leftColor  = drawer.GetBackgroundColor(Drawer.RowType.Even, left);
				rightColor = drawer.GetBackgroundColor(Drawer.RowType.Even, right);
			}

			if (drawRight)
				FillRect(rightColor, x + xend + dpb * drawer.Width, y + yend, drawer.Width, drawer.Height);
			FillRect(gc, x, y + yend, xend + dpb * drawer.Width, drawer.Height);
			RenderRangeHelper(h.Type, rend, 0, bend + 1);
		}
	}

	/// <summary>Blank the background at the given offset position</summary>
	protected internal void BlankOffset(long offs)
	{
		if (isAreaRealized == false)
			return;

		int nrows = height / drawer.Height;
		long bytesInView = nrows * bpr;

		if (offs >= areaGroup.Offset && offs < areaGroup.Offset + bytesInView) {
			int pcRow, pcByte, pcX, pcY;
			GetDisplayInfoByOffset(offs, out pcRow, out pcByte, out pcX, out pcY);
			Gdk.RGBA backEvenColor = drawer.GetBackgroundColor(Drawer.RowType.Even, Drawer.HighlightType.Normal);
			FillRect(backEvenColor, x + pcX, y + pcY, drawer.Width * dpb, drawer.Height);
		}
	}

	///<summary>Render the cursor indicator</summary>
	protected internal void RenderCursor()
	{
		if (isAreaRealized == false)
			return;

		int cRow, cByte, cX, cY;
		GetDisplayInfoByOffset(areaGroup.CursorOffset, out cRow, out cByte, out cX, out cY);

		Gdk.RGBA cursorColor = isActive ? activeCursorColor : inactiveCursorColor;

		// Underline
		FillRect(cursorColor, x + cX, y + cY + drawer.Height - 2, drawer.Width * dpb, 2);

		if (cursorFocus) {
			// Thin vertical line at current digit
			FillRect(cursorColor, x + cX + cursorDigit * drawer.Width, y + cY, 1, drawer.Height - 2);
		}
	}

	/// <summary>Dispose cached Cairo surfaces used by the Drawer.</summary>
	public void DisposePixmaps()
	{
		if (isAreaRealized == false)
			return;
		drawer.DisposePixmaps();
	}

	internal virtual void BlankBackground()
	{
		Gdk.RGBA backEvenColor = drawer.GetBackgroundColor(Drawer.RowType.Even, Drawer.HighlightType.Normal);
		FillRect(backEvenColor, x, y, width, height);
	}

	internal virtual void BlankEof()
	{
		int pcRow, pcByte, pcX, pcY;
		GetDisplayInfoByOffset(areaGroup.Buffer.Size, out pcRow, out pcByte, out pcX, out pcY);
		Gdk.RGBA backEvenColor = drawer.GetBackgroundColor(Drawer.RowType.Even, Drawer.HighlightType.Normal);
		FillRect(backEvenColor, x + pcX, y + pcY, drawer.Width * dpb, drawer.Height);
	}

	public virtual void ShowPopup(Gtk.UIManager uim)
	{
		Gtk.Widget popup = uim.GetWidget("/DefaultAreaPopup");
		(popup as Gtk.Menu).Popup();
	}

}// Area

} //namespace
