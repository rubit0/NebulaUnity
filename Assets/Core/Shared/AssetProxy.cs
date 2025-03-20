using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nebula.Shared
{
    [CreateAssetMenu(menuName = "Nebula/Create Asset Proxy")]
    public class AssetProxy : ScriptableObject
    {
        [field: SerializeField]
        public string Id { get; set; } = "";
        [field: SerializeField]
        public string InternalName { get; set; } = "";

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
        [field: SerializeField]
        public List<string> Dependencies { get; set; }
    }
}