# Nebula Unity

Nebula is a Unity package (`com.studio-delatorre.nebula`) that connects Unity projects to a remote **Nebula backend** for publishing and consuming **AssetBundles**. It provides:

- **Runtime client** — authenticate, discover, download, and load asset containers on device or in play mode.
- **Editor tooling** — build AssetBundles from project folders and upload platform-specific packages to the backend.
- **Shared configuration** — ScriptableObjects for backend endpoint and per-container build proxies.

The backend is a separate service (not in this repo). This package is the Unity client that talks to it over HTTP.

---

## Requirements

| Requirement | Version |
|-------------|---------|
| Unity | 2022.3 LTS (60f1 or compatible) |
| Newtonsoft JSON | `com.unity.nuget.newtonsoft-json` 3.2.1+ |
| Asset Bundle Browser | `com.unity.assetbundlebrowser` 1.7.0 (Editor) |

---

## Installation

### As a UPM package (recommended)

Add the package to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.studio-delatorre.nebula": "https://github.com/rubit0/NebulaUnity.git?path=Assets/Core"
  }
}
```

Or reference a local path while developing:

```json
"com.studio-delatorre.nebula": "file:../NebulaUnity/Assets/Core"
```

### Import the sample

After installing, open **Package Manager → Nebula → Samples → Main Sample → Import** to get a working demo scene.

---

## Quick start

### 1. Configure the backend endpoint

Create a **Nebula Settings** asset:

**Assets → Create → Nebula → Create Settings**

Set `Endpoint` to your Nebula API base URL (e.g. `https://api.example.com`).

### 2. Publish assets (Editor)

1. Create an **Asset Proxy** per container you want to publish: **Assets → Create → Nebula → Create Asset Proxy**.
2. Place the proxy asset in the folder whose contents should be bundled. Set:
   - `Id` — backend container ID (must match an existing container on the server).
   - `InternalName` — label shown in the Editor window.
   - `Dependencies` — optional list of other asset bundle names to include as sub-bundles.
3. Open **Nebula → Nebula Assets Manager** and paste your management auth token.
4. Select a proxy, choose build targets (Web / iOS / visionOS), add release notes, and click **Build and release**.

The window builds AssetBundles, zips them (plus a `content.txt` dependency manifest), and uploads each platform package to a new release on the backend.

### 3. Consume assets (Runtime)

```csharp
using Nebula.Runtime;
using Nebula.Shared;

// Reference a NebulaSettings ScriptableObject (assign in Inspector or load from Resources)
var manager = new AssetsManager(settings);
await manager.Init();

// Option A: email/password login (token stored in PlayerPrefs)
await manager.LoginUser("user@example.com", "password");

// Option B: inject a token directly
manager.LoginUser("your-jwt-token");

await manager.Fetch();   // compare local index vs remote
await manager.Sync();    // fetch + download all new/updated assets

// Load a locally cached container
var localAsset = manager.LocalAssets[0];
var bundle = await manager.LoadAsset(localAsset);
var instances = await manager.LoadAndInstantiateAll(localAsset);

manager.UnloadAsset(localAsset);
```

See `Assets/Core/Samples~/MainSample/DemoAssetManager.cs` for a full UI-driven example.

---

## Concepts

### Asset container

A logical bundle of content on the Nebula backend, identified by a string `Id`. Each container has releases; each release can hold one package per platform (WebGL, iOS, visionOS, etc.).

### Asset Proxy (Editor)

A `ScriptableObject` placed in the folder to bundle. Its `Id` becomes the AssetBundle name and must match the backend container ID.

### Local storage

Downloaded containers are stored under:

| Environment | Path |
|-------------|------|
| Editor | `<project-root>/AssetContainers/<containerId>/` |
| Player build | `Application.persistentDataPath/AssetContainers/<containerId>/` |

An `index.json` at the root tracks locally known containers (version, timestamp, metadata).

### Package layout (zip)

Each uploaded/downloaded zip contains:

- Main bundle file named `<containerId>`
- Optional dependency bundle files (names from `AssetProxy.Dependencies`)
- Optional `content.txt` — comma-separated list of dependency bundle names (used at load time)

---

## Runtime API overview

### `AssetsManager`

Main entry point. Construct with `NebulaSettings`, then call `Init()` once.

| Member / method | Description |
|-----------------|-------------|
| `IsAuthenticated` | Whether a token is available |
| `LocalAssets` | Containers cached on disk |
| `AvailableRemoteAssets` | Remote containers not yet downloaded |
| `UpdateableAssets` | Local containers with a newer remote version |
| `LoadedAssetBundle` | Currently loaded containers |
| `Init()` | Initialize storage, load index, optional auto-fetch |
| `LoginUser(email, password)` | Login; persists token to `PlayerPrefs` |
| `LoginUser(authToken)` | Set token without network call |
| `LogoutUser()` | Clear stored token |
| `Fetch()` | Refresh remote/local diff lists |
| `Sync()` | `Fetch()` then download all new and updated assets |
| `DownloadAsset(AssetDto)` | Download one container |
| `LoadAsset(Asset)` | Load bundle + dependency sub-bundles |
| `LoadAndInstantiateAll(Asset)` | Load and instantiate all `GameObject` assets |
| `UnloadAsset(Asset)` | Unload bundle from memory |

### `AssetManagementUtils` (static)

Lower-level file and bundle helpers: paths, index read/write, download, delete, `ClearAllAssets()`.

### `AssetsWebservice` (Runtime HTTP)

Used internally by `AssetsManager`. Endpoints (relative to `NebulaSettings.Endpoint`):

| Method | Path | Auth |
|--------|------|------|
| POST | `/auth/login` | — |
| GET | `/assets` | Bearer |
| GET | `/assets/{id}` | Bearer |

---

## Editor API overview

### `AssetsManagerWindow`

Menu: **Nebula → Nebula Assets Manager**

Orchestrates: create release → build bundles → zip → upload per platform.

### `ManagementWebService` (Editor HTTP)

Base URL: `{Endpoint}/manage`. All calls use Bearer token auth.

| Method | Purpose |
|--------|---------|
| `GetAllContainer()` | List containers |
| `GetContainer(id)` | Get one container |
| `CreateNewContainer(dto)` | Create container |
| `GetReleases(id)` / `GetRelease(id, releaseId)` | List/get releases |
| `AddRelease(id, dto)` | Create release |
| `AppendPackage(id, releaseId, dto)` | Upload platform zip |
| `UpdateReleaseChannelSlot(id, channel, releaseId)` | Promote to Dev/Release slot |
| `UpdateContainerMeta(id, dto)` | Update metadata |
| `UpdateContainerAccessGroups(id, dto)` | Update access groups |

---

## Project structure

```
Assets/Core/
├── package.json              # UPM manifest
├── Runtime/                  # NebulaUnity assembly — player + editor play mode
│   ├── AssetsManager.cs      # Main runtime API
│   ├── Asset.cs / AssetsIndex.cs
│   ├── API/                  # AssetsWebservice + runtime DTOs
│   └── Misc/                 # AssetManagementUtils
├── Editor/                   # NebulaEditor assembly — Editor only
│   ├── AssetsManagerWindow.cs
│   └── API/                  # ManagementWebService + editor DTOs
├── Shared/                   # NebulaShared assembly
│   ├── NebulaSettings.cs
│   ├── AssetProxy.cs
│   └── API/                  # WebResponse, PackageDto
└── Samples~/MainSample/    # Optional demo (import via Package Manager)
```

### Namespaces

| Namespace | Assembly | Role |
|-----------|----------|------|
| `Nebula.Runtime` | NebulaUnity | Runtime asset management |
| `Nebula.Runtime.API` | NebulaUnity | Runtime HTTP client |
| `Nebula.Editor` | NebulaEditor | Editor window |
| `Nebula.Editor.API` | NebulaEditor | Management HTTP client |
| `Nebula.Shared` | NebulaShared | Settings, proxies, shared DTOs |

---

## Authentication

| Context | Storage key | Mechanism |
|---------|-------------|-----------|
| Runtime (player) | `PlayerPrefs` key `Nebula_AuthToken` | Login or `LoginUser(token)` |
| Editor (publish) | `EditorPrefs` key `Nebula_AuthToken` | Pasted in Assets Manager window |

Runtime and Editor auth are separate stores. Use a management-capable token in the Editor; end-user credentials or tokens at runtime.

---

## Platform support

**Publishing (Editor window):** WebGL, iOS, visionOS toggles.

**Downloading (Runtime):** `DownloadAsset` currently selects the package where `Platform == RuntimePlatform.IPhonePlayer.ToString()`. Extend this when targeting other player platforms.

**Build note:** `.cs` script files are excluded from AssetBundle builds to avoid build errors.

---

## Known limitations

- `DownloadAsset` is hardcoded to the iOS/`IPhonePlayer` platform package. Adapt for WebGL, Android, or visionOS player targets.
- Sub-bundle unload logic in `UnloadAsset` may not cover all dependency edge cases (see TODO in `LoadAsset`).
- `AssetProxy.KeyValueEntry` uses the field name `Vale` (typo for `Value`).
- AssetBundle builds trigger a Unity domain reload; Editor window state is not persisted across reloads unless handled separately.

---

## Development

This repository is the package source. The consumable UPM root is `Assets/Core/`.

**Version:** see `Assets/Core/package.json` (currently `1.1.2`).

**Author:** Ruben de la Torre — [GitHub](https://github.com/rubit0/NebulaUnity)

For LLM/agent-oriented documentation (architecture, conventions, extension points), see [AGENTS.md](./AGENTS.md).
