using System.Collections;
using UnityEngine;

public class SecondFloorManager : MonoBehaviour
{
    [Header("Start Hints")]
    public string[] startHints =
    {
        "What's wrong here?",
        "Why is it messier than the third floor?",
        "I better be careful in case another monster is here."
    };

    public float delayBeforeFirstHint = 1f;
    public float timeBetweenHints = 3f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delayBeforeFirstHint);

        foreach (string hint in startHints)
        {
            HintManager.Instance?.ShowHint(hint);
            yield return new WaitForSeconds(timeBetweenHints);
        }
    }
}