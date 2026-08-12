# Verdant Replay, Persistence, Serialization and Compatibility Specification

## 1. Replay authority

```typescript
Replay(
  ruleset,
  rulePack,
  initialParameters,
  actionLog,
  actionCount
): ReplayResult /* State, Events, Hash */
```

Replay MUST be deterministic, side-effect-free, authoritative, repeatable, and isolated from storage, audio, haptics, UI, analytics, and live session mutation.

Valid bounds are `0 <= actionCount <= ActionLog.Count`. Invalid counts return `INVALID_REPLAY_ACTION_COUNT`; implementations MUST NOT clamp.

## 2. Single reconstruction authority

```text
Replay -> Undo     (mutating consumer)
Replay -> Timeline (read-only consumer)
Replay -> Reload   (persistence consumer)
Replay -> Sharing  (portable-history consumer)
```

No consumer may contain a separate reducer.

### 2.1 Undo

Undo reconstructs the target prefix first. Only after successful reconstruction may it replace live state and truncate the active log. If reconstruction fails, the live session is unchanged.

### 2.2 Timeline

Timeline reconstructs states and event history by action count. Scrubbing is read-only. Returning to Live restores the current authoritative session directly.

### 2.3 Reload

Cold load reads compatibility identities, canonical initial inputs, explicit fixture/random inputs, and ActionLog, then invokes Replay to the log count.

## 3. Persistence authority

Authoritative persistence contains:

- engine compatibility identity;
- ruleset identity;
- RulePack identity and content digest as required;
- catalog identity;
- initial parameters;
- initial seed and deterministic state contract;
- explicit fixture deck input when used;
- ordered successful ActionLog.

Caches include snapshots, state images, statistics, previews, and spatial indices. Caches MAY accelerate loading but MUST be validated and MUST lose to Replay on conflict.

## 4. Commit ordering

1. Complete deterministic execution.
2. Acquire transaction lock.
3. Commit state.
4. Append exactly one successful command.
5. Commit deterministic random state.
6. Release lock.
7. Enqueue persistence.
8. Enqueue committed semantic events.

Storage failure after in-memory commit does not recalculate or roll back gameplay unless a separate explicit durability policy is adopted.

## 5. Compatibility dimensions

Version independently:

- `EngineVersion`
- `RulesetVersion`
- `RulePackVersion`
- `CatalogVersion`
- `SaveFormatVersion`
- `ReplayFormatVersion`
- `FixtureVersion`

A numeric RulePack change uses a new immutable RulePack identity. A semantic execution change requires a new ruleset compatibility identity. Serializer or fixture-envelope changes do not automatically change gameplay ruleset identity. Old data MUST be replayed under compatible historical identities or rejected explicitly.

## 6. Canonical serialization pipeline

```text
Authoritative object
-> Canonical projection
-> RFC 8785 JCS serialization
-> UTF-8 bytes
-> SHA-256
-> 64 lowercase hexadecimal digest
```

A hash is meaningless without the projection and serialization contract.

### 6.1 Profile

- object members ordered by JCS rules;
- arrays in domain-defined canonical order;
- unordered coordinates sorted row-major when applicable;
- exact integers only; authoritative floating point prohibited;
- strings encoded as canonical JSON strings;
- null only where schema permits;
- absence and explicit null remain distinct;
- enums use canonical symbolic values;
- maps and sets project to sorted arrays/objects;
- runtime insertion order is irrelevant.

### 6.2 Signed score representation

Scores serialize as signed base-10 strings: `"0"`, `"110"`, `"-10"`. No plus sign or leading zeros except zero itself.

## 7. Distinct hashes

- `CanonicalStateHash`: authoritative state at one action count; excludes caches, diagnostics, presentation, timestamps, and storage metadata.
- `CanonicalEventBatchHash`: ordered semantic events for one accepted action.
- `CanonicalReplayIdentityHash`: compatibility identities, canonical initial inputs, command prefix, and target action count.

Hashes are verification and identity tools. Replay inputs remain the reconstruction authority.

## 8. Canonical projection boundary

A generic projection contract may include schema version, action count, score representation, world/board adapter output, hand/inventory adapter output, game extension state, capabilities, terminal state, and deterministic state.

Verdant Core MUST not encode First Bloom nouns. First Bloom supplies a game-owned adapter that projects its board materials, memories, capabilities, and other authoritative game state into the game extension section.

## 9. Sharing boundary

Structural replay sharing may be implemented before D-026. The normative 2 KB cap remains blocked until payload evidence is accepted. Sharing MUST serialize canonical authoritative inputs, not cached state or presentation data. Imported shares MUST pass compatibility validation before Replay.

## 10. Corruption and incompatibility handling

- invalid cache: discard and reconstruct;
- invalid command prefix: typed replay failure, no partial state;
- unavailable compatible ruleset/catalog: explicit incompatibility;
- malformed canonical payload: typed format failure;
- hash mismatch: treat cache/share as untrusted and do not silently reinterpret;
- unknown fields: behavior governed by the versioned format, never ad hoc.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
