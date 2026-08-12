# First Bloom Decision Register

## D-020 Bloom Predicate Selection

**Status:** ACCEPTED  
**Decision:** Bloom when connected organism size is at least six cells or a D-034 `ALL_NON_TRAVERSABLE` scoped query returns at least one causally attributed enclosed region. Moss and Mycelium count equally; no mandatory core. Resolve a frozen snapshot batch in ascending `MinCoord`. Cascade passes require a real accepted mechanic that changes membership/connectivity/qualification. Limit eight iterations; ninth attempt rolls back.

**Rejected:** pure size >=5, mandatory Mycelium, fixed 3x3 density.

## D-021 Scoring and Chain Semantics

**Status:** ACCEPTED  
**Decision:** +10 per member, +50 per newly awarded eligible enclosure coordinate, +100 for every Bloom after the first in the same command. Chain index is global across all Bloom batches for that action. One enclosure coordinate may score once per action. Checked signed 64-bit exact arithmetic; no floor; overflow rolls back. Score events remain per Bloom and causally linked.

**Rejected:** multiplicative chains, increasing staircase, decaying chains, cascade-only chains.

## D-022R Canonical Piece Catalog

**Status:** ACCEPTED  
**Decision:** ten stable piece definitions; 4x4 maximum local frame; reflection then rotation composition; material-sensitive transformation uniqueness; ruleset-controlled reflection unlock; structural placement payload; deterministic bag abstraction without selecting the RNG algorithm.

**Correction history:** supersedes the proposal that prohibited reflection, prematurely fixed Mulberry32, used geometry-only symmetry, contained an incorrect T rotation vector, and proposed ambiguous concatenated wire tokens.

## D-034 Enclosure Topology and Consequence Set

**Status:** ACCEPTED  
**Decision:** four-connected traversal and boundary continuity; traversable perimeter connects to virtual exterior; edge-assisted isolation of interior is possible; traversability and supporting-boundary policy are explicit; results preserve regions and subject/support boundaries; causal attribution uses subject removal; one immutable snapshot per query; row-major output; no scoring/mutation; bounded typed failure.

## D-025 Authoritative RNG Algorithm

**Status:** OPEN, ISOLATED  
**Candidate:** Mulberry32.  
**Blocked:** canonical seed-derived deck vectors and production random compatibility identity.  
**Not blocked:** Replay, transactions, placement, Bloom, scoring, serialization, and F1 fixtures using explicit deck sequences.

## D-026 Share Payload Cap

**Status:** OPEN, ISOLATED  
**Blocked:** normative 2 KB cap assertion.  
**Not blocked:** share schema, canonical serializer, structural encoder, compression pipeline, or replay import validation.

## D-031 Barren Consequence

**Status:** OPEN, ISOLATED  
**Accepted boundary:** a -10 Barren decay consequence exists conceptually and component kind `BARREN_CONSEQUENCE` is reserved.  
**Unresolved:** qualifying relationship, quantity, timing, post-consequence state, and whether it applies per cell, region, Bloom, or action.  
**Rule:** normative execution MUST NOT emit the component or infer an implicit subtraction until accepted.

## Decision dependency graph

```text
D-020 + D-021 + D-022R + D-034
    -> canonical First Bloom replay fixtures
    -> F1 exit evidence

D-031 -> Barren scoring fixtures -> full release DoD
D-025 -> canonical RNG deck vectors
Share schema/serializer -> structural encoder
D-026 -> normative 2 KB assertion
```

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
