/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using System.Xml;
using Bless.Plugins;
using Bless.Gui.Drawers;
using Gtk;

namespace Bless.Gui.Areas.Plugins {

public class OctalAreaPlugin : AreaPlugin
{
	public OctalAreaPlugin()
	{
		name   = "octal";
		author = "Alexandros Frantzis";
	}

	public override Area CreateArea(AreaGroup ag)
	{
		return new OctalArea(ag);
	}
}

///<summary>An area that displays octal values</summary>
public class OctalArea : GroupedArea {

	public OctalArea(AreaGroup ag)
			: base(ag)
	{
		type = "octal";
		dpb  = 3;
	}

	public override void Realize()
	{
		Gtk.DrawingArea da = areaGroup.DrawingArea;
		drawer = new OctalDrawer(da, drawerInformation);
		base.Realize();
	}
}

}//namespace
