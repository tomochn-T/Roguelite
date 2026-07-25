using UnityEngine;
using TPSRoguelite.InGame.Player;

namespace TPSRoguelite.InGame.Item
{
    public class ExperienceOrb : MonoBehaviour
    {
        private const float MAGNET_RANGE = 5.0f;
        private const float MAGNET_SPEED = 15f;
        private const string PLAYER_TAG = "Player";

        private Transform playerTarget;
        private bool isFollowing = false;

        private void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if(playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
            else
            {
                Debug.Log("playerが見つかりませんでした。");
            }
        }

        private void Update()
        {
            if(playerTarget == null)
            {
                return;
            }

            if (isFollowing)
            {
                transform.position = Vector3.MoveTowards(transform.position, playerTarget.position, MAGNET_SPEED * Time.deltaTime);
            }
            else
            {
                float distToPlayer = Vector3.Distance(transform.position, playerTarget.position);
                if(distToPlayer <= MAGNET_RANGE)
                {
                    isFollowing = true;
                }
            }
        }

        /// <summary>
        /// Playerに触れた時の処理(コライダーの[IsTrriger]がONになっていると動かない)
        /// </summary>
        /// <param name="other"></param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(PLAYER_TAG))
            {
                playerController player = other.GetComponent<playerController>();
                if(player != null)
                {
                    player.AddExp(1);
                }
                else
                {
                    Debug.Log("playerControllerが見つかりませんでした。");
                }

                Destroy(gameObject);
            }
        }
    }
}

