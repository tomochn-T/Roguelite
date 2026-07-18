using System.Collections.Generic;
using UnityEngine;

namespace Core.MasterData
{
    public interface IMasterDataContainer<T> where T : IMasterData
    {
        List<T> Records { get; }
    }
}
