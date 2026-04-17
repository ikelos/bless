/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using Cairo;

namespace Bless.Gui.Drawers {

///<summary>Draws the hex representation of a byte</summary>
public class HexDrawer : Drawer {

	// Use Zero Width Non-Joiner \u200c to avoid ligatures
	static readonly string HexTableLower = "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9fa0a1a2a3a4a5a6a7a8a9aaabacadaeafb0b1b2b3b4b5b6b7b8b9babbbcbdbebfc0c1c2c3c4c5c6c7c8c9cacbcccdcecfd0d1d2d3d4d5d6d7d8d9dadbdcdddedfe0e1e2e3e4e5e6e7e8e9eaebecedeeef\u200Cf0f1f2f3f4f5f6f7f8f9fafbfcfdfef\u200cff";
	static readonly string HexTableUpper = "000102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F202122232425262728292A2B2C2D2E2F303132333435363738393A3B3C3D3E3F404142434445464748494A4B4C4D4E4F505152535455565758595A5B5C5D5E5F606162636465666768696A6B6C6D6E6F707172737475767778797A7B7C7D7E7F808182838485868788898A8B8C8D8E8F909192939495969798999A9B9C9D9E9FA0A1A2A3A4A5A6A7A8A9AAABACADAEAFB0B1B2B3B4B5B6B7B8B9BABBBCBDBEBFC0C1C2C3C4C5C6C7C8C9CACBCCCDCECFD0D1D2D3D4D5D6D7D8D9DADBDCDDDEDFE0E1E2E3E4E5E6E7E8E9EAEBECEDEEEFF0F1F2F3F4F5F6F7F8F9FAFBFCFDFEFF";

	public HexDrawer(Gtk.Widget wid, Information inf)
		: base(wid, inf)
	{
	}

	protected override void Draw(Cairo.Context cr, int x, int y, byte b, Cairo.ImageSurface surf)
	{
		BlitSurface(cr, surf, b * 2 * width, 0, x, y, 2 * width, height);
	}

	protected override Cairo.ImageSurface Create(Gdk.RGBA fg, Gdk.RGBA bg)
	{
		string s = info.Uppercase ? HexTableUpper : HexTableLower;

		// Render in two halves to avoid driver bugs with very wide surfaces
		// First half: chars 0x00-0x7f (256 chars total but string chars 0..255)
		// Actually render the full table in two Pango calls each 256 printable chars

		Cairo.ImageSurface surf = new Cairo.ImageSurface(Cairo.Format.Argb32, 256 * 2 * width, height);
		using (Cairo.Context cr = new Cairo.Context(surf)) {
			Pango.CairoHelper.UpdateLayout(cr, pangoLayout);

			// Fill background
			cr.SetSourceRGBA(bg.Red, bg.Green, bg.Blue, bg.Alpha);
			cr.Paint();

			// Render text
			cr.SetSourceRGBA(fg.Red, fg.Green, fg.Blue, fg.Alpha);

			// First 256 printable chars (0x00–0x7f)
			pangoLayout.SetText(s.Substring(0, 256));
			cr.MoveTo(0, 0);
			Pango.CairoHelper.ShowLayout(cr, pangoLayout);

			// Second 256 printable chars (0x80–0xff), includes the ZWNJs
			pangoLayout.SetText(s.Substring(256));
			cr.MoveTo(128 * 2 * width, 0);
			Pango.CairoHelper.ShowLayout(cr, pangoLayout);
		}

		return surf;
	}
}

} // namespace
