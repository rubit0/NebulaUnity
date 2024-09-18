using System.Collections.Generic;
using Core.Misc;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Runtime.Demo
{
    public class DemoAssetManager : MonoBehaviour
    {
        [SerializeField]
        private NebulaSettings settings;
        [SerializeField]
        private GameObject panelBusy;
        [SerializeField]
        private GameObject panelListView;
        [SerializeField]
        private Transform rootBundlesList;
        [SerializeField]
        private ListItemAssetBundle listItemBundlePrefab;
        [SerializeField]
        private Button buttonFetch;

        private AssetBundleManager _assetBundleManager;
        private List<ListItemAssetBundle> _listItemInstances = new ();

        private void Awake()
        {
            buttonFetch.onClick.AddListener(HandleOnButtonFetchClick);
        }

        private async void Start()
        {
            SetBusyState(true);
            _assetBundleManager = new AssetBundleManager(settings);
            await _assetBundleManager.Init();
            SetBusyState(false);
            
            var report = _assetBundleManager.ReportAssets();
            foreach (var assetBundleInfo in report.UpToDate)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Ready);
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in report.Updated)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Stale);
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in report.Remaining)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Remote);
                _listItemInstances.Add(item);
            }
        }
        
        private async void HandleOnButtonFetchClick()
        {
            SetBusyState(true);
            await _assetBundleManager.Fetch();
            foreach (var listItemAssetBundle in _listItemInstances)
            {
                Destroy(listItemAssetBundle.gameObject);
            }
            _listItemInstances.Clear();
            
            var report = _assetBundleManager.ReportAssets();
            foreach (var assetBundleInfo in report.UpToDate)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Ready);
            }
            foreach (var assetBundleInfo in report.Updated)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Stale);
            }
            foreach (var assetBundleInfo in report.Remaining)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Remote);
            }
            SetBusyState(false);
        }

        private void SetBusyState(bool isBusy)
        {
            panelBusy.SetActive(isBusy);
            panelListView.SetActive(!isBusy);
        }
    }
}
