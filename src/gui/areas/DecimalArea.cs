/*
 *   Copyright (c) 2004, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
 *   This file is part of Bless (GPL v2+).
 */
using System.Xml;
using Bless.Plugins;
using Bless.Gui.Drawers;
using Gtk;

namespace Bless.Gui.Areas.Plugins {

public class DecimalAreaPlugin : AreaPlugin
{
	public DecimalAreaPlugin()
	{
		name   = "decimal";
		author = "Alexandros Frantzis";
	}

	public override Area CreateArea(AreaGroup ag)
	{
		return new DecimalArea(ag);
	}
}

///<summary>An area that displays decimal values</summary>
public class DecimalArea : GroupedArea {

	public DecimalArea(AreaGroup ag)
			: base(ag)
	{
		type = "decimal";
		dpb  = 3;
	}

	public override void Realize()
	{
		Gtk.DrawingArea da = areaGroup.DrawingArea;
		drawer = new DecimalDrawer(da, drawerInformation);
		base.Realize();
	}
}

}//namespace
