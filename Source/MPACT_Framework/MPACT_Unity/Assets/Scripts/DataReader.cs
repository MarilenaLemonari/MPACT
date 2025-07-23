using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

public class DataReader : MonoBehaviour
{
    private string m_dataPath = "";
    private string m_trajectoriesPath = "";
    
    private void Awake()
    {
        SetPaths();
    }

    private void SetPaths()
    {
        String currentPath = Directory.GetCurrentDirectory();
        DirectoryInfo parent = Directory.GetParent(currentPath);
        m_dataPath = parent.ToString() + "/MPACT_Model/Data/JSONs/";
        m_trajectoriesPath = parent.ToString() + "/MPACT_Model/Data/Trajectories/";
    }

    public class SceneParams
    {
        [JsonProperty("min_width")]
        public float MinWidth { get; set; }
        [JsonProperty("max_width")]
        public float MaxWidth { get; set; }
        [JsonProperty("min_height")]
        public float MinHeight { get; set; }
        [JsonProperty("max_height")]
        public float MaxHeight { get; set; }
    }
    
    public class SceneObject
    {
        public float pos_x { get; set; }
        public float pos_z { get; set; }
        public float scale_x { get; set; }
        public float scale_z { get; set; }
        public float type { get; set; }
    }
    
    public class SceneRootObject
    {
        [JsonProperty("EnvironmentParams")]
        public SceneParams EnvironmentParams { get; set; }
        
        [JsonProperty("EnvironmentObjects")]
        public List<SceneObject> SceneObjects { get; set; }
    }

    public class Environment
    {
        public int width { get; set; }
        public int height { get; set; }
        public int frame_interval { get; set; }
        public int framerate { get; set; }
    }
    
    public class SimulationRootObject
    {
        public Environment Environment { get; set; }
        public Dictionary<string, Dictionary<string, BehaviorProfile>> Classes { get; set; }
        public List<List<float>> Clusters { get; set; } 
        public Dictionary<string,  List<List<float>>> Agents { get; set; }
    }
    
    public SimulationRootObject ReadSimulationJsonFile(string datasetName)
    {
        string path = m_dataPath + datasetName + "/simulation_data.json";
        string jsonContent = File.ReadAllText(path);
        SimulationRootObject simulationRootObject = JsonConvert.DeserializeObject<SimulationRootObject>(jsonContent);
        return simulationRootObject;
    }
    
    public SceneRootObject ReadSceneObjectsJsonFile(string datasetName)
    {
        int lastIndex = datasetName.LastIndexOf('-');
        string result = datasetName;
        if (lastIndex >= 0)  // Check if underscore is present
        {
            result = datasetName.Substring(0, lastIndex);
        }

        string path = m_trajectoriesPath + result + "/env.json";
        if (!File.Exists(path))
            return null;
        
        string jsonContent = File.ReadAllText(path);
        SceneRootObject sceneRootObject = JsonConvert.DeserializeObject<SceneRootObject>(jsonContent);
        return sceneRootObject;
    }

    public class RealData
    {
        public float timestep;
        public int frame;
        public Vector4 position;
    }

    public Dictionary<string, List<RealData>> ReadCsvFilesInDirectory(string datasetName, int framerate, SceneParams sceneParams)
    {
        Dictionary<string, List<RealData>> agentData = new Dictionary<string, List<RealData>>();

        float interval = 1f / (float)framerate;
        string directoryPath = m_trajectoriesPath + datasetName + "/";
        // Get all CSV files in the directory
        string[] csvFiles = Directory.GetFiles(directoryPath, "*.csv");
        // Iterate through each file
        foreach (string csvFile in csvFiles)
        {
            // Read all lines in the CSV file
            string[] lines = File.ReadAllLines(csvFile, Encoding.UTF8);
            List<RealData> agentPositions = new List<RealData>();
            // Iterate through each line (row) in the CSV file
            bool isFirstLine = true;
            
            foreach (string line in lines)
            {
                if (isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }
                
                // Split the line into columns using the semicolon delimiter
                string[] columns = line.Split(';');
                // Check if there are enough columns
                RealData data = new RealData
                {
                    timestep = float.Parse(columns[0]),
                    frame = (int)(float.Parse(columns[0]) / interval),
                    position = new Vector4(float.Parse(columns[1]), float.Parse(columns[2]), 0, 0)
                };

                agentPositions.Add(data);
            }

            string key = Path.GetFileNameWithoutExtension(csvFile);
            agentData.Add(key, agentPositions);
        }

        // Normalize the positions
        foreach (var positions in agentData.Values)
        {
            foreach (RealData data in positions)
            {
                float normalizedX = Mathf.InverseLerp(sceneParams.MinWidth, sceneParams.MaxWidth, data.position.x) * 2 - 1;
                float normalizedZ = Mathf.InverseLerp(sceneParams.MinHeight, sceneParams.MaxHeight, data.position.y) * 2 - 1;
                data.position = new Vector4(normalizedX, normalizedZ, data.position.x, data.position.y);
            }
        }
        return agentData;
    }
}
