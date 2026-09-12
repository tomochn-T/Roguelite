using UnityEngine;
using UnityEngine.SceneManagement;
using TPSRoguelite.InGame.Manager;
using TPSRoguelite.UI;

public class TitlePresenter : MonoBehaviour
{
    private const string IN_GAME_SEENE_NAME = "main";

    [SerializeField] private TitleView titleView;
    private TitleModel resultModel;

    private void Start()
    {
        if (titleView == null)
        {
            return;
        }

        resultModel = new TitleModel();
        resultModel.Initialize();

        titleView.OnTitleAction += InGame;
    }

    private void OnDestroy()
    {
        if (titleView != null)
        {
            titleView.OnTitleAction -= InGame;
        }
    }

    private void InGame()
    {
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        SceneManager.LoadScene(IN_GAME_SEENE_NAME);
    }
}
