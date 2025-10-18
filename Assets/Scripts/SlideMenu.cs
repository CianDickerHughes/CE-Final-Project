using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SlideMenu : MonoBehaviour
{
    [Header("Positions (anchoredPosition)")]
    public Vector2 hiddenPosition = new Vector2(-200f, 0f); // set in Inspector
    public Vector2 shownPosition  = new Vector2(0f, 0f);     // set in Inspector

    [Header("Timing & easing")]
    public float duration = 0.35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    RectTransform rt;
    Coroutine moveRoutine;
    bool isShown = false;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
        // Ensure initial state is hidden on start:
        rt.anchoredPosition = hiddenPosition;
    }

    // Public method you can hook to a Button OnClick
    public void Toggle()
    {
        Toggle(!isShown);
    }

    public void Toggle(bool show)
    {
        if (moveRoutine != null) StopCoroutine(moveRoutine);
        moveRoutine = StartCoroutine(MoveTo(show ? shownPosition : hiddenPosition));
        isShown = show;
    }

    IEnumerator MoveTo(Vector2 target)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // UI usually uses unscaled time
            float normalized = Mathf.Clamp01(t / duration);
            float e = ease.Evaluate(normalized);
            rt.anchoredPosition = Vector2.Lerp(start, target, e);
            yield return null;
        }
        rt.anchoredPosition = target;
        moveRoutine = null;
    }
}
