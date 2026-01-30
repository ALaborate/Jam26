using ALaborateUnityUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] float maxSpeed = 5;

    InputActionMap actionMap;
    InputAction iMove;
    InputAction iUpDown;
    InputAction iFaster;
    InputAction iSlower;
    InputAction iLook;
    InputAction iLookButton;

    private void Awake()
    {
        actionMap = InputRef.DefaultActionAsset.FindActionMapLevenstein(GetType().Name, out _);
        InputUtils.FillInputActions(actionMap, this);
    }

    // Update is called once per frame
    void Update()
    {
        var fwdSlide = iMove.ReadValue<Vector2>();
        var upDown = iUpDown.ReadValue<float>();

        Vector3 newVelocity = new Vector3(fwdSlide.x, upDown, fwdSlide.y).normalized;
        newVelocity = transform.TransformVector(newVelocity);

        //if (iFaster.ReadValue<float>() > 0)
        //    newVelocity *= shiftSpeed;
        //if (iSlower.ReadValue<float>() > 0)
        //    newVelocity *= ctrlSpeed;

        transform.Translate(maxSpeed * Time.deltaTime * newVelocity);
    }
}
