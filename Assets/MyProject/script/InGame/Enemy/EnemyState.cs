using UnityEngine;
using UnityEngine.Events;
using Core.Interface;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 敵のデータ
        /// </summary>
        [field: SerializeField] public EnemyData EnemyDataAsset { get; private set; }

        /// <summary>
        /// 現在の体力
        /// </summary>
        public double CurrentHP { get; private set; }//int

        public event UnityAction<EnemyState> OnReturnToPoolAction;

        private void OnEnable()
        {
            if (EnemyDataAsset == null)
            {
                Debug.Log("EnemyDataAssetがセットされていません");
                return;
            }

            CurrentHP = EnemyDataAsset.MaxHP;
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
    }
}
