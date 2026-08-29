using UnityEngine;
using UnityEngine.InputSystem;

namespace TPSRoguelite.InGame.Camera
{
    public class CameraController : MonoBehaviour
    {
        /// <summary>
        /// 追尾するプレイヤー
        /// </summary>
        [SerializeField] private Transform target;

        [Header("カメラの基本設定")]

        /// <summary>
        /// カメラの感度
        /// </summary>
        [SerializeField] private float lookSensitivity = 0.2f;

        /// <summary>
        /// 縦の最小角度
        /// </summary>
        [SerializeField] private float minPitch = -10f;

        /// <summary>
        /// 縦の最大角度
        /// </summary>
        [SerializeField] private float maxPitch = 60f;

        /// <summary>
        /// ズーム速度
        /// </summary>
        [SerializeField] private float zoomSpeed = 5.0f;

        [Header("カメラの視点")]

        /// <summary>
        /// 後ろに下がる距離
        /// </summary>
        [SerializeField] private float targetDistance = -5.0f;

        /// <summary>
        /// 高さ
        /// </summary>
        [SerializeField] private float targetHeightOffset = 1.2f;

        /// <summary>
        /// 右にずらす距離(右肩くらい)
        /// </summary>
        [SerializeField] private float targetShoulderOffset = 0.8f;

        /// <summary>
        /// 自動生成されたクラス
        /// </summary>
        private PlayerInputAction inputActions;

        /// <summary>
        /// マウスの移動量
        /// </summary>
        private Vector2 lookInput = Vector2.zero;

        /// <summary>
        /// 横の回転角度(Y軸回転)
        /// </summary>
        private float currentYaw = 0f;

        /// <summary>
        /// 縦の回転角度(X軸回転)
        /// </summary>
        private float currentPitch = 20f;

        private float currentDistance = 0f;
        private float currenttHeightOffset = 0f;
        private float currentShoulderOffset = 0f;

        private void Awake()
        {
            inputActions = new PlayerInputAction();
            
            //マウスカーソルを画面中央にロックして非表示にする
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }

            //マウスの移動量を取得
            lookInput = inputActions.Player.Look.ReadValue<Vector2>();

            //感度woooooを掛けて現在の角度に足し引きする
            currentYaw += lookInput.x * lookSensitivity;
            currentPitch -= lookInput.y * lookSensitivity;

            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        }

        private void LateUpdate()
        {
            //カメラの移動は、プレイヤーの移動が終わった後に行う

            //ターゲットが設定されていない場合はエラー回避
            if(target == null)
            {
                return;
            }

            //現在の数値を目標の数値に向かったら滑らかに変動させる(変化させる機能が[Mathf.Lerp])
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);
            currenttHeightOffset = Mathf.Lerp(currenttHeightOffset, targetHeightOffset, zoomSpeed * Time.deltaTime);
            currentShoulderOffset = Mathf.Lerp(currentShoulderOffset, targetShoulderOffset, zoomSpeed * Time.deltaTime);

            //角度をQuaternionに変換
            Quaternion rotate = Quaternion.Euler(currentPitch,currentYaw,0f);

            //カメラの回転計算
            Vector3 basePosition = target.position + Vector3.up * currenttHeightOffset;

            //肩越しの視点にするために右側に寄せる
            Vector3 sholderPosition = basePosition + (rotate * Vector3.right * currentShoulderOffset);

            //カメラに取って後ろ方向へ距離をずらす
            Vector3 cameraPosition = sholderPosition + (rotate * Vector3.forward * currentDistance);

            //カメラの位置と回転
            transform.position = cameraPosition;
            transform.rotation = rotate;
        }
    }
}