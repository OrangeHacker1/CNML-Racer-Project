using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class TrackManager : MonoBehaviour
{
    public enum RunMode
    {
        Train,
        Validation,
        Test
    }

    [Header("References")]
    public TrackLoader loader;
    public RewardManager rewardManager;
    public RLCarAgent agent;
    public Transform car;
    public Rigidbody carRb;

    [Header("Experiment")]
    public RunMode mode = RunMode.Train;
    public int totalEpisodes = 100;
    public float episodeTimeout = 60f;

    [Header("Track Rules")]
    public int maxCrashesPerTrack = 5;
    public int maxTracksToRun = 100;

    private int currentEpisode = 0;
    private int tracksUsed = 0;
    private float timer = 0f;
    private bool running = false;

    private string[] trackFiles;
    private string currentTrack;

    private Dictionary<string, int> crashCounts =
        new Dictionary<string, int>();

    public int CurrentEpisode => currentEpisode;

    //private int currentEpisode;
    //private float timer;
    //private string[] trackFiles;
    //private bool running;

    // Public read-only access
    //public int CurrentEpisode => currentEpisode;

    void Start()
    {
        BeginExperiment();
    }

    void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;

        if (timer >= episodeTimeout)
        {
            EndEpisode(false, false);
        }
    }

    // ==================================================
    // START
    // ==================================================
    public void BeginExperiment()
    {
        LoadTrackList();

        currentEpisode = 0;
        tracksUsed = 0;

        StartEpisode();
    }

    void LoadTrackList()
    {
        string mode_tag = "";

        if (mode == RunMode.Train)
        {
             mode_tag = "train";
        }

        string folder =
            Application.dataPath +
            "/Scenes/GeneratedTracks/" +
            mode_tag+"/";

        if (!Directory.Exists(folder))
        {
            Debug.LogError("Track folder missing: " + folder);
            return;
        }

        trackFiles = Directory.GetFiles(folder, "*.json");
    }

    // ==================================================
    // EPISODE START
    // ==================================================
    void StartEpisode()
    {

        if (trackFiles == null || trackFiles.Length == 0)
        {
            Debug.LogError("No tracks found.");
            return;
        }

        if (currentEpisode >= totalEpisodes)
        {
            FinishExperiment();
            return;
        }

        if (tracksUsed >= maxTracksToRun)
        {
            FinishExperiment();
            return;
        }


        timer = 0f;

        currentTrack = SelectTrack();

        loader.LoadByPath(currentTrack);

        ResetCar();
        rewardManager.ResetReward();

        if (agent != null)
            agent.BeginExternalEpisode();


        running = true;
    }

    string SelectTrack()
    {
        List<string> valid = new List<string>();

        foreach (string file in trackFiles)
        {
            if (!crashCounts.ContainsKey(file))
                crashCounts[file] = 0;

            if (crashCounts[file] < maxCrashesPerTrack)
                valid.Add(file);
        }

        if (valid.Count == 0)
        {
            Debug.Log("All tracks exhausted.");
            FinishExperiment();
            return null;
        }

        if (mode == RunMode.Train)
        {
            return valid[Random.Range(0, valid.Count)];
        }

        int idx = currentEpisode % valid.Count;
        return valid[idx];
    }

    // ==================================================
    // RESET CAR
    // ==================================================
    void ResetCar()
    {
        if (car == null) return;

        car.position += Vector3.up * 0.5f;

        car.rotation = Quaternion.identity;

        if (carRb != null)
        {
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
        }
    }

    // ==================================================
    // END EPISODE
    // ==================================================
    public void EndEpisode(bool finished, bool crashed)
    {
        if (!running) return;

        running = false;

        if (crashed)
        {
            crashCounts[currentTrack]++;
        }

        EpisodeResult result = new EpisodeResult();
        result.mode = mode.ToString();
        result.trackName =
            Path.GetFileNameWithoutExtension(currentTrack);
        result.episodeIndex = currentEpisode;
        result.reward = rewardManager.TotalReward;
        result.distance = rewardManager.TotalDistance;
        result.time = timer;
        result.crashed = crashed;
        result.finished = finished;

        ResultLogger.Save(result);

        currentEpisode++;
        tracksUsed++;

        StartEpisode();
    }

    void FinishExperiment()
    {
        running = false;
        Debug.Log("Experiment Complete.");
    }

    public void LogEpisode(float reward)
    {
        Debug.Log("Episode " + currentEpisode +
                  " Reward: " + reward);
    }

    /*
    public void LogEpisode(float reward)
    {
        Debug.Log("Episode " + currentEpisode + " Reward: " + reward);
    }

    public void EndEpisode(bool finished, bool crashed)
    {
        running = false;

        EpisodeResult result = new EpisodeResult();
        result.mode = mode.ToString();
        result.trackName = Path.GetFileNameWithoutExtension(loader.CurrentTrackFile);
        result.episodeIndex = currentEpisode;
        result.reward = rewardManager.TotalReward;
        result.distance = rewardManager.TotalDistance;
        result.time = timer;
        result.crashed = crashed;
        result.finished = finished;

        ResultLogger.Save(result);

        currentEpisode++;
        StartEpisode();
    }
    */
}
