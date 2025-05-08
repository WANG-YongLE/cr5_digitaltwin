using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetScene : MonoBehaviour
{
    // 呼叫這個方法可以重載當前場景
    public void ResetCurrentScene()
    {
        // 取得目前場景的 build index 並重新載入該場景
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
