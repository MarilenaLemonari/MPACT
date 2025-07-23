using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public class ConsoleManager : MonoBehaviour
{
    private string m_dataPath = "";
    private void Awake()
    {
        SetPath();
    }

    private void SetPath()
    {
        String currentPath = Directory.GetCurrentDirectory();
        DirectoryInfo parent = Directory.GetParent(currentPath);
        m_dataPath = parent.ToString() + "/UnityData/";
    }
    
    public class ParametersGrid
    {
        public float goal;
        public float group;
        public float interaction;
        public float interconn;
    }

    public class EnvironmentGrid
    {
        public float pos_x;
        public float pos_z;
        public float scale_x;
        public float scale_z;
        public float type;
    }

    public void WriteEnvJson(ParametersGrid pg, List<EnvironmentGrid> objects, string env, int index)
    {
        string folderName = "Trajectories/" + env + "_" + index;
        string dir = m_dataPath + folderName;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        string filePath = dir + "/" + "env.json";

        List<ParametersGrid> data = new List<ParametersGrid>();
        data.Add(pg);

        string json = "\"ParametersGrid\":" + JsonConvert.SerializeObject(data.ToArray());
        json += ",\"EnvironmentGrid\":" + JsonConvert.SerializeObject(objects.ToArray());

        //write string to file
        System.IO.File.WriteAllText(filePath, "{" + json + "}");
    }

    public void WriteTrajectories(List<float> timesteps, List<Vector3> pos, List<float> speed, List<float> rot, Vector3 startPos, Vector3 goalPos, int len, string env, int index, int agent)
    {
        string folderName = "Trajectories/" + env + "_" + index;
        string dir = m_dataPath + folderName;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string filePath = dir + "/" + agent + ".csv";
        StreamWriter writer = new StreamWriter(filePath);
        writer.WriteLine(startPos.x.ToString("F2")+ ";" + startPos.z.ToString("F2")
                         + ";" + goalPos.x.ToString("F2")+ ";" + goalPos.z.ToString("F2"));
        for (int i = 0; i < len; i++)
            writer.WriteLine(timesteps[i].ToString("F2") + ";" + pos[i].x.ToString("F2") + ";" + pos[i].z.ToString("F2") + ";" + rot[i].ToString("F2") 
                             + ";" + speed[i].ToString("F2"));
        writer.Flush();
        writer.Close();
    }
}
