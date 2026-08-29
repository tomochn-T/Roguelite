using UnityEngine;
using System;
using System.Collections.Generic;

namespace Core.MasterData
{
    [Serializable]
    public class WeaponDataRecord : IMasterData
    {
        [field: SerializeField] public ulong Id { get; private set; }

        /// <summary>
        /// 武器の名前
        /// </summary>
        [field: SerializeField] public string WeaporName { get; private set; }

        /// <summary>
        /// 射撃タイプ
        /// </summary>
        [field: SerializeField] public int WeaponFiretype { get; private set; }

        /// <summary>
        /// 攻撃力
        /// </summary>
        [field: SerializeField] public int AttackPower { get; private set; }//int

        /// <summary>
        /// 射撃のインターバル時間(バーストやフルオートの連射間隔)
        /// </summary>
        [field: SerializeField] public float FireInteval { get; private set; }

        /// <summary>
        /// 次の弾が撃てるまでの待機時間
        /// </summary>
        [field: SerializeField] public float FireRate { get; private set; }

        /// <summary>
        /// 最大弾数
        /// </summary>
        [field: SerializeField] public int MaxAmmo { get; private set; }//int

        /// <summary>
        /// リロード時間
        /// </summary>
        [field: SerializeField] public float ReloadTime { get; private set; }
    }

    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Scriptable Objects/WeaponData")]
    public class WeaponData : ScriptableObject, IMasterDataContainer<WeaponDataRecord>
    {
        [field: SerializeField] public List<WeaponDataRecord> Records { get; private set; }
    }
}
