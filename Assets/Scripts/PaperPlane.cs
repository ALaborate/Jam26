using ALaborateUnityUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaperPlane : MonoBehaviour
{
    const float MAX_SPEED = 24;

    [SerializeField] float dropTime = 1f;

    [Header("Flight")]
    [SerializeField] float aileronInputFilter = 0.95f;
    [SerializeField] float stabilizerStrength = 1;
    [SerializeField] AnimationCurve aileronTorqueWrtSpeed = AnimationCurve.EaseInOut(0, 0, MAX_SPEED, 4);
    [SerializeField] AnimationCurve liftWrtSpeed = AnimationCurve.EaseInOut(0, 0, MAX_SPEED, 4);
    [SerializeField] AnimationCurve linearDumpWrtAngularVelocity = AnimationCurve.EaseInOut(0, .2f, .5f, .54f);

    InputActionMap actionMap;
    //InputAction iMove;
    InputAction iAilerons;
    InputAction iRestart;


    //InputAction iUpDown;
    //InputAction iFaster;
    //InputAction iSlower;
    //InputAction iLook;
    //InputAction iLookButton;

    public void DropIntoTrash(Trashcan can)
    {
        if(dropRoutine != null)
            StopCoroutine(dropRoutine);
        dropRoutine = StartCoroutine(DropRoutine(can));
    }


    [SerializeField] private StateVars state = new();
    private Rigidbody rb;
    private Vector3 initialPosition;
    private bool Simulated
    {
        get => !rb.isKinematic;
        set
        {
            rb.isKinematic = !value;
            if (!rb.isKinematic)
            {
                if(dropRoutine != null)
                {
                    StopCoroutine(dropRoutine);
                    dropRoutine = null;
                }
            }
        }
    }


    private void Awake()
    {
        actionMap = InputRef.DefaultActionAsset.FindActionMapLevenstein(GetType().Name, out _);
        InputUtils.FillInputActions(actionMap, this);

        rb = GetComponent<Rigidbody>();
        rb.maxLinearVelocity = MAX_SPEED;

        initialPosition = transform.position;
    }

    private void Update()
    {
        if(iRestart.ReadValue<float>() != 0f)
        {
            rb.Move(initialPosition, transform.rotation);
            Simulated = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            GameManager.instance.Restart();
        }
    }

    private void FixedUpdate()
    {
        state.velocityMagnitude = rb.linearVelocity.magnitude;
        state.velocityNormalized = rb.linearVelocity / state.velocityMagnitude;
        var forwardVelocity = Vector3.Project(rb.linearVelocity, transform.forward);
        state.fwdSpeed = forwardVelocity.magnitude;

        var verticalVelocity = Vector3.Project(rb.linearVelocity, Vector3.up);
        state.verticalSpeed = verticalVelocity.magnitude;
        var horizontalVelocity = Vector3.Project(rb.linearVelocity, Vector3.Cross(Vector3.up, transform.right));
        state.ldRatio = horizontalVelocity.magnitude / state.verticalSpeed;

        if (Simulated && state.velocityMagnitude > 0.1f)
        {
            SimulateStabilizer();
            SimulateLift();
            SimulateAilerons();
        }
    }

    private void SimulateStabilizer()
    {
        Quaternion targetRotation = Quaternion.LookRotation(state.velocityNormalized, transform.up);
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) angle -= 360f;
        angle = Mathf.Min(angle, angle * angle);
        Vector3 torque = axis * (angle * Mathf.Deg2Rad) * stabilizerStrength;
        rb.AddTorque(torque, ForceMode.Force);

        rb.linearDamping = linearDumpWrtAngularVelocity.Evaluate(Mathf.Abs(rb.angularVelocity.x));
    }
    private void SimulateLift()
    {
        var force = liftWrtSpeed.Evaluate(state.fwdSpeed);
        rb.AddForce(transform.up * force, ForceMode.Force);
    }
    float aileronInput = 0f;
    private void SimulateAilerons()
    {
        var control = iAilerons.ReadValue<float>();
        aileronInput = Mathf.Lerp(control, aileronInput, aileronInputFilter);
        var torque = new Vector3(0, 0, aileronInput * aileronTorqueWrtSpeed.Evaluate(state.fwdSpeed));
        rb.AddRelativeTorque(torque, ForceMode.Force);
    }

    Coroutine dropRoutine = null;
    System.Collections.IEnumerator DropRoutine(Trashcan trashcan)
    {
        Simulated = false;
        var tStart = Time.time;
        var trashTransform = trashcan.transform;
        var pStart = trashTransform.InverseTransformPoint(transform.position);
        while (true)
        {
            var t = (Time.time - tStart) / dropTime;

            var pCurr = Vector3.Slerp(pStart, Vector3.zero, t);
            transform.position = trashTransform.TransformPoint(pCurr);

            if (t > 1)
                break;

            yield return null;
        }

        dropRoutine = null;
    }



    [System.Serializable]
    class StateVars
    {
        public Vector3 velocityNormalized;
        public float velocityMagnitude;
        public float fwdSpeed;
        public float verticalSpeed;
        public float ldRatio;
    }
}
