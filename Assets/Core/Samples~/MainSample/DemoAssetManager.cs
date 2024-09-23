using System.Collections.Generic;
using System.IO;
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
            
            var report = _assetBundleManager.ReportAssets();
            foreach (var assetBundleInfo in report.UpToDate)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Ready);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in report.Updated)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Stale);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in report.Remaining)
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
            await _assetBundleManager.Fetch();
            foreach (var listItemAssetBundle in _listItemInstances)
            {
                listItemAssetBundle.OnActionClicked -= HandleOnItemClicked;
                Destroy(listItemAssetBundle.gameObject);
            }
            _listItemInstances.Clear();
            _lastSelectedButton = null;
            
            var report = _assetBundleManager.ReportAssets();
            foreach (var assetBundleInfo in report.UpToDate)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Ready);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in report.Updated)
            {
                var item = Instantiate(listItemBundlePrefab, rootBundlesList);
                item.Init(assetBundleInfo);
                item.SetState(ListItemAssetBundle.BundleItemState.Stale);
                item.OnActionClicked += HandleOnItemClicked;
                _listItemInstances.Add(item);
            }
            foreach (var assetBundleInfo in report.Remaining)
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
                    _assetBundleManager.LoadAndInstantiateAll(_lastSelectedButton.BundleInfo.BundleName);
                    break;
                case ListItemAssetBundle.BundleItemState.Stale:
                    SetBusyState(true);
                    await _assetBundleManager.DownloadRemoteAssetBundle(_lastSelectedButton.BundleInfo);
                    _lastSelectedButton.SetState(ListItemAssetBundle.BundleItemState.Ready);
                    HandleOnItemClicked(this, _lastSelectedButton);
                    SetBusyState(false);
                    break;
                case ListItemAssetBundle.BundleItemState.Remote:
                    SetBusyState(true);
                    await _assetBundleManager.DownloadRemoteAssetBundle(_lastSelectedButton.BundleInfo);
                    if (_lastSelectedButton.BundleInfo.Dependencies.Count > 0)
                    {
                        HandleOnButtonFetchClick();
                    }
                    else
                    {
                        _lastSelectedButton.SetState(ListItemAssetBundle.BundleItemState.Ready);
                        HandleOnItemClicked(this, _lastSelectedButton);
                        SetBusyState(false);
                    }
                    break;
            }
        }
        
        private void HandleOnButtonClearClick()
        {
            AssetBundle.UnloadAllAssetBundles(true);
            if (!Directory.Exists(AssetManagementUtils.GetAssetBundlePath()))
            {
                return;
            }
            
            // Check if it its not empty
            if (!AssetManagementUtils.IsAssetDataDirectoryEmpty())
            {
                var path = AssetManagementUtils.GetAssetBundlePath();
                // Delete directory recursive
                Directory.Delete(path, true);
                
                Debug.Log("Deleted AssetBundles from " + path);
            }
            
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
