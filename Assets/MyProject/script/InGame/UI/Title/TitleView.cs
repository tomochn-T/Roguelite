using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TitleView : MonoBehaviour
{
    [SerializeField] private Button TitleButton;

    public event UnityAction OnTitleAction;

    private void Awake()
    {

        if(TitleButton != null)
        {
            TitleButton.onClick.AddListener(
                () => OnTitleAction?.Invoke());
        }
    }
}
