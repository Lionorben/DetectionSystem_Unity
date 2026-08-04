using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public DetectionManager[] enemyDetectionManagers;


    void Start()
    {
        if(enemyDetectionManagers != null) 
        {
            foreach (DetectionManager enemyDetectionManager in enemyDetectionManagers)
            {
                enemyDetectionManager.OnSentientDetected -= PlayerDetected;
                enemyDetectionManager.OnSentientDetected += PlayerDetected;
            }
        }
    }


    void Update()
    {
        
    }

    public void PlayerDetected(Sentient sentient) 
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDestroy()
    {
        if (enemyDetectionManagers != null)
        {
            foreach (DetectionManager enemyDetectionManager in enemyDetectionManagers)
            {
                enemyDetectionManager.OnSentientDetected -= PlayerDetected;
            }
        }
    }
}
