using UnityEngine;
using System;

/// <summary>
/// RL Agent bridge between neural policy server and PrometeoCarController.
/// 
/// DESIGN GOALS:
/// - No modification to PrometeoCarController
/// - Discrete action mapping layer
/// - Stable reward based on physics, not inputs
/// - Reproducible episodic training
/// </summary>
public class RLCarAgent : MonoBehaviour
{
    [Header("References")]
    public PrometeoCarController car;
    //public ProceduralTrackGenerator generator;
    public TrackManager trackManager;

    [Header("Episode Settings")]
    public float maxEpisodeTime = 30f;

    private float timer;
    private Vector3 lastPosition;
    private float totalReward;


    /// <summary>
    /// RAY TRACING INFOR
    /// </summary>

    [Header("Ray Perception")]
    public int rayCount = 7;
    public float raySpreadAngle = 90f;
    public float rayLength = 20f;
    public LayerMask rayMask;

    // ----------------------------------------------------
    // ACTION MEMORY (used only for reward shaping if needed)
    // ----------------------------------------------------
    private float lastSteer;
    private float lastThrottle;
    private float lastBrake;

    // ----------------------------------------------------
    // NETWORK PACKETS
    // ----------------------------------------------------
    [Serializable]
    public class StatePacket
    {
        public float[] state;
        public float reward;
        public bool done;
        public int episode;
        
        public bool newEpisode;
        public bool newTrack;
        public string trackName;
    }

    [Serializable]
    public class ActionPacket
    {
        public float steer;     // [-1, 1]
        public float throttle;  // [0, 1]
        public float brake;     // [0, 1]
    }

    // Flag for changes.
    private bool isNewEpisode; // This should be triggered every time the track changes.

    public string trackName;
    public bool newTrack;

    // ----------------------------------------------------
    // INITIALIZATION
    // ----------------------------------------------------
    private void Start()
    {
        lastPosition = transform.position;
        BeginEpisode();
    }


    public void BeginExternalEpisode()
    {
        timer = 0f;
        totalReward = 0f;
        lastPosition = transform.position;
    }

    // ----------------------------------------------------
    // MAIN LOOP
    // ----------------------------------------------------
    private void FixedUpdate()
    {
        if (!RLServerBridge.Instance.Connected)
            return;

        timer += Time.fixedDeltaTime;

        float reward = CalculateReward();
        bool done = CheckDone();

        SendState(reward, done);

        string msg = RLServerBridge.Instance.ReceiveAction();
        if (!string.IsNullOrEmpty(msg))
        {
            ActionPacket action =
                JsonUtility.FromJson<ActionPacket>(msg);

            ApplyAction(action);
        }

        totalReward += reward;
        lastPosition = transform.position;

        if (done)
        {
            EndEpisode();
            BeginEpisode();
        }
    }

    // ----------------------------------------------------
    // STATE SEND
    // ----------------------------------------------------
    /* OLD
    void SendState(float reward, bool done)
    {
        StatePacket packet = new StatePacket
        {
            state = CollectState(),
            reward = reward,
            done = done,
            episode = trackManager.CurrentEpisode
        };

        string json = JsonUtility.ToJson(packet);
        RLServerBridge.Instance.SendState(json);
    } OLD 2
    void SendState(float reward, bool done)
    {
        StatePacket packet = new StatePacket
        {
            state = CollectState(),
            reward = reward,
            done = done,
            episode = trackManager.CurrentEpisode,
            newEpisode = isNewEpisode, // TRUE on first step

            newTrack = trackManager.IsNewTrack,   // NEW
            trackName = trackManager.CurrentTrackName // NEW
        };

        isNewEpisode = false;

        string json = JsonUtility.ToJson(packet);
        RLServerBridge.Instance.SendState(json);
    }*/
    void SendState(float reward, bool done)
    {
        StatePacket packet = new StatePacket
        {
            state = CollectState(),
            reward = reward,
            done = done,
            episode = trackManager.CurrentEpisode,

            newEpisode = isNewEpisode,
            newTrack = trackManager.IsNewTrack,
            trackName = trackManager.CurrentTrackName != null
                ? System.IO.Path.GetFileNameWithoutExtension(trackManager.CurrentTrackName)
                : "unknown"
        };

        isNewEpisode = false;

        string json = JsonUtility.ToJson(packet);
        RLServerBridge.Instance.SendState(json);
    }


    // ----------------------------------------------------
    // OBSERVATION SPACE
    // ----------------------------------------------------
    float[] CollectState()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        float[] rays = GetRayDistances();

        float[] state = new float[rays.Length + 1];

        // Copy rays
        for (int i = 0; i < rays.Length; i++)
            state[i] = rays[i];

        // Add speed
        state[rays.Length] = rb.linearVelocity.magnitude;

        return state;
    }
    /*
    float[] CollectState()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        return new float[]
        {
            transform.position.x,
            transform.position.z,
            transform.forward.x,
            transform.forward.z,
            rb.linearVelocity.magnitude
        };
    }*/

    // Get Raytracing Distance
    float[] GetRayDistances()
    {
        float[] distances = new float[rayCount];

        float angleStep = raySpreadAngle / (rayCount - 1);

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -raySpreadAngle / 2 + angleStep * i;

            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, dir);

            if (Physics.Raycast(ray, out RaycastHit hit, rayLength, rayMask))
            {
                distances[i] = hit.distance / rayLength;
            }
            else
            {
                distances[i] = 1f;
            }

            // Debug visualization
            Debug.DrawRay(ray.origin, dir * rayLength, Color.red);
        }

        return distances;
    }

    // Helper
    float GetMinRayDistance()
    {
        float[] rays = GetRayDistances();

        float min = 1f;
        for (int i = 0; i < rays.Length; i++)
        {
            if (rays[i] < min)
                min = rays[i];
        }

        return min;
    }

    // ----------------------------------------------------
    // ACTION MAPPING (IMPORTANT FIX)
    // ----------------------------------------------------
    void ApplyAction(ActionPacket a)
    {
        lastSteer = Mathf.Clamp(a.steer, -1f, 1f);
        lastThrottle = Mathf.Clamp01(a.throttle);
        lastBrake = Mathf.Clamp01(a.brake);

        // Reset state first (prevents stacking physics calls)
        car.ThrottleOff();

        // Steering
        if (lastSteer < -0.3f)
            car.TurnLeft();
        else if (lastSteer > 0.3f)
            car.TurnRight();

        // Acceleration / braking
        if (lastThrottle > 0.5f)
            car.GoForward();
        else if (lastBrake > 0.5f)
            car.Brakes();
    }

    // ----------------------------------------------------
    // REWARD FUNCTION (FIXED)
    // ----------------------------------------------------
    float CalculateReward()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        Vector3 movement = transform.position - lastPosition;
        float forwardProgress = Vector3.Dot(movement, transform.forward);
        float speed = rb.linearVelocity.magnitude;

        float reward = 0f;

        // ----------------------------------
        // PRIMARY: forward movement
        // ----------------------------------
        reward += forwardProgress * 2.0f;

        // ----------------------------------
        // SECONDARY: speed (reduced weight)
        // ----------------------------------
        reward += speed * 0.2f;

        // ----------------------------------
        // WALL AVOIDANCE (CRITICAL FIX)
        // ----------------------------------
        float minRay = GetMinRayDistance();
        reward -= (1f - minRay) * 1.5f;

        // ----------------------------------
        // STABILITY (optional but helpful)
        // ----------------------------------
        reward -= Mathf.Abs(lastSteer) * 0.05f;

        // ----------------------------------
        // BACKWARD PENALTY
        // ----------------------------------
        if (forwardProgress < 0)
            reward += forwardProgress * 2.0f;

        return reward;
    }
    /*
    float CalculateReward()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        Vector3 movement = transform.position - lastPosition;

        // Reward ONLY forward movement (not sideways drifting)
        float forwardProgress = Vector3.Dot(movement, transform.forward);

        float speed = rb.linearVelocity.magnitude;

        float reward = 0f;

        // ----------------------------------
        // PRIMARY: forward progress
        // ----------------------------------
        reward += forwardProgress * 2.0f;

        // ----------------------------------
        // SECONDARY: speed bonus
        // ----------------------------------
        reward += speed * 0.4f;

        // ----------------------------------
        // SMALL penalty for going backwards
        // ----------------------------------
        if (forwardProgress < 0)
            reward += forwardProgress * 2.0f;

        return reward;
    }*/

    /*
    float CalculateReward()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        float distance = Vector3.Distance(transform.position, lastPosition);
        float speed = rb.linearVelocity.magnitude;

        float reward = 0f;

        // Forward progress reward
        reward += distance * 1.0f;

        // Speed efficiency reward
        reward += speed * 0.05f;

        // Penalize aggressive control (stability learning)
        reward -= Mathf.Abs(lastSteer) * 0.01f;
        reward -= lastThrottle * 0.01f;
        reward -= lastBrake * 0.02f;

        return reward;
    }*/

    // ----------------------------------------------------
    // EPISODE TERMINATION
    // ----------------------------------------------------
    bool CheckDone()
    {
        if (timer > maxEpisodeTime)
            return true;

        if (transform.position.y < -2f)
            return true;

        return false;
    }

    void BeginEpisode()
    {
        timer = 0f;
        totalReward = 0f;
        isNewEpisode = true;

        // Tracks are already generated.
        //generator.GenerateTrack();

        // Small lift to prevent spawn collision
        transform.position += Vector3.up * 0.2f;

        lastPosition = transform.position;
    }

    void EndEpisode()
    {
        trackManager.LogEpisode(totalReward);
    }

    // ----------------------------------------------------
    // COLLISION PENALTY
    // ----------------------------------------------------
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Barrier"))
        {
            totalReward -= 100f; // stronger penalty for hitting walls
        }
    }
}

