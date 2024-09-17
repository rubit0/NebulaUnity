using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Runtime.Demo
{
    public class ListItemAssetBundle : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private TMP_Text textBundleName;

        public void Init(string assetBundleName)
        {
            textBundleName.text = assetBundleName;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log(textBundleName.text);
        }
    }
}
