# Verdant Specification Suite

**Status:** Authoring baseline derived from the Verdant brainstorming archive  
**Purpose:** Provide a complementary, implementation-enabling specification set for the reusable Verdant deterministic simulation platform and its First Bloom reference integration.  
**Normative vocabulary:** **MUST**, **MUST NOT**, **SHOULD**, **MAY**.

## 1. Executive decision

Verdant is a reusable **Deterministic Spatial Simulation Engine (DSSE)**, not a MOLD-specific engine. It hosts First Bloom and future games with related architectural needs: ecosystem building, cellular-growth puzzles, tile placement, abstract strategy, replay sharing, and local-first board simulation.

> **Platform invariant:** Canonical initial inputs plus an ordered successful-command prefix are sufficient and authoritative to reproduce complete game state and semantic history. Replay, Undo, Timeline, reload, and sharing MUST derive from one reconstruction authority. Alternative historical reducers are forbidden.

First Bloom is the reference integration that proves the contract. It is not the source of Verdant Core abstractions.

## 2. Document map

1. **01-verdant-platform-architecture-specification.md**  
   Product vision, boundaries, layers, packages, component model, dependency rules, non-goals, and implementation architecture.
2. **02-verdant-deterministic-execution-contracts.md**  
   Command pipeline, transactions, validation, events, topology, rulesets, randomness, preview, failure contracts, and deterministic ordering.
3. **03-verdant-replay-persistence-compatibility-specification.md**  
   Replay authority, historical consumers, persistence authority, canonical serialization, hashing, version compatibility, and sharing boundaries.
4. **04-verdant-experience-host-specification.md**  
   Tutorial, presentation, input, explanation, accessibility, adapter, playback, and failure-isolation contracts.
5. **05-first-bloom-reference-integration-specification.md**  
   First Bloom domain mapping, placement, catalog, enclosure, Bloom, scoring, hand/deck, events, and integration boundaries.
6. **06-first-bloom-decision-register.md**  
   Frozen decisions D-020, D-021, D-022R, D-034 and isolated open decisions D-025, D-026, D-031.
7. **07-verification-conformance-and-fixture-specification.md**  
   Three-suite test architecture, F1.1-F1.6 fixtures, freeze gates, generic conformance, evidence requirements, and cross-runtime verification.
8. **08-delivery-governance-and-implementation-plan.md**  
   G1-G4 workstreams, F0-F5 phases, dependency graph, evidence pipeline, implementation order, and Definition of Done.
9. **09-contract-catalog-and-traceability.md**  
   Consolidated interfaces, schemas, error codes, event identities, invariants, glossary, ownership matrix, and traceability map.

## 3. Authority and interpretation rules

- Accepted and frozen decisions override earlier proposals in the archive.
- Corrective revisions override the superseded text they explicitly correct.
- Open decisions are isolated and MUST NOT be inferred by production code or normative fixtures.
- Game-specific terms such as Bloom, Fertile, Barren, Moss, Mycelium, and Stone MUST NOT enter Verdant Core domain types.
- Presentation MUST consume engine facts and MUST NOT calculate gameplay.
- Caches are optional accelerators and never authority.
- A successful command contributes exactly one ActionLog entry. Rejected commands contribute none.
- Diagnostics are out-of-band and are not semantic history.

## 4. Implementation-enablement checklist

The suite is sufficient to begin implementation when teams can map every implementation unit to:

- an owning package and layer;
- an authoritative input/output contract;
- deterministic ordering and failure behavior;
- a compatibility identity;
- a normative fixture or conformance assertion;
- an explicit boundary between generic platform and First Bloom;
- an accepted decision or an isolated open decision.

## 5. Current governance snapshot

- D-020 Bloom predicate: **ACCEPTED**
- D-021 scoring and chain: **ACCEPTED**
- D-022R canonical piece catalog: **ACCEPTED**
- D-034 enclosure topology: **ACCEPTED**
- D-025 RNG algorithm: **OPEN, ISOLATED**
- D-026 payload cap: **OPEN, ISOLATED**
- D-031 Barren consequence semantics: **OPEN, ISOLATED**
- F1.1-F1.3: **ACCEPTED & FROZEN**
- F1.4-F1.5: **SPECIFICATION ACCEPTED, EXECUTABLE EVIDENCE PENDING**
- F1.6: **SPECIFICATION PROPOSED, GOLDEN HASHES PENDING**

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
