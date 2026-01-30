using ALaborateUnityUtils;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaperPlane : MonoBehaviour
{
    const float MAX_SPEED = 24;

    [SerializeField] float stabilizerStrength = 1;
    [SerializeField] AnimationCurve aileronTorqueWrtSpeed = AnimationCurve.EaseInOut(0, 0, MAX_SPEED, 4);
    [SerializeField] AnimationCurve liftWrtSpeed = AnimationCurve.EaseInOut(0, 0, MAX_SPEED, 4);
    [SerializeField] AnimationCurve linearDumpWrtAngularVelocity = AnimationCurve.EaseInOut(0, .2f, .5f, .54f);

    InputActionMap actionMap;
    //InputAction iMove;
    InputAction iAilerons;


    //InputAction iUpDown;
    //InputAction iFaster;
    //InputAction iSlower;
    //InputAction iLook;
    //InputAction iLookButton;

    private Rigidbody rb;
    [SerializeField] private StateVars state = new();

    private void Awake()
    {
        actionMap = InputRef.DefaultActionAsset.FindActionMapLevenstein(GetType().Name, out _);
        InputUtils.FillInputActions(actionMap, this);

        rb = GetComponent<Rigidbody>();
        rb.maxLinearVelocity = MAX_SPEED;
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

        if (state.velocityMagnitude > 0.1f)
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
        Vector3 torque = axis * (angle * Mathf.Deg2Rad) * stabilizerStrength;
        rb.AddTorque(torque, ForceMode.Force);

        rb.linearDamping = linearDumpWrtAngularVelocity.Evaluate(rb.angularVelocity.magnitude);
    }
    private void SimulateLift()
    {
        var force = liftWrtSpeed.Evaluate(state.fwdSpeed);
        rb.AddForce(transform.up * force, ForceMode.Force);
    }
    private void SimulateAilerons()
    {
        var control = iAilerons.ReadValue<float>();
        var torque = new Vector3(0, 0, control * aileronTorqueWrtSpeed.Evaluate(state.fwdSpeed));
        rb.AddRelativeTorque(torque, ForceMode.Force);
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
