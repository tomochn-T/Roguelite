using Core.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using UnityEditor.Rendering;
using System;
using System.Threading;

namespace TPSRoguelite.InGame.Player
{
    public class playerController : MonoBehaviour
    {
        /// <summary>
        /// 移動速度
        /// </summary>
        private const float MOVE_SPEED = 5.0f;

        /// <summary>
        /// 回転速度
        /// </summary>
        private const float ROTATE_SPEED = 10f;

        /// <summary>
        /// レーザーポインターの描画距離
        /// </summary>
        private const float LASER_MAX_DISTANCE = 50;

        /// <summary>
        /// 相手に与えるダメージ量
        /// </summary>
        private const int ATTACK_DAMAGE = 20;

        /// <summary>
        /// 攻撃距離(射撃範囲)
        /// </summary>
        private const float ATTACK_RANGE = 50f;

        /// <summary>
        /// 最大弾薬
        /// </summary>
        private const int MAX_AMMO = 30;

        /// <summary>
        /// リロード時間
        /// </summary>
        private const float RELOAD_TIME = 1.5f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody rigidbody;

        /// <summary>
        /// 銃口のトランスフォーム
        /// </summary>
        [SerializeField] private Transform weponOrigine;

        /// <summary>
        /// レーザーポインターの描画コンポーネント
        /// </summary>
        [SerializeField] private LineRenderer laserlineRenderer;

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
        /// カメラのトランスフォーム
        /// </summary>
        private Transform mainCameraTransform;

        /// <summary>
        /// リロードしているか
        /// </summary>
        private bool isReloading;

        /// <summary>
        /// 外部(アニメーションとかUI)に現在の速度を教えるために保存するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }

        /// <summary>
        /// 現在の弾数
        /// </summary>
        public int CurrentAmmo { get; private set; }

        private void Awake()
        {
            CurrentAmmo = MAX_AMMO;

            inputActions = new PlayerInputAction();
            inputActions.Player.Fire.performed += OnFire;
            inputActions.Player.Reload.performed += OnReload;

            if(UnityEngine.Camera.main != null)
            {
                mainCameraTransform = UnityEngine.Camera.main.transform;
            }
            else
            {
                Debug.Log("Main Cameraが見つかりません");
            }
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
            DrawLaserPointer();
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
                return;
            }

            //カメラの基準の計算に変更
            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;


            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

            //キャラクターを進行方向へ滑らかに振り向かせる
            Quaternion trageRotation = Quaternion.LookRotation(moveDirection);
            rigidbody.rotation = Quaternion.Slerp(rigidbody.rotation, trageRotation, ROTATE_SPEED * Time.fixedDeltaTime);

            Vector3 targetVelocity = moveDirection * MOVE_SPEED;
            rigidbody.linearVelocity = new Vector3(targetVelocity.x, rigidbody.linearVelocity.y, targetVelocity.z);

            //外部(アニメーションとかUI)などに現在の速度を教えるためのプロパティを更新）
            CurrentVelocity = rigidbody.linearVelocity;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);

            //光線に何かが当たったか判定
            if (Physics.Raycast(ray,out RaycastHit hitInfo,ATTACK_RANGE))
            {
                Debug.Log($"{hitInfo.collider.name}に命中!");

                //当たった相手がIDamageableを持っているか確認
                IDamageable traget = hitInfo.collider.GetComponent<IDamageable>();

                //ダメージを受ける性質の持ったオブジェクトであればダメージを与える
                if (traget != null)
                {
                    traget.TakeDamage(ATTACK_DAMAGE);
                }
            }
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if(isReloading || CurrentAmmo == MAX_AMMO)
            {
                return;
            }

            ReloadAsync().Forget();
        }

        private async UniTask ReloadAsync()
        {
            isReloading = true;
            Debug.Log("リロード中");

            await UniTask.Delay(TimeSpan.FromSeconds(RELOAD_TIME), cancellationToken: this.GetCancellationTokenOnDestroy());

            CurrentAmmo = MAX_AMMO;
            isReloading = false;
            Debug.Log("リロード完了");
        }

        /// <summary>
        /// レーザーポインターの描画
        /// </summary>
        private void DrawLaserPointer()
        {
            if(laserlineRenderer == null  || weponOrigine == null || mainCameraTransform ==null)
            {
                return;
            }

            laserlineRenderer.SetPosition(0,weponOrigine.position);

            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, LASER_MAX_DISTANCE))
            {
                laserlineRenderer.SetPosition(1, hitInfo.point);
            }
            else
            {
                laserlineRenderer.SetPosition(1, ray.GetPoint(LASER_MAX_DISTANCE));
            }


        }
    }
}
