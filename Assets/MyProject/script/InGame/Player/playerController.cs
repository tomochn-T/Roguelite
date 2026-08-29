using Core.Interface;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Core.MasterData;
using TPSRoguelite.InGame.Enum;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using TPSRoguelite.InGame.Manager;

namespace TPSRoguelite.InGame.Player
{
    public class playerController : MonoBehaviour, IDamageable
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
        /// レベルアップエフェクトの描画時間
        /// </summary>
        private const float LEVEL_UP_EFFECT_DURATION = 2f;

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
        /// 武器の名前
        /// </summary>
        [SerializeField] private TextMeshProUGUI weaponName;

        /// <summary>
        /// 弾のテキスト
        /// </summary>
        [SerializeField] private TextMeshProUGUI ammoText;

        /// <summary>
        /// リロード中のテキストと画像をまとめたオブジェクト
        /// </summary>
        [SerializeField] private GameObject reloadUI;

        /// <summary>
        /// リロード中の時間が分かるサークル画像
        /// </summary>
        [SerializeField] private Image reloadCircleImage;

        [SerializeField] private Slider expBar;
        [SerializeField] private TextMeshProUGUI levelUpText;
        [SerializeField] private ParticleSystem levelUpEffect;
        [SerializeField] private Slider hpBer;
        
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
        /// ---　スキルによるバフ ---
        /// </summary>
        private float moveSpeedBuff = 0f;
        private float attackPowerBuff = 0f;
        private float fireRateBuff = 0f;
        private float reloadSpeedBuff = 0f;
        private int maxAmmoBuff = 0;

        /// <summary>
        /// 外部(アニメーションとかUI)に現在の速度を教えるために保存するVelocity
        /// </summary>
        public Vector3 CurrentVelocity { get; private set; }

        /// <summary>
        /// 現在の弾数
        /// </summary>
        public double CurrentAmmo { get; private set; }

        public int CurrentExp { get; private set; }

        public int CurrentLevel {  get; private set; }

        public int MaxHp { get; private set; } = 100;
        public int CurrentHp {  get; private set; }

        private int RequiredExp => CurrentLevel * 5;

        private int FinalAttackPwer => currentWeapon != null
            ? Mathf.RoundToInt(currentWeapon.AttackPower * (1f + attackPowerBuff))
            : 0;

        private int FinalMaxAmmo => currentWeapon != null
            ? currentWeapon.MaxAmmo + maxAmmoBuff
            : 0;

        private float FinalReloadTime => currentWeapon != null
            ? currentWeapon.ReloadTime * Mathf.Max(0.1f,1f - reloadSpeedBuff)
            : 0;

        private float FinalFireRate => currentWeapon != null
            ? currentWeapon.FireRate * Mathf.Max(0.1f, 1f - fireRateBuff)
            : 0;

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
                UpdateWeaponUI();
            }
            else
            {
                Debug.Log("tWeaponDataがありません");
            }

            moveSpeedBuff = 0f;
            attackPowerBuff = 0f;
            fireRateBuff = 0f;
            reloadSpeedBuff = 0f;
            maxAmmoBuff = 0;
            
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

            if(reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

            CurrentExp = 0;
            CurrentLevel = 1;

            if(levelUpText != null)
            {
                levelUpText.enabled = false;
            }

            CurrentHp = MaxHp;
            UpdateHpBar();
            UpdateExpUI();

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

            float finalMoveSpeed = MOVE_SPEED * (1f + moveSpeedBuff);
            Vector3 targetVelocity = moveDirection * finalMoveSpeed;

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
                Reload();
                return;
            }

            canShoot = false;

            CurrentAmmo--;
            UpdateCurrentAmmoUI();

            Debug.Log($"セミオートで撃った！弾数:{CurrentAmmo}");
            Shoot();
            await UniTask.Delay(System.TimeSpan.FromSeconds(FinalFireRate), cancellationToken: token);

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
                UpdateCurrentAmmoUI();

                Shoot();
                Debug.Log($"バースト！残弾数:{CurrentAmmo}");

                await UniTask.Delay(TimeSpan.FromSeconds(currentWeapon.FireInteval), cancellationToken:token);
            }

            await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken: token);
            canShoot = true;
        }

        private async UniTaskVoid ShootFullAutoAsync(CancellationToken token)
        {
            canShoot = false;

            while (!token.IsCancellationRequested)
            {
                if (CurrentAmmo <= 0)
                {
                    Reload();
                    break;

                }
                CurrentAmmo--;
                UpdateCurrentAmmoUI();

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

            await UniTask.Delay(TimeSpan.FromSeconds(FinalFireRate), cancellationToken:this.GetCancellationTokenOnDestroy());
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
                    traget.TakeDamage(FinalAttackPwer);//
                }
            }
        }

        private void OnReload(InputAction.CallbackContext context)
        {
            if(isReloading || CurrentAmmo == FinalMaxAmmo)
            {
                return;
            }

            Reload();
        }

        private void Reload()
        {
            isReloading = true;

            if (reloadUI != null)
            {
                reloadUI.SetActive(true);
            }

            if(reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = 0;
            }

            float finalReloadTime = currentWeapon != null
               ? currentWeapon.ReloadTime * Mathf.Max(0.1f,1f - reloadSpeedBuff)
               : 0;

            DOVirtual.Float(0f, 1f, finalReloadTime, UpdateReloadUI).SetEase(Ease.Linear).OnComplete(FnishReload);
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

        private void UpdateWeaponUI()
        {
            if(weaponName != null)
            {
                weaponName.SetText(currentWeapon.WeaporName);

                switch ((FireType)currentWeapon.WeaponFiretype)
                {
                    case FireType.SemiAuto:
                        weaponName.color = Color.wheat;
                        break;

                    case FireType.Burst:
                        weaponName.color = Color.yellow;
                        break;

                    case FireType.FillAuto:
                        weaponName.color = Color.red;
                        break;

                }
            }
            UpdateCurrentAmmoUI();
        }

        private void UpdateCurrentAmmoUI()
        {
            if(ammoText != null)
            {
                ammoText.SetText($"{CurrentAmmo} / {FinalMaxAmmo}");
            }
        }

        private void UpdateReloadUI(float value)
        {
            if(reloadCircleImage != null)
            {
                reloadCircleImage.fillAmount = value;
            }
        }

        private void FnishReload()
        {
            if(reloadUI != null)
            {
                reloadUI.SetActive(false);
            }

            CurrentAmmo = FinalMaxAmmo;
            UpdateCurrentAmmoUI();
            isReloading = false;
        }

        public void AddExp(int amount)
        {
            CurrentExp += amount;

            if(CurrentExp >= RequiredExp)
            {
                LevelUp();
            }

            UpdateExpUI();
        }

        private void UpdateExpUI()
        {
            if(expBar != null)
            {
                expBar.value = (float)CurrentExp / (RequiredExp);
            }
        } 

        private void LevelUp()
        {
            CurrentExp -= RequiredExp;
            CurrentLevel++;

            if(levelUpEffect != null)
            {
                levelUpEffect.Play();
            }

            ShowLevelUpTextAsync().Forget();
        }

        private async UniTaskVoid ShowLevelUpTextAsync()
        {
            if(levelUpText == null)
            {
                return;
            }

            levelUpText.enabled = true;
            levelUpText.SetText($"Level Up!\n<size=50%>Lv.{CurrentLevel}</size>");

            await UniTask.Delay(TimeSpan.FromSeconds(LEVEL_UP_EFFECT_DURATION),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            levelUpText.enabled = false;
            LevelUpManager.Instance.OnLevelUp(inputActions, this);
        }

        public void ApplySkill(SkillDataRecord skill)
        {
            switch ((SkillType)skill.SkillType)
            {
                case SkillType.MoveSpeedUp:
                    moveSpeedBuff += skill.Value;
                    break;

                case SkillType.AttackPowerUp:
                    attackPowerBuff += skill.Value;
                    break;
                
                case SkillType.FireRateUp:
                    fireRateBuff += skill.Value;
                    break;

                case SkillType.ReloadSpeedUp:
                    reloadSpeedBuff += skill.Value;
                    break;

                case SkillType.MaxAmmoUp:
                    maxAmmoBuff += (int)skill.Value;
                    UpdateCurrentAmmoUI();
                    break;
            }
        }

        private void UpdateHpBar()
        {
            if(hpBer != null)
            {
                hpBer.value = (float)CurrentHp / MaxHp;
            }
        }

        private void Die()
        {
            gameObject.SetActive(false);

            if(GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }

        public void TakeDamage(int damageAmount)
        {
            if(damageAmount <= 0 || CurrentHp <= 0)
            {
                return;
            }

            CurrentHp -= damageAmount;
            UpdateHpBar();

            if(CurrentHp <= 0)
            {
               Die();
            }
        }
    }
}
