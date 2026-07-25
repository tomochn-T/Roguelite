using UnityEngine;
using UnityEngine.Events;
using Core.Interface;
using Core.MasterData;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 点滅時間
        /// </summary>
        private const float FLASH_DUEATION = 0.1f;

        /// <summary>
        /// キャラクターのレンダラー
        /// </summary>
        [SerializeField] private Renderer[] modeelRenderers;

        /// <summary>
        /// キャラクターのもともとの色
        /// </summary>
        private Color[] defaultColors;

        /// <summary>
        /// 点滅するアニメーションのキャンセルトークン
        /// </summary>
        private CancellationTokenSource flashCts;

        /// <summary>
        /// 敵のデータ
        /// </summary>
        public EnemyDataRecord EnemyDataAsset { get; private set; }

        /// <summary>
        /// 現在の体力
        /// </summary>
        public double CurrentHP { get; private set; }//int

        public event UnityAction<EnemyState> OnReturnToPoolAction;

        public event UnityAction OnDamageAction;

        public void Initialize(ulong id)
        {
            EnemyDataAsset = MasterDataAccessor.Instance.GetById<EnemyDataRecord>(id);

            if(modeelRenderers != null)
            {
                defaultColors = new Color[modeelRenderers.Length];
                for(int i= 0;i< modeelRenderers.Length; i++)
                {
                    if (modeelRenderers[i] != null)
                    {
                        defaultColors[i] = modeelRenderers[i].material.color;
                    }
                }
            }
        }


        public void Setup()
        {
            if (EnemyDataAsset == null)
            {
                Debug.Log("EnemyDataAssetがセットされていません");
                return;
            }

            CurrentHP = EnemyDataAsset.MaxHP;
            gameObject.SetActive(true);
            ResetColor();
        }

        public void TakeDamage(double damageAmount)//int
        {
            //マイナスダメージ(回復)を防ぐ
            if (damageAmount <= 0)
            {
                return;
            }

            CurrentHP -= damageAmount;
            Debug.Log($"{EnemyDataAsset.EnemyName}に{damageAmount}のダメージ!残りHP:{CurrentHP}");

            if (CurrentHP > 0)
            {
                OnDamageAction?.Invoke();

                flashCts?.Cancel();
                flashCts?.Dispose();
                flashCts = null;

                flashCts = new CancellationTokenSource();
                var linlkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    flashCts.Token, this.GetCancellationTokenOnDestroy());
                DamageFlashAsync(linlkedCts.Token).Forget();

            }

            if (CurrentHP <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Debug.Log($"{EnemyDataAsset.EnemyName}を倒しました");
            gameObject.SetActive(false);
            OnReturnToPoolAction?.Invoke(this);
        }

        /// <summary>
        /// 色のリセット
        /// </summary>
        private void ResetColor()
        {
            if(modeelRenderers == null || defaultColors == null)
            {
                return;
            }

            for(int i = 0;i < modeelRenderers.Length; i++)
            {
                if (modeelRenderers[i] != null)
                {
                    modeelRenderers[i].material.color = defaultColors[i];
                }
            }
        }

        private async UniTaskVoid DamageFlashAsync(CancellationToken token)
        {
            if(modeelRenderers == null)
            {
                return;
            }

            foreach(var renderers in modeelRenderers)
            {
                if(renderers != null)
                {
                    renderers.material.color = Color.red;
                }
            }

            bool isCanceled = await UniTask.Delay(TimeSpan.FromSeconds(FLASH_DUEATION),
                cancellationToken: token).SuppressCancellationThrow();

            if (!isCanceled)
            {
                ResetColor();
            }
        }

    }
}
