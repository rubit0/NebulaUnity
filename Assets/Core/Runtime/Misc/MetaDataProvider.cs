using System.Collections.Generic;
using UnityEngine;

namespace Nebula.Runtime.Misc
{
    public abstract class MetaDataProvider : ScriptableObject
    {
        public abstract Dictionary<string, string> GetMeta();
    }
}