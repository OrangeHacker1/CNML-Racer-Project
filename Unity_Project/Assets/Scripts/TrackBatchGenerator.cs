using UnityEngine;

public class TrackBatchGenerator : MonoBehaviour
{
    [SerializeField] private ProceduralTrackGenerator generator;
    [SerializeField] private int totalTracks = 100;
    [SerializeField] private int startingSeed = 1000;

    void Start()
    {
        GenerateBatch();
    }

    public void GenerateBatch()
    {
        if (generator == null)
        {
            generator = FindFirstObjectByType<ProceduralTrackGenerator>();
        }

        for (int i = 0; i < totalTracks; i++)
        {
            generator.SetSeed(startingSeed + i);
            generator.GenerateTrack();

            Debug.Log($"Generated Track {i}");
        }

        Debug.Log("Finished generating tracks.");
    }
}