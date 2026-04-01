using System.Collections;
using UnityEngine;

//All this does is require whatever object its attached to to have a rectTransform
[RequireComponent(typeof(RectTransform))]
public class SlideMenu : MonoBehaviour
{
    //All this bit does is add a custom header to the variables in the unity inspector - reuse this - it makes it easier on us
    [Header("Positions (anchoredPosition)")]
    public Vector2 hiddenPosition = new Vector2(-200f, 0f); // set in Inspector
    public Vector2 shownPosition  = new Vector2(0f, 0f);     // set in Inspector

    //Same thing here for headers
    [Header("Timing & easing")]
    public float duration = 0.35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    //Other variables for handling things like the rect transform, the current routine & tracking if screen is shown
    RectTransform rt;
    Coroutine moveRoutine;
    bool isShown = false;

    //Upon boot up - the initial view is hidden
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

    //Method to toggle between the view being shown or not
    public void Toggle(bool show)
    {
        //Stops any currently running sliding animation so animations wont overlap
        if (moveRoutine != null) {
            StopCoroutine(moveRoutine);
        }
        //We animate the move to either shown or hidden position
        moveRoutine = StartCoroutine(MoveTo(show ? shownPosition : hiddenPosition));
        //Then update isShown accordingly
        isShown = show;
    }

    //Method/enumerator for smoothe animations of moving the slide based on the input of a vector
    IEnumerator MoveTo(Vector2 target)
    {
        //We initially set a vector with the anchored position
        Vector2 start = rt.anchoredPosition;
        //Measuring time - for how long the animation takes
        float t = 0f;
        while (t < duration)
        {
            //This ensures the animation still takes place even if the game is paused
            t += Time.unscaledDeltaTime;
            //This clamps the normalized movement/animation speed between 2 values
            float normalized = Mathf.Clamp01(t / duration);
            //Shapes the motion using the chosen curve we entered - in the inspector in unity
            float e = ease.Evaluate(normalized);
            //Interpolates between the starting position and the ending position - smoother animation
            rt.anchoredPosition = Vector2.Lerp(start, target, e);
            yield return null;
        }
        //We then reset the anchored position to where we currently are - which is the target
        //Then set the move routine to null again
        rt.anchoredPosition = target;
        moveRoutine = null;
    }
}
