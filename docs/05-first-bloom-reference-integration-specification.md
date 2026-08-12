# First Bloom Reference Integration Specification

## 1. Purpose

First Bloom proves that a domain-rich game can run on Verdant without contaminating Core. All Bloom, organism, material, memory, piece, scoring, and progression concepts are game-owned.

## 2. Game profile

- topology: 8x8 SquareGrid;
- adjacency: orthogonal four-connectivity;
- authoritative materials include Empty, Moss, Mycelium, and Stone;
- memory includes Fertile and Barren while cell occupancy remains a distinct concept;
- active hand: three slots;
- successful placement draws exactly one replacement into the vacated slot;
- explicit fixture deck input is used by normative F1 fixtures pending D-025.

## 3. Placement command

```typescript
interface PlacePieceCommand {
  type: "PLACE_PIECE";
  expectedActionCount: number;
  slot: number;
  diagnosticPieceId?: string;
  anchorCoordinate: { x: number; y: number };
  rotation: number;
  isReflected: boolean;
}
```

`slot` is authority. The engine/ruleset resolves `activeHand[slot]`. A diagnostic ID cannot substitute another piece.

Transformation order is frozen:

```text
Canonical Definition -> Optional Reflection -> Normalize
-> Rotate R times clockwise 90 degrees -> Normalize -> Anchor Translation
```

Clockwise rotation: `u'=-v, v'=u`, then subtract minima. Horizontal reflection: `u'=(W-1)-u, v'=v`.

## 4. Catalog

| ID | Name | Cells | Transformations | Weight | Unlock / Tags |
|---|---|---|---:|---:|---|
| P01_MONO | Monomino | (0,0,MOSS) | 1 | 10 | default; starter,filler |
| P02_DUO_M | Moss Duo | (0,0,MOSS),(1,0,MOSS) | 2 | 15 | default; starter,linear |
| P03_DUO_C | Core Duo | (0,0,MOSS),(1,0,MYCELIUM) | 4 | 15 | default; core,linear |
| P04_TRIO_I | Straight Trio | three horizontal Moss | 2 | 12 | default; linear |
| P05_TRIO_L | Corner Trio | (0,0),(0,1),(1,1) Moss | 4 | 12 | default; corner |
| P06_TETRA_I | Long Tetra | four horizontal Moss | 2 | 8 | default; linear,heavy |
| P07_TETRA_O | Square Tetra | 2x2 Moss | 1 | 8 | default; block |
| P08_TETRA_T | T-Tetra | Moss, Mycelium center, Moss; stem Moss | 4 | 8 | default; core,branch |
| P09_TETRA_L | L-Tetra | vertical three plus foot | 8 | 6 | unlock after 3 Blooms; corner,heavy |
| P10_TETRA_S | Step Tetra | S geometry | 4 | 6 | unlock after 3 Blooms; complex |

Transformation uniqueness compares `(u,v,material)`, not geometry alone. Reflection capability is controlled by `canReflect`; attempting reflection while locked returns `REFLECTION_LOCKED`.

## 5. Hand replenishment

On success:

1. remove selected slot;
2. draw one item at `fixtureDeckCursor` or through the accepted deck provider;
3. write it into the same slot;
4. do not reorder other slots;
5. increment cursor/consume random exactly once.

On rejection, hand and cursor/random state are unchanged.

## 6. Enclosure semantics (D-034)

### 6.1 Traversability

Traversable: Empty, Fertile memory, Barren memory.  
Boundary: Moss, Mycelium, Stone, foreign organism.  
Accepted policy: `ALL_NON_TRAVERSABLE`.

### 6.2 Board edge

The board exterior lies beyond x in {-1,8} or y in {-1,8}. Every traversable perimeter cell connects to virtual exterior and cannot be enclosed. Non-traversable perimeter cells may combine with the edge limit to isolate interior cells.

### 6.3 Query

```typescript
type SupportingBoundaryPolicy =
  | "SUBJECT_ONLY"
  | "SUBJECT_PLUS_STONE"
  | "ALL_NON_TRAVERSABLE";

interface EnclosureQuery {
  evaluationBoard: BoardState;
  subjectBoundaryCoordinates: Coordinate[];
  supportingBoundaryPolicy: SupportingBoundaryPolicy;
}

interface EnclosureRegion {
  regionId: string; // reg_{minY}_{minX}
  enclosedCoordinates: Coordinate[];
  subjectBoundaryCoordinates: Coordinate[];
  supportingBoundaryCoordinates: Coordinate[];
  touchesBoardBoundary: boolean;
}
```

A region is attributed to subject O only if it is exterior-unreachable with all boundaries and becomes reachable when O is reclassified as traversable. Output is row-major sorted and deduplicated. The virtual edge is not emitted.

D-034 evaluates one immutable caller-supplied snapshot. It does not score or mutate. Guard limits are 64 nodes and 256 edge checks.

## 7. Bloom semantics (D-020)

An organism qualifies if:

```text
member count >= 6
OR
at least one D-034 region is causally attributed to it
```

Moss and Mycelium both count. Mycelium is not mandatory.

After an action, capture `S_batch`, discover all organisms, freeze qualifiers, and sort by `MinCoord(O)=min(y*8+x)`. Resolve sequentially while confirming members remain. After a batch, a new snapshot may be evaluated only when an accepted mechanic changed membership, connectivity, or qualification facts. Base clearing alone produces no new cascade.

Maximum cascade iterations per action: 8. Attempting a ninth returns `DETERMINISTIC_GUARD_EXCEEDED` and rolls back.

## 8. Scoring semantics (D-021)

For Bloom `Bi`:

```text
BaseScore       = 10 * memberCoordinates.Count
EnclosureScore  = 50 * newly awarded eligible coordinates
ChainBonus      = 0 for actionBloomIndex 0, otherwise 100
Total           = sum of components and accepted consequences
```

Eligibility requires a D-034 attributed coordinate that was traversable in the evaluation snapshot and has not been awarded earlier in the same action.

A coordinate scores enclosure at most once per action. Ownership goes to the earliest Bloom by cascade depth and canonical organism order. Later Blooms retain structural geometry but receive no duplicate +50.

Scoring uses checked signed 64-bit exact integers, allows negative totals, uses no floor, consumes no RNG, and returns `SCORE_ARITHMETIC_OVERFLOW` on overflow with full rollback.

Component order:

1. BASE_CELLS
2. ENCLOSURE
3. CHAIN
4. FERTILE_USE
5. BARREN_CONSEQUENCE
6. OTHER_ACCEPTED_ADJUSTMENT

Only non-zero components are emitted. `BARREN_CONSEQUENCE` is reserved but inactive pending D-031.

## 9. Semantic event order

Per action, placement emits first. Per Bloom:

```text
PIECE_PLACED
ORGANISM_BLOOMED
SCORE_CHANGED
Memory/Ground conversion events
```

For several Blooms, each Bloom event is immediately followed by its score event before the next Bloom pair.

Deterministic identities:

- `evt_placed_{actionCount}`
- `evt_bloom_{actionCount}_{organismId}`
- `evt_score_{actionCount}_{actionBloomIndex}`

## 10. Integration boundary

Public Verdant APIs remain generic. First Bloom invokes its enclosure, organism, Bloom, scoring, and memory services through ruleset resolution. Consumers call `verdant.Execute(state, command, firstBloomRuleset)`, not public Core methods named after First Bloom mechanics.

---

**Source basis:** Exhaustive synthesis of the uploaded `verdant.md` brainstorming archive. Superseded proposals were reconciled in favor of later accepted corrective decisions and frozen milestone statements.
