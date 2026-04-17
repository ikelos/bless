/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using System;
using Gtk;
using Gdk;
using Bless.Gui.Drawers;
using Bless.Util;
using Bless.Plugins;

namespace Bless.Gui.Areas.Plugins {

public class SeparatorAreaPlugin : AreaPlugin
{
	public SeparatorAreaPlugin()
	{
		name   = "separator";
		author = "Alexandros Frantzis";
	}

	public override Area CreateArea(AreaGroup ag)
	{
		return new SeparatorArea(ag);
	}
}

///<summary>An area that shows a vertical separator line</summary>
public class SeparatorArea : Area
{
	Gdk.RGBA lineColor;

	public SeparatorArea(AreaGroup ag)
			: base(ag)
	{
		type = "separator";
	}

	public override void Realize()
	{
		Gtk.DrawingArea da = areaGroup.DrawingArea;
		drawer    = new DummyDrawer(da, drawerInformation);
		lineColor = drawer.Info.fgNormal[(int)Drawer.RowType.Even, (int)Drawer.ColumnType.Even].RgbaColor;
		base.Realize();
	}

	protected override void RenderHighlight(Highlight h, Drawer.HighlightType left, Drawer.HighlightType right)
	{
		// Separator shows no highlights
	}

	protected override void RenderRowNormal(int i, int p, int n, bool blank)
	{
	}

	protected override void RenderRowHighlight(int i, int p, int n, bool blank, Drawer.HighlightType ht)
	{
	}

	protected override void RenderExtra()
	{
		if (isAreaRealized == false)
			return;

		int nrows = height / drawer.Height;
		long bleft = nrows * bpr;
		int rfull = 0;
		int blast = 0;

		if (bpr > 0) {
			if (bleft + areaGroup.Offset > areaGroup.Buffer.Size)
				bleft = areaGroup.Buffer.Size - areaGroup.Offset + 1;

			rfull = (int)(bleft / bpr);
			blast = (int)(bleft % bpr);
			if (blast != 0)
				rfull++;
		}

		if (rfull == 0)
			return;

		// Draw the vertical separator line using Cairo
		backCr.SetSourceRGBA(lineColor.Red, lineColor.Green, lineColor.Blue, lineColor.Alpha);
		backCr.LineWidth = 1;
		backCr.MoveTo(x + drawer.Width / 2, 0);
		backCr.LineTo(x + drawer.Width / 2, drawer.Height * rfull);
		backCr.Stroke();
	}

	public override int CalcWidth(int n, bool force)
	{
		return drawer.Width;
	}

	public override void GetDisplayInfoByOffset(long off, out int orow, out int obyte, out int ox, out int oy)
	{
		orow  = (int)((off - areaGroup.Offset) / bpr);
		obyte = (int)((off - areaGroup.Offset) % bpr);
		oy    = orow * drawer.Height;
		ox    = 0;
	}

	public override long GetOffsetByDisplayInfo(int x, int y, out int digit, out GetOffsetFlags flags)
	{
		flags = 0;
		int row  = y / drawer.Height;
		long off = row * bpr + areaGroup.Offset;
		if (off >= areaGroup.Buffer.Size)
			flags |= GetOffsetFlags.Eof;
		digit = 0;
		return off;
	}
}

}//namespace
