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

///<summary>Draws the binary representation of a byte</summary>
public class BinaryDrawer : Drawer {

	public BinaryDrawer(Gtk.Widget wid, Information inf)
		: base(wid, inf)
	{
	}

	protected override void Draw(Cairo.Context cr, int x, int y, byte b, Cairo.ImageSurface surf)
	{
		// Draw from MSB to LSB in 4 groups of 2 bits
		int rx = x + 6 * width;
		for (int i = 0; i < 4; i++) {
			byte k = (byte)(b & 3);
			BlitSurface(cr, surf, k * 2 * width, 0, rx, y, 2 * width, height);
			rx -= 2 * width;
			b = (byte)(b >> 2);
		}
	}

	protected override Cairo.ImageSurface Create(Gdk.RGBA fg, Gdk.RGBA bg)
	{
		// Surface contains "00011011" (4 * 2-char entries for bit-pairs 00,01,10,11)
		Cairo.ImageSurface surf = new Cairo.ImageSurface(Cairo.Format.Argb32, 4 * 2 * width, height);
		using (Cairo.Context cr = new Cairo.Context(surf)) {
			Pango.CairoHelper.UpdateLayout(cr, pangoLayout);

			cr.SetSourceRGBA(bg.Red, bg.Green, bg.Blue, bg.Alpha);
			cr.Paint();

			pangoLayout.SetText("00011011");
			cr.SetSourceRGBA(fg.Red, fg.Green, fg.Blue, fg.Alpha);
			cr.MoveTo(0, 0);
			Pango.CairoHelper.ShowLayout(cr, pangoLayout);
		}
		return surf;
	}
}

} // namespace
