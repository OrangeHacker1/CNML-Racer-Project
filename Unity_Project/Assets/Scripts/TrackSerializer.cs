using UnityEngine;
using System.IO;

public static class TrackSerializer
{
    public static void SaveTrack(TrackData data, string folder)
    {
        // string folder = Application.dataPath + "/Scenes/GeneratedTracks/";

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string json = JsonUtility.ToJson(data, true);
        string path = folder + data.trackName + ".json";

        File.WriteAllText(path, json);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}