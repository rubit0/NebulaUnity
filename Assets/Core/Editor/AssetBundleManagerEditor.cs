using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;

public class AssetBundleManagerEditor : EditorWindow
{
    private string[] assetBundleNames;
    private int selectedBundleIndex = -1;
    private string selectedBundleName = "";
    private string[] assetPaths;
    private string[] dependencies;
    private string version = "1.0.0";
    private uint crc = 0;

    private string manifestPath = "Assets/AssetBundles";
    private Vector2 leftPaneScrollPos;
    private Vector2 rightPaneAssetsScrollPos;
    private Vector2 rightPaneDependenciesScrollPos;

    [MenuItem("Nebula/AssetBundle Manager")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AssetBundleManagerEditor), false, "AssetBundle Manager");
    }

    void OnEnable()
    {
        assetBundleNames = AssetDatabase.GetAllAssetBundleNames();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // Left Pane: List of AssetBundles
        DrawLeftPane();

        // Right Pane: Details of the selected AssetBundle
        DrawRightPane();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPane()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(250));  // Set fixed width for the left pane

        EditorGUILayout.LabelField("Available AssetBundles", EditorStyles.boldLabel);

        leftPaneScrollPos = EditorGUILayout.BeginScrollView(leftPaneScrollPos, GUILayout.ExpandHeight(true));

        if (assetBundleNames.Length == 0)
        {
            EditorGUILayout.LabelField("No AssetBundles found.");
        }
        else
        {
            for (int i = 0; i < assetBundleNames.Length; i++)
            {
                if (GUILayout.Button(assetBundleNames[i], EditorStyles.toolbarButton))
                {
                    selectedBundleIndex = i;
                    selectedBundleName = assetBundleNames[selectedBundleIndex];
                    LoadSelectedBundleDetails();
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawRightPane()
    {
        EditorGUILayout.BeginVertical();

        if (selectedBundleIndex >= 0 && selectedBundleIndex < assetBundleNames.Length)
        {
            // Top Section: Meta Data and Upload Button
            EditorGUILayout.LabelField("AssetBundle Details", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Bundle Name:", selectedBundleName);

            EditorGUILayout.LabelField("Version:", version);
            EditorGUILayout.LabelField("CRC:", crc.ToString());

            if (GUILayout.Button("Upload to Backend"))
            {
                UploadBundleToBackend(selectedBundleName);
            }

            // Scrollable Section: Assets in Bundle
            EditorGUILayout.LabelField("Assets in Bundle:");
            rightPaneAssetsScrollPos = EditorGUILayout.BeginScrollView(rightPaneAssetsScrollPos, GUILayout.Height(150));
            if (assetPaths != null && assetPaths.Length > 0)
            {
                foreach (string path in assetPaths)
                {
                    EditorGUILayout.LabelField("- " + path);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No assets found in this bundle.");
            }
            EditorGUILayout.EndScrollView();

            // Scrollable Section: Dependencies
            EditorGUILayout.LabelField("Dependencies:");
            rightPaneDependenciesScrollPos = EditorGUILayout.BeginScrollView(rightPaneDependenciesScrollPos, GUILayout.Height(150));
            if (dependencies != null && dependencies.Length > 0)
            {
                foreach (string dependency in dependencies)
                {
                    EditorGUILayout.LabelField("- " + dependency);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No dependencies.");
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.LabelField("Select an AssetBundle from the left pane to view details.");
        }

        EditorGUILayout.EndVertical();
    }

    private void LoadSelectedBundleDetails()
    {
        if (selectedBundleName != "")
        {
            assetPaths = AssetDatabase.GetAssetPathsFromAssetBundle(selectedBundleName);
            LoadBundleDependencies(selectedBundleName);
            LoadBundleCRC(selectedBundleName);
        }
    }

    private void LoadBundleDependencies(string bundleName)
    {
        string manifestFilePath = Path.Combine(manifestPath, manifestPath.Split('/')[^1]);
        AssetBundle manifestBundle = AssetBundle.LoadFromFile(manifestFilePath);

        if (manifestBundle != null)
        {
            AssetBundleManifest manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            dependencies = manifest.GetAllDependencies(bundleName);
            manifestBundle.Unload(false);
        }
        else
        {
            dependencies = new string[] { "Manifest file not found." };
        }
    }

    private void LoadBundleCRC(string bundleName)
    {
        string bundlePath = Path.Combine(manifestPath, bundleName);
        if (File.Exists(bundlePath))
        {
            BuildPipeline.GetCRCForAssetBundle(bundlePath, out crc);
        }
        else
        {
            crc = 0;
        }
    }

    private void UploadBundleToBackend(string bundleName)
    {
        string bundlePath = Path.Combine(manifestPath, bundleName);

        if (!File.Exists(bundlePath))
        {
            EditorUtility.DisplayDialog("Error", "AssetBundle not found. Please build the AssetBundle first.", "OK");
            return;
        }

        EditorApplication.delayCall += () =>
        {
            string uploadURL = "https://your-backend-server.com/upload";
            byte[] fileData = File.ReadAllBytes(bundlePath);

            UnityWebRequest www = UnityWebRequest.Put(uploadURL, fileData);
            www.SetRequestHeader("Content-Type", "application/octet-stream");
            www.SetRequestHeader("AssetBundle-Name", bundleName);
            www.SetRequestHeader("Version", version);
            www.SetRequestHeader("CRC", crc.ToString());

            var operation = www.SendWebRequest();
            operation.completed += (asyncOp) =>
            {
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Upload complete: " + bundleName);
                    EditorUtility.DisplayDialog("Success", "AssetBundle uploaded successfully!", "OK");
                }
                else
                {
                    Debug.LogError("Upload failed: " + www.error);
                    EditorUtility.DisplayDialog("Upload Failed", www.error, "OK");
                }

                www.Dispose();
            };
        };
    }
}
