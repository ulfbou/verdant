# Verification, Conformance and Fixture Specification

## 1. Suite isolation

### Verdant.ConformanceFixtures

Generic platform invariants: transaction rollback, deterministic replay, ActionLog order, serialization, hashing, compatibility, failure atomicity, topology guard handling, and side-effect isolation.

### FirstBloom.NormativeReleaseFixtures

Accepted First Bloom behavior: D-022R geometry, D-020 Bloom predicate, D-034 enclosure, D-021 scoring, accepted memory conversion, and complete execution through the public Verdant command boundary.

### FirstBloom.CandidateSimulationFixtures

D-025, D-031, and unaccepted experiments. These use the real engine but non-release RulePack identities. They MUST NOT enter release manifests, production dependency graphs, public replay identities, compatibility gates, or golden release vectors.

## 2. F1.1 Canonical fixture schema

Each fixture records identity and schema version, engine compatibility, ruleset, RulePack, catalog, seed, initial parameters, 64-cell initial board, explicit hand/deck input, score, memories, deterministic state, ActionLog, expected state/events/serialization/hashes as applicable.

Normative F1 uses `fixtureDeckSequence` and `fixtureDeckCursor`, not seed-derived RNG.

## 3. F1.2 Placement and validation

Required cases:

- valid anchor translation for P03 at (3,4);
- P08 T rotation normalization at (2,2);
- out-of-bounds P06 at (6,2) with full rollback;
- occupied collision;
- reflection locked;
- reflection composition when unlocked;
- invalid rotation without modulo repair;
- empty hand slot;
- invalid slot -1 and 3;
- stale action count;
- Preview/Execute equivalence.

Every success asserts state, exactly one log append, canonical placement event, hand replacement, cursor increment, and Replay equality. Every failure asserts total preservation and zero semantic events.

## 4. F1.3 Bloom, enclosure and scoring

All cases enter through `Execute(state, command, firstBloomRuleset)`:

1. exact size six Bloom, +60;
2. size five non-Bloom success;
3. sub-six enclosure-qualified Bloom;
4. open four-connected path, no enclosure;
5. edge-assisted enclosure with no perimeter cell emitted;
6. six cells plus one enclosure, +110;
7. two regions, five cells, +250 enclosure;
8. simultaneous dual Bloom, +60 then +160, total +220;
9. overlapping enclosure single award;
10. Barren receives +50 enclosure but no D-031 penalty.

Each success verifies board, score, hand, cursor, count, memories, capabilities, one ActionLog append, complete event identity/order/payload, and Replay(1) equality.

## 5. F1.4 Multi-action replay prefixes

**Status:** specification accepted, evidence pending.

Canonical run includes initial state plus five successful actions: ordinary placement, transformed placement, topology-altering placement, size Bloom, and post-Bloom placement. Every prefix is reconstructed independently from initialization.

Cases verify Replay(0), Replay(1), every-prefix structural equality, event-prefix equality, exact Bloom boundary, hand/cursor equality, rejected attempts absent from log, repeatable side-effect-free Replay, and explicit prefix-bound failures.

Equality before F1.6 is canonical structural equality of authoritative fields, not runtime object bytes.

## 6. F1.5 Consumer equivalence

**Status:** specification accepted, evidence pending.

Cases verify:

- Undo equals Replay of previous prefix;
- failed Undo preserves live state;
- Timeline state/events equal Replay at every position;
- Timeline scrubbing is read-only;
- reload equals fresh Replay;
- corrupted cache loses to Replay;
- historical Bloom boundary is exact;
- navigation is path-independent;
- presentation mode cannot change reconstructed state/events.

## 7. F1.6 serialization and hashes

**Status:** proposed; hashes pending.

Cases verify repeatable bytes, insertion-order independence, live/replay prefix state hash equality, live/replay event hash equality, cache exclusion, presentation exclusion, authoritative mutation sensitivity, and fixture-version decoupling.

Golden digests are generated only after schema freeze, byte inspection, and cross-runtime/vector verification.

## 8. Generic conformance requirements

The generic suite MUST include:

- accepted command commits state+log+random atomically;
- rejection consumes no randomness and emits no semantic events;
- diagnostics are out-of-band;
- command ordering and event ordering are stable;
- Replay starts from canonical initialization each invocation;
- Replay is deterministic and side-effect-free;
- invalid prefix bounds do not clamp;
- caches cannot become authority;
- canonical projection excludes host/experience state;
- compatibility mismatch is explicit;
- deterministic guards map to typed failures and total rollback;
- hash computation uses canonical bytes only.

## 9. Evidence policy

A specification status does not imply executable proof. Freeze requires passing fixtures, preserved command inputs and outputs, exact canonical bytes where applicable, deterministic digests, and reproducibility in an independent rerun. Evidence reports MUST identify engine, ruleset, RulePack, catalog, serializer, and fixture identities.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
