# Verdant Experience and Host Specification

## 1. Boundary principle

The engine decides what happened. The experience layer decides how to teach, explain, animate, announce, and present it. Experience and host code MUST NOT calculate placement, growth, score, memory, terminal outcomes, or historical states.

## 2. Tutorial framework

```typescript
interface ITutorialScenario {
  Id: ScenarioId;
  CreateInitialGame(): InitialGameDescriptor;
  GetInitialStep(): TutorialStep;
  Evaluate(step: TutorialStep, result: CommandResult): TutorialTransition;
}
```

A scenario may define initial state, permitted commands, completion conditions, prompts, highlights, expected semantic events, hints, recovery, Undo/restart policy, progress, and accessibility announcements.

Separate state into:

1. scenario game state, owned by the engine;
2. tutorial orchestration state, such as lesson, step, hints, and permissions;
3. presentation state, such as focus, tooltips, and highlight animation.

Mini-boards use the same rules and validation as normal play, do not mutate the active run, and remain replayable.

## 3. Presentation profile

```typescript
interface IPresentationProfile {
  Map(events: ReadonlyArray<GameEvent>, context: PresentationContext): PresentationSequence;
}
```

Profiles define event grouping, order, timing, input lock policy, interruption, fast-forward, reduced-motion substitutions, sound/haptic mapping, explanation templates, and accessibility announcements.

The First Bloom 900 ms action story belongs to its profile. Other games may use instant, card-based, or longer tactical portrayals without changing state or events.

## 4. Semantic input

```text
Touch / mouse / keyboard / controller
-> semantic intent
-> Preview or canonical Command
-> deterministic engine
```

Supported interaction patterns may include two-tap confirmation, drag and drop, keyboard navigation, controller input, long press, rotation/reflection, hover/touch previews, contextual actions, historical inspection, and presentation input locking.

Gesture timing and raw device coordinates MUST NOT enter canonical commands unless the game explicitly models them as authoritative data.

## 5. Explanation data

```typescript
interface Explanation {
  LocalizationKey: string;
  Arguments: Map<string, ExplanationValue>;
  Cause: RuleReference;
}
```

Events contain structured arguments and rule references, not final text. Consumers include teaching strips, tooltips, logs, tutorials, accessibility, localization, debugging, and Timeline.

## 6. Adapters

- `IAudioAdapter`
- `IHapticsAdapter`
- `IAnimationClock`
- `IAccessibilityAnnouncer`
- `ILocalizationService`
- `IViewportAdapter`

Adapter failure or absence MUST NOT change committed gameplay. Rendering interruption settles to authoritative state. Reduced motion preserves meaning through alternate presentation.

## 7. Playback modes

- Instant
- Normal
- Slow
- Step-by-step
- Reduced motion
- Skip presentation

Modes alter presentation only. Reconstructed state and semantic events are identical.

## 8. Event dispatch and failure isolation

Committed events are dispatched after in-memory commit. Failing audio, haptics, rendering, storage, sharing, or accessibility consumers cannot alter the committed state. The dispatcher SHOULD isolate consumers and surface operational diagnostics outside semantic history.

## 9. Timeline and replay UX

Historical views consume Replay results. Scrubbing never mutates live state. Event playback may be instant or animated. Explanations reference semantic event facts. Exiting history returns to current authority, not a UI-maintained approximation.

## 10. Accessibility requirements

Every meaningful state change exposed visually SHOULD have a non-visual semantic representation. Presentation profiles provide reduced-motion alternatives, accessible announcements, focus targets, and explanation arguments. Accessibility settings are presentation state and excluded from canonical gameplay hashes.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
