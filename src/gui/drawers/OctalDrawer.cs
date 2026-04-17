/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using Cairo;

namespace Bless.Gui.Drawers {

///<summary>Draws the octal representation of a byte</summary>
public class OctalDrawer : Drawer {

	static readonly string OctalTable =
		"000001002003004005006007010011012013014015016017020021022023024025026027030031032033034035036037" +
		"040041042043044045046047050051052053054055056057060061062063064065066067070071072073074075076077" +
		"100101102103104105106107110111112113114115116117120121122123124125126127130131132133134135136137" +
		"140141142143144145146147150151152153154155156157160161162163164165166167170171172173174175176177" +
		"200201202203204205206207210211212213214215216217220221222223224225226227230231232233234235236237" +
		"240241242243244245246247250251252253254255256257260261262263264265266267270271272273274275276277" +
		"300301302303304305306307310311312313314315316317320321322323324325326327330331332333334335336337" +
		"340341342343344345346347350351352353354355356357360361362363364365366367370371372373374375376377";

	public OctalDrawer(Gtk.Widget wid, Information inf)
		: base(wid, inf)
	{
	}

	protected override void Draw(Cairo.Context cr, int x, int y, byte b, Cairo.ImageSurface surf)
	{
		BlitSurface(cr, surf, b * 3 * width, 0, x, y, 3 * width, height);
	}

	protected override Cairo.ImageSurface Create(Gdk.RGBA fg, Gdk.RGBA bg)
	{
		Cairo.ImageSurface surf = new Cairo.ImageSurface(Cairo.Format.Argb32, 256 * 3 * width, height);
		using (Cairo.Context cr = new Cairo.Context(surf)) {
			Pango.CairoHelper.UpdateLayout(cr, pangoLayout);

			cr.SetSourceRGBA(bg.Red, bg.Green, bg.Blue, bg.Alpha);
			cr.Paint();

			pangoLayout.SetText(OctalTable);
			cr.SetSourceRGBA(fg.Red, fg.Green, fg.Blue, fg.Alpha);
			cr.MoveTo(0, 0);
			Pango.CairoHelper.ShowLayout(cr, pangoLayout);
		}
		return surf;
	}
}

} // namespace
