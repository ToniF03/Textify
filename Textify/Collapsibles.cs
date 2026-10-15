using System;
using System.Collections;
using System.Collections.Generic;

namespace Textify
{
    /// <summary>A code-defined, one-based inclusive line range that can be collapsed in a CodeEditor.</summary>
    public sealed class CollapsibleRegion
    {
        private readonly Action _changed;
        private int _from;
        private int _to;
        private bool _isCollapsed;

        internal CollapsibleRegion(int from, int to, Action changed)
        {
            ValidateRange(from, to);
            _from = from;
            _to = to;
            _changed = changed;
        }

        /// <summary>First line of the region, one-based. This line remains visible when collapsed.</summary>
        public int From { get { return _from; } }

        /// <summary>Last line of the region, one-based and inclusive.</summary>
        public int To { get { return _to; } }

        /// <summary>Gets or sets whether the region is currently collapsed.</summary>
        public bool IsCollapsed
        {
            get { return _isCollapsed; }
            set
            {
                if (_isCollapsed == value) return;
                _isCollapsed = value;
                if (_changed != null) _changed();
            }
        }

        internal int StartLine { get { return _from - 1; } }
        internal int EndLineExclusive { get { return _to; } }
        internal void SetCollapsedWithoutNotification(bool value) { _isCollapsed = value; }
        internal void SetRangeWithoutNotification(int startLine, int endLineExclusive)
        {
            _from = startLine + 1;
            _to = endLineExclusive;
        }

        private static void ValidateRange(int from, int to)
        {
            if (from < 1) throw new ArgumentOutOfRangeException(nameof(from), "Line numbers start at 1.");
            if (to <= from) throw new ArgumentOutOfRangeException(nameof(to), "A collapsible range must include at least two lines.");
        }
    }

    /// <summary>Collection of code-defined foldable line ranges for a CodeEditor.</summary>
    public sealed class CollapsibleRegionCollection : IEnumerable<CollapsibleRegion>
    {
        private readonly List<CollapsibleRegion> _items = new List<CollapsibleRegion>();
        private readonly Action _changed;

        internal CollapsibleRegionCollection(Action changed) { _changed = changed; }

        public int Count { get { return _items.Count; } }
        public CollapsibleRegion this[int index] { get { return _items[index]; } }

        /// <summary>Adds a one-based inclusive line range. It starts expanded.</summary>
        public CollapsibleRegion Add(int from, int to)
        {
            var region = new CollapsibleRegion(from, to, _changed);
            foreach (var existing in _items)
            {
                if (existing.From == from)
                    throw new InvalidOperationException("Only one collapsible region can start on a line.");

                bool overlaps = from <= existing.To && existing.From <= to;
                bool nested = (from <= existing.From && to >= existing.To) ||
                              (existing.From <= from && existing.To >= to);
                if (overlaps && !nested)
                    throw new InvalidOperationException("Overlapping collapsible regions must be nested.");
            }
            _items.Add(region);
            if (_changed != null) _changed();
            return region;
        }

        public bool Remove(CollapsibleRegion region)
        {
            bool removed = _items.Remove(region);
            if (removed && _changed != null) _changed();
            return removed;
        }

        public void Clear()
        {
            if (_items.Count == 0) return;
            _items.Clear();
            if (_changed != null) _changed();
        }

        internal List<CollapsibleRegion> Items { get { return _items; } }
        internal void ReplaceAll(IEnumerable<CollapsibleRegion> regions)
        {
            _items.Clear();
            if (regions != null) _items.AddRange(regions);
        }

        public IEnumerator<CollapsibleRegion> GetEnumerator() { return _items.GetEnumerator(); }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }
}
