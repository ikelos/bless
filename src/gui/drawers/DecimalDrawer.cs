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
using Cairo;

namespace Bless.Gui.Drawers {

///<summary>Draws the decimal representation of a byte</summary>
public class DecimalDrawer : Drawer {

	static readonly string DecimalTable =
		"000001002003004005006007008009010011012013014015016017018019020021022023024025026027028029" +
		"030031032033034035036037038039040041042043044045046047048049050051052053054055056057058059" +
		"060061062063064065066067068069070071072073074075076077078079080081082083084085086087088089" +
		"090091092093094095096097098099100101102103104105106107108109110111112113114115116117118119" +
		"120121122123124125126127128129130131132133134135136137138139140141142143144145146147148149" +
		"150151152153154155156157158159160161162163164165166167168169170171172173174175176177178179" +
		"180181182183184185186187188189190191192193194195196197198199200201202203204205206207208209" +
		"210211212213214215216217218219220221222223224225226227228229230231232233234235236237238239" +
		"240241242243244245246247248249250251252253254255";

	public DecimalDrawer(Gtk.Widget wid, Information inf)
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

			pangoLayout.SetText(DecimalTable);
			cr.SetSourceRGBA(fg.Red, fg.Green, fg.Blue, fg.Alpha);
			cr.MoveTo(0, 0);
			Pango.CairoHelper.ShowLayout(cr, pangoLayout);
		}
		return surf;
	}
}

} // namespace
