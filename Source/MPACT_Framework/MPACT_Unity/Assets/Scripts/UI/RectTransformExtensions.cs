using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectTransformExtensions
{
    public static void SetAnchoredPosition(this RectTransform rectTransform, Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }

    public static void SetPositionRelativeTo(this RectTransform rectTransform, RectTransform relativeTo, Vector2 position)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(relativeTo, position, null, out localPoint);
        rectTransform.SetAnchoredPosition(localPoint);
    }
}