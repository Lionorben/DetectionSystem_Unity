A speed-driven AI detection system for stealth and awareness gameplay, implemented in Unity. This is a tech demo built for portfolio purposes; the underlying system has been used in production game projects. A companion implementation of the same system in Unreal Engine is available here: [DetectionSystem_Unreal](https://github.com/Lionorben/DetectionSystem_Unreal).

## Overview

The system's core idea is that **movement speed drives detection rate**. Every detectable agent (a `Sentient`) reports a normalized speed (current speed relative to its maximum), and observers evaluate that value against designer-authored curves to determine how quickly suspicion builds. A sentient sprinting through an observer's field of view is detected far faster than one creeping along its edge, and a stationary sentient can be tuned to build no suspicion at all.``

On top of speed, each sentient carries a **detection multiplier** that scales its detection rate globally. This provides a simple hook for gameplay events: firing a gun, breaking a window, or knocking over an object can temporarily raise the multiplier so that noisy player actions make the player easier to detect, independent of how fast they are moving.`

The system supports **multiple simultaneous detection targets**. Observers monitor every sentient inside their sensing radius and accumulate suspicion for each one independently, so a single enemy can track the player, companions, and other AI agents at the same time. In single-player contexts where the player is the only meaningful target, this generality leaves a lot of room for optimization: monitoring collections, per-sentient suspicion bookkeeping, and the batched spatial queries can all be collapsed into a single-target path.

## How It Works

Detection is split across four cooperating components:`

### `Sentient` ``

The base class for anything that can be detected. It tracks `currentSpeed` against `maxSpeed` to produce a `normalizedSpeed` in the 0–1 range, and exposes the public `detectionMultiplier` used to scale detection rates. The demo's `SimplePlayerController` derives from `Sentient` and feeds its Rigidbody velocity into `currentSpeed`.``

### `AISensor` ``

The perception layer attached to each observer. It gathers nearby colliders through a spatial overlap query, then filters them into two lists:`

- **Objects in radius**: everything inside the sensing sphere.`

- **Objects in sight**: the subset that also passes a field-of-view angle check, a height check, and an occlusion linecast against blocking geometry.`

The sensor also builds a procedural view-cone mesh each frame, raycasting against occlusion geometry and resolving edges through iterative bisection so the visualized cone hugs walls and obstacles accurately. The mesh's color is driven by the observer's current suspicion level, giving immediate visual feedback as detection builds.`

### `AISensorManager` ``

A singleton that batches the overlap-sphere queries of every sensor in the scene into a single jobified `OverlapSphereCommand.ScheduleBatch` call using Unity's Job System and Native Collections. Queries are scheduled on one frame and collected on the next, alternating so the main thread never blocks on physics results. This keeps the cost of many concurrent sensors low and scales well as observer counts grow.``

### `DetectionManager` ``

The decision layer. On a configurable frame interval, it syncs the monitored sentient list with the sensor's radius results, then updates a suspicion value for each monitored sentient:`

1. The sentient's `normalizedSpeed` is evaluated against one of two `AnimationCurve`s, depending on whether the sentient is currently in sight or merely in radius. Faster movement yields a higher base detection rate.``

2. The result is scaled by the sentient's `detectionMultiplier`.``

3. If the rate is still positive, it is further scaled by a distance curve, so closer sentients are detected faster than distant ones.`

Suspicion accumulates until it reaches the `suspicionThreshold`, at which point the sentient is marked as detected and the `OnSentientDetected` event fires. The number of simultaneously detected sentients is capped for performance. Because every stage of the calculation runs through `AnimationCurve`s, the entire detection feel is tunable in the Inspector without touching code.``

## Designer-Facing Tuning`

| Parameter | Purpose |

``| `halfSightFOV`, `sightDistance`, `sightHeight` | Shape of the observer's vision cone |``

``| `suspicionThreshold` | Total suspicion required for a full detection |``

``| `inSightDetectionRate` | Curve mapping normalized speed to detection rate while in sight |``

``| `outSightDetectionRate` | Curve mapping normalized speed to detection rate while in radius but out of sight |``

``| `distanceMult` | Curve scaling detection rate by normalized distance to the sentient |``

``| `frameTimeBetweenSightLoops` | Frames between detection logic updates |``

``| `detectionMultiplier` (on `Sentient`) | Per-agent scalar, usable as a hook for player actions such as gunfire |``

## Demo Scene`

The included demo scene wires the system into a minimal stealth loop: patrolling observers sweep their vision cones back and forth (`EnemyRotation`), the player moves with a simple Rigidbody controller, and the view cone shifts color as suspicion rises. When the player is fully detected, the `GameManager` restarts the scene.``

## Project Structure`
`Assets/Scripts/`

`├── Sentient.cs // Base class for detectable agents`

`├── SimplePlayerController.cs // Demo player, derives from Sentient`

`├── AISensor.cs // FOV, occlusion, and view mesh generation`

`├── AISensorManager.cs // Jobified batching of all sensor queries`

`├── DetectionManager.cs // Suspicion accumulation and detection events`

`├── EnemyRotation.cs // Demo patrol sweep`

`└── GameManager.cs // Demo detection handling`

` ``` `

`## License`

`MIT`