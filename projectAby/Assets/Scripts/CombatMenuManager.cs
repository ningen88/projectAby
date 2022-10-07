using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CombatMenuManager : MonoBehaviour
{
    [SerializeField] GridMap gridMap;
    [SerializeField] string menuTitleName;
    [SerializeField] GameObject endGameMsg;

    public void OnEndTurnClick()
    {
        gridMap.isClick = true;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(menuTitleName);
    }

    public void ActivateEndGameMsg()
    {
        endGameMsg.SetActive(true);
    }
}
