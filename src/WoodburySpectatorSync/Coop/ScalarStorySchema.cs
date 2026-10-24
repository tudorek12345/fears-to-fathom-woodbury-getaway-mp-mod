using System;
using System.Reflection;

namespace WoodburySpectatorSync.Coop
{
    // Only explicitly named scalar fields are writable. No methods, properties,
    // progression callbacks or local player/controller state are invoked.
    internal sealed class ScalarStorySchema
    {
        public readonly string[] Names;
        private readonly FieldInfo[] _fields;
        public bool IsComplete { get; private set; } = true;
        public ScalarStorySchema(Type type, params string[] names)
        {
            Names = names; _fields = new FieldInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                for (var t = type; t != null && _fields[i] == null; t = t.BaseType)
                    _fields[i] = t.GetField(names[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                var field = _fields[i];
                if (field == null || field.IsInitOnly || field.IsLiteral ||
                    (field.FieldType != typeof(bool) && field.FieldType != typeof(int) && !field.FieldType.IsEnum))
                { _fields[i] = null; IsComplete = false; }
            }
        }
        public bool Read(object target, int index, out int value)
        {
            value = 0;
            if (target == null || index < 0 || index >= _fields.Length || _fields[index] == null) return false;
            value = Convert.ToInt32(_fields[index].GetValue(target)); return true;
        }
        public bool Apply(object target, string name, int value)
        {
            int index = Array.IndexOf(Names, name);
            if (target == null || index < 0 || _fields[index] == null) return false;
            var field = _fields[index]; object boxed = value;
            if (field.FieldType == typeof(bool)) { if (value != 0 && value != 1) return false; boxed = value == 1; }
            else if (field.FieldType.IsEnum) { if (!Enum.IsDefined(field.FieldType, value)) return false; boxed = Enum.ToObject(field.FieldType, value); }
            field.SetValue(target, boxed); return true;
        }
        internal static int Hash(int hash, int value) => unchecked(hash * 16777619 ^ value);
    }
}
