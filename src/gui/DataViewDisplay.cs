/*
 *   Copyright (c) 2005, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
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
using Gtk;
using Bless.Buffers;
using Bless.Gui.Areas;
using Bless.Gui.Drawers;
using Bless.Util;

namespace Bless.Gui {

///<summary>A widget that displays data from a buffer</summary>
public class DataViewDisplay : Gtk.Box {
	Layout layout;
	Gtk.Box hbox;
	Gtk.DrawingArea drawingArea;
	Gtk.VScrollbar vscroll;
	FileChangedBar fileChangedBar;
	bool widgetRealized;

	DataViewControl dvControl;
	DataView dataView;

	public enum ShowType { Closest, Start, End, Cursor }

	public DataView View { get { return dataView; } }

	public DataViewControl Control {
		set {
			DisconnectFromControl();
			dvControl = value;
			ConnectToControl();
		}
		get { return dvControl; }
	}

	public Layout Layout {
		get { return layout; }

		set {
			Layout prevLayout = layout;
			layout.DisposePixmaps();
			layout = value;
			layout.AreaGroup.Buffer = dataView.Buffer;

			if (widgetRealized) {
				layout.Realize(drawingArea);
				Gdk.Rectangle alloc = drawingArea.Allocation;
				Resize(alloc.Width, alloc.Height);

				long prevOffset = 0;

				if (prevLayout != null && prevLayout.AreaGroup.Areas.Count > 0) {
					layout.AreaGroup.SetCursor(prevLayout.AreaGroup.CursorOffset, 0);
					layout.AreaGroup.Selection = prevLayout.AreaGroup.Selection;
				} else {
					layout.AreaGroup.SetCursor(0, 0);
				}

				MakeOffsetVisible(prevOffset, ShowType.Closest);
			}
		}
	}

	internal Gtk.VScrollbar VScroll { get { return vscroll; } }

	public new bool HasFocus {
		get { return drawingArea.HasFocus; }
		set { drawingArea.HasFocus = value; }
	}

	///<summary>Create a DataViewDisplay</summary>
	public DataViewDisplay(DataView dv) : base(Gtk.Orientation.Vertical, 0)
	{
		dataView = dv;

		layout = new Layout(FileResourcePath.GetDataPath("bless-default.layout"));

		Gtk.Adjustment adj = new Gtk.Adjustment(0.0, 0.0, 1.0, 1.0, 10.0, 0.0);
		vscroll = new Gtk.VScrollbar(adj);
		adj.ValueChanged += OnScrolled;

		drawingArea = new Gtk.DrawingArea();
		drawingArea.Realized      += OnRealized;
		drawingArea.Drawn         += OnDrawn;
		drawingArea.ConfigureEvent += OnConfigured;

        // White background via CSS provider
        var cssProvider = new Gtk.CssProvider();
        cssProvider.LoadFromData("* { background-color: white; }");
        // 600 = GTK_STYLE_PROVIDER_PRIORITY_APPLICATION
        drawingArea.StyleContext.AddProvider(cssProvider, 600);

		drawingArea.AddEvents((int)Gdk.EventMask.ButtonPressMask);
		drawingArea.AddEvents((int)Gdk.EventMask.ButtonReleaseMask);
		drawingArea.AddEvents((int)Gdk.EventMask.PointerMotionMask);
		drawingArea.AddEvents((int)Gdk.EventMask.PointerMotionHintMask);
		drawingArea.AddEvents((int)Gdk.EventMask.KeyPressMask);
		drawingArea.AddEvents((int)Gdk.EventMask.KeyReleaseMask);
		drawingArea.AddEvents((int)Gdk.EventMask.ScrollMask);
		drawingArea.AddEvents((int)Gdk.EventMask.SmoothScrollMask);

		drawingArea.CanFocus = true;

		hbox = new Gtk.Box(Gtk.Orientation.Horizontal, 0);
		hbox.PackStart(drawingArea, true,  true,  0);
		hbox.PackStart(vscroll,     false, false, 0);

		this.PackStart(hbox, true, true, 0);
	}

	///<summary>Force a complete redraw of the view</summary>
	public void Redraw()
	{
		if (!widgetRealized)
			return;

		Gdk.Rectangle alloc = drawingArea.Allocation;
		Resize(alloc.Width, alloc.Height);
		layout.AreaGroup.Invalidate();
		drawingArea.QueueDraw();
	}

	private int FindBestBpr(int width)
	{
		int n       = 1;
		int bestBpr = -1;
		int swBest  = 0;

		while (true) {
			int sw             = 0;
			bool breaksGrouping = false;
			bool breaksFixed    = false;

			foreach (Area a in layout.AreaGroup.Areas) {
				int w = a.CalcWidth(n, false);

				if (w == -1) {
					if (a.FixedBytesPerRow != -1 && n > a.FixedBytesPerRow)
						breaksFixed = true;
					else
						breaksGrouping = true;
					break;
				}
				sw += w;
			}

			if (breaksFixed && bestBpr != -1)
				break;

			if (!breaksGrouping) {
				bool shouldBreak = (sw > width || sw == swBest);

				if ((shouldBreak && bestBpr == -1) || !shouldBreak) {
					bestBpr = n;
					swBest  = sw;
				}

				if (shouldBreak)
					break;
			}

			n++;
		}

		return bestBpr;
	}

	///<summary>Benchmark the rendering</summary>
	public void Benchmark()
	{
		System.DateTime t1;
		System.DateTime t2;
		int sum = 0;

		Gdk.Rectangle alloc = drawingArea.Allocation;
		Gdk.Rectangle rect1 = new Gdk.Rectangle(0, 0, alloc.Width, alloc.Height);

		for (int i = 0; i < 100; i++) {
			t1 = System.DateTime.Now;

            var benchSurf = new Cairo.ImageSurface(Cairo.Format.Argb32, rect1.Width, rect1.Height);
            using (Cairo.Context cr = new Cairo.Context(benchSurf)) {
				layout.AreaGroup.Render(true, cr);
			}
			benchSurf.Dispose();

			t2 = System.DateTime.Now;
			sum += (t2 - t1).Milliseconds;
		}

		Gdk.Rectangle rect = drawingArea.Allocation;
		Console.WriteLine("100 render screen ({0},{1}): {2} ms", rect.Width, rect.Height, sum / 100);
	}

	private void SetupScrollbarRange()
	{
		if (layout.AreaGroup.Areas.Count <= 0)
			return;

		long bpr  = ((Area)layout.AreaGroup.Areas[0]).BytesPerRow;
		long nrows = ((dataView.Buffer.Size + 1) / bpr);

		if (nrows < vscroll.Adjustment.PageSize) {
			vscroll.Value = 0;
			vscroll.Adjustment.Lower = 0;
			vscroll.Adjustment.Upper = nrows;
			vscroll.Hide();
		} else if ((dataView.Buffer.Size + 1) % bpr == 0) {
			vscroll.SetRange(0, nrows);
			vscroll.Show();
		} else {
			vscroll.SetRange(0, nrows + 1);
			vscroll.Show();
		}
	}

	private void Resize(int winWidth, int winHeight)
	{
		int bpr = FindBestBpr(winWidth);

		if (bpr > 0)
			layout.AreaGroup.Offset = (layout.AreaGroup.Offset / bpr) * bpr;

		int s          = 0;
		int fontHeight = winHeight;

		foreach (Area a in layout.AreaGroup.Areas) {
			a.Height      = winHeight;
			a.Width       = a.CalcWidth(bpr, true);
			a.X           = s;
			a.BytesPerRow = bpr;
			s += a.Width;
			if (a.Drawer.Height < fontHeight)
				fontHeight = a.Drawer.Height;
		}

		vscroll.Adjustment.PageSize = (winHeight / fontHeight);
		vscroll.SetIncrements(3, vscroll.Adjustment.PageSize - 1);

		if (bpr != 0)
			SetupScrollbarRange();

		layout.AreaGroup.Invalidate();
	}

	void OnConfigured(object o, ConfigureEventArgs args)
	{
		if (widgetRealized == false)
			return;

		Gdk.EventConfigure conf = args.Event;
		Resize(conf.Width, conf.Height);
		MakeOffsetVisible(dataView.Offset, ShowType.Start);
	}

	///<summary>Handle the Drawn Event (GTK3 replacement for ExposeEvent)</summary>
	void OnDrawn(object o, DrawnArgs args)
	{
		layout.AreaGroup.Render(true, args.Cr);
	}

	void OnRealized(object o, EventArgs args)
	{
		layout.Realize(drawingArea);
		widgetRealized = true;

		Gdk.Rectangle alloc = ((Widget)o).Allocation;
		Resize(alloc.Width, alloc.Height);
	}

	void OnScrolled(object o, EventArgs args)
	{
		int bpr = 0;
		if (layout.AreaGroup.Areas.Count > 0)
			bpr = ((Area)layout.AreaGroup.Areas[0]).BytesPerRow;

		long offset = (long)vscroll.Adjustment.Value * bpr;
		layout.AreaGroup.Offset = offset;
	}

	public void MakeOffsetVisible(long offset, ShowType type)
	{
		if (layout.AreaGroup.Areas.Count <= 0)
			return;

		int bpr = ((Area)layout.AreaGroup.Areas[0]).BytesPerRow;
		if (bpr == 0)
			return;

		long curOffset    = layout.AreaGroup.Offset;
		int h             = ((Area)layout.AreaGroup.Areas[0]).Height;
		Drawer font       = ((Area)layout.AreaGroup.Areas[0]).Drawer;
		int nrows         = h / font.Height;

		long curOffsetRow    = curOffset / bpr;
		long curOffsetEndRow = curOffsetRow + nrows - 1;
		long offsetRow       = offset / bpr;

		if (type == ShowType.Closest) {
			if (curOffsetRow > offsetRow)
				type = ShowType.Start;
			else if (curOffsetEndRow < offsetRow)
				type = ShowType.End;
		}

		SetupScrollbarRange();

		if (type == ShowType.Cursor) {
			long cursorRow = layout.AreaGroup.CursorOffset / bpr;
			int diff = (int)(cursorRow - curOffsetRow);

			if (diff <= nrows && diff >= 0)
				vscroll.Value = offsetRow - diff;
			else if (diff > nrows)
				type = ShowType.End;
			else if (diff < 0)
				type = ShowType.Start;
		}

		if (type == ShowType.Start) {
			vscroll.Value = offsetRow;
		} else if (type == ShowType.End) {
			if (offsetRow - nrows >= 0)
				vscroll.Value = offsetRow - nrows + 1;
			else
				vscroll.Value = 0;
		}
	}

	public void ShowFileChangedBar()
	{
		if (fileChangedBar == null) {
			fileChangedBar = new FileChangedBar(this.View);
			this.PackStart(fileChangedBar, false, false, 0);
		}

		this.ReorderChild(fileChangedBar, 0);
		fileChangedBar.ShowAll();
	}

	public void Cleanup()
	{
		layout.DisposePixmaps();
		layout    = null;
		dataView  = null;
		dvControl = null;
	}

	public void GrabKeyboardFocus()
	{
		if (!drawingArea.HasFocus)
			drawingArea.GrabFocus();
	}

	private void ConnectToControl()
	{
		if (dvControl == null)
			return;

		drawingArea.ButtonPressEvent   += dvControl.OnButtonPress;
		drawingArea.ButtonReleaseEvent += dvControl.OnButtonRelease;
		drawingArea.MotionNotifyEvent  += dvControl.OnMotionNotify;
		drawingArea.KeyPressEvent      += dvControl.OnKeyPress;
		drawingArea.KeyReleaseEvent    += dvControl.OnKeyRelease;
		drawingArea.ScrollEvent        += dvControl.OnMouseWheel;
		drawingArea.FocusInEvent       += dvControl.OnFocusInEvent;
		drawingArea.FocusOutEvent      += dvControl.OnFocusOutEvent;
	}

	private void DisconnectFromControl()
	{
		if (dvControl == null)
			return;

		drawingArea.ButtonPressEvent   -= dvControl.OnButtonPress;
		drawingArea.ButtonReleaseEvent -= dvControl.OnButtonRelease;
		drawingArea.MotionNotifyEvent  -= dvControl.OnMotionNotify;
		drawingArea.KeyPressEvent      -= dvControl.OnKeyPress;
		drawingArea.KeyReleaseEvent    -= dvControl.OnKeyRelease;
		drawingArea.ScrollEvent        -= dvControl.OnMouseWheel;
		drawingArea.FocusInEvent       -= dvControl.OnFocusInEvent;
		drawingArea.FocusOutEvent      -= dvControl.OnFocusOutEvent;
	}

}// end DataViewDisplay

}//namespace
