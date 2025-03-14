using System.Collections.Generic;
using System.Linq;
using Nebula.Runtime;
using Nebula.Runtime.Misc;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Nebula.Sample.Demo
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
        private Button buttonClear;
        [SerializeField]
        private Button buttonFetch;
        [SerializeField]
        private Button buttonAction;
        [SerializeField]
        private TMP_Text labelButtonAction;

        private ListItemAssetBundle _lastSelectedButton;
        private AssetBundleManager _assetBundleManager;
        private List<ListItemAssetBundle> _listItemInstances = new ();

        private void Awake()
        {
            buttonClear.onClick.AddListener(HandleOnButtonClearClick);
            buttonFetch.onClick.AddListener(HandleOnButtonFetchClick);
            buttonAction.onClick.AddListener(HandleOnActionButtonClick);
        }

        private async void Start()
        {
            SetBusyState(true);
            _assetBundleManager = new AssetBundleManager(settings);
            await _assetBundleManager.Init();
            SetBusyState(false);
            buttonAction.interactable = false;
            labelButtonAction.text = "-";
            
            await _assetBundleManager.Fetch();
            foreach (var assetBundleInfo in _assetBundleManager.LocalAssetBundles)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                // Skip if it should be updated
                if (_assetBundleManager.UpdateableAssets.Any(a => a.Id == assetBundleInfo.Id))
                {
                    item.Init(_assetBundleManager.UpdateableAssets.Single(a => a.Id == assetBundleInfo.Id));
                    item.SetState(ListItemAssetBundle.BundleItemState.Stale);
                }
                else
                {
                    item.SetState(ListItemAssetBundle.BundleItemState.Ready);
                }
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            // foreach (var assetBundleInfo in _assetBundleManager.UpdateableAssets)
            // {
            //     var item = Instantiate(listItemBundlePrefab, rootBundlesList);
            //     item.Init(assetBundleInfo);
            //     item.SetState(ListItemAssetBundle.BundleItemState.Stale);
            //     item.OnActionClicked += HandleOnItemClicked;
            //     _listItemInstances.Add(item);
            // }
            foreach (var assetBundleInfo in _assetBundleManager.RemoteAvailableAssets)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Remote);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
        }
        
        private async void HandleOnButtonFetchClick()
        {
            SetBusyState(true);
            foreach (var listItemAssetBundle in _listItemInstances)
            {
                listItemAssetBundle.OnActionClicked -= HandleOnItemClicked;
                Destroy(listItemAssetBundle.gameObject);
            }
            _listItemInstances.Clear();
            _lastSelectedButton = null;
            
            await _assetBundleManager.Fetch();
            foreach (var assetBundleInfo in _assetBundleManager.LocalAssetBundles)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Ready);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in _assetBundleManager.UpdateableAssets)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Stale);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in _assetBundleManager.RemoteAvailableAssets)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Remote);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            SetBusyState(false);
            buttonAction.interactable = false;
        }
        
        private async void HandleOnActionButtonClick()
        {
            switch (_lastSelectedButton.CurrentBundleState)
            {
                case ListItemAssetBundle.BundleItemState.Ready:
                    await _assetBundleManager.LoadAndInstantiateAll(_lastSelectedButton.LocalAsset);
                    break;
                case ListItemAssetBundle.BundleItemState.Stale:
                    SetBusyState(true);
                    await _assetBundleManager.DownloadAsset(_lastSelectedButton.AssetContainerDto);
                    _lastSelectedButton.SetState(ListItemAssetBundle.BundleItemState.Ready);
                    HandleOnItemClicked(this, _lastSelectedButton);
                    SetBusyState(false);
                    break;
                case ListItemAssetBundle.BundleItemState.Remote:
                    SetBusyState(true);
                    await _assetBundleManager.DownloadAsset(_lastSelectedButton.AssetContainerDto);
                    _lastSelectedButton.SetState(ListItemAssetBundle.BundleItemState.Ready);
                    _lastSelectedButton.Init(_assetBundleManager.
                        LocalAssetBundles.Single(a => a.Id == _lastSelectedButton.AssetContainerDto.Id));
                    HandleOnItemClicked(this, _lastSelectedButton);
                    SetBusyState(false);
                    break;
            }
        }
        
        private async void HandleOnButtonClearClick()
        {
            AssetBundle.UnloadAllAssetBundles(true);
            await AssetManagementUtils.ClearAllAssets();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }

        private void HandleOnItemClicked(object sender, ListItemAssetBundle item)
        {
            if (_lastSelectedButton != null)
            {
                _lastSelectedButton.SetSelected(false);
            }
            
            _lastSelectedButton = item;
            _lastSelectedButton.SetSelected(true);
            buttonAction.interactable = true;
            switch (item.CurrentBundleState)
            {
                case ListItemAssetBundle.BundleItemState.Ready:
                    labelButtonAction.text = "Instantiate";
                    break;
                case ListItemAssetBundle.BundleItemState.Stale:
                    labelButtonAction.text = "Update";
                    break;
                case ListItemAssetBundle.BundleItemState.Remote:
                    labelButtonAction.text = "Download";
                    break;
            }
        }

        private void SetBusyState(bool isBusy)
        {
            panelBusy.SetActive(isBusy);
            panelListView.SetActive(!isBusy);
            buttonAction.interactable = !isBusy;
            buttonFetch.interactable = !isBusy;
        }
    }
}
