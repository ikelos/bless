/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using System.Xml;
using Bless.Plugins;
using Bless.Gui.Drawers;
using Gtk;

namespace Bless.Gui.Areas.Plugins {

public class BinaryAreaPlugin : AreaPlugin
{
	public BinaryAreaPlugin()
	{
		name   = "binary";
		author = "Alexandros Frantzis";
	}

	public override Area CreateArea(AreaGroup ag)
	{
		return new BinaryArea(ag);
	}
}

///<summary>An area that displays binary values</summary>
public class BinaryArea : GroupedArea {

	public BinaryArea(AreaGroup ag)
			: base(ag)
	{
		type = "binary";
		dpb  = 8;
	}

	public override void Realize()
	{
		Gtk.DrawingArea da = areaGroup.DrawingArea;
		drawer = new BinaryDrawer(da, drawerInformation);
		base.Realize();
	}
}

}//namespace
