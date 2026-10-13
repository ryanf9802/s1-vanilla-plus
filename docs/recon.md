# Recon notes

Game build 0.4.7f12 (Steam build 25796331), Unity 2022.3.62f2, MelonLoader 0.7.3, IL2CPP.

## Decompiling game types

Generated interop assemblies: `<game>\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll`. They contain signatures and fields only, not method bodies (calls are native stubs), so behavior has to be confirmed at runtime (UnityExplorer, logging).

Decompile one type with ilspycmd 8.2.0.7535 (the latest ilspycmd failed to install with SDK 8):

    dotnet tool install ilspycmd --version 8.2.0.7535 --tool-path <dir>
    ilspycmd Assembly-CSharp.dll -t Il2CppScheduleOne.UI.Compass.CompassManager -r <game>\MelonLoader\Il2CppAssemblies

## Vanilla compass: `Il2CppScheduleOne.UI.Compass.CompassManager`

A `Singleton<CompassManager>` that already supports per-target markers:

- `AddElement(Transform transform, RectTransform contentPrefab, bool visible = true)` returns an `Element`; `RemoveElement(Transform | Element, bool alsoDestroyRect = true)`.
- `Element` has `Rect`, `Group` (CanvasGroup), `DistanceLabel` (TextMeshProUGUI), `TargetTransform`, `Visible`, `SetTarget(Transform)`.
- `UpdateElement(Element)` runs per element each frame; `GetCompassData(Vector3 worldPosition, out float xPos, out float alpha)` maps a world position to a strip position and alpha.
- Tunables: `CompassUIRange`, `FullAlphaRange`, `AngleDivisor`, `ElementContentSize`, static `DISTANCE_LABEL_THRESHOLD`, `NOTCH_COUNT`.
- Lists: `elements`, `notches`; `cam` is the camera transform; `ElementPrefab`, `Container`, `ElementUIContainer`.

## Implications

- Ping markers can be native compass elements: spawn a GameObject at the ping position and `AddElement` it, then `RemoveElement` after 5 s. No custom UI strip needed.
- Elevation: postfix `UpdateElement` and append a height delta to `Element.DistanceLabel` (target `y` minus player/camera `y`).
- Distance: `DISTANCE_LABEL_THRESHOLD` suggests the label only shows within some range; check its value and the per-frame logic at runtime.
- Still unknown: what the label text format is, how existing waypoint/quest markers register elements, and the player camera and player-position accessors.
