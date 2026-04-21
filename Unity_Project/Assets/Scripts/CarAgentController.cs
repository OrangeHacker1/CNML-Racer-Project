using UnityEngine;

[RequireComponent(typeof(PrometeoCarController))]
[RequireComponent(typeof(Rigidbody))]
public class CarAgentController : MonoBehaviour
{
    private PrometeoCarController car;
    private Rigidbody rb;
    private CarRecorder recorder;

    private Vector3 startPosition;
    private float totalDistance = 0f;
    private Vector3 lastPosition;

    [Header("Control Settings")]
    public bool autoRecord = true;
    public float recordInterval = 0.1f;

    private float recordTimer = 0f;

    void Awake()
    {
        car = GetComponent<PrometeoCarController>();
        rb = GetComponent<Rigidbody>();
        recorder = GetComponent<CarRecorder>();

        if (recorder == null)
            recorder = gameObject.AddComponent<CarRecorder>();

        startPosition = transform.position;
        lastPosition = transform.position;
    }

    void Update()
    {
        UpdateDistance();

        if (autoRecord)
        {
            recordTimer += Time.deltaTime;

            if (recordTimer >= recordInterval)
            {
                recorder.Record(GetState());
                recordTimer = 0f;
            }
        }
    }

    private void UpdateDistance()
    {
        totalDistance += Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
    }

    // ====================================
    // ACTION FUNCTIONS
    // ====================================

    public void Accelerate()
    {
        car.GoForward();
    }

    public void Reverse()
    {
        car.GoReverse();
    }

    public void Brake()
    {
        car.Brakes();
    }

    public void TurnLeft()
    {
        car.TurnLeft();
    }

    public void TurnRight()
    {
        car.TurnRight();
    }

    public void Handbrake()
    {
        car.Handbrake();
    }

    public void RecoverTraction()
    {
        car.RecoverTraction();
    }

    public void ResetSteering()
    {
        car.ResetSteeringAngle();
    }

    public void StopCar()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // ====================================
    // RL DISCRETE ACTION INTERFACE
    // ====================================

    public void ExecuteAction(int action)
    {
        switch (action)
        {
            case 0:
                Accelerate();
                break;

            case 1:
                Brake();
                break;

            case 2:
                TurnLeft();
                break;

            case 3:
                TurnRight();
                break;

            case 4:
                Reverse();
                break;

            case 5:
                Handbrake();
                break;

            default:
                ResetSteering();
                break;
        }
    }

    // ====================================
    // STATE OBSERVATION
    // ====================================

    public CarState GetState()
    {
        CarState state = new CarState();

        state.worldPosition = transform.position;
        state.worldRotation = transform.eulerAngles;
        state.velocity = rb.linearVelocity;
        state.localVelocity = transform.InverseTransformDirection(rb.linearVelocity);

        state.speed = Mathf.Abs(car.carSpeed);
        state.isDrifting = car.isDrifting;
        state.tractionLocked = car.isTractionLocked;

        state.distanceTravelled = totalDistance;
        state.timeAlive = Time.timeSinceLevelLoad;

        return state;
    }

    // ====================================
    // RESET FOR TRAINING EPISODES
    // ====================================

    public void ResetAgent(Vector3 spawnPos, Quaternion spawnRot)
    {
        transform.position = spawnPos;
        transform.rotation = spawnRot;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        totalDistance = 0f;
        lastPosition = spawnPos;

        recorder.ClearLog();
    }
}