using UnityEngine;
using UnityEngine.Video;

public class Trashcan : MonoBehaviour
{
    [SerializeField] Collider receiveCollider;
    [SerializeField] Collider zoneTrigger;

    [SerializeField] float lidRotationTime = 1f;
    [SerializeField] Vector3 lidOpenRotation = new Vector3(68, 0);

    Transform lid;
    Vector3 lidClosedRotation;


    private void Awake()
    {
        lid = transform.GetChild(0);
        lidClosedRotation = lid.localEulerAngles;
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponentInParent<PaperPlane>();
        if (player)
        {
            //player.gameObject.SetActive(false);
            receiveCollider.enabled = true;
            if (lidRoutine != null)
                StopCoroutine(lidRoutine);
            lidRoutine = StartCoroutine(ChangeLidState(lidOpenRotation));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponentInParent<PaperPlane>();
        if (player)
        {
            receiveCollider.enabled = false;
            if (lidRoutine != null) StopCoroutine(lidRoutine);
            lidRoutine = StartCoroutine(ChangeLidState(lidClosedRotation));
        }
    }

    Coroutine lidRoutine = null;
    System.Collections.IEnumerator ChangeLidState(Vector3 targetLidRotation)
    {
        var tStart = Time.time;
        var rStart = lid.localEulerAngles;
        do
        {
            lid.localEulerAngles = Vector3.Lerp(rStart, targetLidRotation, (Time.time - tStart) / lidRotationTime);
            yield return null;
        } while (Time.time < tStart + lidRotationTime);

        lidRoutine = null;
    }


}
