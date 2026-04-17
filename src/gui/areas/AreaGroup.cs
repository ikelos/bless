/*
 *   Copyright (c) 2008, Alexandros Frantzis (alf82 [at] freemail [dot] gr)
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

using System;
using System.Collections.Generic;
using Bless.Buffers;
using Bless.Util;
using Bless.Gui.Drawers;
using Cairo;

namespace Bless.Gui.Areas
{

/// <summary>An atomic highlight</summary>
class AtomicHighlight : Highlight
{
	IList<Highlight> containers;

	public IList<Highlight> Containers {
		get { return containers; }
	}

	public void AddContainer(Highlight h)
	{
		if (h.Type > type)
			return;

		for (int i = 0; i < containers.Count; i++) {
			if (h.Type >= containers[i].Type) {
				containers.Insert(i, h);
				return;
			}
		}
		containers.Add(h);
	}

	public void GetAbyssHighlights(out Drawer.HighlightType left, out Drawer.HighlightType right)
	{
		left  = Drawer.HighlightType.Sentinel;
		right = Drawer.HighlightType.Sentinel;

		foreach (Highlight h in containers) {
			if (left  == Drawer.HighlightType.Sentinel && h.Contains(start - 1))
				left  = h.Type;
			if (right == Drawer.HighlightType.Sentinel && h.Contains(end + 1))
				right = h.Type;
		}

		if (left  == Drawer.HighlightType.Sentinel) left  = Drawer.HighlightType.Normal;
		if (right == Drawer.HighlightType.Sentinel) right = Drawer.HighlightType.Normal;
	}

	public AtomicHighlight(Highlight parent) : base(parent)
	{
		this.containers = new System.Collections.Generic.List<Highlight>(1);
		this.containers.Add(parent);
	}

	public AtomicHighlight(AtomicHighlight h) : base(h)
	{
		this.containers = new System.Collections.Generic.List<Highlight>(h.containers);
	}

	public override string ToString()
	{
		string str = base.ToString() + " Containers: ";
		foreach (Highlight h in containers)
			str += h.ToString() + ", ";

		Drawer.HighlightType left, right;
		this.GetAbyssHighlights(out left, out right);
		str += string.Format("Left: {0} Right: {0}", left, right);
		return str;
	}
}

/// <summary>
/// A group of areas that display data from the same source and are synchronized.
/// </summary>
public class AreaGroup
{
	IList<Area> areas;
	ByteBuffer byteBuffer;
	Gtk.DrawingArea drawingArea;
	Area focusedArea;

	IntervalTree<Highlight> highlights;

	enum Changes { Offset = 1, Cursor = 2, Highlights = 4 }

	Changes changes;

	// Current offset of view in the buffer
	long offset;

	// Current cursor
	long cursorOffset;

	// Track changes
	long prevCursorOffset;

	byte[] bufferCache;

	/// <value>
	/// Previous atomic highlight ranges of the view (for efficient diff rendering).
	/// </value>
	IntervalTree<AtomicHighlight> prevAtomicHighlights;

	Highlight selection;

	public IList<Area> Areas { get { return areas; } }

	public Area FocusedArea {
		get { return focusedArea; }
		set { UpdateFocusedArea(value); }
	}

	public long Offset {
		get { return offset; }
		set {
			if (offset == value)
				return;
			offset = value;
			SetChanged(Changes.Offset);
		}
	}

	public long CursorOffset { get { return cursorOffset; } }

	public int CursorDigit {
		get {
			return (focusedArea != null) ? focusedArea.CursorDigit : 0;
		}
	}

	public long PrevCursorOffset { get { return prevCursorOffset; } }

	public ByteBuffer Buffer {
		get { return byteBuffer; }
		set { byteBuffer = value; SetChanged(Changes.Offset); }
	}

	public Gtk.DrawingArea DrawingArea {
		get { return drawingArea; }
		set { drawingArea = value; }
	}

	public Util.Range Selection {
		get { return selection; }
		set {
			if (selection == value)
				return;
			highlights.Delete(selection);
			selection.Start = value.Start; selection.End = value.End;
			SetChanged(Changes.Highlights | Changes.Cursor);
		}
	}

	public void SetCursor(long coffset, int cdigit)
	{
		prevCursorOffset = cursorOffset;

		if (cursorOffset == coffset && this.CursorDigit == cdigit)
			return;

		cursorOffset = coffset;
		foreach (Area a in areas)
			a.CursorDigit = cdigit;

		SetChanged(Changes.Cursor);
	}

	public byte GetCachedByte(long pos)
	{
		return bufferCache[pos - offset];
	}

	public AreaGroup()
	{
		areas                = new System.Collections.Generic.List<Area>();
		highlights           = new IntervalTree<Highlight>();
		selection            = new Highlight(Drawer.HighlightType.Selection);
		prevAtomicHighlights = new IntervalTree<AtomicHighlight>();
		bufferCache          = new byte[0];
	}

	/// <summary>Get the range of bytes and number of rows in the current view.</summary>
	public Util.Range GetViewRange(out int nrows)
	{
		int minRows = int.MaxValue;
		int minBpr  = int.MaxValue;
		foreach (Area a in areas) {
			minRows = Math.Min(minRows, a.Height / a.Drawer.Height);
			minBpr  = Math.Min(minBpr,  a.BytesPerRow);
		}

		nrows = minRows;
		long bleft = minRows * minBpr;

		if (bleft + offset >= byteBuffer.Size)
			bleft = byteBuffer.Size - offset;

		return (bleft > 0)
			? new Util.Range(offset, offset + bleft - 1)
			: new Util.Range();
	}

	private bool HasChanged(Changes c)      { return ((changes & c) != 0); }
	private bool HasAnythingChanged()       { return changes != 0; }
	private void ClearChanges()             { changes = 0; }

	private void SetChanged(Changes c)
	{
		changes |= c;

		Gtk.Application.Invoke(delegate {
			if (drawingArea == null || drawingArea.Window == null)
				return;

            drawingArea.QueueDraw();
		});
	}

	public void Invalidate()   { changes |= Changes.Offset; }
	public void RedrawNow()    { SetChanged(Changes.Offset); }

	private void InitializeHighlights()
	{
		ClearHighlights();
		if (!selection.IsEmpty())
			highlights.Insert(selection);
	}

	public void AddHighlight(long start, long end, Drawer.HighlightType ht)
	{
		highlights.Insert(new Highlight(start, end, ht));
		changes |= Changes.Highlights;
	}

	private void ClearHighlights() { highlights.Clear(); }

	private void SetupBufferCache()
	{
		int nrows;
		Util.Range view = GetViewRange(out nrows);
		if (view.Size != bufferCache.Length)
			bufferCache = new byte[view.Size];

		for (int i = 0; i < view.Size; i++)
			bufferCache[i] = byteBuffer[view.Start + i];
	}

	public void CycleFocus()
	{
		int faIndex;
		for (faIndex = 0; faIndex < areas.Count; faIndex++)
			if (focusedArea == areas[faIndex])
				break;

		if (faIndex >= areas.Count)
			faIndex = -1;

		int end = faIndex + areas.Count;

		for (faIndex++; faIndex < end; faIndex++) {
			Area a = (areas[faIndex % areas.Count] as Area);
			if (a.CanFocus == true) {
				UpdateFocusedArea(a);
				return;
			}
		}

		focusedArea = null;
	}

	private void UpdateFocusedArea(Area fa)
	{
		focusedArea = fa;

		foreach (Area a in areas)
			a.HasCursorFocus = false;

		focusedArea.HasCursorFocus = true;
		prevCursorOffset = cursorOffset;
		SetChanged(Changes.Cursor);
	}

	private void RenderExtra()
	{
		foreach (Area a in areas)
			a.RenderExtra();
	}

	private void RenderHighlight(AtomicHighlight h)
	{
		Drawer.HighlightType left, right;
		h.GetAbyssHighlights(out left, out right);

		foreach (Area a in areas)
			a.RenderHighlight(h, left, right);
	}

	private void BlankBackground()
	{
		foreach (Area a in areas)
			a.BlankBackground();
	}

	private AtomicHighlight[] SplitAtomicPrioritized(AtomicHighlight q, Highlight r)
	{
		AtomicHighlight[] ha;

		if (q.Type > r.Type) {
			ha = new AtomicHighlight[3] {
				new AtomicHighlight(r), new AtomicHighlight(q), new AtomicHighlight(r)
			};
			Util.Range.SplitAtomic(ha, r, q);
			ha[1].AddContainer(r);
		} else {
			ha = new AtomicHighlight[3] {
				new AtomicHighlight(q), new AtomicHighlight(r), new AtomicHighlight(q)
			};
			Util.Range.SplitAtomic(ha, q, r);
			foreach (Highlight h in q.Containers)
				ha[1].AddContainer(h);
		}

		return ha;
	}

	private IntervalTree<AtomicHighlight> BreakDownHighlights(Highlight s, IList<Highlight> lst)
	{
		IntervalTree<AtomicHighlight> it = new IntervalTree<AtomicHighlight>();

		if (!s.IsEmpty())
			it.Insert(new AtomicHighlight(s));

		foreach (Highlight r in lst) {
			IList<AtomicHighlight> overlaps = it.SearchOverlap(r);
			foreach (AtomicHighlight q in overlaps) {
				it.Delete(q);
				AtomicHighlight[] ha = SplitAtomicPrioritized(q, r);
				foreach (AtomicHighlight h in ha) {
					h.Intersect(q);
					if (!h.IsEmpty())
						it.Insert(h);
				}
			}
		}

		return it;
	}

	private IntervalTree<AtomicHighlight> GetAtomicHighlights()
	{
		int nrows;
		Util.Range clip = GetViewRange(out nrows);
		Highlight view  = new Highlight(clip, Drawer.HighlightType.Normal);

		IList<Highlight> viewableHighlights = highlights.SearchOverlap(view);
		return BreakDownHighlights(view, viewableHighlights);
	}

	private void RenderAtomicHighlights(IntervalTree<AtomicHighlight> atomicHighlights)
	{
		IList<AtomicHighlight> hl = atomicHighlights.GetValues();
		foreach (AtomicHighlight h in hl)
			RenderHighlight(h);
	}

	private void RenderAll(IntervalTree<AtomicHighlight> atomicHighlights)
	{
		SetupBufferCache();
		BlankBackground();
		RenderExtra();
		RenderAtomicHighlights(atomicHighlights);
		RenderCursor(atomicHighlights);
	}

	private void RenderHighlightDiffs(IntervalTree<AtomicHighlight> atomicHighlights)
	{
		IList<AtomicHighlight> hl = atomicHighlights.GetValues();

		foreach (AtomicHighlight h in hl) {
			IList<AtomicHighlight> overlaps = prevAtomicHighlights.SearchOverlap(h);
			foreach (AtomicHighlight overlap in overlaps) {
				AtomicHighlight hTmp = new AtomicHighlight(h);
				hTmp.Intersect(overlap);
				AtomicHighlight oTmp = new AtomicHighlight(overlap);
				oTmp.Intersect(h);

				bool diffType = oTmp.Type != hTmp.Type;

				Drawer.HighlightType left, right, oleft, oright;
				hTmp.GetAbyssHighlights(out left,  out right);
				oTmp.GetAbyssHighlights(out oleft, out oright);

				bool diffAbyss = (left != oleft) || (right != oright);

				if (diffType || diffAbyss)
					RenderHighlight(hTmp);
			}
		}
	}

	private void RenderCursor(IntervalTree<AtomicHighlight> atomicHighlights)
	{
		IList<AtomicHighlight> overlaps = atomicHighlights.SearchOverlap(
			new Util.Range(prevCursorOffset, prevCursorOffset));

		AtomicHighlight h = null;

		if (overlaps.Count > 0) {
			h = new AtomicHighlight(overlaps[0]);
			h.Start = prevCursorOffset;
			h.End   = prevCursorOffset;
		}

		bool prevCursorBeyondEof = (prevCursorOffset >= byteBuffer.Size);

		if (h != null)
			RenderHighlight(h);
		else if (prevCursorBeyondEof) {
			foreach (Area a in areas)
				a.BlankOffset(prevCursorOffset);
		}

		if (selection.IsEmpty())
			foreach (Area a in areas)
				a.RenderCursor();
	}

	/// <summary>
	/// Render this area group. Pass force=true to force a full redraw.
	/// The Cairo context must be provided by the caller.
	/// </summary>
	public void Render(bool force, Cairo.Context cr)
	{
		if (byteBuffer == null)
			return;

		// Push Cairo context to all areas
		foreach (Area a in areas)
			a.SetCairoContext(cr);

		InitializeHighlights();

		if (PreRenderEvent != null)
			PreRenderEvent(this);

		IntervalTree<AtomicHighlight> atomicHighlights;

		if (!force && !HasChanged(Changes.Highlights) && !HasChanged(Changes.Offset))
			atomicHighlights = prevAtomicHighlights;
		else
			atomicHighlights = GetAtomicHighlights();

		if (force || HasChanged(Changes.Offset)) {
			RenderAll(atomicHighlights);
		} else if (HasChanged(Changes.Highlights)) {
			RenderHighlightDiffs(atomicHighlights);
		}

		if (HasChanged(Changes.Cursor))
			RenderCursor(atomicHighlights);

		prevAtomicHighlights = atomicHighlights;
		ClearChanges();
	}

	public delegate void PreRenderHandler(AreaGroup ag);
	public event PreRenderHandler PreRenderEvent;
}

} // end namespace
