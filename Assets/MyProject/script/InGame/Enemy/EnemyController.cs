using System.Globalization;
using UnityEngine;
using UnityEngine.AI;

namespace TPSRoguelite.InGame.Enemy
{
    public class EnemyController : MonoBehaviour
    {
        private const string PLAYER_TAG_NAME = "Player";

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

    }
}
