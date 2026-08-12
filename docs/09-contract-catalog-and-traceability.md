# Contract Catalog, Ownership and Traceability

## 1. Ownership matrix

| Concern | Verdant Core | Experience | Host | First Bloom |
|---|---:|---:|---:|---:|
| command transaction | owner | consume | route | rules |
| Replay | owner | consume | invoke | compatible rules |
| topology primitives | owner | no | no | policy/config |
| Bloom/enclosure/scoring | no | present facts | no | owner |
| canonical serializer | owner | excluded | transport | adapter |
| tutorials/presentation | no | owner | adapters | profiles/content |
| storage | contracts | no | implementation | authority payload |
| audio/haptics | no | mapping | implementation | profile mapping |

## 2. Key interfaces

```typescript
interface ITopology {
  GetNeighbors(node: NodeId): Iterable<NodeId>;
  Distance(a: NodeId, b: NodeId): number;
}

interface IRandomProvider { NextUInt(): number; }

interface IGameRuleset {
  Validate(command: Command): ValidationResult;
  Resolve(state: GameState, command: Command): ResolutionResult;
  GenerateEvents(): Iterable<GameEvent>;
}

interface ITutorialScenario {
  Id: ScenarioId;
  CreateInitialGame(): InitialGameDescriptor;
  GetInitialStep(): TutorialStep;
  Evaluate(step: TutorialStep, result: CommandResult): TutorialTransition;
}

interface IPresentationProfile {
  Map(events: ReadonlyArray<GameEvent>, context: PresentationContext): PresentationSequence;
}
```

## 3. Key result and payload contracts

```typescript
interface ReplayResult {
  State: GameState;
  Events: GameEvent[];
  Hash?: string;
}

interface ReplayPrefixExpectation {
  actionCount: number;
  expectedState: GameState;
  expectedEventBatch: GameEvent[];
  expectedScore: string;
  expectedHand: HandSlot[];
  expectedFixtureDeckCursor: number;
  expectedStateHash?: string;
  expectedEventHash?: string;
}
```

## 4. Error catalog

- `COMMAND_SCHEMA_INVALID`
- `STATE_VERSION_CONFLICT`
- `INVALID_HAND_SLOT`
- `EMPTY_HAND_SLOT`
- `CATALOG_IDENTITY_MISMATCH`
- `REFLECTION_LOCKED`
- `INVALID_PIECE_ROTATION`
- `GEOMETRY_TRANSFORMATION_ERROR`
- `OUT_OF_BOUNDS_PLACEMENT`
- `OCCUPIED_CELL_COLLISION`
- `GAME_RULE_LEGALITY_VIOLATION`
- `DETERMINISTIC_GUARD_EXCEEDED`
- `SCORE_ARITHMETIC_OVERFLOW`
- `INVALID_REPLAY_ACTION_COUNT`
- explicit format and compatibility errors defined by versioned persistence/replay schemas.

## 5. Event identity catalog

- placement: `evt_placed_{actionCount}`
- Bloom: `evt_bloom_{actionCount}_{organismId}`
- scoring: `evt_score_{actionCount}_{actionBloomIndex}`

IDs must be deterministic, stable, unlocalized, and independent of timestamps or score display.

## 6. Invariant catalog

- one authoritative deterministic engine;
- successful-command log only;
- Replay reconstructs truth;
- presentation never calculates gameplay;
- transactions are atomic;
- rejected commands consume no randomness;
- diagnostics are out-of-band;
- caches are never authority;
- compatibility dimensions are independent;
- unordered data is canonicalized;
- First Bloom remains outside Core;
- candidate rules never enter release authority.

## 7. Glossary

- **Action count:** number of successful commands applied; zero means initial state.
- **ActionLog:** ordered successful commands only.
- **Authoritative state:** complete deterministic gameplay state required for future execution.
- **Cache:** derived data that can be discarded and reconstructed.
- **Canonical initialization:** ruleset, RulePack, catalog, parameters, seed/deck inputs, and initial state contract.
- **Diagnostic:** non-semantic information about a failed attempt or operational issue.
- **Event:** resolved gameplay fact from a successful command.
- **Fixture deck:** explicit deterministic sequence used to isolate unresolved RNG.
- **Presentation state:** non-authoritative animation, focus, and playback state.
- **Replay authority:** the single state/history reconstruction mechanism.
- **RulePack:** immutable data/configuration consumed by a ruleset.
- **Ruleset:** deterministic executable game behavior.
- **Semantic history:** ordered authoritative events reconstructed from accepted commands.

## 8. Traceability map

- Architecture and package boundaries -> Platform Architecture §§1-6.
- Atomic command behavior -> Deterministic Execution §§1-3.
- Replay, persistence, compatibility -> Replay Specification §§1-10.
- Tutorial/presentation/accessibility -> Experience Specification §§1-10.
- First Bloom placement/catalog -> Reference Integration §§3-5.
- D-034 enclosure -> Reference Integration §6 and Decision Register.
- D-020 Bloom -> Reference Integration §7 and Decision Register.
- D-021 scoring -> Reference Integration §8 and Decision Register.
- F1 fixtures -> Verification Specification §§2-7.
- G3 execution -> Delivery Plan §§2-7.

## 9. Open-item register

- D-025: select authoritative RNG and seed normalization before RNG-derived vectors.
- D-026: gather evidence and accept/reject normative 2 KB share cap.
- D-031: define Barren qualifying relationship and update affected events/fixtures/hashes.
- F1.4/F1.5: execute evidence suites.
- F1.6: freeze canonical byte vectors and golden digests after reproducibility checks.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
