using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour
{
    static readonly Color TRANSPARENT = new Color(0, 0, 0, 0);

    [SerializeField] float showTime = 2f;
    [SerializeField] float fadeTime = 1f;
    [SerializeField] Image background;
    [SerializeField] Image Check;
    [SerializeField] Image Cross;
    [SerializeField] Image Wind;
    [SerializeField] Image Plot;
    [Space]
    [SerializeField] Text pressR;
    [SerializeField] float pressRFadeInTime = 8;
    [SerializeField] float pressRFrequency = 1f;



    public UnityEvent<bool> onPauseChange;
    public UnityEvent onFinishingQueue;
    public bool IsShowing => timeToShow > 0f || queue.Count > 1;
    public void Queue(Target target) => queue.Enqueue(target);
    public void InterruptQueue(Target target)
    {
        queue.Clear();
        timeToShow = float.NegativeInfinity;
        foreach (var item in images)
        {
            item.enabled = false;
        }
        queue.Enqueue(target);
    }
    public Target? Peek => queue.Count > 0 ? queue.Peek() : null;

    public bool pause = false;


    Queue<Target> queue = new();
    float timeToShow = float.NegativeInfinity;
    Image[] images = null;

    private void Awake()
    {
        images = new Image[] { Check, Cross, Wind, Plot };
        background.enabled = true;
        Queue(Target.Plot);
        pause = true;
        Queue(Target.Wind);
        onPauseChange.AddListener(RemovePressRHint);
        pressRFadeInRoutine = StartCoroutine(PressRFadeInRoutine());
    }

    bool prevPause = false;
    private void Update()
    {
        if (pause != prevPause)
        {
            onPauseChange?.Invoke(pause);
            prevPause = pause;
        }

        if (queue.Count > 0 && timeToShow == float.NegativeInfinity)
        {
            timeToShow = fadeTime + showTime;
            images[(int)queue.Peek()].enabled = true;
        }

        if (float.IsNormal(timeToShow))
        {
            if (timeToShow > 0f)
            {
                var currColor = Color.Lerp(TRANSPARENT, Color.white, fadeTime + showTime - timeToShow);
                foreach (var item in images)
                {
                    item.color = currColor;
                }
            }
            else
            {
                var currColor = Color.Lerp(Color.white, TRANSPARENT, -timeToShow);
                foreach (var item in images)
                {
                    item.color = currColor;
                }

                if (timeToShow < -1)
                {
                    timeToShow = float.NegativeInfinity;
                    images[(int)queue.Dequeue()].enabled = false;
                }
            }
        }

        var currBgColor = background.color;
        if (timeToShow > 0f)
            currBgColor.a += Time.deltaTime * fadeTime;
        else if (queue.Count < 2)
            currBgColor.a -= Time.deltaTime * fadeTime;
        currBgColor.a = Mathf.Clamp01(currBgColor.a);
        if (currBgColor.a == 0f && background.color.a > 0f)
            onFinishingQueue?.Invoke();
        background.color = currBgColor;

        if(!pause || timeToShow > showTime)
            timeToShow -= Time.deltaTime;

    }

    private void RemovePressRHint(bool _)
    {
        if (!pause)
        {
            pressR.gameObject.SetActive(false);
            onPauseChange.RemoveListener(RemovePressRHint);
            if (pressRFadeInRoutine != null)
                StopCoroutine(pressRFadeInRoutine);
        }
    }

    Coroutine pressRFadeInRoutine = null;
    private System.Collections.IEnumerator PressRFadeInRoutine()
    {
        var color = pressR.color;
        while (true) {
            color.a = Mathf.Lerp(0, 1, Time.time / pressRFadeInTime);
            pressR.color = color;
            yield return null;
            if (color.a == 1)
                break;
        }

        yield return new WaitForSeconds(showTime);

        while (true)
        {
            color.a = (Mathf.Sin(Time.time * Mathf.PI * pressRFrequency) + 1) / 2f;
            pressR.color = color;
            yield return null;
        }
    }

    public enum Target
    {
        Check,
        Cross,
        Wind,
        Plot,
    }
}
