using UnityEngine;

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
    /// 移動方向のベクトル
    /// </summary>
    private Vector3 moveDirection = Vector3.zero;


    /// <summary>
    /// 外部(アニメーションとかUI)に現在の速度を教えるために保存するVelocity
    /// </summary>
    public Vector3 CurrentVelocity { get; private set; }


    private void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        //入力から移動方向のベクトルを生成
        moveDirection = new Vector3(x, 0, z).normalized ;

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
        if(rigidbody == null)
        {
            Debug.Log("リジットボディがアタッチされていません。");
            return;
        }

        //入力がない場合は、入力を止めておく
        if(moveDirection == Vector3.zero)
        {
            rigidbody.linearVelocity = new Vector3(0,rigidbody.linearVelocity.y,0);
            CurrentVelocity = Vector3.zero;
        }

        //実際の移動速度の計算
        Vector3 targetVelocity = moveDirection * MOVE_SPEED;

        rigidbody.linearVelocity = new Vector3(
            targetVelocity.x,
            rigidbody.linearVelocity.y,
            targetVelocity.z);

        CurrentVelocity = rigidbody.linearVelocity;
    }
}
