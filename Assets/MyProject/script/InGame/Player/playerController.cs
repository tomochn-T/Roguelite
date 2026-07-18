using Core.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Core.MasterData;
using TPSRoguelite.InGame.Enum;

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
        /// 攻撃距離(射撃範囲)
        /// </summary>
        private const float ATTACK_RANGE = 50f;

        /// <summary>
        /// 物理演算コンポーネント
        /// </summary>
        [SerializeField] private Rigidbody Rigidbody;

        /// <summary>
        /// 銃口のトランスフォーム
        /// </summary>
        [SerializeField] private Transform weponOrigine;

        /// <summary>
        /// レーザーポインターの描画コンポーネント
        /// </summary>
        [SerializeField] private LineRenderer laserlineRenderer;

        /// <summary>
        /// 武器のID(デフォルトは1)
        /// </summary>
        [SerializeField] private ulong weaponId = 1;

        /// <summary>
        /// マズルフラッシュのエフェクト
        /// </summary>
        [SerializeField] private ParticleSystem muzzleFlah;

        /// <summary>
        /// 武器のデーター
        /// </summary>
        private WeaponDataRecord currentWeapon;

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
        /// 射撃可能か
        /// </summary>
        private bool canShoot = true;
        
        /// <summary>
        /// 射撃のキャンセルトークン
        /// </summary>
        private CancellationTokenSource fireCts;

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
            gameObject.SetActive(false);
        }

        public void Setup()
        {

            currentWeapon = MasterDataAccessor.Instance.GetById<WeaponDataRecord>(weaponId);

            if (currentWeapon != null)
            {
                CurrentAmmo = currentWeapon.MaxAmmo;
            }
            else
            {
                Debug.Log("tWeaponDataがありません");
            }

            inputActions = new PlayerInputAction();
            inputActions.Player.Fire.performed += OnFire;//押し続けると呼ばれる
            inputActions.Player.Fire.canceled += OnFire;
            inputActions.Player.Reload.performed += OnReload;

            if (UnityEngine.Camera.main != null)
            {
                mainCameraTransform = UnityEngine.Camera.main.transform;
            }
            else
            {
                Debug.Log("Main Cameraが見つかりません");
            }
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            inputActions?.Enable();
        }

        private void OnDisable()
        {
            inputActions?.Disable();
        }

        private void Update()
        {
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            DrawLaserPointer();
        }

        private void FixedUpdate()
        {
            Move();
        }

        /// <summary>
        /// 移動処理
        /// </summary>
        private void Move()
        {
            if(Rigidbody == null || mainCameraTransform == null)
            {
                Debug.Log("リジットボディまたはmainCameraTransformがNULLです。");
                return;
            }

            Vector3 cameraForward = mainCameraTransform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            if (cameraForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                Rigidbody.rotation = Quaternion.Slerp(Rigidbody.rotation, targetRotation, ROTATE_SPEED * Time.deltaTime);
            }

            //入力がない場合は、入力を止めておく
            if (moveInput == Vector2.zero)
            {
                Rigidbody.linearVelocity = new Vector3(0, Rigidbody.linearVelocity.y, 0);
                CurrentVelocity = Vector3.zero;
                return;
            }

            //カメラの基準の計算に変更
            Vector3 cameraRight = mainCameraTransform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

            Vector3 targetVelocity = moveDirection * MOVE_SPEED;

            Rigidbody.linearVelocity = 
                new Vector3(targetVelocity.x, Rigidbody.linearVelocity.y, targetVelocity.z);

            //外部(アニメーションとかUI)などに現在の速度を教えるためのプロパティを更新）
            CurrentVelocity = Rigidbody.linearVelocity;
        }

        private void OnFire(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                if(!canShoot || isReloading || currentWeapon == null)
                {
                    return;
                }

                fireCts = new CancellationTokenSource();
                CancellationTokenSource linkedCts =
                    CancellationTokenSource.CreateLinkedTokenSource(fireCts.Token, this.GetCancellationTokenOnDestroy());

                switch ((FireType)currentWeapon.WeaponFiretype)   
                {
                    case Enum.FireType.SemiAuto:
                        ShootSemiAutoAsnc(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.Burst:
                        ShootBurstAsync(this.GetCancellationTokenOnDestroy()).Forget();
                        break;

                    case Enum.FireType.FillAuto:
                        ShootFullAutoAsync(linkedCts.Token).Forget();
                        break;

                    default:
                        Debug.Log($"割り当てていない射撃タイプがあります。{currentWeapon.WeaponFiretype}");
                        break;
                }
            }

            if (context.canceled)
            {
                fireCts?.Cancel();
                fireCts?.Dispose();
                fireCts = null;
            }
        }

        /// <summary>
        /// セミオートの射撃処理
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async UniTaskVoid ShootSemiAutoAsnc(CancellationToken token)
        {
            if(CurrentAmmo == 0)
            {
                ReloadAsync().Forget();
                return;
            }

            canShoot = false;

            CurrentAmmo--;
            Debug.Log($"セミオートで撃った！弾数:{CurrentAmmo}");
            Shoot();
            await UniTask.Delay(System.TimeSpan.FromSeconds(currentWeapon.FireRate), cancellationToken: token);

            canShoot=true;
        }

        private async UniTaskVoid ShootBurstAsync(CancellationToken token)
        {
            canShoot = false;

            for (int i = 0; i < 3; i++)
            {
                if(CurrentAmmo <= 0)
                {
                    canShoot = true;
                    return;
                }

                CurrentAmmo --;
                Shoot();
                Debug.Log($"バースト！残弾数:{CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval), cancellationToken:token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval), cancellationToken: token);
            canShoot = true;
        }

        private async UniTaskVoid ShootFullAutoAsync(CancellationToken token)
        {
            canShoot = false;

            while (!token.IsCancellationRequested)
            {
                if (CurrentAmmo <= 0)
                {
                    ReloadAsync().Forget();
                    break;

                }
                CurrentAmmo--;
                Debug.Log($"フルオート！残弾数:{CurrentAmmo}");
                Shoot();

                bool isCanceled =
                    await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval),
                        cancellationToken: token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    break;
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval), cancellationToken:this.GetCancellationTokenOnDestroy());
            canShoot = true;
        }

        /// <summary>
        /// 共通の射撃処理
        /// </summary>
        private void Shoot()
        {
            if(muzzleFlah != null)
            {
                muzzleFlah.Play();
            }


            Ray ray = new Ray(mainCameraTransform.position, mainCameraTransform.forward);

            //光線に何かが当たったか判定
            if (Physics.Raycast(ray, out RaycastHit hitInfo, ATTACK_RANGE))
            {
                Debug.Log($"{hitInfo.collider.name}に命中!");

                //当たった相手がIDamageableを持っているか確認
                IDamageable traget = hitInfo.collider.GetComponent<IDamageable>();

                //ダメージを受ける性質の持ったオブジェクトであればダメージを与える
                if (traget != null)
                {
                    traget.TakeDamage(currentWeapon.AttackPower);
                }
            }
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if(isReloading || CurrentAmmo == currentWeapon.MaxAmmo)
            {
                return;
            }

            ReloadAsync().Forget();
        }

        private async UniTask ReloadAsync()
        {
            isReloading = true;
            Debug.Log("リロード中");

            await UniTask.Delay
                (TimeSpan.FromSeconds(currentWeapon.ReloadTime), cancellationToken: this.GetCancellationTokenOnDestroy());

            CurrentAmmo = currentWeapon.MaxAmmo;
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
