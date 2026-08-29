using UnityEngine;
using UnityEngine.AI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TPSRoguelite.InGame.Enemy;
using Core.MasterData;


namespace TPSRoguelite.InGame.Spawner
{
    public class EnemySpawner : MonoBehaviour
    {

        /// <summary>
        /// 出現時間
        /// </summary>
        private const float SPAWN_INTERVAL = 3.0f;//3

        /// <summary>
        /// 出現範囲
        /// </summary>
        private const float MAX_SPAWN_DISTANCE = 2.0f;//2

        /// <summary>
        /// 最初に用意する敵のがず
        /// </summary>
        private const int POOL_SIZE = 20;

        /// <summary>
        /// 敵のプレハブ
        /// </summary>
        [SerializeField] GameObject enemyPrefab = null;

        /// <summary>
        /// 出現ポイント
        /// </summary>
        [SerializeField] private Transform[] spawnPoints;

        /// <summary>
        /// 敵を待機させておくプール
        /// </summary>
        private Queue<EnemyState> enemyPool = new Queue<EnemyState>();


        

        public void Setup()
        {
            if (enemyPrefab == null)
            {
                return;
            }

            //ゲーム開始時に、あらかじめ用意した数だけ生成しておく
            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject enemyObj = Instantiate(enemyPrefab);
                EnemyState enemy = enemyObj.GetComponent<EnemyState>();
                if (enemy != null)
                {
                    ulong randomId = (ulong)UnityEngine.Random.Range(1, MasterDataAccessor.Instance.Count<EnemyDataRecord>());
                    enemy.Initialize(randomId);
                    enemy.gameObject.SetActive(false);
                    enemyPool.Enqueue(enemy);
                }
            }
            SpawnLoopAsync().Forget();
        }

        /// <summary>
        /// UniTaskを用いた非同期の生成ループ
        /// </summary>
        /// <returns></returns>
        private async UniTaskVoid SpawnLoopAsync()
        {
            //発生装置が壊れた時にタイマーを安全に止めるためのトークン取
            var token = this.GetCancellationTokenOnDestroy();

            //無限ループ
            while (true)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(SPAWN_INTERVAL), cancellationToken : token);
                SpawnEnemyFromPool();
            }
        }

        /// <summary>
        /// 敵の生成
        /// </summary>
        private void SpawnEnemyFromPool()
        {
            if(enemyPrefab == null || spawnPoints.Length == 0)
            {
                return;
            }

            //ランダムな出現場所を決める
            int randomIndex = 
                UnityEngine.Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];


            Vector3 safePosition = spawnPoint.position;

            //選んだポイントにNavMeshがあるか
            if (NavMesh.SamplePosition(spawnPoint.position, 
                out NavMeshHit hit, MAX_SPAWN_DISTANCE, NavMesh.AllAreas))
            {
                //見つかったら,安全な座標で上書きする
                safePosition = hit.position;
            }
            else
            {
                //見つからなかったら生成をあきらめる
                Debug.Log("近くに安全なスポーンポイントが見つかりませんでした。");
                return;
            }

            EnemyState enemy = null;

            if(enemyPool.Count > 0)
            {
                enemy = enemyPool.Dequeue();
            }
            else
            {
                Debug.Log("プールに空きがなかったため、Instantiateで生成します。プールのサイズを増やすか、制限をかけてください");
                GameObject enemyObj = Instantiate(enemyPrefab);
                enemy = enemyObj.GetComponent<EnemyState>();
                if(enemy == null)
                {
                    Debug.Log("EnemyStateの取得に失敗しました。");
                    return;
                }
            }

            enemy.OnReturnToPoolAction -= RetunToPool;
            enemy.OnReturnToPoolAction += RetunToPool;

            enemy.transform.position = safePosition;
            enemy.transform.rotation = spawnPoint.rotation;

            enemy.Setup();
        }

        /// <summary>
        /// プールへ戻す
        /// </summary>
        /// <param name="enemy"></param>
        private void RetunToPool(EnemyState enemy)
        {
            enemyPool.Enqueue(enemy);
            enemy.OnReturnToPoolAction -= RetunToPool;
        }
    }
}
