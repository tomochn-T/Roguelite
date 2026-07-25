using UnityEngine;
using UnityEngine.AI;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private const string PLAYER_TAG_NAME = "Player";

        /// <summary>
        /// ノックバックする強度
        /// </summary>
        private const float KNOCKBACK_FORCE = 2.0f;

        /// <summary>
        /// ノックバックする時間
        /// </summary>
        private const float KNOCKBACK_DURARION = 0.15f;

        /// <summary>
        /// 敵の本体
        /// </summary>
        [field: SerializeField] private EnemyState enemyState = null;

        /// <summary>
        /// NavMeshAgent
        /// </summary>
        [SerializeField] private NavMeshAgent navMeshAgent = null;

        /// <summary>
        /// 目的地となるplayerのTransform
        /// </summary>
        private Transform targetplayer = null;

        /// <summary>
        /// ノックバック動作のキャンセルトークン
        /// </summary>
        private CancellationTokenSource hitCts;


        private void Awake()
        {
            //シーンから"Playerというタグがついたオブジェクトを探す"
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if(player != null)
            {
                targetplayer = player.transform;
            }
            else
            {
                Debug.Log($"{PLAYER_TAG_NAME}というタグのついたオブジェクトが見つかりませんでした。");
            }

            if (navMeshAgent != null && enemyState != null && enemyState.EnemyDataAsset != null)
            {
                navMeshAgent.speed = enemyState.EnemyDataAsset.MoveSpeed;
            }
        }

        private void Update()
        {
            
            if(targetplayer != null && navMeshAgent != null)
            {
                navMeshAgent.SetDestination(targetplayer.position);
            }
        }

        private void OnEnable()
        {
            if(enemyState != null)
            {
                enemyState.OnDamageAction -= HandleDamage;
                enemyState.OnDamageAction += HandleDamage;
            }
        }

        private void OnDisable()
        {
            if (enemyState != null)
            {
                enemyState.OnDamageAction -= HandleDamage;
            }

            if(navMeshAgent != null && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = false;
            }
        }

        private async UniTaskVoid KnockbackAsync(CancellationToken token)
        {
            if(navMeshAgent == null)
            {
                return;
            }

            bool wasStopped = navMeshAgent.isStopped;
            navMeshAgent.isStopped = true;

            if(targetplayer != null)
            {
                Vector3 dir = (transform.position - transform.position).normalized;
                dir.y = 0;
                transform.position += dir * KNOCKBACK_FORCE;
            }

            bool isCanceeled = await UniTask.Delay(
                TimeSpan.FromSeconds(KNOCKBACK_DURARION), cancellationToken: token)
                .SuppressCancellationThrow();
                
            if(!isCanceeled && navMeshAgent.isActiveAndEnabled)
            {
                navMeshAgent.isStopped = wasStopped;
            }
        }

        private void HandleDamage()
        {
            hitCts?.Cancel();
            hitCts?.Dispose();
            hitCts = null;

            hitCts = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                hitCts.Token, this.GetCancellationTokenOnDestroy()); 

            KnockbackAsync(linkedCts.Token).Forget();
        }


    }
}
