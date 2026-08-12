# Delivery, Governance and Implementation Plan

## 1. Workstreams and phases

### Governance/specification workstreams

- G1 Decision Closure Packets
- G2 Prototype Parity Audit
- G3 Harness and Fixture Architecture
- G4 Derived Engineering Reference

### Delivery phases

- F0 Foundation Shell
- F1 Thesis Slice
- F2 Complete Memory
- F3 Historical Product
- F4 Release Feel
- F5 Distribution

G1 blocks completion of F1 core rules. G2 informs F0/F1 scope. G3 begins in F0 and provides evidence. G4 stabilizes before F1 exit.

## 2. Immediate engineering sequence

1. Create package skeleton and dependency guards.
2. Implement generic immutable state and compatibility identities.
3. Implement command transaction and successful-only ActionLog.
4. Implement SquareGrid and deterministic query infrastructure.
5. Implement Replay from canonical initialization and prefix bounds.
6. Implement First Bloom catalog and placement transformation.
7. Implement hand replacement using explicit fixture deck provider.
8. Implement organism discovery, D-034 enclosure, D-020 Bloom, D-021 scoring.
9. Implement generic conformance harness.
10. Materialize F1.1-F1.3 fixtures.
11. Materialize canonical F1.4 run and all prefix assertions.
12. Implement Undo, Timeline, and reload as Replay consumers.
13. Implement canonical projection, JCS, UTF-8, SHA-256.
14. Inspect vectors and run independent reproducibility verification.
15. Freeze F1.4-F1.6 only after evidence passes.

## 3. Component backlog by package

### Verdant.Core

- compatibility identity model;
- command/result/error contracts;
- transaction coordinator;
- generic event envelope and deterministic IDs;
- ActionLog and initialization descriptor;
- replay projector and prefix validator;
- topology interfaces and SquareGrid;
- deterministic query guards;
- canonical projection/serializer/hash interfaces;
- persistence authority DTOs.

### Verdant.Experience

- presentation event mapper;
- input gate and playback mode;
- tutorial scenario coordinator;
- Timeline read model backed by Replay;
- structured explanations and accessibility announcements.

### Verdant.Host

- storage adapter and cache validator;
- audio/haptic/viewport/localization adapters;
- browser/desktop/headless composition roots;
- isolation and operational diagnostics.

### First Bloom

- catalog and piece transforms;
- command validation and placement;
- organism/component discovery;
- enclosure policy and attribution;
- Bloom batch/cascade coordinator;
- scoring and memory conversion;
- explicit fixture deck provider;
- canonical game-state adapter;
- normative fixture definitions.

## 4. Blocking policy

Only an unavailable existing prerequisite may block implementation. Open D-025, D-026, and D-031 are isolated and do not block generic Replay, transactions, serialization, or accepted First Bloom core fixtures. Their dependent evidence remains explicitly omitted.

## 5. Definition of Done

### Platform architecture DoD

- package boundaries are enforced by build dependencies;
- no First Bloom nouns exist in Verdant Core public types;
- one execution pipeline and one Replay authority exist;
- rollback and side-effect isolation are proven;
- compatibility identities prevent silent reinterpretation;
- storage caches lose to Replay;
- canonical serialization and hashes are reproducible.

### First Bloom F1 DoD

- F1.1-F1.3 pass;
- F1.4 every-prefix state and event equality pass;
- F1.5 Undo/Timeline/reload equivalence pass;
- F1.6 canonical bytes and golden hashes pass;
- unresolved D-025 and D-031 behavior is absent from release vectors;
- candidate assemblies are absent from production dependency and manifest scans.

## 6. Risk register

- **Generic-core leakage:** mitigate with type/package review and dependency tests.
- **Alternative reducers:** prohibit and test consumer calls to Replay.
- **Hidden randomness:** instrument consumption and assert rejection neutrality.
- **Unstable collection order:** canonicalize before resolution and serialization.
- **Premature golden hashes:** require byte inspection and schema freeze.
- **Candidate contamination:** separate assemblies, RulePack identities, manifests, and CI lanes.
- **Presentation authority drift:** compare Preview/Execute and disable presentation during headless proofs.
- **Cache authority drift:** inject intentionally corrupt snapshots in reload tests.

## 7. Evidence artifacts

G3 SHOULD produce:

- machine-readable fixture results;
- canonical input envelopes;
- per-prefix state/event projections;
- canonical UTF-8 byte files;
- SHA-256 digest manifest;
- engine/ruleset/RulePack/catalog/fixture identity manifest;
- candidate-isolation dependency report;
- cross-runtime or independent-rerun comparison report.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
