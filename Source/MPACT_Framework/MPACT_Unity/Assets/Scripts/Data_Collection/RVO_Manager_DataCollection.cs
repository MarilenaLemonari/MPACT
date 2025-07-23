using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

public class RVO_Manager_DataCollection : Singleton<RVO_Manager_DataCollection>
{
    private List<CustomSceneManager> m_envs;
    public bool obstaclesProcessed;
    private bool m_running;

    // Start is called before the first frame update
    void Start()
    {
        NewSimulatorRVO();
        
        m_envs = new List<CustomSceneManager>(transform.childCount);
        foreach (Transform env in transform)
        {
            m_envs.Add(env.GetComponent<CustomSceneManager>());
        }
    }

    private void NewSimulatorRVO()
    {
        Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
        Simulator.Instance.setAgentDefaults(
            3.0f,
            5,
            0.5f,
            0.5f,
            0.4f,
            2.0f,
            new RVO.Vector2(0f, 0f));
        Simulator.Instance.SetNumWorkers(16);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(m_running)
            Simulator.Instance.doStep();
        
        if(CheckAllReady())
            StartCoroutine(StartAllSimulations());
    }

    private bool CheckAllReady()
    {
        foreach (var env in m_envs)
        {
            if (env.m_envReady == false)
                return false;
        }

        return true;
    }

    private IEnumerator StartAllSimulations()
    {
        m_running = false;
        if (Simulator.Instance.obstacles_.Count > 0)
        {
            Simulator.Instance.Clear();
            NewSimulatorRVO();
        }

        obstaclesProcessed = false;
        
        foreach (var env in m_envs)
        {
            env.StartNextSimulation();
        }

        yield return new WaitForSeconds(0.01f);
        
        ReprocessObstacles();
        yield return new WaitForSeconds(0.1f);
        m_running = true;
    }

    private void ReprocessObstacles()
    {
        // Get all objects with the relevant tags
        List<GameObject> objs = new List<GameObject>(GameObject.FindGameObjectsWithTag("Interaction"));

        foreach (var obj in objs)
        {
            if (obj.transform.position.y < -10f)
                continue;

            MeshFilter box = obj.GetComponent<MeshFilter>();
            Vector3[] localVertices = box.mesh.vertices;
        
            IList<RVO.Vector2> obstacle = new List<RVO.Vector2>(localVertices.Length);
        
            // Directly convert local 3D vertices to world 3D, then to 2D
            for (int i = 0; i < localVertices.Length; i++)
            {
                Vector3 worldVertex = box.transform.TransformPoint(localVertices[i]);
                obstacle.Add(new RVO.Vector2(worldVertex.x, worldVertex.z));
            }
        
            Simulator.Instance.addObstacle(obstacle);   
        }
        Simulator.Instance.processObstacles();
        obstaclesProcessed = true;
    }
}
