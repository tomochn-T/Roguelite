using UnityEngine;
using Core.Interface;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyState : MonoBehaviour, IDamageable
    {
        /// <summary>
        /// 体力の最大値
        /// </summary>
        private const int MAX_HP = 100;

        /// <summary>
        /// 現在の体力
        /// </summary>
        public int CurrentHP { get; private set; }

        private void Awake()
        {
            CurrentHP = MAX_HP;
        }

        public void TakeDamage(int damageAmount)
        {
            //マイナスダメージ(回復)を防ぐ
            if (damageAmount <= 0)
            {
                return;
            }

            CurrentHP -= damageAmount;
            Debug.Log($"敵に{damageAmount}のダメージ!残りHP:{CurrentHP}");

            if (CurrentHP <= 0)
            {
                Die();
            }

        }

        private void Die()
        {
            Debug.Log("敵を倒しました");
            Destroy(gameObject);
        }
    }
}
