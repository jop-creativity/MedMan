# MedMan — Coding Conventions
**Version:** 1.0  
**Author:** Janusz Otto-Pawlicki / JOP Creativity  
**Engine:** Unity 6.3 LTS (6000.3.x) — URP  

---

## Namespaces

Each system has its own namespace reflecting its responsibility.

| Namespace | Scope |
|---|---|
| `MedMan.Core` | GameManager, EventBus, SceneLoader, SaveSystem |
| `MedMan.Player` | PlayerController, CameraController, InteractionSystem |
| `MedMan.Narrative` | DialogueSystem, WorldSpaceText, NarrativeSequencer |
| `MedMan.Audio` | AudioManager, Snapshots, SFX helpers |
| `MedMan.Visual` | Shaders, VolumeTransitions, VFX, ScreenShake |
| `MedMan.Data` | ScriptableObjects, SaveData, FearProfileSO |

---

## Naming Conventions

| Scope | Style | Example |
|---|---|---|
| Public fields & methods | PascalCase | `PublicMethod()`, `PublicField` |
| Protected fields & methods | camelCase | `protectedMethod()`, `protectedField` |
| Private fields | _camelCase | `_privateField` |
| Constants | UPPER_SNAKE_CASE | `MAX_PILL_COUNT` |
| Interfaces | IPascalCase | `IInteractable` |
| ScriptableObjects | PascalCase + SO suffix | `FearProfileSO` |
| Events | PascalCase + On prefix | `OnPillConsumed` |
| Coroutines | PascalCase + Cor suffix | `FadeOutCor()` |

---

## File & Class Structure

Each file contains one class. Class structure follows this order:

```
1. Fields (public, then protected, then private)
2. Properties
3. Unity lifecycle methods (Awake, Start, Update, etc.)
4. Public methods
5. Protected methods
6. Private methods
```

---

## Documentation

All public members require XML summary comments.

```csharp
/// <summary>
/// Consumes one pill and triggers the perceptual shift effect.
/// </summary>
public void ConsumePill()
{
    // ...
}
```

---

## Inspector Organisation

NaughtyAttributes is used for all inspector grouping and validation.

```csharp
[BoxGroup("Pill Settings")]
[SerializeField] private int _maxPills = 5;

[BoxGroup("Pill Settings")]
[SerializeField] private float _pillDuration = 30f;
```

---

## Patterns in Use

| Pattern | Usage |
|---|---|
| Singleton\<T\> | GameManager, AudioManager |
| EventBus (Observer) | Cross-system communication |
| State Machine | GameState, PillState |
| Object Pool | Reusable VFX, audio sources |
| Command | Interaction system actions |
| ScriptableObject | FearProfileSO, DialogueSO |

---

## Async & Coroutines

- **UniTask** is the standard for all async operations
- Coroutines are only used when UniTask is not applicable
- No `async void` — always `async UniTask` or `async UniTaskVoid`

---

## Branch Strategy

| Branch | Purpose |
|---|---|
| `main` | Stable, reviewed commits only |
| `develop` | Daily work, WIP commits |

Merge `develop` → `main` at the end of each completed task group.
