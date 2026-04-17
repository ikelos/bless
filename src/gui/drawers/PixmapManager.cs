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
 *   Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA *   
 */

using System.Collections.Generic;
using Cairo;

namespace Bless.Gui.Drawers {

///<summary>Manages Cairo.ImageSurface instances efficiently (formerly Gdk.Pixmap manager)</summary>
class PixmapManager
{
	static private PixmapManager manager;

	static public PixmapManager Instance {
		get {
			if (manager == null)
				manager = new PixmapManager();
			return manager;
		}
	}

	Dictionary<string, Cairo.ImageSurface> pixmaps;
	Dictionary<string, int> references;

	private PixmapManager()
	{
		pixmaps    = new Dictionary<string, Cairo.ImageSurface>();
		references = new Dictionary<string, int>();
	}

	public string GetPixmapId(System.Type type, Drawer.Information info, Gdk.RGBA fg, Gdk.RGBA bg)
	{
		return string.Format("{0}{1}{2}{3}{4}{5}",
		                     type, info.FontName, info.FontLanguage,
		                     info.Uppercase, fg.ToString(), bg.ToString());
	}

	public Cairo.ImageSurface GetPixmap(string id)
	{
		Cairo.ImageSurface surf = null;
		if (pixmaps.ContainsKey(id))
			surf = pixmaps[id];
		return surf;
	}

	public void AddPixmap(string id, Cairo.ImageSurface surf)
	{
		pixmaps[id]    = surf;
		references[id] = 0;
	}

	public void ReferencePixmap(string id)
	{
		if (references.ContainsKey(id))
			references[id]++;
	}

	public void DereferencePixmap(string id)
	{
		if (!references.ContainsKey(id))
			return;

		references[id]--;

		if (references[id] <= 0) {
			if (pixmaps.ContainsKey(id)) {
				pixmaps[id].Dispose();
				pixmaps.Remove(id);
			}
			references.Remove(id);
		}
	}
}

} // end namespace
