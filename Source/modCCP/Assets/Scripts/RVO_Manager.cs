using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RVO;

public class RVO_Manager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // RVO Initialization
        Simulator.Instance.setTimeStep(Time.fixedDeltaTime);
        Simulator.Instance.setAgentDefaults(
            3.0f,
            8,
            0.5f,
            0.5f,
            0.5f,
            2.0f,
            new RVO.Vector2(0.0f, 0.0f));
        Simulator.Instance.SetNumWorkers(12);
        Simulator.Instance.processObstacles();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Simulator.Instance.doStep();
    }
}
