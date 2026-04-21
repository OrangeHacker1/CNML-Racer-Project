using System.IO;
using UnityEngine;

public static class ResultLogger
{
    public static void Save(EpisodeResult result)
    {
        string folder = Application.dataPath + "/Results/" + result.mode + "/";

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string file =
            folder +
            $"Episode_{result.episodeIndex:D4}_{result.trackName}.json";

        string json = JsonUtility.ToJson(result, true);
        File.WriteAllText(file, json);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
