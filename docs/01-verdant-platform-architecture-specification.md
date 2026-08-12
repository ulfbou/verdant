# Verdant Platform Architecture Specification

## 1. Purpose and scope

Verdant provides reusable deterministic execution for turn-based or action-log-driven spatial simulations. The platform owns deterministic simulation, command processing, validation, resolution, triggers, state mutation, event production, replay, persistence contracts, topology services, state reconstruction, hashing, and compatibility.

Hosted games provide rules, content catalogs, topology definitions or selections, scoring, progression, presentation profiles, and game-specific mechanics.

### 1.1 Primary equation

```text
Engine + Ruleset + Initial Parameters + ActionLog Prefix = Authoritative State + Semantic History
```

For identical compatible inputs, every implementation MUST produce identical authoritative results across live execution, replay, undo reconstruction, timeline inspection, cold reload, and sharing.

### 1.2 Success criterion

First Bloom MUST be implementable without modifying Verdant Core internals. If a First Bloom concept is required in a generic Core type, the abstraction boundary is defective.

## 2. Architectural stack

```text
Host Product
  MOLD / Future Game A / Future Game B
        |
Verdant Runtime
  Session Manager
  Replay Manager
  Persistence Manager
  Compatibility Manager
  Event Dispatcher
        |
Verdant Engine
  Command Pipeline
  Validation / Resolution / Triggers
  State Mutation / Temporal Updates / End Conditions
  Random Services / Hashing / Replay Reconstruction
        |
Game Module
  Ruleset / RulePack / Catalog / Game Services / Presentation Profile
```

## 3. Package model

### 3.1 Verdant.Core

Pure deterministic domain and execution code:

- World, Entity, Definition, Command, Event
- Ruleset, RulePack, GameState, Session
- ActionLog, Replay, canonical projection and hashing contracts
- topology interfaces and deterministic query guards
- command transaction and failure contracts

Core MUST NOT reference UI frameworks, host storage, audio, haptics, localization, clocks, wall time, platform RNG, or game-specific nouns.

### 3.2 Verdant.Experience

Reusable non-authoritative experience orchestration:

- tutorials and scenario orchestration
- semantic input policy and presentation gates
- presentation sequence mapping
- history projection and Timeline coordination
- explanations and accessibility contracts
- playback modes and interruption policy

Experience MAY hold transient state but MUST NOT mutate or reinterpret authoritative game facts.

### 3.3 Verdant.Host

Platform integration boundaries:

- browser, desktop, mobile, console, headless runner
- storage implementations
- audio and haptic adapters
- viewport, localization, sharing and accessibility adapters
- lifecycle and dependency composition

### 3.4 Game packages

Recommended First Bloom package split:

```text
Mold.Game
Mold.Content
Mold.Tutorials
Mold.Presentation
Mold.Host.Web
```

## 4. Core domain model

Generic types include:

- `World`: topology-bound authoritative simulation space.
- `Entity`: instance with stable deterministic identity.
- `Definition`: immutable catalog data.
- `Command`: player/system intention submitted for evaluation.
- `Event`: resolved semantic fact emitted after successful resolution.
- `Ruleset`: executable deterministic behavior contract.
- `RulePack`: immutable configuration and constants.
- `GameState`: complete authoritative state projection.
- `Session`: current state, initialization, compatibility identities, and successful log.
- `ActionLog`: ordered successful commands only.
- `Replay`: side-effect-free reconstruction primitive.

## 5. Runtime components and responsibilities

### Session Manager

- owns the current authoritative session reference;
- serializes command evaluation through the transaction lock;
- commits state, log, and deterministic random state atomically;
- routes committed events and diagnostics to distinct channels.

### Replay Manager

- reconstructs any valid successful-command prefix from canonical initialization;
- provides state and semantic history to Undo, Timeline, reload, and sharing;
- never mutates live sessions or host services.

### Persistence Manager

- persists authority after in-memory commit;
- stores optional caches with explicit cache identity;
- reconstructs via Replay and rejects conflicting caches.

### Compatibility Manager

- validates all independent version dimensions;
- prevents silent reinterpretation;
- selects compatible ruleset/catalog/serializer implementations or returns incompatibility.

### Event Dispatcher

- receives committed semantic events only after state commit;
- supports presentation, history, accessibility, analytics adapters as non-authoritative consumers;
- isolates consumer failure from gameplay commitment.

## 6. Dependency rules

- Core MAY depend only on deterministic libraries and explicitly versioned algorithms.
- Experience MAY depend on Core contracts, never the reverse.
- Host MAY depend on Core and Experience, never the reverse.
- Game modules MAY implement Core interfaces and supply Experience profiles.
- Presentation adapters MUST NOT be callable from reducers for authoritative decisions.
- Candidate fixture assemblies MAY depend on production engine assemblies. Production assemblies MUST have zero dependency on candidate fixtures or experimental RulePacks.

## 7. Deployment and technology guidance

The archive recommends .NET/C# with browser, desktop, mobile, console, and headless hosts. This is implementation guidance rather than a platform semantic requirement. The deterministic contracts must remain portable and testable across runtimes. Avoid claiming NetStandard compatibility where a selected runtime API would prevent it; compatibility targets belong in build specifications.

## 8. Non-goals

Verdant MUST NOT add schema, types, or persisted hooks for:

- required gameplay backends;
- account systems or authentication;
- cloud-save backends or sync;
- remote leaderboards;
- hosted multiplayer or matchmaking.

First Bloom excludes Sanctuary, Rot, Hand Age, Prune, and Fossils. Authorized optional mechanics require RulePack gates. Future boundaries require explicit hooks. Unapproved variants require decision records.

## 9. Quality attributes

1. **Determinism:** stable results for compatible inputs.
2. **Atomicity:** all-or-nothing authoritative command outcomes.
3. **Replayability:** any valid prefix reconstructs independently.
4. **Isolation:** presentation and host failures cannot alter committed gameplay.
5. **Compatibility:** independent version dimensions and explicit mismatch handling.
6. **Extensibility:** game rules and content evolve outside Core.
7. **Explainability:** semantic events carry structured causal facts.
8. **Testability:** every invariant is observable through conformance or normative fixtures.
9. **Local-first operation:** normal gameplay does not require network services.
10. **Bounded execution:** deterministic guards prevent unbounded rule queries and cascades.

## 10. Minimum viable engine

The first implementable platform slice includes:

- Core domain contracts;
- command pipeline and atomic transaction boundary;
- Replay and ActionLog;
- event system;
- canonical projection and hashing interfaces;
- persistence authority contracts;
- SquareGrid topology;
- catalog loading and identity;
- First Bloom reference module;
- generic and game-specific fixture runners.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
