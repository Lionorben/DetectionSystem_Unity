using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private DetectionManager[] enemyDetectionManagers;


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

    /// <summary>
    /// Callback triggered when a player is detected by an enemy's DetectionManager.
    /// Restarts the current scene to reset the game state.
    /// </summary>
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
