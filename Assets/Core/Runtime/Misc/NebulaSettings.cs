using UnityEngine;

namespace Nebula.Runtime.Misc
{
    [CreateAssetMenu(menuName = "Nebula/Create Settings")]
    public class NebulaSettings : ScriptableObject
    {
        [field: SerializeField]
        public string Endpoint { get; set; }
    }
}
