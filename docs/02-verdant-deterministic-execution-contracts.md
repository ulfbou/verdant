# Verdant Deterministic Execution Contracts

## 1. Canonical command pipeline

Every command MUST pass through one pipeline:

```text
Validate -> Apply -> Resolve -> Triggers -> Temporal Updates
-> End Conditions -> Emit Candidate Events -> Commit
```

Public operations are conceptually:

```text
ExecuteCommand()
Validate()
Resolve()
ApplyTriggers()
UpdateState()
EmitEvents()
```

Rulesets may provide behavior at extension points, but the engine owns execution ordering and transaction boundaries.

## 2. Atomic transaction model

```text
candidate = Engine.Execute(currentState, command)
if candidate.Status != SUCCESS:
    preserve currentState
    preserve ActionLog
    preserve randomState
    preserve score and semantic history
    emit DiagnosticPayload out-of-band
else:
    acquire transaction lock
    commit candidate.State
    append exactly one command
    commit candidate.RandomState
    release transaction lock
    enqueue persistence write
    enqueue committed semantic events
```

### 2.1 Dual locks

- **Transaction Lock:** synchronous mutual exclusion around state, ActionLog, and deterministic random commit. It prevents concurrent evaluation against stale authority.
- **Resolution Input Lock:** presentation-owned gate that prevents new input while an action story is playing. First Bloom's 900 ms sequence is a presentation-profile rule, not a Core invariant.

Storage is asynchronous after in-memory commit and MUST NOT delay presentation.

## 3. Rollback invariant

Any failure, including malformed input, rule rejection, invalid catalog identity, transformation error, corrupt state, deterministic guard breach, or score overflow, MUST preserve:

- board/world state;
- hand/inventory;
- score;
- ActionLog;
- actionCount;
- fixture deck cursor or random state;
- memories and progression capabilities;
- authoritative semantic history.

No semantic gameplay events are published on failure. Diagnostics are never appended to ActionLog.

## 4. Command and event separation

- **Command:** intention.
- **Event:** resolved authoritative fact.
- **Presentation cue:** product-specific portrayal.

Generic event examples: `EntityPlaced`, `EntityRemoved`, `RegionCreated`, `ScoreChanged`, `EffectApplied`, `ConditionMet`, `GameEnded`.

Games MAY extend semantic events. Event identity MUST be deterministic and MUST NOT use UUIDs, wall time, localized text, or display values.

## 5. Ruleset and catalog contracts

```typescript
interface IGameRuleset {
  Validate(command: Command): ValidationResult;
  Resolve(state: GameState, command: Command): ResolutionResult;
  GenerateEvents(): Iterable<GameEvent>;
}
```

The engine owns execution. Rulesets own behavior. Catalogs are immutable data addressed by stable IDs and compatibility identity. Content changes that alter authoritative behavior require a new catalog or RulePack identity.

## 6. Topology contract

```typescript
interface ITopology {
  GetNeighbors(node: NodeId): Iterable<NodeId>;
  Distance(a: NodeId, b: NodeId): number;
}
```

Built-in targets: SquareGrid, HexGrid, TriangleGrid, Graph, LayeredGrid, InfiniteGrid. Each topology MUST define canonical node identity, neighbor order, distance semantics, finite/infinite guard strategy, and serialization.

First Bloom uses an 8x8 orthogonal SquareGrid and a game-owned enclosure policy.

## 7. Deterministic random contract

```typescript
interface IRandomProvider { NextUInt(): number; }
```

Rules:

- no hidden randomness;
- no host or UI randomness in authoritative behavior;
- stable consumption order;
- rejected commands consume nothing;
- random algorithm and seed normalization are compatibility-critical;
- random state is committed atomically with game state and log.

D-025 is open. Normative First Bloom fixtures therefore use explicit `fixtureDeckSequence` and `fixtureDeckCursor`.

## 8. Preview contract

```typescript
Preview(state, proposedCommand) -> {
  validity,
  failureReason,
  affectedPositions,
  expectedEvents,
  projectedState,
  scoreChanges,
  uncertainty,
  alternatives
}
```

Preview MUST be side-effect-free and MUST share validation and simulation logic with commit. It MUST NOT mutate state, ActionLog, deck/random state, persistence, history, or presentation authority. For deterministic known outcomes, preview projections and committed facts MUST match.

## 9. Validation model

Generic validation occurs before game-specific placement legality. A ruleset may add stable failure codes, but precedence MUST be frozen per command schema. First failure wins.

### 9.1 First Bloom placement precedence

1. `COMMAND_SCHEMA_INVALID`
2. `STATE_VERSION_CONFLICT`
3. `INVALID_HAND_SLOT`
4. `EMPTY_HAND_SLOT`
5. `CATALOG_IDENTITY_MISMATCH`
6. `REFLECTION_LOCKED`
7. `INVALID_PIECE_ROTATION`
8. `GEOMETRY_TRANSFORMATION_ERROR`
9. `OUT_OF_BOUNDS_PLACEMENT`
10. `OCCUPIED_CELL_COLLISION`
11. `GAME_RULE_LEGALITY_VIOLATION`

## 10. Deterministic guards

Rule services MUST return typed failures rather than leak uncontrolled exceptions across the engine boundary.

```typescript
type QueryResult<T> =
  | { status: "SUCCESS"; data: T }
  | { status: "DETERMINISTIC_GUARD_EXCEEDED"; diagnostic: DiagnosticPayload };
```

Guard counts, traversal order, and failure codes are compatibility-critical. First Bloom enclosure is bounded to 64 node visits and 256 edge checks. Bloom cascade processing is bounded to eight sequential iterations; attempting a ninth causes rollback.

## 11. Ordering requirements

The following ordering MUST be explicit and canonical:

- command log order;
- pipeline phase order;
- topology neighbor order where it can affect output;
- coordinate order, row-major `(y,x)` for First Bloom;
- trigger and event order;
- organism resolution order;
- score component order;
- map/set projections before serialization.

Runtime collection iteration order MUST never determine authority.

## 12. Concurrency and optimistic versioning

Commands SHOULD carry an expected action count. If it differs from current authority, the command fails with a stable state-version conflict before any game-specific mutation. This provides local concurrency safety without introducing networking requirements.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
