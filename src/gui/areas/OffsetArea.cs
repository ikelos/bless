/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using System;
using Gtk;
using Gdk;
using Bless.Gui.Drawers;
using Bless.Util;
using System.Xml;
using Bless.Plugins;

namespace Bless.Gui.Areas.Plugins {

public class OffsetAreaPlugin : AreaPlugin
{
	public OffsetAreaPlugin()
	{
		name   = "offset";
		author = "Alexandros Frantzis";
	}

	public override Area CreateArea(AreaGroup ag)
	{
		return new OffsetArea(ag);
	}
}

///<summary>An area that displays offsets</summary>
public class OffsetArea : Area {

	int bytes;

	public int Bytes {
		get { return bytes; }
		set { bytes = value; }
	}

	public OffsetArea(AreaGroup ag)
			: base(ag)
	{
		type  = "offset";
		bytes = 4;
	}

	protected override void RenderExtra()
	{
		if (bpr <= 0)
			return;

		int nrows = height / drawer.Height;
		long bleft = nrows * bpr;

		if (bleft + areaGroup.Offset > areaGroup.Buffer.Size)
			bleft = areaGroup.Buffer.Size - areaGroup.Offset + 1;

		int rfull = (int)(bleft / bpr);
		int blast = (int)(bleft % bpr);

		if (blast > 0)
			rfull++;

		for (int i = 0; i < rfull; i++)
			RenderRowNormal(i, 0, bpr, true);
	}

	protected override void RenderHighlight(Highlight h, Drawer.HighlightType left, Drawer.HighlightType right)
	{
		// Offset area doesn't show highlights
	}

	protected override void RenderRowNormal(int i, int p, int n, bool blank)
	{
		int rx = (bytes - 1) * 2 * drawer.Width + x;
		int ry = i * drawer.Height + y;
		long roffset = areaGroup.Offset + i * bpr;
		bool odd;
		Gdk.RGBA backEvenColor = drawer.GetBackgroundColor(Drawer.RowType.Even, Drawer.HighlightType.Normal);
		Gdk.RGBA backOddColor  = drawer.GetBackgroundColor(Drawer.RowType.Odd,  Drawer.HighlightType.Normal);

		odd = (((roffset / bpr) % 2) == 1);

		if (blank == true) {
			FillRect(odd ? backOddColor : backEvenColor, x, ry, width, drawer.Height);
		}

		Drawer.RowType rowType = odd ? Drawer.RowType.Odd : Drawer.RowType.Even;

		if (n == 0)
			return;

		for (int j = 0; j < bytes; j++) {
			drawer.DrawNormal(backCr, rx, ry, (byte)(roffset & 0xff), rowType, Drawer.ColumnType.Even);
			roffset = roffset >> 8;
			rx -= 2 * drawer.Width;
		}
	}

	protected override void RenderRowHighlight(int i, int p, int n, bool blank, Drawer.HighlightType ht)
	{
		RenderRowNormal(i, p, n, blank);
	}

	public override int CalcWidth(int n, bool force)
	{
		return 2 * bytes * drawer.Width;
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

	public override void Configure(XmlNode parentNode)
	{
		base.Configure(parentNode);

		XmlNodeList childNodes = parentNode.ChildNodes;
		foreach (XmlNode node in childNodes) {
			if (node.Name == "case")
				drawerInformation.Uppercase = (node.InnerText == "upper");
			if (node.Name == "bytes")
				this.Bytes = Convert.ToInt32(node.InnerText);
		}
	}

	public override void Realize()
	{
		Gtk.DrawingArea da = areaGroup.DrawingArea;
		drawer = new HexDrawer(da, drawerInformation);
		base.Realize();
	}
}

}//namespace
