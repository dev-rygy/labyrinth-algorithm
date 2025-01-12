#if UNITY_2019_1_OR_NEWER
using System;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Utilities.BetterHierarchy
{
    public static partial class BetterHierarchyPreferences
    {
        private class UIElementBuilder
        {
            private VisualElement wrapper;

            public UIElementBuilder(VisualElement wrapper)
            {
                this.wrapper = wrapper;
            }
            
            public VisualElement AddTooltip(string title, string tooltip)
            {
                var tooltipWrapper = new VisualElement()
                {
                    tooltip = tooltip,
                    style =
                    {
                        marginTop = 5,
                        marginBottom = 2,
                        flexDirection = FlexDirection.Row
                    }
                };

                var tooltipShortLabel = new Label(title)
                {
                    style =
                    {
                        fontSize = 10,
                        unityFontStyleAndWeight = FontStyle.Italic,
                        marginTop = 2,
                        marginLeft = 4
                    }
                };

                var questionMark = new Label("?")
                {
                    style =
                    {
                        unityFontStyleAndWeight = FontStyle.Bold,
                        color = new Color(0.2f, 0.4f, 0.8f)
                    }
                };


                tooltipWrapper.Add(tooltipShortLabel);
                tooltipWrapper.Add(questionMark);
                wrapper.Add(tooltipWrapper);

                return tooltipWrapper;
            }

            public Label AddHeader(string text)
            {
                var header = new Label()
                {
                    text = text,
                    style =
                    {
                        fontSize = 20,
                        marginBottom = 12,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    },
                };
                wrapper.Add(header);

                return header;
            }

            public Toggle AddToggle(BoolPreference toggle, string info)
            {
                var toggleElement = new Toggle
                {
                    label = toggle.Label,
                    value = toggle.Get(),
                    tooltip = info,
                };

                toggleElement.labelElement.style.minWidth = MIN_LABEL_WIDTH;
                toggleElement.RegisterValueChangedCallback((changeEvt) => toggle.Set(changeEvt.newValue));

                wrapper.Add(toggleElement);

                return toggleElement;
            }

            public PopupField<T> AddSelection<T>(Preference<T> selection, string info)
                where T : Enum
            {
                var enumOptions = Enum.GetValues(typeof(T)).Cast<T>().ToList();

                var selectionDropdown = new PopupField<T>
                {
                    value = selection.Get(),
                    label = selection.Label,
                    tooltip = info,
                    choices = enumOptions
                };

                selectionDropdown.labelElement.style.minWidth = MIN_LABEL_WIDTH;
                selectionDropdown.RegisterValueChangedCallback((changeEvt) => selection.Set(changeEvt.newValue));

                wrapper.Add(selectionDropdown);

                return selectionDropdown;
            }
        }
    }
}
#endif