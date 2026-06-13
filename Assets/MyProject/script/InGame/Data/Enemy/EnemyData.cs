using UnityEngine;


[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    /// <summary>
    /// “G‚Ì–¼‘O
    /// </summary>
    [field:SerializeField] public string EnemyName {  get; private set; }

    /// <summary>
    /// ‘Ì—Í
    /// </summary>
    [field:SerializeField] public int MaxHP { get; private set; }

    /// <summary>
    /// ˆÚ“®‘¬“x
    /// </summary>
    [field:SerializeField] public float MoveSpeed { get; private set; }

}
