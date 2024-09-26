using System;
using System.Collections.Generic;
using System.Linq;
using Nebula.Runtime.Misc;
using UnityEngine;

namespace Nebula.Runtime.Misc
{
    [CreateAssetMenu(menuName = "Nebula/Create Key-Value Meta Data Provider")]
    public class KeyValueMetaDataProvider : MetaDataProvider
    {
        [Serializable]
        public class KeyValueEntry
        {
            [field: SerializeField]
            public string Key { get; set; }
            [field: SerializeField]
            public string Vale { get; set; }
        }

        [field: SerializeField]
        public List<KeyValueEntry> MetaData { get; set; }

        public override Dictionary<string, string> GetMeta()
        {
            return MetaData.ToDictionary(d => d.Key, d => d.Vale);
        }
    }
}
