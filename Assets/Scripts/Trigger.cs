using UnityEngine;
using UnityEngine.Events;


public class Trigger : MonoBehaviour
{
    public event UnityAction<Collider> OnEnter;
    public event UnityAction<Collider> OnExit;


    private void OnTriggerEnter(Collider other) => OnEnter?.Invoke(other);
    private void OnTriggerExit(Collider other) => OnExit?.Invoke(other);
}
