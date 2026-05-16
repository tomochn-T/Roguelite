using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSRoguelite.InGame.Player
{
    public class playerController : MonoBehaviour
    {
        /// <summary>
        /// 移動速度
        /// </summary>
        private const float MOVE_SPEED = 5.0f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody rigidbody;

        /// <summary>
        /// 自動生成されたInputクラス
        /// </summary>
        [SerializeField] private PlayerInputAction inputActions;

        /// <summary>
        /// 入力方向
        /// </summary>
        private Vector2 moveInput = Vector2.zero;

        /// <summary>
        /// 移動方向のベクトル
        /// </summary>
        private Vector3 moveDirection = Vector3.zero;

        /// <summary>
        /// 外部(アニメーションとかUI)に現在の速度を教えるために保存するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }


        private void Awake()
        {
            inputActions = new PlayerInputAction();
            inputActions.Player.Fire.performed += OnFire;
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        private void Update()
        {
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        }

        private void FixedUpdate()
        {
            move();
        }

        /// <summary>
        /// 移動処理
        /// </summary>
        private void move()
        {
            if (rigidbody == null)
            {
                Debug.Log("リジットボディがアタッチされていません。");
                return;
            }

            //入力がない場合は、入力を止めておく
            if (moveInput == Vector2.zero)
            {
                rigidbody.linearVelocity = new Vector3(0, rigidbody.linearVelocity.y, 0);
                CurrentVelocity = Vector3.zero;
            }

            //実際の移動速度の計算
            Vector3 targetVelocity = new Vector3(moveInput.x, rigidbody.linearVelocity.y, moveInput.y);
            targetVelocity.Normalize();


            rigidbody.linearVelocity = targetVelocity * MOVE_SPEED;

            //外部(アニメーションとかUI)などに現在の速度を教えるためのプロパティを更新）
            CurrentVelocity = rigidbody.linearVelocity;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            Debug.Log("Fire");

        }
    }
}
