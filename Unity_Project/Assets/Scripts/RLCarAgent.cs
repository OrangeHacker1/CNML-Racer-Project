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
    }

    [Serializable]
    public class ActionPacket
    {
        public float steer;     // [-1, 1]
        public float throttle;  // [0, 1]
        public float brake;     // [0, 1]
    }

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
    }

    // ----------------------------------------------------
    // OBSERVATION SPACE
    // ----------------------------------------------------
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
    }

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
        totalReward -= 5f;
    }
}

/*using UnityEngine;
using System;

public class rlcaragent : MonoBehaviour
{
    public PrometeoCarController car;
    public ProceduralTrackGenerator generator;
    public TrackManager trackmanager;

    [Header("episode")]
    public float maxepisodetime = 30f;

    private float timer;
    private Vector3 lastposition;
    private float totalreward;

    [Serializable]
    public class StatePacket
    {
        public float[] state;
        public float reward;
        public bool done;
        public int episode;
    }

    [Serializable]
    public class actionpacket
    {
        public float steer;
        public float throttle;
        public float brake;
    }

    private void start()
    {
        lastposition = transform.position;
        beginepisode();
    }

    private void fixedupdate()
    {
        if (!rlserverbridge.instance.connected)
            return;

        timer += time.fixeddeltatime;

        float reward = calculatereward();
        bool done = checkdone();

        sendstate(reward, done);

        string msg = rlserverbridge.instance.receiveaction();
        if (!string.isnullorempty(msg))
        {
            actionpacket action = jsonutility.fromjson<actionpacket>(msg);
            applyaction(action);
        }

        totalreward += reward;
        lastposition = transform.position;

        if (done)
        {
            endepisode();
            beginepisode();
        }
    }

    void sendstate(float reward, bool done)
    {
        statepacket packet = new statepacket();
        packet.state = collectstate();
        packet.reward = reward;
        packet.done = done;
        packet.episode = trackmanager.currentepisode;

        string json = jsonutility.tojson(packet);
        rlserverbridge.instance.sendstate(json);
    }

    float[] collectstate()
    {
        rigidbody rb = getcomponent<rigidbody>();

        return new float[]
        {
            transform.position.x,
            transform.position.z,
            transform.forward.x,
            transform.forward.z,
            rb.linearvelocity.magnitude
        };
    }

    void applyaction(actionpacket a)
    {
        car.steerinput = mathf.clamp(a.steer, -1f, 1f);
        car.throttleinput = mathf.clamp01(a.throttle);
        car.brakeinput = mathf.clamp01(a.brake);
    }

    float calculatereward()
    {
        float distance = Vector3.distance(transform.position, lastposition);
        float speed = getcomponent<rigidbody>().linearvelocity.magnitude;

        float reward = 0f;

        reward += distance * 0.5f;
        reward += speed * 0.1f;

        reward -= mathf.abs(car.throttleinput) * 0.02f;
        reward -= mathf.abs(car.brakeinput) * 0.03f;

        return reward;
    }

    bool checkdone()
    {
        if (timer > maxepisodetime)
            return true;

        if (transform.position.y < -2f)
            return true;

        return false;
    }

    void beginepisode()
    {
        timer = 0f;
        totalreward = 0f;

        generator.generatetrack();
        transform.position += vector3.up * 0.2f;

        lastposition = transform.position;
    }

    void endepisode()
    {
        trackmanager.logepisode(totalreward);
    }

    private void oncollisionenter(Collision collision)
    {
        totalreward -= 5f;
    }
}
*/