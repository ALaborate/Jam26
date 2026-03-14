using ALaborateUnityUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PaperPlane : MonoBehaviour
{
    const float MAX_SPEED = 24;
    const float G = 9.81f;

    [SerializeField] float dropTime = 1f;
    [SerializeField] PlayerView playerView;

    [Header("Flight")]
    [SerializeField] float aileronsSmoothtime = 2f;
    [SerializeField] float stabilizerMaxAngularSpeed = 90f;
    [SerializeField] float stabilizerSmoothTime = 1f;
    [SerializeField] float drag = 0.5f;
    [SerializeField] float aileronsAngulardDamping = 0.3f;
    [SerializeField] float stabilizerAngularDamping = 1f;
    [SerializeField] AnimationCurve aileronTorqueWrtSpeed = AnimationCurve.EaseInOut(0, 0, MAX_SPEED, 4);
    [SerializeField] AnimationCurve liftWrtSpeed = AnimationCurve.EaseInOut(0, 0, MAX_SPEED, 4);

    public UnityEvent<RaycastHit> OnCollision;

    static InputActionMap _actionMap;
    //InputAction iMove;
    InputAction iAilerons;
    InputAction iRestart;

    
    public static InputActionMap ActionMap => _actionMap ??= InputRef.DefaultActionAsset.FindActionMapLevenstein(nameof(PaperPlane), out _);
    public void DropIntoTrash(Trashcan can)
    {
        if (dropRoutine != null)
            StopCoroutine(dropRoutine);
        droppedCount++;
        playerView.OnDrop(droppedCount);
        dropRoutine = StartCoroutine(DropRoutine(can));
    }


    [SerializeField] private StateVars state = new();


    private Rigidbody rb;
    private GameObject[] spawners = null;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    private bool _simulated = false;
    private bool Simulated
    {
        get => _simulated;
        set
        {
            _simulated = value;
            if (Simulated)
            {
                if (dropRoutine != null)
                {
                    StopCoroutine(dropRoutine);
                    dropRoutine = null;
                }
            }
        }
    }


    private void Awake()
    {
        InputUtils.FillInputActions(ActionMap, this);

        rb = GetComponent<Rigidbody>();
        rb.maxLinearVelocity = MAX_SPEED;

        spawners = GameObject.FindGameObjectsWithTag("Respawn");
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        rb.isKinematic = true;
        state.velocityNormalized = transform.forward;
        state.velocityMagnitude = 1f;

        playerView.onFinishingQueue.AddListener(() => alreadyOncePressedR = true); //here to ignore all subsequent presses before gameplay actually starts
    }

    bool prevRestart = false;
    bool alreadyOncePressedR = false;
    private void Update()
    {
        Simulate(Time.deltaTime);

        var currRestart = iRestart.ReadValue<float>() != 0f;
        if (currRestart && !prevRestart)
        {
            playerView.pause = false;
            var currView = playerView.Peek;

            bool pressingToGetHigherAfterRestart = currView.HasValue && currView.Value == PlayerView.Target.Wind;
            if (alreadyOncePressedR && !pressingToGetHigherAfterRestart)
            {
                var pos = initialPosition;
                var rot = initialRotation;
                if (droppedCount > 0 && spawners.Length > 0)
                {
                    pos = spawners[Random.Range(0, spawners.Length)].transform.position;
                    rot = transform.rotation;
                }

                Simulated = true;
                Teleport(pos, rot);

                if (playerView.Peek.HasValue && playerView.Peek.Value == PlayerView.Target.Check)
                {
                    playerView.InterruptQueue(PlayerView.Target.Plot);
                    playerView.Queue(PlayerView.Target.Wind);
                }
                else
                    playerView.InterruptQueue(PlayerView.Target.Wind);

                GameManager.instance.Restart();
            }
            else if (!alreadyOncePressedR)
            {
                Simulated = true;
            }
            else if(pressingToGetHigherAfterRestart)
            {
                Teleport(transform.position + Vector3.up * 5, transform.rotation);
            }
        }
        prevRestart = currRestart;

        if (state.velocityMagnitude < 0.1f && (!playerView.Peek.HasValue || playerView.Peek.Value != PlayerView.Target.Wind))
            RemindPlayerOfRestart();
    }

    private void Teleport(Vector3 position, Quaternion rotation)
    {
        rb.Move(position, rotation);
        state.velocityNormalized = rotation * Vector3.forward;
        aileronInput = 0f;
        state.velocityMagnitude = 1f;
        state.stabilizerAngularVelocity = 0f;
    }

    private void Simulate(float dt)
    {
        if (Simulated)
        {
            var velocity = state.velocityNormalized * state.velocityMagnitude;
            velocity += dt * G * Vector3.down;
            var rotation = Quaternion.identity;
            rotation = GetStabilizerRot(dt) * rotation;

            var forwardVelocity = Vector3.Project(velocity, transform.forward);
            state.fwdSpeed = forwardVelocity.magnitude;
            SimulateLift(ref velocity, dt);

            rotation = Quaternion.AngleAxis(GetAilerons(dt), transform.forward) * rotation;

            var dragStep = 0.5f * drag * velocity.sqrMagnitude * dt;
            dragStep = Mathf.Min(state.velocityMagnitude, dragStep);
            velocity -= dragStep * velocity;

            var newMagnitude = velocity.magnitude;
            if (newMagnitude > MAX_SPEED)
            {
                velocity = velocity.normalized * MAX_SPEED;
                newMagnitude = MAX_SPEED;
            }
            state.velocityMagnitude = newMagnitude;
            state.velocityNormalized = velocity / newMagnitude;

            state.verticalSpeed = Vector3.Project(velocity, Vector3.up).magnitude;
            var hrzSpeed = Vector3.Project(velocity, Vector3.Cross(transform.right, Vector3.up)).magnitude;
            state.ldRatio = hrzSpeed / Mathf.Abs(state.verticalSpeed);


            if (Physics.Raycast(transform.position, velocity, out var rhi, newMagnitude * dt * 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                OnCollisionDetected(rhi);
            else
                rb.Move(transform.position + velocity * dt, rotation * transform.rotation); //rotation dt is applied to rotation before creating a quaternion
        }
        else
            RemindPlayerOfRestart();
    }

    private Quaternion GetStabilizerRot(float dt)
    {
        Quaternion targetRotation = Quaternion.LookRotation(state.velocityNormalized, transform.up);
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(transform.rotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) 
            angle -= 360f;

        var stepAngle = Mathf.SmoothDampAngle(angle, 0, ref state.stabilizerAngularVelocity, stabilizerSmoothTime, stabilizerMaxAngularSpeed, dt);
        state.stabilizerAngularVelocity *= (1 - stabilizerAngularDamping * dt);
        var stepRotation = Quaternion.AngleAxis(stepAngle, axis);
        return stepRotation;
    }
    private void SimulateLift(ref Vector3 velocity, float dt)
    {
        var lift = liftWrtSpeed.Evaluate(state.fwdSpeed);
        velocity += dt * lift * transform.up;
    }
    float aileronInput = 0f;
    private float GetAilerons(float dt)
    {
        var control = iAilerons.ReadValue<float>();
        var alpha = 1f - Mathf.Exp(-Time.deltaTime / aileronsSmoothtime);
        aileronInput = Mathf.Lerp(aileronInput, control, alpha);

        var effect = aileronTorqueWrtSpeed.Evaluate(state.fwdSpeed);
        return aileronInput * effect * dt;
    }

    Coroutine dropRoutine = null;
    int droppedCount = 0;
    System.Collections.IEnumerator DropRoutine(Trashcan trashcan)
    {
        Simulated = false;

        var tStart = Time.time;
        var trashTransform = trashcan.transform;
        var pStart = trashTransform.InverseTransformPoint(transform.position);
        playerView.Queue(PlayerView.Target.Check);
        playerView.pause = true;
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

    private void OnCollisionDetected(RaycastHit rhi)
    {
        Debug.Log($"Collision with {rhi.collider.gameObject.name}.");
        Simulated = false;
        RemindPlayerOfRestart();
        OnCollision?.Invoke(rhi);
    }

    private void RemindPlayerOfRestart()
    {
        if (!Simulated)
        {
            if (!playerView.IsShowing)
                playerView.Queue(PlayerView.Target.Cross);
        }
    }

    [System.Serializable]
    class StateVars
    {
        public Vector3 velocityNormalized;
        public float velocityMagnitude;
        public float stabilizerAngularVelocity;
        public float aileronsAngularVelocity;

        public float fwdSpeed;
        public float verticalSpeed;
        public float ldRatio;
    }
}
