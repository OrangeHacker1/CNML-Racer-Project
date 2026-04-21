using UnityEngine;
using System;

public class RLCarAgent : MonoBehaviour
{
    public PrometeoCarController car;
    public ProceduralTrackGenerator generator;
    public TrackManager trackManager;

    [Header("Episode")]
    public float maxEpisodeTime = 30f;

    private float timer;
    private Vector3 lastPosition;
    private float totalReward;

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
        public float steer;
        public float throttle;
        public float brake;
    }

    private void Start()
    {
        lastPosition = transform.position;
        BeginEpisode();
    }

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
            ActionPacket action = JsonUtility.FromJson<ActionPacket>(msg);
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

    void SendState(float reward, bool done)
    {
        StatePacket packet = new StatePacket();
        packet.state = CollectState();
        packet.reward = reward;
        packet.done = done;
        packet.episode = trackManager.CurrentEpisode;

        string json = JsonUtility.ToJson(packet);
        RLServerBridge.Instance.SendState(json);
    }

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

    void ApplyAction(ActionPacket a)
    {
        car.steerInput = Mathf.Clamp(a.steer, -1f, 1f);
        car.throttleInput = Mathf.Clamp01(a.throttle);
        car.brakeInput = Mathf.Clamp01(a.brake);
    }

    float CalculateReward()
    {
        float distance = Vector3.Distance(transform.position, lastPosition);
        float speed = GetComponent<Rigidbody>().linearVelocity.magnitude;

        float reward = 0f;

        reward += distance * 0.5f;
        reward += speed * 0.1f;

        reward -= Mathf.Abs(car.throttleInput) * 0.02f;
        reward -= Mathf.Abs(car.brakeInput) * 0.03f;

        return reward;
    }

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

        generator.GenerateTrack();
        transform.position += Vector3.up * 0.2f;

        lastPosition = transform.position;
    }

    void EndEpisode()
    {
        trackManager.LogEpisode(totalReward);
    }

    private void OnCollisionEnter(Collision collision)
    {
        totalReward -= 5f;
    }
}
