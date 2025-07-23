using UnityEngine;

public class EnvSpawner : MonoBehaviour
{
    [SerializeField] private int m_quanity;
    [SerializeField] private GameObject m_envA;
    [SerializeField] private GameObject m_envB;
    [SerializeField] private bool m_saveTrajectories;

    private const float PADDING = 20f;

    private void Awake()
    {
        if (m_quanity % 2 != 0)
        {
            Debug.LogWarning("Quantity should be even for equal distribution of envs.");
            return;
        }

        int halfQuantity = m_quanity / 2;
        Vector3 spawnPosition = Vector3.zero;

        int sqrtQty = Mathf.CeilToInt(Mathf.Sqrt(m_quanity)); // Get the square root to form a grid

        for (int i = 0; i < sqrtQty; i++)
        {
            for (int j = 0; j < sqrtQty; j++)
            {
                if (i * sqrtQty + j >= m_quanity)
                    return;

                // Choose prefab
                GameObject prefabToSpawn = (i * sqrtQty + j < halfQuantity) ? m_envA : m_envB;

                // Instantiate prefab
                GameObject env = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity, transform);
                env.name += i + "_" + j;
                env.GetComponent<CustomSceneManager>().m_saveTrajectories = m_saveTrajectories;
                
                // Move to the next position
                spawnPosition.x += prefabToSpawn.transform.localScale.x + PADDING;
            }

            spawnPosition.x = 0; // Reset x position for next row
            spawnPosition.z += m_envB.transform.localScale.z + PADDING; // Move to next row
        }
    }
}

