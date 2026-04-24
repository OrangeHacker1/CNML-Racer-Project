using UnityEngine;
using System.IO;

public class TrackBatchGenerator : MonoBehaviour
{
    [SerializeField] private ProceduralTrackGenerator generator;
    [SerializeField] private int totalTracks = 100;
    [SerializeField] private int startingSeed = 1000;

    [Header("Save Settings")]
    [SerializeField] private string folderName = "GeneratedTracks";


    void Start()
    {
        GenerateBatch();
    }

    public void GenerateBatch()
    {
        // STEP 1: Try to auto-find generator if not assigned
        if (generator == null)
        {
            generator = FindFirstObjectByType<ProceduralTrackGenerator>();
        }

        // STEP 2: HARD SAFETY CHECK (prevents crash)
        if (generator == null)
        {
            Debug.LogError(
                "TrackBatchGenerator: No ProceduralTrackGenerator found in scene. " +
                "Make sure one exists and is active."
            );
            return;
        }

        // Create save folder
        string folder = Path.Combine(Application.dataPath, "Scenes", folderName);

        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        // STEP 3: Generate tracks safely
        for (int i = 0; i < totalTracks; i++)
        {
            int seed = startingSeed + i;

            // 1. Generate track
            generator.SetSeed(seed);
            generator.GenerateTrack();




            TrackData data = generator.GetTrackData();


            // 2. Save track
            if (data != null)
            {
                data.trackName = $"Track_{seed}";

                TrackSerializer.SaveTrack(data, folder);
            }
            else
            {
                Debug.LogError($"Track generation failed at seed {seed}");
            }

            Debug.Log($"Saved Track {i} (Seed {seed})");
        }

        Debug.Log($"Finished generating and saving {totalTracks} tracks.");
        //Debug.Log("Finished generating tracks.");
    }


}