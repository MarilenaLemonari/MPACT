using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [SerializeField] private float m_interactionProbability;
    [SerializeField] private int m_changeInterval;
    [SerializeField] private List<Transform> m_allObjects;
    [SerializeField] private List<Transform> m_interactionObjects;
    [SerializeField] private Material m_obstacleMaterial;
    [SerializeField] private Material m_interactionMaterial;

    // Start is called before the first frame update
    private void Awake()
    {
        m_allObjects = new List<Transform>();
        m_interactionObjects = new List<Transform>();
        foreach (Transform obj in transform)
        {
            m_allObjects.Add(obj);
            if(obj.CompareTag("Interaction"))
                m_interactionObjects.Add(obj);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if(Time.fixedTime % m_changeInterval == 0)
            ShuffleSceneObjects();
    }
    
    public Vector3 GetCloserInteraction(Vector3 currentPos)
    {
        Vector3 objectMin = currentPos;
        float minDist = Mathf.Infinity;
        foreach (Transform obj in m_interactionObjects)
        {
            Collider objectCollider = obj.GetComponent<Collider>();
            Vector3 closestPointOnObject = objectCollider.ClosestPoint(currentPos);
            float distanceToEdge = Vector3.Distance(currentPos, closestPointOnObject);
            
            if (distanceToEdge < minDist)
            {
                objectMin = closestPointOnObject;
                minDist = distanceToEdge;
            }
        }

        return objectMin;
    }
    
    private void ShuffleSceneObjects()
    {
        m_interactionObjects.Clear();
        foreach (Transform obj in m_allObjects)
        {
            float rand = UnityEngine.Random.Range(0f, 1f);
            if (rand <= m_interactionProbability)
            {
                obj.tag = "Interaction";
                obj.GetComponent<Renderer>().material = m_interactionMaterial;
                m_interactionObjects.Add(obj);
            }
            else
            {
                obj.tag = "Obstacle";
                obj.GetComponent<Renderer>().material = m_obstacleMaterial;
            }
        }
    }
}
