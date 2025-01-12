using System;
using System.Text.RegularExpressions;
using UnityEditor;

namespace Utilities.BetterHierarchy
{
    public abstract class Preference<T>
    {
        public event Action<T> OnValueChanged = delegate { };

        public string Label
        {
            get
            {
                if (string.IsNullOrEmpty(label))
                    label = ConvertCamelCaseToWords(Key);

                return label;
            }
            set => label = value;
        }

        public T DefaultValue { get; set; }
        public string Key { get; set; }

        private string label = "";
        private T cachedValue;
        private bool isCached;

        public T Get()
        {
            if (isCached) return cachedValue;

            isCached = true;

            if (!EditorPrefs.HasKey(Key))
            {
                SetImpl(DefaultValue);
                cachedValue = DefaultValue;
                return GetImpl();
            }

            cachedValue = GetImpl();
            return cachedValue;
        }

        public void Set(T value)
        {
            cachedValue = value;
            SetImpl(value);
            OnValueChanged.Invoke(value);
        }


        public override string ToString()
        {
            return Label;
        }

        private static string ConvertCamelCaseToWords(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            string[] words = Regex.Split(input, @"(?<!^)(?=[A-Z])");
            string result = string.Join(" ", words);
            result = char.ToUpper(result[0]) + result.Substring(1);

            return result;
        }

        protected abstract T GetImpl();
        protected abstract void SetImpl(T value);
    }
}