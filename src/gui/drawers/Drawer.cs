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

using System;
using System.Collections.Specialized;
using Gtk;
using Gdk;
using Pango;
using Cairo;

namespace Bless.Gui.Drawers {

///<summary>Fast font drawing class</summary>
public abstract class Drawer {

	public class Color {
		public Color(Gdk.RGBA color)
		{
			this.RgbaColor = color;
		}

		public Gdk.RGBA RgbaColor;
	}

	public class Information {
		public string FontName;
		public string FontLanguage;

		public Drawer.Color[,] fgNormal;
		public Drawer.Color[,] bgNormal;

		public Drawer.Color[,] fgHighlight;
		public Drawer.Color[,] bgHighlight;

		public bool Uppercase;

		public Information()
		{
			FontName = "BlessCourier, Courier 12";
			FontLanguage = "utf-8";

			fgNormal = new Drawer.Color[2, 2];
			bgNormal = new Drawer.Color[2, 2];

			fgHighlight = new Drawer.Color[2, (int)HighlightType.Sentinel];
			bgHighlight = new Drawer.Color[2, (int)HighlightType.Sentinel];

			fgNormal[(int)RowType.Even, (int)ColumnType.Even] = new Drawer.Color(ParseRGBA("black"));
			bgNormal[(int)RowType.Even, (int)ColumnType.Even] = new Drawer.Color(ParseRGBA("white"));

			fgNormal[(int)RowType.Even, (int)ColumnType.Odd]  = new Drawer.Color(ParseRGBA("blue"));
			bgNormal[(int)RowType.Even, (int)ColumnType.Odd]  = new Drawer.Color(ParseRGBA("white"));

			fgNormal[(int)RowType.Odd,  (int)ColumnType.Even] = new Drawer.Color(ParseRGBA("black"));
			bgNormal[(int)RowType.Odd,  (int)ColumnType.Even] = new Drawer.Color(ParseRGBA("white"));

			fgNormal[(int)RowType.Odd,  (int)ColumnType.Odd]  = new Drawer.Color(ParseRGBA("blue"));
			bgNormal[(int)RowType.Odd,  (int)ColumnType.Odd]  = new Drawer.Color(ParseRGBA("white"));

			// leave unspecified - will be set up using theme defaults
			for (int i = 0; i < (int)HighlightType.Sentinel; i++) {
				fgHighlight[(int)RowType.Even, i] = null;
				bgHighlight[(int)RowType.Even, i] = null;
				fgHighlight[(int)RowType.Odd, i] = null;
				bgHighlight[(int)RowType.Odd, i] = null;
			}

			Uppercase = false;
		}

		/// <summary>Parse a CSS colour name or spec into a Gdk.RGBA value.</summary>
		public static Gdk.RGBA ParseRGBA(string name)
		{
			var c = new Gdk.RGBA();
			c.Parse(name);
			return c;
		}

		/// <summary>Make a colour lighter while keeping its hue</summary>
		static Gdk.RGBA MakeRgbaLighter(Gdk.RGBA col, double factor)
		{
			return new Gdk.RGBA {
				Red   = col.Red   + (1.0 - col.Red)   * factor,
				Green = col.Green + (1.0 - col.Green) * factor,
				Blue  = col.Blue  + (1.0 - col.Blue)  * factor,
				Alpha = col.Alpha
			};
		}

		/// <summary>Make a colour darker while keeping its hue</summary>
		static Gdk.RGBA MakeRgbaDarker(Gdk.RGBA col, double factor)
		{
			return new Gdk.RGBA {
				Red   = col.Red   * factor,
				Green = col.Green * factor,
				Blue  = col.Blue  * factor,
				Alpha = col.Alpha
			};
		}

		/// <summary>Setup unspecified highlight colours using theme default colours</summary>
		public void SetupHighlight(Gtk.Widget widget)
		{
			Gdk.RGBA selFg = widget.StyleContext.GetColor(Gtk.StateFlags.Selected);

			Gdk.RGBA selBg;
			if (!widget.StyleContext.LookupColor("theme_selected_bg_color", out selBg))
				selBg = new Gdk.RGBA { Red = 0.2, Green = 0.4, Blue = 0.8, Alpha = 1.0 };

			Gdk.RGBA patMatchBg = MakeRgbaLighter(selBg, 0.6);
			Gdk.RGBA patMatchFg = MakeRgbaDarker(selFg, 0.4);

			// Selection
			if (fgHighlight[(int)RowType.Even, (int)HighlightType.Selection] == null)
				fgHighlight[(int)RowType.Even, (int)HighlightType.Selection] = new Drawer.Color(selFg);
			if (bgHighlight[(int)RowType.Even, (int)HighlightType.Selection] == null)
				bgHighlight[(int)RowType.Even, (int)HighlightType.Selection] = new Drawer.Color(selBg);
			if (fgHighlight[(int)RowType.Odd, (int)HighlightType.Selection] == null)
				fgHighlight[(int)RowType.Odd, (int)HighlightType.Selection] = new Drawer.Color(selFg);
			if (bgHighlight[(int)RowType.Odd, (int)HighlightType.Selection] == null)
				bgHighlight[(int)RowType.Odd, (int)HighlightType.Selection] = new Drawer.Color(selBg);

			// Pattern match (secondary selection)
			if (fgHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch] == null)
				fgHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch] = new Drawer.Color(patMatchFg);
			if (bgHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch] == null)
				bgHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch] = new Drawer.Color(patMatchBg);
			if (fgHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch] == null)
				fgHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch] = new Drawer.Color(patMatchFg);
			if (bgHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch] == null)
				bgHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch] = new Drawer.Color(patMatchBg);
		}
	}

	// Highlight drawing priority: Normal < Bookmark < PatternMatch < Selection
	public enum HighlightType { Normal, Bookmark, PatternMatch, Selection, Sentinel }
	public enum RowType { Even, Odd }
	public enum ColumnType { Even, Odd }

	protected Gtk.Widget widget;
	protected Pango.FontDescription fontDescription;
	protected Information info;

	// Cairo ImageSurface-based pre-rendered character caches (replaces Gdk.Pixmap)
	protected Cairo.ImageSurface[,] surfacesNormal;
	protected Cairo.ImageSurface[,] surfacesHighlight;
	protected StringCollection surfaceIds;

	protected Pango.Layout pangoLayout;

	// Background colours
	protected Gdk.RGBA[,] backColor;

	protected int width;
	protected int height;

	///<summary>Constructor</summary>
	public Drawer(Gtk.Widget wid, Information inf)
	{
		widget = wid;
		info = inf;
		surfaceIds = new StringCollection();

		// Ensure highlight colors are set from the widget's theme
		info.SetupHighlight(wid);

		fontDescription = Pango.FontDescription.FromString(info.FontName);
		Pango.Language lang = Pango.Language.FromString(info.FontLanguage);

		Pango.Context pangoCtx = widget.PangoContext;
		pangoCtx.FontDescription = fontDescription;
		pangoCtx.Language = lang;

		// Measure character size using a monospaced font
		pangoLayout = new Pango.Layout(pangoCtx);
		pangoLayout.SetText("X");
		pangoLayout.GetPixelSize(out width, out height);
		pangoLayout.SetText("");

		InitializeSurfaces();
		InitializeBackgroundColors();
	}

	void InitializeSurfaces()
	{
		surfacesNormal    = new Cairo.ImageSurface[2, 2];
		surfacesHighlight = new Cairo.ImageSurface[2, (int)HighlightType.Sentinel];

		Drawer.Color colorFg, colorBg;

		// Even rows
		colorFg = info.fgNormal[(int)RowType.Even, (int)ColumnType.Even];
		colorBg = info.bgNormal[(int)RowType.Even, (int)ColumnType.Even];
		surfacesNormal[(int)RowType.Even, (int)ColumnType.Even] = CreateWrapper(colorFg, colorBg);

		colorFg = info.fgNormal[(int)RowType.Even, (int)ColumnType.Odd];
		colorBg = info.bgNormal[(int)RowType.Even, (int)ColumnType.Odd];
		surfacesNormal[(int)RowType.Even, (int)ColumnType.Odd] = CreateWrapper(colorFg, colorBg);

		colorFg = info.fgHighlight[(int)RowType.Even, (int)HighlightType.Selection];
		colorBg = info.bgHighlight[(int)RowType.Even, (int)HighlightType.Selection];
		surfacesHighlight[(int)RowType.Even, (int)HighlightType.Selection] = CreateWrapper(colorFg, colorBg);

		colorFg = info.fgHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch];
		colorBg = info.bgHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch];
		surfacesHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch] = CreateWrapper(colorFg, colorBg);

		// Odd rows
		colorFg = info.fgNormal[(int)RowType.Odd, (int)ColumnType.Even];
		colorBg = info.bgNormal[(int)RowType.Odd, (int)ColumnType.Even];
		surfacesNormal[(int)RowType.Odd, (int)ColumnType.Even] = CreateWrapper(colorFg, colorBg);

		colorFg = info.fgNormal[(int)RowType.Odd, (int)ColumnType.Odd];
		colorBg = info.bgNormal[(int)RowType.Odd, (int)ColumnType.Odd];
		surfacesNormal[(int)RowType.Odd, (int)ColumnType.Odd] = CreateWrapper(colorFg, colorBg);

		colorFg = info.fgHighlight[(int)RowType.Odd, (int)HighlightType.Selection];
		colorBg = info.bgHighlight[(int)RowType.Odd, (int)HighlightType.Selection];
		surfacesHighlight[(int)RowType.Odd, (int)HighlightType.Selection] = CreateWrapper(colorFg, colorBg);

		colorFg = info.fgHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch];
		colorBg = info.bgHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch];
		surfacesHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch] = CreateWrapper(colorFg, colorBg);
	}

	void InitializeBackgroundColors()
	{
		backColor = new Gdk.RGBA[2, (int)Drawer.HighlightType.Sentinel];

		Drawer.Color col;

		// Normal even/odd
		col = info.bgNormal[(int)RowType.Even, (int)ColumnType.Even];
		backColor[(int)RowType.Even, (int)HighlightType.Normal] = col.RgbaColor;

		col = info.bgNormal[(int)RowType.Odd, (int)ColumnType.Even];
		backColor[(int)RowType.Odd, (int)HighlightType.Normal] = col.RgbaColor;

		// Selection
		col = info.bgHighlight[(int)RowType.Even, (int)HighlightType.Selection];
		backColor[(int)RowType.Even, (int)HighlightType.Selection] = col.RgbaColor;

		col = info.bgHighlight[(int)RowType.Odd, (int)HighlightType.Selection];
		backColor[(int)RowType.Odd, (int)HighlightType.Selection] = col.RgbaColor;

		// PatternMatch
		col = info.bgHighlight[(int)RowType.Even, (int)HighlightType.PatternMatch];
		backColor[(int)RowType.Even, (int)HighlightType.PatternMatch] = col.RgbaColor;

		col = info.bgHighlight[(int)RowType.Odd, (int)HighlightType.PatternMatch];
		backColor[(int)RowType.Odd, (int)HighlightType.PatternMatch] = col.RgbaColor;
	}

	///<summary>Wrapper that avoids creating duplicate surfaces for the same fg/bg combination</summary>
	private Cairo.ImageSurface CreateWrapper(Drawer.Color fg, Drawer.Color bg)
	{
		string id = PixmapManager.Instance.GetPixmapId(this.GetType(), info, fg.RgbaColor, bg.RgbaColor);

		Cairo.ImageSurface surf = PixmapManager.Instance.GetPixmap(id);
		if (surf == null) {
			surf = Create(fg.RgbaColor, bg.RgbaColor); // may return null for DummyDrawer
			if (surf != null) {
				PixmapManager.Instance.AddPixmap(id, surf);
				PixmapManager.Instance.ReferencePixmap(id);
				surfaceIds.Add(id);
			}
		}
		else {
			PixmapManager.Instance.ReferencePixmap(id);
			surfaceIds.Add(id);
		}

		return surf;
	}

	///<summary>Creates a Cairo.ImageSurface with the pre-rendered character strip</summary>
	abstract protected Cairo.ImageSurface Create(Gdk.RGBA fg, Gdk.RGBA bg);

	///<summary>Draws a single byte at (x,y) using the provided surface strip and Cairo context</summary>
	abstract protected void Draw(Cairo.Context cr, int x, int y, byte b, Cairo.ImageSurface surf);

	///<summary>Copy a horizontal strip of pixels from surf to cr at (destX, destY)</summary>
	protected static void BlitSurface(Cairo.Context cr, Cairo.ImageSurface surf,
	                                  int srcX, int srcY, int destX, int destY,
	                                  int w, int h)
	{
		cr.Save();
		cr.Rectangle(destX, destY, w, h);
		cr.Clip();
		cr.SetSourceSurface(surf, destX - srcX, destY - srcY);
		cr.Paint();
		cr.Restore();
	}

	public void DrawNormal(Cairo.Context cr, int x, int y, byte b, RowType rowType, ColumnType colType)
	{
		Draw(cr, x, y, b, surfacesNormal[(int)rowType, (int)colType]);
	}

	public void DrawHighlight(Cairo.Context cr, int x, int y, byte b, RowType rowType, HighlightType ht)
	{
		Draw(cr, x, y, b, surfacesHighlight[(int)rowType, (int)ht]);
	}

	public Gdk.RGBA GetBackgroundColor(RowType rowType, HighlightType ht)
	{
		return backColor[(int)rowType, (int)ht];
	}

	public void DisposePixmaps()
	{
		foreach (string id in surfaceIds)
			PixmapManager.Instance.DereferencePixmap(id);
		surfaceIds.Clear();
	}

	public int Width  { get { return width;  } }
	public int Height { get { return height; } }

	public Drawer.Information Info { get { return info; } }
}

///<summary>Dummy drawer (no-op)</summary>
public class DummyDrawer : Drawer {

	public DummyDrawer(Gtk.Widget wid, Information inf)
		: base(wid, inf)
	{
	}

	protected override void Draw(Cairo.Context cr, int x, int y, byte b, Cairo.ImageSurface surf)
	{
	}

	protected override Cairo.ImageSurface Create(Gdk.RGBA fg, Gdk.RGBA bg)
	{
		return null;
	}
}

} // end namespace
