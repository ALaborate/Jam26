using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour
{
    [SerializeField] float showTime = 2f;
    [SerializeField] float fadeTime = 1f;
    [SerializeField] Image background;
    [SerializeField] Image Check;
    [SerializeField] Image Cross;
    [SerializeField] Image Wind;
    [SerializeField] Image Plot;

    public enum Target
    {
        Check,
        Cross,
        Wind,
        Plot,
    }


    static readonly Color TRANSPARENT = new Color(0, 0, 0, 0);
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
        Queue(Target.Plot);
        Queue(Target.Wind);
    }

    private void Update()
    {
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
        background.color = currBgColor;

        if(!pause || timeToShow > showTime)
            timeToShow -= Time.deltaTime;

    }
}
