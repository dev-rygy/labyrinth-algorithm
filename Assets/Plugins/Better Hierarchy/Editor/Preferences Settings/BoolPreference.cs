using UnityEditor;

namespace Utilities.BetterHierarchy
{
    public static partial class BetterHierarchyPreferences
    {
        private class BoolPreference : Preference<bool>
        {
            protected override bool GetImpl()
            {
                return EditorPrefs.GetBool(Key);
            }

            protected override void SetImpl(bool value)
            {
                EditorPrefs.SetBool(Key, value);
                OnSettingsChangedEvents.Invoke();
            }
        }
    }
}