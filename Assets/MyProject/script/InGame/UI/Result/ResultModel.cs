using TPSRoguelite.InGame.Manager;
using UnityEngine.PlayerLoop;

namespace TPSRoguelite.UI
{
    public class ResultModel
    {
        public bool IsClear { get; private set; }
        public int Level { get; private set; }
        public float SuvivedTime { get; private set; }

        public void Initialize()
        {
            if (GameManager.Instance != null)
            {
                IsClear = GameManager.Instance.isGameClear;
                Level = GameManager.Instance.FinalLevel;
                SuvivedTime = GameManager.Instance.SurviedTime;
            }
        }
    }
}

