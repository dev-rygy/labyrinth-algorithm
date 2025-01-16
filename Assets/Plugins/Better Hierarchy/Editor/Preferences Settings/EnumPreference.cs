using System;
using UnityEditor;

namespace Utilities.BetterHierarchy
{
    public static partial class BetterHierarchyPreferences
    {
        private class EnumPreference<T> : Preference<T>
            where T : Enum
        {
            protected override T GetImpl()
            {
                return (T)Enum.ToObject(typeof(T), EditorPrefs.GetInt(Key));
            }

            protected override void SetImpl(T value)
            {
                EditorPrefs.SetInt(Key, Convert.ToInt32(value));
                OnSettingsChangedEvents.Invoke();
            }
        }
    }
}