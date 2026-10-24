using System;
using System.Collections.Generic;

namespace WoodburySpectatorSync.Coop
{
    // Main-thread transaction cache. Nothing survives the outer scope, including
    // negative lookups; newly spawned/activated objects are visible next update.
    internal sealed class BatchLookupCache
    {
        private readonly Dictionary<Type, object> _values = new Dictionary<Type, object>();
        private int _depth;
        public long Requests { get; private set; }
        public long Searches { get; private set; }
        public IDisposable Begin() { _depth++; return new Scope(this); }
        public T Get<T>(Func<T> search) where T : class
        {
            Requests++;
            if (_depth > 0 && _values.TryGetValue(typeof(T), out var value)) return (T)value;
            Searches++;
            var result = search();
            if (_depth > 0) _values[typeof(T)] = result;
            return result;
        }
        public void Invalidate() => _values.Clear();
        private sealed class Scope : IDisposable
        {
            private BatchLookupCache _owner;
            public Scope(BatchLookupCache owner) { _owner = owner; }
            public void Dispose()
            {
                if (_owner == null) return;
                if (--_owner._depth == 0) _owner.Invalidate();
                _owner = null;
            }
        }
    }
}
