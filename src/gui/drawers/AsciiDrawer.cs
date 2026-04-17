/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using Cairo;

namespace Bless.Gui.Drawers {

///<summary>Draws the ASCII representation of a byte</summary>
public class AsciiDrawer : Drawer {

	static readonly string AsciiTable =
		"................................ !\"#$%&'()*+,-./0123456789:;<=>?" +
		"@ABCDEFGHI\u200cJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghi\u200cjklmnopqrs\u200ctuvwxyz{|}~" +
		".................................................................................................................................";

	public AsciiDrawer(Gtk.Widget wid, Information inf)
		: base(wid, inf)
	{
	}

	protected override void Draw(Cairo.Context cr, int x, int y, byte b, Cairo.ImageSurface surf)
	{
		BlitSurface(cr, surf, b * width, 0, x, y, width, height);
	}

	protected override Cairo.ImageSurface Create(Gdk.RGBA fg, Gdk.RGBA bg)
	{
		Cairo.ImageSurface surf = new Cairo.ImageSurface(Cairo.Format.Argb32, 256 * width, height);
		using (Cairo.Context cr = new Cairo.Context(surf)) {
			Pango.CairoHelper.UpdateLayout(cr, pangoLayout);

			cr.SetSourceRGBA(bg.Red, bg.Green, bg.Blue, bg.Alpha);
			cr.Paint();

			pangoLayout.SetText(AsciiTable);
			cr.SetSourceRGBA(fg.Red, fg.Green, fg.Blue, fg.Alpha);
			cr.MoveTo(0, 0);
			Pango.CairoHelper.ShowLayout(cr, pangoLayout);
		}
		return surf;
	}
}

} // namespace
