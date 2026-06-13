# AGENTS.md — Nebula Unity

This file is written for AI coding agents working in this repository. It describes architecture, APIs, data flows, conventions, and safe extension points.

---

## What this project is

**Nebula Unity** is a Unity Package Manager (UPM) library at `Assets/Core/` (`com.studio-delatorre.nebula`). It is **not** the Nebula backend server — it is the **Unity client** that:

1. **Publishes** AssetBundles from the Unity Editor to a remote Nebula API.
2. **Consumes** those bundles at runtime: sync metadata, download zips, load `AssetBundle`s, optionally instantiate prefabs.

There is no game logic here beyond a sample demo. The core deliverable is a reusable asset-delivery SDK.

---

## Repository map

```
NebulaUnity/                          # Git repo root (this README/AGENTS live here)
└── Assets/Core/                      # UPM package root — all package code lives here
    ├── package.json
    ├── Runtime/                      # Assembly: NebulaUnity
    ├── Editor/                       # Assembly: NebulaEditor (Editor platform only)
    ├── Shared/                       # Assembly: NebulaShared
    └── Samples~/MainSample/          # Optional sample; not auto-included
```

**Do not** treat `Assets/TextMesh Pro/` or `ProjectSettings/` as part of the Nebula package — they support the dev/test Unity project hosting the package.

---

## Assembly dependency graph

```
NebulaShared          (no Nebula deps)
    ↑
NebulaUnity           (references NebulaShared)
NebulaEditor          (references NebulaShared, Editor-only)
```

| Assembly | asmdef path | Platforms | Root namespace |
|----------|-------------|-----------|----------------|
| `NebulaShared` | `Shared/NebulaShared.asmdef` | Any | `Nebula.Shared` |
| `NebulaUnity` | `Runtime/NebulaUnity.asmdef` | Any | `Nebula.Runtime` |
| `NebulaEditor` | `Editor/NebulaEditor.asmdef` | Editor | `Nebula.Editor` |

**Rule:** Runtime code must not reference `Nebula.Editor`. Editor code may use Shared types and optionally duplicate HTTP patterns from Runtime.

External NuGet/UPM deps (via `package.json`):

- `com.unity.nuget.newtonsoft-json` — JSON (de)serialization in web services
- `com.unity.assetbundlebrowser` — declared dependency; primary build path uses `BuildPipeline.BuildAssetBundles` in `AssetsManagerWindow`

---

## Domain model

### Backend concepts (external API)

| Concept | Description |
|---------|-------------|
| **Container** | Identified by string `Id`. Holds metadata, access groups, release slots (Dev/Production). |
| **Release** | A versioned publish attempt on a container. Has `Version`, `Notes`, `Timestamp`, and `Packages[]`. |
| **Package** | Platform-specific blob for one release. `PackageDto`: `BlobUrl`, `Platform` (e.g. `IPhonePlayer`, `WebGLPlayer`, `VisionOS`). |
| **Release channel slot** | `Dev` or `Release` — which release is active for clients in that channel. |

### Unity concepts

| Type | Location | Role |
|------|----------|------|
| `NebulaSettings` | `Shared/NebulaSettings.cs` | ScriptableObject with `Endpoint` (API base URL). |
| `AssetProxy` | `Shared/AssetProxy.cs` | Editor-side proxy in the folder to bundle. `Id` = container ID = asset bundle name. |
| `Asset` | `Runtime/Asset.cs` | Local index entry (cached container metadata). |
| `AssetDto` | `Runtime/API/Dtos/AssetDto.cs` | Remote container metadata from `/assets`. |
| `AssetsIndex` | `Runtime/AssetsIndex.cs` | `{ "Assets": [ ... ] }` persisted as `index.json`. |

### Local filesystem layout

Root: `AssetManagementUtils.GetAssetsContainerPath()`

- **Editor:** `<project-root>/AssetContainers/`
- **Player:** `Application.persistentDataPath/AssetContainers/`

Per container `<id>/`:

```
AssetContainers/
├── index.json
└── <containerId>/
    ├── <containerId>              # main AssetBundle file
    ├── <dependencyBundleName>     # optional sub-bundles
    └── content.txt                # optional: "dep1,dep2" for LoadAsset dependency resolution
```

Download flow: fetch zip → write `download.zip` → `ZipFile.ExtractToDirectory` → delete zip.

---

## Data flows

### Flow A — Publish (Editor)

```
AssetProxy (in folder)
    → AssetsManagerWindow assigns assetBundleName = proxy.Id on folder
    → BuildPipeline.BuildAssetBundles (per selected BuildTarget)
    → Exclude *.cs from assetNames
    → Zip bundle files + content.txt (dependencies)
    → ManagementWebService.AddRelease(containerId, notes)
    → ManagementWebService.AppendPackage(containerId, releaseId, zip bytes, platform)
```

**Entry point:** `AssetsManagerWindow.StartBuildProcessForProxy`

**Auth:** `EditorPrefs` key `Nebula_AuthToken` (management token, Bearer header).

**Settings:** first `NebulaSettings` asset found in project via `AssetDatabase.FindAssets`.

**Side effect:** `BuildPipeline.BuildAssetBundles` causes a **domain reload**. Window instance fields (`_buildStatusMessage`, `_isPerformingBuild`, etc.) are lost unless persisted (e.g. `SessionState`) and restored in `OnEnable`. Current code does **not** persist build state across reload — agents fixing upload UX should address this.

### Flow B — Consume (Runtime)

```
NebulaSettings.Endpoint
    → AssetsManager.Init()
        → InitAssetContainersDirectory, LoadAssetsIndex
        → new AssetsWebservice(endpoint) — reads PlayerPrefs token
        → if authenticated && no local assets → Fetch()
    → LoginUser (optional)
    → Fetch() — GET /assets, diff vs local index
        → AvailableRemoteAssets (new)
        → UpdateableAssets (remote Timestamp > local)
        → removes obsolete local containers
    → DownloadAsset(AssetDto) — pick platform package, download zip, update index
    → LoadAsset(Asset) — LoadFromFileAsync + load dependency sub-bundles from content.txt
    → LoadAndInstantiateAll — LoadAllAssetsAsync<GameObject> + Instantiate
    → UnloadAsset — unload main + tracked sub-bundles
```

**Entry point:** `new AssetsManager(settings)` then `await Init()`.

**Auth:** `PlayerPrefs` key `Nebula_AuthToken` (same key name as Editor, different store).

---

## HTTP API reference

Base URL: `NebulaSettings.Endpoint` (no trailing slash assumed in code — endpoints are concatenated as `$"{endpoint}/assets"`).

### Runtime — `AssetsWebservice` (`Nebula.Runtime.API`)

| Method | HTTP | Path | Body | Response |
|--------|------|------|------|----------|
| `Login` | POST | `/auth/login` | WWWForm: `Email`, `Password` | `TokenDto` |
| `GetAllAssets` | GET | `/assets` | — | `List<AssetDto>` |
| `GetAsset` | GET | `/assets/{assetId}` | — | `AssetDto` |

All authenticated calls: `Authorization: Bearer {token}`.

### Editor — `ManagementWebService` (`Nebula.Editor.API`)

Base: `{Endpoint}/manage`

| Method | HTTP | Path | Notes |
|--------|------|------|-------|
| `GetAllContainer` | GET | `/container` | |
| `GetContainer` | GET | `/container/{id}` | |
| `CreateNewContainer` | POST | `/container` | Form: `Name` |
| `GetReleases` | GET | `/container/{id}/releases` | |
| `GetRelease` | GET | `/container/{id}/releases/{releaseId}` | |
| `AddRelease` | POST | `/container/{id}/releases` | Form: `Notes` |
| `AppendPackage` | POST | `/container/{id}/releases/{releaseId}/packages` | Form: `FileMain` (bytes), `PackagePlatform` |
| `UpdateReleaseChannelSlot` | POST | `/container/{id}/slot/{channel}/{releaseId}` | `channel`: `Dev` \| `Release` |
| `UpdateContainerMeta` | POST | `/container/{id}/meta` | Form: `Meta[key]` entries |
| `UpdateContainerAccessGroups` | POST | `/container/{id}/access` | Form: `AccessGroups` (JSON string) |

### Response wrapper

`Nebula.Shared.API.WebResponse<T>` / `WebResponse`:

- `IsSuccess`, `Content` (generic), `ErrorMessage`
- Factories: `Success(...)`, `Failed(errorMessage)`

---

## Key classes — agent cheat sheet

### `AssetsManager` (`Runtime/AssetsManager.cs`)

**Primary runtime API.** Stateful service object (not a MonoBehaviour).

Construction: `new AssetsManager(NebulaSettings settings)`.

Must call `Init()` before `Sync()`. `Init()` is idempotent (`_didInit` guard).

State buckets after `Fetch()`:

- `LocalAssets` — from `AssetsIndex`
- `AvailableRemoteAssets` — on server, not local
- `UpdateableAssets` — local exists but server `Timestamp` is newer

**Important implementation detail — platform selection in `DownloadAsset`:**

```csharp
var package = assetDto.Packages.SingleOrDefault(p =>
    p.Platform == RuntimePlatform.IPhonePlayer.ToString());
```

This is **hardcoded to iOS**. Agents adding multi-platform download support should map `Application.platform` to `PackageDto.Platform` using the same mapping as `AssetsManagerWindow.NeutralTargetPlatform`:

| BuildTarget | Platform string |
|-------------|-----------------|
| iOS | `IPhonePlayer` |
| Android | `Android` |
| WebGL | `WebGLPlayer` |
| VisionOS | `VisionOS` |

### `AssetManagementUtils` (`Runtime/Misc/AssetManagementUtils.cs`)

Static helpers. Safe to call from runtime or tests.

Notable: `ClearAllAssets()` wipes `AssetContainers/` and re-inits empty index.

### `AssetsManagerWindow` (`Editor/AssetsManagerWindow.cs`)

`EditorWindow` with `[MenuItem("Nebula/Nebula Assets Manager")]`.

Build pipeline details:

- Temp build dir: `<project-root>/AssetBuildTemp/`
- Assigns `folderImporter.assetBundleName = proxy.Id` on proxy's parent folder
- Filters `assetNames` with `.Where(path => !path.EndsWith(".cs", ...))` — **required**; including `.cs` breaks builds
- Writes `content.txt` with comma-joined `proxy.Dependencies`
- Zips only files whose basename matches manifest bundle names
- Cleans up: `AssetDatabase.RemoveAssetBundleName`, deletes `AssetBuildTemp`

### `AssetProxy` (`Shared/AssetProxy.cs`)

Fields:

- `Id` — backend container ID (required for window to list proxy)
- `InternalName` — UI label
- `MetaData` — `List<KeyValueEntry>` with `Key` and `Vale` (**typo preserved in API**)
- `Dependencies` — other bundle names to build and list in `content.txt`

---

## DTO catalog

### Shared

- `PackageDto` — `BlobUrl`, `Platform`
- `WebResponse<T>`, `WebResponse`

### Runtime (`Nebula.Runtime.API.Dtos`)

- `LoginDto` — `Email`, `Password`
- `TokenDto` — `Token`, `Roles`
- `AssetDto` — `Id`, `Name`, `Meta`, `Version`, `Notes`, `Packages`, `Timestamp`

### Editor requests (`Nebula.Editor.API.Dtos.Requests`)

- `CreateContainerDto` — `Name`
- `UploadReleaseDto` — `Notes`
- `UploadPackageDto` — `FileMain`, `PackagePlatform`
- `UpdateContainerMetaDto` — `Meta` dictionary
- `UpdateContainerAccessGroupsDto` — `AccessGroups`
- `ReleaseChannel` enum — `Dev`, `Release`

### Editor responses (`Nebula.Editor.API.Dtos.Responses`)

- `AssetContainerDto` — full container admin view
- `ReleaseDto` — release with packages

---

## Sample code

Import path: `Samples~/MainSample/` (becomes `Samples/MainSample` after import).

| File | Purpose |
|------|---------|
| `DemoAssetManager.cs` | Full lifecycle UI: login, fetch, download, instantiate |
| `ListItemAssetBundle.cs` | List item with states: `Ready`, `Stale`, `Remote` |

**Canonical integration pattern** (from sample):

```csharp
_assetsManager = new AssetsManager(settings);
await _assetsManager.Init();
if (!_assetsManager.IsAuthenticated) { /* show login */ }
await _assetsManager.Fetch();
// UI binds to LocalAssets, UpdateableAssets, AvailableRemoteAssets
await _assetsManager.DownloadAsset(assetDto);
await _assetsManager.LoadAndInstantiateAll(asset);
```

`AssetsManager` is **not** a singleton — the host app owns the instance lifetime.

---

## Conventions for agents

### Naming

- **Container** / **asset container** — backend + local cached unit (id string).
- **Asset** (`Nebula.Runtime.Asset`) — local index model; not `UnityEngine.Object`.
- **AssetDto** — remote API model.
- **Proxy** — `AssetProxy` ScriptableObject for Editor publishing.
- **Package** — platform-specific zip/blob within a release.

### Async patterns

All network and `AssetBundle` async operations use `Task` + `TaskCompletionSource` wrapping Unity `AsyncOperation` / `UnityWebRequest.completed`. Do not assume `async/await` cancellation is implemented.

### Error handling

- Web layer returns `WebResponse` with `IsSuccess == false`; rarely throws.
- `AssetsManager.Fetch()` logs and returns early on auth failure.
- `Sync()` throws if called before `Init()`.

### Serialization

- Index + API: **Newtonsoft.Json** (`JsonConvert`)
- `AssetsIndex` / `Asset` stored as JSON on disk

### What not to do

- Do not reference `UnityEditor` from `Runtime/` or `Shared/`.
- Do not include `.cs` files in `AssetBundleBuild.assetNames`.
- Do not assume Editor and Runtime share auth persistence (`EditorPrefs` vs `PlayerPrefs`).
- Do not add game-specific logic to Core — keep it in the consuming project or sample.

---

## Common agent tasks

### Add a new player platform for downloads

1. Extend `DownloadAsset` in `AssetsManager.cs` to resolve `PackageDto` by `Application.platform`.
2. Reuse or extract `NeutralTargetPlatform` logic from `AssetsManagerWindow.cs`.
3. Document the platform string contract (must match backend / upload side).

### Add a new publish target in Editor

1. Add toggle in `AssetsManagerWindow.DrawMainUI`.
2. Add branch in `StartBuildProcessForProxy` calling `CreateBuild` + `UploadBuild`.
3. Add case in `NeutralTargetPlatform`.

### Fix domain reload losing build/upload progress

1. Before `BuildPipeline.BuildAssetBundles`, serialize state to `SessionState` (preferred over `EditorPrefs` for transient build session data).
2. Restore in `OnEnable` / `ShowWindow` and resume upload if needed.
3. Clear `SessionState` on successful completion.

### Expose runtime API to game code

- Prefer wrapping `AssetsManager` in a project-specific service MonoBehaviour.
- Inject `NebulaSettings` via SerializeField or addressables.
- Call `Init()` from `Start` or an explicit bootstrap phase.

### Create containers programmatically (Editor)

```csharp
var client = new ManagementWebService(settings.Endpoint, authToken);
var response = await client.CreateNewContainer(new CreateContainerDto { Name = "My Bundle" });
// Assign response.Content.Id to new AssetProxy.Id
```

---

## Known bugs and quirks (do not "fix" silently without user intent)

| Issue | Location | Notes |
|-------|----------|-------|
| iOS-only download | `AssetsManager.DownloadAsset` | See platform table above |
| `Vale` typo | `AssetProxy.KeyValueEntry` | Serialized assets may depend on field name |
| Sub-bundle unload | `UnloadAsset` / `UnloadAssetBundle` | Sub-bundles keyed differently; unload check uses `assetBundle.name` |
| `Sync()` removes from wrong list | `AssetsManager.Sync` | Successful updates call `_availableRemoteAssets.Remove` instead of `_updateableAssets` — behavioral bug |
| Domain reload | `AssetsManagerWindow` | Build step resets EditorWindow state |
| `LoadedAssetBundle` property name | `AssetsManager` | Singular name, returns list |

When fixing behavioral bugs, add a brief comment in commit/PR and update this section.

---

## Testing

There is no automated test assembly in the package. Validation approach:

1. Open sample scene after importing Main Sample.
2. Configure `Nebula Settings` asset with a reachable endpoint.
3. Play mode: login → fetch → download → instantiate.
4. Editor: create `AssetProxy`, run **Build and release**, verify status log and backend release.

For CI, Unity batchmode builds are possible but not configured in this repo.

---

## Versioning and release

- Package version: `Assets/Core/package.json` → `version`
- Unity minimum: `unity` + `unityRelease` fields in `package.json`
- Bump version when publishing to npm/git UPM consumers

---

## Quick file index

| Need to… | Open |
|----------|------|
| Change runtime sync/load behavior | `Runtime/AssetsManager.cs` |
| Change local storage | `Runtime/Misc/AssetManagementUtils.cs` |
| Change runtime HTTP | `Runtime/API/AssetsWebService.cs` |
| Change publish/build/upload | `Editor/AssetsManagerWindow.cs` |
| Change management HTTP | `Editor/API/ManagementWebService.cs` |
| Change endpoint config | `Shared/NebulaSettings.cs` |
| Change bundle folder mapping | `Shared/AssetProxy.cs` |
| See integration example | `Samples~/MainSample/DemoAssetManager.cs` |
| Package metadata | `Assets/Core/package.json` |

---

## Glossary

| Term | Meaning |
|------|---------|
| Nebula backend | External HTTP API serving containers, releases, and blob storage |
| Container | Top-level content unit identified by `Id` |
| Release | Versioned publish record on a container |
| Package | Per-platform binary payload (zip) for a release |
| Proxy | `AssetProxy` SO linking a Unity folder to a container `Id` |
| Index | `AssetsIndex` / `index.json` tracking local cache metadata |
