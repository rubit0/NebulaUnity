using System;
using Nebula.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Nebula.Sample.Demo
{
    public class ListItemAssetBundle : MonoBehaviour, IPointerClickHandler
    {
        public enum BundleItemState
        {
            Ready,
            Stale,
            Remote
        }

        public event EventHandler<ListItemAssetBundle> OnActionClicked; 
        public AssetBundleInfo BundleInfo { get; private set; }
        public BundleItemState CurrentBundleState { get; set; }
        
        [SerializeField]
        private TMP_Text textBundleName;
        [SerializeField]
        private TMP_Text labelStatus;
        [SerializeField]
        private GameObject selectedIndicator;

        public void Init(AssetBundleInfo assetBundleInfo)
        {
            BundleInfo = assetBundleInfo;
            textBundleName.text = BundleInfo.BundleName;
        }

        public void SetState(BundleItemState state)
        {
            CurrentBundleState = state;
            switch (CurrentBundleState)
            {
                case BundleItemState.Ready:
                    labelStatus.text = "Ready";
                    break;
                case BundleItemState.Stale:
                    labelStatus.text = "Stale";
                    break;
                case BundleItemState.Remote:
                    labelStatus.text = "Downloadable";
                    break;
            }
        }

        public void SetSelected(bool state)
        {
            selectedIndicator.SetActive(state);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            OnActionClicked?.Invoke(this, this);
        }
    }
}
