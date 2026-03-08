using ALaborateUnityUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    const float MAX_SMOOTH_TIME = 0.8f;
    const float MIN_SMOOTH_TIME = 0.01f;

    [SerializeField] Transform targetPos;
    [SerializeField] [Range(0, MAX_SMOOTH_TIME)] float smoothTime;
    [SerializeField] float smoothChangeSpeed = 0.1f;

    InputAction iCamDelay;

    private void Awake()
    {
        InputUtils.FillInputActions(PaperPlane.ActionMap, this);
    }

    private void LateUpdate()
    {
        float alpha = 1f - Mathf.Exp(-Time.deltaTime / smoothTime); //exponential moving average
        transform.position = Vector3.Lerp(transform.position, targetPos.position, alpha);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetPos.rotation, alpha);

        var smoothingChange = iCamDelay.ReadValue<float>();
        smoothTime = Mathf.Clamp(smoothTime + smoothingChange * smoothChangeSpeed * Time.deltaTime, MIN_SMOOTH_TIME, MAX_SMOOTH_TIME);
    }
}
