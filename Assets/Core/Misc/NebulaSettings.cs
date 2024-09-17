using UnityEngine;

namespace Core.Misc
{
    [CreateAssetMenu]
    public class NebulaSettings : ScriptableObject
    {
        [field: SerializeField]
        public string Endpoint { get; set; }
        [field: SerializeField]
        public string BucketId { get; set; }
    }
}
