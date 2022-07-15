using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

[Serializable]
public class MediaPipeResult
{
    public MediaPipeHandLMs[] results;
}
[Serializable]
public class MediaPipeHandLMs
{
    public double time;
    public List<LMDataPoint> data;
}
[Serializable]
public class LMDataPoint
{
    public float x;
    public float y;
    public float z;
}

public class jsonDeserializer : MonoBehaviour
{
    

    public MediaPipeResult handLMs = null;
    private void Awake()
    {
        //string jsonText = File.ReadAllText(Path.Combine(Application.dataPath, "jsonData/detectedHandLMs.json"));
        string jsonText = File.ReadAllText(Path.Combine(Application.dataPath, "jsonData/frontKick.json"));
        handLMs = JsonUtility.FromJson<MediaPipeResult>(
            "{\"results\":" + jsonText + "}");
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void serializeAndOutputFile(MediaPipeResult timeData, string fileName)
    {
        string jsonString = JsonUtility.ToJson(timeData);
        print(jsonString);
        StreamWriter file = new StreamWriter(System.IO.Path.Combine(Application.dataPath, fileName));
        file.Write(jsonString);
        file.Close();
        print("Record rotation and output complete");
    }
    /// <summary>
    /// 讀取在python處理完成的rotations
    /// </summary>
    public MediaPipeResult readAndParseRotation(string fileName, bool printText=false)
    {
        string jsonText = File.ReadAllText(Path.Combine(Application.dataPath, fileName));
        MediaPipeResult handRotation = JsonUtility.FromJson<MediaPipeResult>(
            "{\"results\":" + jsonText + "}");
        if(printText)
            print(jsonText.Substring(0, 30));
        return handRotation;
    }
}

/// <summary>
/// Credit: https://stackoverflow.com/questions/36239705/serialize-and-deserialize-json-and-json-array-in-unity
/// Goal: Deal with multiple json objects in a array
/// </summary>
public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}