using System;
using Nebula.Runtime;
using Nebula.Runtime.API.Dtos.Responses;
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
        public LocalAsset LocalAsset { get; private set; }
        public AssetContainerDto AssetContainerDto { get; set; }
        public BundleItemState CurrentBundleState { get; set; }
        
        [SerializeField]
        private TMP_Text textBundleName;
        [SerializeField]
        private TMP_Text labelStatus;
        [SerializeField]
        private GameObject selectedIndicator;

        public void Init(LocalAsset assetBundleInfo)
        {
            LocalAsset = assetBundleInfo;
            textBundleName.text = LocalAsset.Name;
        }
        
        public void Init(AssetContainerDto dto)
        {
            AssetContainerDto = dto;
            textBundleName.text = AssetContainerDto.Name;
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
