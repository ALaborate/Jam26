using UnityEngine;

public class Trashcan : MonoBehaviour
{
    [SerializeField] Collider receiveCollider;
    [SerializeField] Collider zoneTrigger;

    [SerializeField] float lidRotationTime = 1f;
    [SerializeField] Vector3 lidOpenEuler = new Vector3(-68, 0);

    Transform lid;
    Quaternion lidClosedRotation;


    private void Awake()
    {
        lid = transform.GetChild(0);
        lidClosedRotation = lid.localRotation;
        receiveCollider.isTrigger = true;
        if(!receiveCollider.gameObject.TryGetComponent(out Trigger trigger))
            trigger = receiveCollider.gameObject.AddComponent<Trigger>();
        trigger.OnEnter += ReceiverEnter;
    }

    private void ReceiverEnter(Collider other)
    {
        var player = other.GetComponentInParent<PaperPlane>();
        if(player)
        {
            player.DropIntoTrash(this);
        }
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
            lidRoutine = StartCoroutine(ChangeLidState(Quaternion.Euler(lidOpenEuler)));
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
    System.Collections.IEnumerator ChangeLidState(Quaternion targetLidRotation)
    {
        var tStart = Time.time;
        var rStart = lid.localRotation;
        do
        {
            lid.localRotation = Quaternion.Lerp(rStart, targetLidRotation, (Time.time - tStart) / lidRotationTime);
            yield return null;
        } while (Time.time < tStart + lidRotationTime);

        lidRoutine = null;
    }


}
