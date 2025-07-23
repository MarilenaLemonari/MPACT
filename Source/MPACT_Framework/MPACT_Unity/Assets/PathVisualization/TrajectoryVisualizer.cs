using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class FolderData
{
    public string FullName { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
}

public class TrajectoryVisualizer : MonoBehaviour
{
    [SerializeField] private string m_pathToLibFolder;
    [SerializeField] private GameObject m_capsulePrefab;
    [SerializeField] private float m_deltaTimestep = 0.04f;
    public float globalTimer = 0;

    private const string folderPattern = @"^(.+)_([0-9]+)-([0-9]+)$";

    public List<FolderData> ParseSubfolders(string mainFolderPath)
    {
        List<FolderData> parsedData = new List<FolderData>();
        
        foreach (var subfolderPath in Directory.GetDirectories(mainFolderPath))
        {
            string folderName = Path.GetFileName(subfolderPath);
            Match match = Regex.Match(folderName, folderPattern);
            
            if (match.Success)
            {
                FolderData data = new FolderData
                {
                    FullName = folderName,
                    Start = int.Parse(match.Groups[2].Value),
                    End = int.Parse(match.Groups[3].Value)
                };
                parsedData.Add(data);
            }
        }
        return parsedData;
    }
    
    private void Awake()
    {
        Time.fixedDeltaTime = m_deltaTimestep;
    }

    private void Start()
    {
        string twoPathsUp = Directory.GetParent(Application.dataPath).Parent.FullName;
        string libPath = Path.Combine(twoPathsUp, "Framework\\Analysis\\SimulationData\\StudyTrajectories_v2_Final\\" + m_pathToLibFolder);
        List<FolderData> data = ParseSubfolders(libPath);
        StartCoroutine(ProcessFolders(data, libPath));
    }
    
    public List<Vector3> SmoothPoints(List<Vector3> originalPoints, int windowSize)
    {
        if (windowSize <= 1 || originalPoints.Count < windowSize)
            return originalPoints; // No smoothing required

        List<Vector3> smoothedPoints = new List<Vector3>();

        for (int i = 0; i < originalPoints.Count; i++)
        {
            int start = Mathf.Max(i - windowSize + 1, 0);
            int end = Mathf.Min(i + windowSize - 1, originalPoints.Count - 1);
        
            Vector3 sum = Vector3.zero;
            for (int j = start; j <= end; j++)
            {
                sum += originalPoints[j];
            }

            smoothedPoints.Add(sum / (end - start + 1));
        }

        return smoothedPoints;
    }
    
    private IEnumerator ProcessFolders(List<FolderData> data, string path)
    {
        foreach (var d in data)
        {
            string tempPath = path + "\\" + d.FullName;
            print("Running: " + tempPath);
            string[] allFiles = Directory.GetFiles(tempPath, "*.csv");

            globalTimer = 0;
            int i = 0;
            foreach (string file in allFiles)
            {
                List<Vector3> points;
                float startTime;
                float endTime;
                (startTime, endTime, points) = ParseCSV(file);

                List<Vector3> smoothedPoints;
                if(path.Contains("\\P2C\\"))
                    smoothedPoints = SmoothPoints(points, 6);
                else
                    smoothedPoints = points;

                CreateAgent(startTime, endTime, smoothedPoints, i);
                i += 1;
            }
            
            yield return new WaitForSeconds(0.1f);
            string videoPath = tempPath.Replace("_Lib", "_Videos");
            MovieRecorder.Instance.InitializeRecorder(videoPath, d.FullName);
            yield return new WaitForSeconds(d.End - d.Start);
            MovieRecorder.Instance.StopCurrentRecording();
            yield return new WaitForSeconds(1f);
        }
    }

    private void FixedUpdate()
    {
        globalTimer += m_deltaTimestep;
    }

    private (float, float, List<Vector3>) ParseCSV(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        List<Vector3> points = new List<Vector3>();

        float startTime = 0;
        float endTime = 0;
        
        for (int i = 0; i < lines.Length; i++)
        {
            string[] entries = lines[i].Split(';');
            if(i == 0)
                startTime = float.Parse(entries[0]);
            else if(i == lines.Length - 1)
                endTime = float.Parse(entries[0]);

            float posX = float.Parse(entries[1]) + 16f;
            float posZ = float.Parse(entries[2]) + 2f;
            points.Add(new Vector3(posX, 1f, posZ));
        }

        return (startTime, endTime, points);
    }

    private void CreateAgent(float startTime, float endTime, List<Vector3> points, int index)
    {
        GameObject capsule = Instantiate(m_capsulePrefab, points[0], Quaternion.identity, transform.GetChild(0));
        capsule.name += "_" + index;
        capsule.GetComponent<Vis_Agent>().InitializeAgentData(startTime, endTime, points, this);
    }

    public Color CalculateDirectionColor(Vector3 direction)
    { 
        Color northColor = Color.blue;
        Color southColor = Color.yellow;
        Color eastColor = Color.red;
        Color westColor = Color.green; 

        // Interpolate between East/West colors based on X direction
        Color xColor = Color.Lerp(westColor, eastColor, (direction.x + 1) * 0.5f);
        // Interpolate between North/South colors based on Z direction
        Color zColor = Color.Lerp(southColor, northColor, (direction.z + 1) * 0.5f);
        // Blend the results of the above interpolations
        Color final = Color.Lerp(xColor, zColor, 0.5f);
        final.a = 1f;
        return final;
    }
}