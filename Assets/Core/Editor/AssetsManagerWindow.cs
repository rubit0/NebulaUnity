#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Nebula.Editor.API;
using Nebula.Editor.API.Dtos.Requests;
using Nebula.Shared;
using UnityEditor;
using UnityEngine;

namespace Nebula.Editor
{
    public class AssetsManagerWindow : EditorWindow
    {
        // Data
        private AssetProxy[] _proxies;
        private int _selectedProxyIndex;
        
        // Options
        private bool _buildForWeb = true;
        private bool _buildForIOS = true;
        private bool _buildForVisionOS;
        private string _releaseNotes = "";
        
        // Status window
        private string _buildStatusMessage = "";
        private Vector2 _buildStatusScrollViewPosition;
        private bool _isPerformingBuild;
        
        [MenuItem("Nebula/Nebula Assets Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetsManagerWindow>("Nebula Assets Manager");
            window.LoadData();
            window.Show();
        }
        
        private void LoadData()
        {
            // Find all AssetContainerProxy instances in the project
            var guids = AssetDatabase.FindAssets($"t:{nameof(AssetProxy)}");
            _proxies = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<AssetProxy>(AssetDatabase.GUIDToAssetPath((string)guid)))
                .Where(proxy => !string.IsNullOrEmpty(proxy.Id))
                .ToArray();
        }
        
        private void UpdateStatus(string message)
        {
            _buildStatusMessage += $"> {message}\n";
            Repaint();
        }
        
        private void OnGUI()
        {
            if (_proxies == null || _proxies.Length == 0)
            {
                EditorGUILayout.HelpBox("No AssetContainerProxy assets found.", MessageType.Warning);
                return;
            }

            // Dropdown for proxies
            _selectedProxyIndex = EditorGUILayout.Popup("Select Proxy", _selectedProxyIndex, _proxies.Select(p => p.InternalName).ToArray());
            var proxy = _proxies[_selectedProxyIndex];
            EditorGUILayout.LabelField($"Id: {proxy.Id}", EditorStyles.largeLabel);

            // Selection for build target selection
            EditorGUILayout.LabelField("Build targets:", EditorStyles.boldLabel);
            _buildForWeb = EditorGUILayout.Toggle("Web", _buildForWeb);
            _buildForIOS = EditorGUILayout.Toggle("iOS", _buildForIOS);
            _buildForVisionOS = EditorGUILayout.Toggle("visionOS", _buildForVisionOS);
            
            // Release notes
            EditorGUILayout.LabelField("Release Notes:", EditorStyles.boldLabel);
            _releaseNotes = EditorGUILayout.TextArea(_releaseNotes, GUILayout.Height(60));
            
            // Build action
            EditorGUI.BeginDisabledGroup(_isPerformingBuild || 
                                         !_buildForIOS && !_buildForWeb && !_buildForVisionOS);
            if (GUILayout.Button("Build and release"))
            {
                if (_selectedProxyIndex >= 0 && _selectedProxyIndex < _proxies.Length)
                {
                    StartBuildProcessForProxy(proxy);
                }
            }
            EditorGUI.EndDisabledGroup();
            
            // Status window
            GUILayout.FlexibleSpace(); // Pushes status window to the bottom
            EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);

            _buildStatusScrollViewPosition = EditorGUILayout.BeginScrollView(_buildStatusScrollViewPosition, GUILayout.Height(250));
            GUI.enabled = false; // Makes the text area read-only
            EditorGUILayout.TextArea(_buildStatusMessage, GUILayout.ExpandHeight(true));
            GUI.enabled = true;
            EditorGUILayout.EndScrollView();
        }
        
        private async void StartBuildProcessForProxy(AssetProxy proxy)
        {
            _isPerformingBuild = true;
            UpdateStatus($"Creating new release for '{proxy.InternalName} {proxy.Id}'");
            if (string.IsNullOrWhiteSpace(proxy.Id))
            {
                UpdateStatus($"Error - The asset container proxy '{proxy.InternalName}' has no Id assigned.");
                _isPerformingBuild = false;
                return;
            }
            
            UpdateStatus($"Creating a new release for container '{proxy.InternalName} {proxy.Id}'.");
            // Create release on this container
            var client = new ManagementWebService(GetSettings().Endpoint);
            var assetContainer = await client.GetContainer(proxy.Id);
            if (!assetContainer.IsSuccess)
            {
                UpdateStatus($"Error - Could not find asset container for id {proxy.Id}.\nError: {assetContainer.ErrorMessage}");
                _isPerformingBuild = false;
                return;
            }
            var releaseRequest = await client.AddRelease(proxy.Id, new UploadReleaseDto { Notes = _releaseNotes });
            var targetReleaseId = releaseRequest.Content.Id;
            UpdateStatus($"Created new release with Id {targetReleaseId}");
            
            // Assign bundle name to assets
            var proxyPath = AssetDatabase.GetAssetPath(proxy);
            var folderPath = Path.GetDirectoryName(proxyPath);
                
            var folderImporter = AssetImporter.GetAtPath(folderPath);
            folderImporter.assetBundleName = proxy.Id;
            folderImporter.SaveAndReimport();
            AssetDatabase.Refresh();
            
            // Perform build
            if (_buildForWeb)
            {
                var zipPath = CreateBuild(proxy, BuildTarget.WebGL);
                await UploadBuild(client, proxy, targetReleaseId, zipPath, BuildTarget.WebGL);
            }
            if (_buildForIOS)
            {
                var zipPath = CreateBuild(proxy, BuildTarget.iOS);
                await UploadBuild(client, proxy, targetReleaseId, zipPath, BuildTarget.iOS);
            }
            if (_buildForVisionOS)
            {
                var zipPath = CreateBuild(proxy, BuildTarget.VisionOS);
                await UploadBuild(client, proxy, targetReleaseId, zipPath, BuildTarget.VisionOS);
            }
            
            UpdateStatus($"Upload completed for '{proxy.InternalName} {proxy.Id}'.");
            
            // Clean up temp resources
            AssetDatabase.RemoveAssetBundleName(proxy.Id, true);
            var buildPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "AssetBuildTemp");
            if (Directory.Exists(buildPath))
            {
                Directory.Delete(buildPath, true);
            }
            _isPerformingBuild = false;
        }

        private async Task UploadBuild(ManagementWebService client, AssetProxy proxy, string targetReleaseId, string pathToZip, BuildTarget buildTarget)
        {
            UpdateStatus($"Uploading asset build for {buildTarget} to release slot {targetReleaseId}");
            var uploadResponse = await client.AppendPackage(proxy.Id, targetReleaseId, new UploadPackageDto
            {
                FileMain = await File.ReadAllBytesAsync(pathToZip),
                PackagePlatform = NeutralTargetPlatform(buildTarget)
            });
            UpdateStatus(uploadResponse.IsSuccess
                ? $"Uploaded assets for {buildTarget} successfully."
                : $"Failed to upload assets for {buildTarget}, reason:\n{uploadResponse.ErrorMessage}");
        }

        private string CreateBuild(AssetProxy proxy, BuildTarget buildTarget)
        {
            UpdateStatus($"Performing asset build for {buildTarget}");
            // Prepare empty build folder
            var buildPath = Path.Combine(Application.dataPath.Replace("Assets", string.Empty), "AssetBuildTemp");
            if (Directory.Exists(buildPath))
            {
                Directory.Delete(buildPath, true);
            }
            Directory.CreateDirectory(buildPath);
            
            // Perform build
            var build = new AssetBundleBuild
            {
                assetBundleName = proxy.Id,
                assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(proxy.Id)
            };
            var bundleManifest = BuildPipeline.BuildAssetBundles(buildPath, new[] { build }, BuildAssetBundleOptions.None, buildTarget);
            var containingBundles = bundleManifest.GetAllAssetBundles();
            
            // Get all required files and zip together
            var filesToZip = new List<string>();
            foreach (var buildFile in Directory.GetFiles(buildPath))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(buildFile);
                if (containingBundles.Any(bundleName => bundleName == fileNameWithoutExtension))
                {
                    filesToZip.Add(buildFile);
                    Debug.Log(buildFile);
                }
            }
            var zipPath = Path.Combine(buildPath, $"Assets.zip");
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var file in filesToZip)
                {
                    var fileName = Path.GetFileName(file);
                    zip.CreateEntryFromFile(file, fileName);
                }
            }
            
            UpdateStatus($"Build completed for {buildTarget} and zip stored at {zipPath}");
            return zipPath;
        }
        
        private static List<AssetProxy> GetProxies()
        {
            var proxies = new List<AssetProxy>();
            var assets = AssetDatabase.FindAssets($"t:{typeof(AssetProxy)}");
            foreach (var asset in assets)
            {
                var proxy = AssetDatabase.LoadAssetAtPath<AssetProxy>(AssetDatabase.GUIDToAssetPath(asset));
                proxies.Add(proxy);
            }

            return proxies;
        }
        
        private static NebulaSettings GetSettings()
        {
            var assets = AssetDatabase.FindAssets($"t:{typeof(NebulaSettings)}");
            if (!assets.Any())
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<NebulaSettings>(AssetDatabase.GUIDToAssetPath(assets[0]));
        }

        private static string NeutralTargetPlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer.ToString();
                case BuildTarget.Android:
                    return RuntimePlatform.Android.ToString();
                case BuildTarget.WebGL:
                    return RuntimePlatform.WebGLPlayer.ToString();
                case BuildTarget.VisionOS:
                    return RuntimePlatform.VisionOS.ToString();
                default:
                    throw new ArgumentOutOfRangeException(nameof(target), target, "unsupported target");
            }
        }
    }
}
#endif