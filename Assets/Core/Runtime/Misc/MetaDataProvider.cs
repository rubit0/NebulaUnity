using System.Collections.Generic;
using UnityEngine;

namespace Nebula.Runtime.Misc
{
    [CreateAssetMenu(menuName = "Nebula/Create Meta Data Provider")]
    public abstract class MetaDataProvider : ScriptableObject
    {
        public abstract Dictionary<string, string> GetMeta();
    }
}