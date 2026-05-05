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

    // Episode is a run with a car. Every restart is an eppisode. 
    [Header("Experiment")]
    public RunMode mode = RunMode.Train;
    public int totalEpisodes = 100;
    public float episodeTimeout = 60f;

    [Header("Track Rules")]
    public int maxCrashesPerTrack = 5;
    public int maxTracksToRun = 100;

    [Header("Track Timing (NEW)")]
    public float trackDuration = 180f;      // seconds per track
    public int maxEpisodesPerTrack = 5;     // episodes per track

    private float trackTimer = 0f;
    private int episodesOnTrack = 0;

    private int currentEpisode = 0;
    private int tracksUsed = 0;
    private float timer = 0f;
    private bool running = false;

    private string[] trackFiles;
    private string currentTrack;
    private int currentTrackIndex = 0;

    private Dictionary<string, int> crashCounts =
        new Dictionary<string, int>();

    public int CurrentEpisode => currentEpisode;

    // Track Changing
    public bool IsNewTrack { get; private set; }
    public string CurrentTrackName => currentTrack;
    

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
        /*IsNewTrack = false;
        if (!running) return;

        timer += Time.deltaTime;

        if (timer >= episodeTimeout)
        {
            EndEpisode(false, false);
        }*/
        IsNewTrack = false;

        if (!running) return;

        timer += Time.deltaTime;
        trackTimer += Time.deltaTime;

        // ---- Episode timeout ----  (This is for tracking how many car runs occurred.)
        if (timer >= episodeTimeout)
        {
            EndEpisode(false, false);
        }

        // ---- Track timeout ---  (This is for trakcing the Tracks / Tasks)
        if (trackTimer >= trackDuration)
        {
            Debug.Log("Track time expired -> switching track");
            ForceTrackChange();
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

        LoadNewTrack();
        StartEpisode();
    }

    void LoadTrackList()
    {
        string mode_tag = "";

        if (mode == RunMode.Train)
        {
             mode_tag = "train";
        }
        if (mode == RunMode.Test)
        {
            mode_tag = "test";
        }
        if (mode == RunMode.Validation)
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
    // TRACK CONTROL (NEW)
    // ==================================================
    void LoadNewTrack()
    {
        if (trackFiles == null || trackFiles.Length == 0)
        {
            Debug.LogError("No tracks found.");
            return;
        }

        if (tracksUsed >= maxTracksToRun)
        {
            FinishExperiment();
            return;
        }

        currentTrack = SelectTrack();
        loader.LoadByPath(currentTrack);

        trackTimer = 0f;
        episodesOnTrack = 0;
        tracksUsed++;

        IsNewTrack = true;

        Debug.Log($"Loaded NEW TRACK: {currentTrack}");
    }

    // Track Change
    void ForceTrackChange()
    {
        LoadNewTrack();
        StartEpisode();
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

        timer = 0f;

        ResetCar(false);  // Normal Reset
        rewardManager.ResetReward();

        if (agent != null)
            agent.BeginExternalEpisode();

        running = true;

        /*
        if (tracksUsed >= maxTracksToRun)
        {
            FinishExperiment();
            return;
        }


        timer = 0f;

        currentTrack = SelectTrack();
        IsNewTrack = true;   // The track changed.

        loader.LoadByPath(currentTrack);

        ResetCar();
        rewardManager.ResetReward();

        if (agent != null)
            agent.BeginExternalEpisode();


        running = true;*/
    }


    // Select a new Track.
    string SelectTrack()
    {
        List<string> valid = new List<string>();

        foreach (string file in trackFiles)
        {
           
            //if (!crashCounts.ContainsKey(file))
             //   crashCounts[file] = 0;
            crashCounts[file] = 0;
            // Add the files regardless of crashCounts.
            valid.Add(file);
            //if (crashCounts[file] < maxCrashesPerTrack)
            //    valid.Add(file);
        }

        if (currentTrackIndex >= valid.Count)
        {
            Debug.Log("All tracks exhausted.");
            FinishExperiment();
            return null;
        }

        /*
        if (mode == RunMode.Train)
        {
            return valid[Random.Range(0, valid.Count)];
        }

        int idx = currentEpisode % valid.Count;
        return valid[idx];*/
        //return valid[Random.Range(0, valid.Count)]; // RANDOM
        string trackLoading = valid[currentTrackIndex];
        currentTrackIndex++;
        return trackLoading;

    }

    // Crash Deteector Helper
    public void HandleCrash()
    {
        ResetCar(true);   // BACKUP LOGIC

        EndEpisode(false, true);
    }


    // ==================================================
    // RESET CAR
    // ==================================================
    void ResetCar(bool crashed = false)
    {
        if (car == null) return;

        // Stop physics first
        if (carRb != null)
        {
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
        }

        // ----------------------------------
        // Move car backward if crashed
        // ----------------------------------
        if (crashed)
        {
            Vector3 backward = -car.forward;

            // Move back and slightly up
            car.position += backward * 3f + Vector3.up * 0.5f;

            // Optional: small random rotation to escape bad angles
            float randomYaw = Random.Range(-20f, 20f);
            car.rotation = Quaternion.Euler(0, car.eulerAngles.y + randomYaw, 0);
        }
        else
        {
            // Normal reset (start of episode)
            car.position += Vector3.up * 0.5f;
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
        //tracksUsed++;
        episodesOnTrack++;   // correct counter

        // ----------------------------------
        // HARD SWITCH: too many crashes
        // ----------------------------------
        if (crashCounts[currentTrack] >= maxCrashesPerTrack)
        {
            Debug.Log("Track failed too many times -> restarting track");
            // LoadNewTrack();

            StartEpisode();
            return;
        }

        // ---- Decide: stay or switch track ---- Episode Limit
        if (episodesOnTrack >= maxEpisodesPerTrack)
        {
            Debug.Log("Max episodes per track reached -> switching track");
            LoadNewTrack();
        }

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
