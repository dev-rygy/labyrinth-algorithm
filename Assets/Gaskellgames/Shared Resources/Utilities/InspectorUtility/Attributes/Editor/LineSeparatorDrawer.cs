using UnityEditor;
using UnityEngine;

/// <summary>
/// Code created by Gaskellgames
/// </summary>

namespace Gaskellgames
{
    [CustomPropertyDrawer(typeof(LineSeparatorAttribute))]
    public class LineSeparatorDrawer : DecoratorDrawer
    {
        public override float GetHeight()
        {
            LineSeparatorAttribute lineSeparator = attribute as LineSeparatorAttribute;

            float totalSpacing = lineSeparator.thickness;
            if (lineSeparator.spacingBefore)
            {
                totalSpacing += 10;
            }

            if (lineSeparator.spacingAfter)
            {
                totalSpacing += 10;
            }

            return totalSpacing;
        }

        public override void OnGUI(Rect position)
        {
            LineSeparatorAttribute lineSeparator = attribute as LineSeparatorAttribute;

            float positionY = position.yMin;
            if (lineSeparator.spacingBefore)
            {
                positionY += 10;
            }

            Rect separatorRect = new Rect(position.xMin, positionY, position.width, lineSeparator.thickness);
            Color color = new Color32(lineSeparator.R, lineSeparator.G, lineSeparator.B, lineSeparator.A);
            EditorGUI.DrawRect(separatorRect, color);
        }

    } // class end
}
