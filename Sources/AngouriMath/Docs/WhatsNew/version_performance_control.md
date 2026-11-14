We compare performances through different versions with CommonFunctionsInterVersionTest.cs in Tests.

Units: nanoseconds

Each column is headed by how far along the history of `master` the version measured is,
counting the first commit as the 1st, and links to that commit. To find the number for a
new column, count the commits up to the one being measured:

```
git rev-list --count <commit>
```

**Columns are not measured on one machine, so read a row across it as a trend rather than
as a ratio.** The 1620th column was taken on a GitHub-hosted `ubuntu-latest` runner by the
Kernel Benchmark workflow; the earlier ones were taken on contributors' own machines. A
change of a few per cent between neighbouring columns says nothing at all. The order-of-
magnitude moves are what this file is for.

The 1620th column is also the first since the 1446th, because the benchmark workflow filtered
on a path that does not exist and had not run since 2026-01-02 -- ninety-eight merged pull
requests went unmeasured. Fixed in
[#775](https://github.com/asc-community/AngouriMath/issues/775).

**Allocation is reported alongside the timings** from the 1671st column on. The
benchmark class has carried `[MemoryDiagnoser]` all along, so the figure was measured on every
run and then discarded, because the reporting code asked for `Mean`, `Error` and `StdDev` and
nothing else. Asked for by [#167](https://github.com/asc-community/AngouriMath/issues/167).

**`EvalTrig` and `EvalTrigPrecise`** are new in the same column and are the other half of
[#167](https://github.com/asc-community/AngouriMath/issues/167): what arbitrary precision costs
in trigonometry. Both evaluate `sin(1) + cos(1) + tan(1)`, the first at the default hundred
digits and the second at five hundred. Measured while writing them, on one machine, so treat
these as the shape rather than as the numbers:

| digits | per evaluation |
|---|---|
| 100 (default) | 3.8 ms |
| 200 | 10.2 ms |
| 500 | 25.4 ms |
| 1000 | 146 ms |

**Read `EvalEasy` with care.** It evaluates one `Entity` instance, and an `Entity` caches its
own `Evaled`, so from the moment that cache landed the row stopped measuring evaluation and
started measuring a lookup -- which is what the fall to single-digit nanoseconds is, not a
speed-up of the arithmetic. Measured directly: a hundred thousand evaluations of one instance
take a millisecond in total, while a thousand evaluations of freshly built nodes take a second.
The row is kept as it is so the column history stays comparable, and the two trigonometric
benchmarks build their expression afresh on every call for exactly this reason.

---

## The gate, and when the baseline may be updated

This file is a history a person reads. `Sources/Tests/DotnetBenchmark/performance-baseline.json`
is the same measurement in the form a build can fail on, which is what
[#746](https://github.com/asc-community/AngouriMath/issues/746)'s standing condition -- *speed and
memory on popular use cases are measured, not hoped for* -- asks for and what
[#529](https://github.com/asc-community/AngouriMath/issues/529) asked for before it. The Kernel
Benchmark workflow runs the benchmarks and then `dotnet run -c Release PerformanceGate`, which
compares that run against the committed file.

**It gates allocation and not time, because only one of the two is a property of the code.**
Allocated bytes per operation is what the program asked the allocator for: the same code on the
same input asks for the same number of bytes. Wall-clock time on a GitHub-hosted runner is a
property of whoever else is on that host. Measured before choosing the thresholds -- all eighteen
benchmarks, three consecutive runs of one unchanged build, on a machine deliberately left loaded
(eight cores, load average around 16, which is closer to a shared runner than an idle desktop is):

| | run to run |
|---|---|
| allocation, the sixteen gated benchmarks | at most **0.033%** apart -- six bytes on eighteen kilobytes, for `ParseEasy` -- and identical to the byte on seven of them |
| mean time | up to **51.8%** apart, and 8-10% on nine of the eighteen |

Three orders of magnitude between the two is the whole argument. Allocation is held to 3%, a
hundred times the observed spread, and the wall clock fails only above 3.00x, which is a
catastrophe threshold and deliberately catches nothing smaller. A gate that is red for reasons that
are not the change is a gate people learn to ignore, and that is worse than not having one.

**Two benchmarks are excluded by name.** `CompileEasy` and `CompileHard` moved 1.0% and 3.6% over
those same three runs, because what they measure includes the runtime building and JIT-compiling a
delegate, which is not reproducible the way the library's own allocation is. They are listed in the
baseline's `ungated` map with that reason, and the gate prints the reason every time it runs -- an
exclusion is a decision somebody made, not a benchmark that happens to be missing from the file.

**It fails when a number gets better, too**, exactly as the corpus gate does and for the same
reason: a baseline that overstates what the library allocates silently permits giving the gain
back. The failure says `IMPROVED` rather than `REGRESSED`, and says what to do about it.

### How to update it

The run writes its numbers in the baseline's own format, so updating the baseline is a copy rather
than a transcription:

```
cd Sources/Tests/DotnetBenchmark
dotnet run -c Release CommonFunctionsInterVersion
cp benchmark_results.csv/measured-CommonFunctionsInterVersion.json performance-baseline.json
dotnet run -c Release --no-build PerformanceGate
```

The same file is in the `benchmark-results-<sha>` artifact of the CI job -- including of the job
that just failed, which is why the upload step runs `if: always()`. That copy is the better source
of the two, because it was taken on the machine and the runtime the gate is checked against.

### When updating it is legitimate

- **The allocation went down.** Record it, in the same change. This is the case the gate exists to
  stop being absorbed, and it is the only one where a red build means nothing is wrong.
- **The allocation went up and buys something.** [#918](https://github.com/asc-community/AngouriMath/pull/918)
  is the worked example, below in this file: the polynomial layer made `SolveMedium` allocate 19%
  more and made an incomplete root set complete. Say which in the pull request, next to the
  `BREAKING-CHANGES.md` entry that a changed answer already owes.
- **A benchmark was added or removed.** The gate reports `NEW` or `MISSING`, and the baseline
  follows in the same change.
- **The runtime moved under it.** Allocation is a property of the code *and* of the BCL beneath it,
  so a .NET version bump can move a row with nothing here having changed. The baseline records the
  `runtime` and `machine` it was taken on, and the gate prints both against the run's own, so check
  those two lines before concluding this -- and say so in the commit, because it is the one reason
  on this list that leaves no trace in the diff.

And when it is not: **because the build is red and the number is inconvenient.** The gate names the
benchmark and the size of the move. A move nobody can explain is the finding, not the obstacle.

---

|          Method |         [331st](https://github.com/asc-community/AngouriMath/commit/10e6e5a90e7270336b68dc5fd6aa36f3e0e65d2b) |          [380th](https://github.com/asc-community/AngouriMath/commit/73ae36488ddb863c1d6f35db5ed2f5dcf1484a26) |     [391st](https://github.com/asc-community/AngouriMath/commit/c7e08e6936bfdc2373377bec81ffd160e406244f)      |         [410th](https://github.com/asc-community/AngouriMath/commit/20814936bc740a9f410af4a4368e9895eab7aaf7) |           [483rd](https://github.com/asc-community/AngouriMath/commit/355963dcdf0ff9da568e9f1144ad2b7b68c19584) |         [520th](https://github.com/asc-community/AngouriMath/commit/70aa71acb73307c9f7df0aac006faae31b06058c) |         [690th](https://github.com/asc-community/AngouriMath/commit/5cc894939cb3657f0aa7ef5a25fd55011058929f) |         [826th](https://github.com/asc-community/AngouriMath/commit/87e33ec3590a95dd4ec59ff5c1f77064a64196d1) |         [914th](https://github.com/asc-community/AngouriMath/commit/6134338df083a908369b6bcfb69e70a4269ec51b) |         [920th](https://github.com/asc-community/AngouriMath/commit/501a0a3a9b2e07cddf92c4446b73ad6e2748253a) |        [1034th](https://github.com/asc-community/AngouriMath/commit/a3f48b47795b2dc2b3435152989a6e15639a65b4) |        [1066th](https://github.com/asc-community/AngouriMath/commit/a33746651f56a380b6c17913aa844f162f258d8c) |        [1090th](https://github.com/asc-community/AngouriMath/commit/9530c5b04484e98023941f7693bbeb1a3282cee6) |           [1446th](https://github.com/asc-community/AngouriMath/commit/2abb2b537c03977281f3fc2cab1da2c78c36a5f5) | [1620th](https://github.com/asc-community/AngouriMath/commit/e05d71797f53a9ac7dbdad6075cd7302a91036e7) | [1671st](https://github.com/asc-community/AngouriMath/commit/253b5a8d598a9a1d721b777340bd53ae3be12f99) | [1769th](https://github.com/asc-community/AngouriMath/commit/3ac24bc241e898509533bc980ec5dc91c5d79a07) | [1844th](https://github.com/asc-community/AngouriMath/commit/55cd1a58) | [2943rd](https://github.com/asc-community/AngouriMath/commit/842e8e9517ac0db3f3c31b906f197b3480fd07ee) |
|---------------- |--------------:|---------------:|---------------:|--------------:|----------------:|--------------:|--------------:|--------------:|--------------:|--------------:|--------------:|--------------:|--------------:|-----------------:|--------------:|--------------:|--:| --------------: |--:|
|       ParseEasy |        28,599 |         73,669 |        134,120 |        44,328 |          54,675 |        21,722 |        32,212 |        32,138 |        34,702 |        32,199 |        33,008 |        27,483 |        33,043 |        34,664 | 11,760 ns | 10,857 ns | 10,080 ns |           6,846 ns | 11,055 ns |
|       ParseHard |        92,037 |        208,710 |        415,440 |       178,760 |         287,865 |       209,853 |       624,769 |       698,094 |     4,272,898 |     3,862,375 |     4,731,275 |     4,051,823 |     5,284,613 |     5,657,058 | 2,617,362 ns | 2,493,337 ns | 2,235,852 ns |       1,621,902 ns | 53,198 ns |
|    SimplifyEasy |     6,225,707 |     16,387,374 |     41,081,822 |       397,973 |       2,594,367 |       168,462 |       225,393 |       281,626 |       122,809 |        79,184 |        94,505 |        86,478 |       102,600 |        87,759 | 153,704 ns | 152,631 ns | 136,841 ns |         111,852 ns | 95,565 ns |
|    SimplifyHard | 3,245,613,128 |  8,120,319,273 |  9,715,629,251 | 7,477,089,153 |  13,529,147,530 | 3,015,302,746 | 3,162,675,550 | 3,826,831,860 | 4,024,002,060 | 2,633,113,653 | 3,262,112,233 | 4,540,783,876 | 4,639,170,780 | 4,880,264,786 | 2,848,037,261 ns | 2,916,127,943 ns | 2,489,408,789 ns |   1,762,805,433 ns | 166,915,182 ns |
|        Derivate |         5,550 |         39,895 |        161,507 |        50,858 |          56,255 |        37,702 |        46,083 |        86,751 |        68,604 |        41,388 |        58,340 |        48,376 |        54,228 |        60,425 | 26,808 ns | 25,629 ns | 23,674 ns |          16,507 ns | 29,118 ns |
|       SolveEasy |       677,259 |      4,066,049 |     33,760,616 |     7,556,902 |     124,888,489 |    38,935,084 |    42,101,926 |    43,102,578 |    43,778,486 |    42,818,770 |    42,230,213 |    35,415,096 |    45,032,006 |    45,587,157 | 21,829,186 ns | 21,322,368 ns | 8,530,148 ns |       6,240,038 ns | 447,578 ns |
| SolveEasyMedium |       192,319 |        741,114 |      2,861,751 |       646,063 |         789,993 |       124,082 |       154,729 |       142,744 |       138,836 |        78,712 |       110,144 |        88,021 |       106,430 |       108,175 | 48,368 ns | 48,863 ns | 50,203 ns |          37,304 ns | 38,390 ns |
|     SolveMedium |    22,487,990 |    126,650,858 |    690,232,457 |   178,691,349 |     350,752,882 |     2,961,363 |     2,174,299 |     2,108,050 |     2,248,458 |     2,449,733 |     2,996,001 |     2,661,744 |     3,272,498 |     4,409,082 | 783,737 ns | 796,814 ns | 696,969 ns |         510,664 ns | 497,053 ns |
| SolveMediumHard |       > 1 h   |       > 1 h    | 12,033,099,074 | 2,754,157,261 |   5,779,298,835 |   240,930,082 |   258,560,720 |   309,944,856 |   346,532,318 |   258,799,676 |   305,404,083 |   354,286,153 |   409,437,694 |   453,280,166 | 124,316,631 ns | 129,750,583 ns | 117,103,179 ns |      94,087,468 ns | 14,234,151 ns |
|       SolveHard |       > 1 h   |       > 1 h    | 21,565,808,100 | 5,636,112,783 | 100,663,275,757 | 1,862,768,773 | 2,532,972,635 | 3,000,655,673 | 3,606,412,386 | 2,425,286,433 | 2,918,112,980 | 3,162,162,893 | 3,353,403,193 | 3,494,150,836 | 1,134,532,585 ns | 1,365,230,469 ns | 1,144,927,429 ns |     887,253,553 ns | 118,299,251 ns |
|        EvalEasy |        12,059 |        200,766 |      1,548,901 |       550,676 |       3,354,484 |            34 |            72 |            68 |            84 |            28 |            11 |            11 |            13 |             9 | 2 ns | 2 ns | 1 ns |               2 ns | 4 ns |
|        EvalTrig |             -- |              -- |              -- |            -- |               -- |            -- |            -- |            -- |            -- |            -- |            -- |            -- |            -- |               -- |               -- | 1,344,115 ns | 1,280,227 ns |         977,789 ns | 60,728 ns |
| EvalTrigPrecise |             -- |              -- |              -- |            -- |               -- |            -- |            -- |            -- |            -- |            -- |            -- |            -- |            -- |               -- |               -- | 43,285,515 ns | 37,048,158 ns |      29,174,367 ns | 726,611 ns |
| EvalPolynomialFresh | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | 8,143 ns |
| EvalTranscendentalFresh | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | 30,802 ns |
| EvalPolynomialFresh15Digits | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | -- | 9,104 ns |
|     CompileEasy |        24,771 |         38,300 |         62,328 |        33,684 |          40,751 |         9,208 |        15,467 |         5,487 |         5,623 |         6,283 |         6,254 |         5,143 |       565,597 |       589,540 | 352,800 ns | 352,449 ns | 314,521 ns |         215,593 ns | 407,752 ns |
|     CompileHard |        61,572 |         84,042 |        139,499 |        73,738 |          96,203 |        18,274 |        26,390 |        14,782 |        15,657 |        17,986 |        17,672 |        14,902 |     1,234,995 |     1,321,375 | 595,593 ns | 596,664 ns | 585,717 ns |         424,923 ns | 509,801 ns |
|         RunEasy |           138 |            126 |            123 |           117 |             128 |           157 |           170 |           165 |           175 |           167 |           161 |           149 |            48 |            43 | 42 ns | 42 ns | 28 ns |              21 ns | 49 ns |
|       RunMedium |           833 |            950 |            920 |           844 |             838 |         1,008 |         1,081 |         1,064 |         1,155 |         1,089 |           982 |           875 |           630 |           549 | 318 ns | 320 ns | 238 ns |             181 ns | 303 ns |
|         RunHard |         1,403 |          1,644 |          1,558 |         1,427 |           1,391 |         1,974 |         2,102 |         2,037 |         2,046 |         2,044 |         1,855 |         1,655 |         1,041 |           928 | 586 ns | 582 ns | 433 ns |             318 ns | 505 ns |
																													   


## Allocation, from the 1671st

Bytes allocated per operation, measured in the same run as the timing column of the same name
above. The 1671st is the first version measured for it, so the history is shorter than the
timings'; later runs add to it the same way.

| Method | [1671st](https://github.com/asc-community/AngouriMath/commit/253b5a8d598a9a1d721b777340bd53ae3be12f99) | [1769th](https://github.com/asc-community/AngouriMath/commit/3ac24bc241e898509533bc980ec5dc91c5d79a07) | [1844th](https://github.com/asc-community/AngouriMath/commit/55cd1a58) | [2943rd](https://github.com/asc-community/AngouriMath/commit/842e8e9517ac0db3f3c31b906f197b3480fd07ee) |
|---|--:|--:| --: |--:|
| ParseEasy | 18,052 B | 18,040 B | 18,109 B | 18,424 B |
| ParseHard | 3,486,004 B | 3,522,411 B | 3,522,458 B | 59,927 B |
| SimplifyEasy | 127,578 B | 128,096 B | 128,096 B | 79,785 B |
| SimplifyHard | 3,710,538,008 B | 3,620,663,136 B | 3,632,010,176 B | 161,716,160 B |
| Derivate | 52,821 B | 52,822 B | 52,822 B | 57,227 B |
| SolveEasy | 20,213,448 B | 8,852,065 B | 8,853,472 B | 448,295 B |
| SolveEasyMedium | 79,613 B | 95,788 B | 97,192 B | 66,370 B |
| SolveMedium | 553,368 B | 658,051 B | 661,915 B | 457,712 B |
| SolveMediumHard | 152,243,208 B | 162,301,856 B | 164,544,992 B | 1,362,652 B |
| SolveHard | 1,341,716,112 B | 1,431,834,672 B | 1,446,779,520 B | 9,530,109 B |
| EvalEasy | none | none | none | none |
| EvalTrig | 1,341,378 B | 1,341,378 B | 1,341,377 B | 69,888 B |
| EvalTrigPrecise | 12,743,208 B | 12,742,256 B | 12,742,216 B | 336,498 B |
| EvalPolynomialFresh | -- | -- | -- | 12,392 B |
| EvalTranscendentalFresh | -- | -- | -- | 34,640 B |
| EvalPolynomialFresh15Digits | -- | -- | -- | 11,920 B |
| CompileEasy | 16,200 B | 16,139 B | 10,952 B | 14,898 B |
| CompileHard | 37,944 B | 37,793 B | 20,377 B | 28,760 B |
| RunEasy | none | none | none | none |
| RunMedium | none | none | none | none |
| RunHard | none | none | none | none |

**`EvalEasy` allocating nothing is the proof of the caching note above**, rather than a further
argument for it. Evaluating `1 + 2 + log(2, 3) + sqrt(4) - 4 ^ 7 + e * pi` cannot allocate zero
bytes; a dictionary lookup can. `RunEasy`, `RunMedium` and `RunHard` allocate nothing for the
honest reason -- a compiled delegate over `Complex` has nothing to put on the heap.

---

## The 2664th and the 2943rd, measured together on one runner — the pair for 2.7.0

The pair the release checklist owes: `v2.6.0`
([`1a72d4da`](https://github.com/asc-community/AngouriMath/commit/1a72d4da351a0666675ef09991138b4f8222707a), the 2664th)
re-measured beside
[`842e8e95`](https://github.com/asc-community/AngouriMath/commit/842e8e9517ac0db3f3c31b906f197b3480fd07ee), the 2943rd,
which is the last change to the kernel before the release. The period is the whole of 2.7.0, 279 commits.

Both columns come from one run of the Kernel Benchmark Across Key Commits workflow,
[run 43536393809](https://github.com/asc-community/AngouriMath/actions/runs/43536393809), on 2026-11-14, on one
`ubuntu-latest` runner. The list was the two commits twice, interleaved — `v2.6.0`, the 2943rd, `v2.6.0`, the 2943rd —
so that the runner's own spread on an unchanged commit is measured in the same run as the change. The 2943rd column added
to the two tables at the top of this file is the Kernel Benchmark's run on the same commit,
[run 43534063686](https://github.com/asc-community/AngouriMath/actions/runs/43534063686).

Three controls, all on allocation:

- **Repeat.** Each commit's two measurements agree to within 0.03% on every gated row, and to the byte on twelve of the
  nineteen, four of those being the rows that allocate nothing.
- **Across machines.** The `v2.6.0` kernel is the 2662nd's — no kernel file changed between them — and the 2662nd was
  measured on 2026-10-22 on one desktop machine, in the pair for 2.6.0 below. The two agree on every gated row to within
  0.13% (`Derivate`, 56,247 there and 56,174 here), and to the byte on five.
- **Across workflows.** The 2943rd column agrees with the Kernel Benchmark's run on the same commit to within 0.10% on
  every gated row.

### Allocation

| benchmark | v2.6.0 | 2943rd | change |
|---|--:|--:|--:|
| `CompileEasy` | 10,971 | 14,594 | **+33.02%** |
| `CompileHard` | 20,397 | 29,063 | **+42.49%** |
| `Derivate` | 56,174 | 57,216 | **+1.85%** |
| `EvalPolynomialFresh` | 12,536 | 12,392 | −1.15% |
| `EvalPolynomialFresh15Digits` | 12,064 | 11,920 | −1.19% |
| `EvalTranscendentalFresh` | 34,760 | 34,640 | −0.35% |
| `EvalTrig` | 69,944 | 69,888 | −0.08% |
| `EvalTrigPrecise` | 336,554 | 336,498 | −0.02% |
| `ParseEasy` | 18,418 | 18,442 | +0.13% |
| `ParseHard` | 60,023 | 59,927 | −0.16% |
| `SimplifyEasy` | 79,681 | 79,785 | +0.13% |
| `SimplifyHard` | 157,897,000 | 161,715,872 | **+2.42%** |
| `SolveEasy` | 453,423 | 448,301 | −1.13% |
| `SolveEasyMedium` | 66,602 | 66,369 | −0.35% |
| `SolveHard` | 10,001,696 | 9,530,109 | **−4.72%** |
| `SolveMedium` | 458,872 | 457,711 | −0.25% |
| `SolveMediumHard` | 1,412,148 | 1,362,652 | **−3.51%** |

Bytes allocated per call, from the first of each commit's two measurements. `EvalEasy`, `RunEasy`, `RunMedium` and
`RunHard` allocate nothing in both columns and are left out.

The steps below are read from the gate's output on each push to `master`: the Kernel Benchmark runs on every merge that
touches the kernel, and allocation is deterministic, so the run where a row first moves names the commit that moved it.
The gate baseline itself was re-recorded once in the release, by
[#2310](https://github.com/asc-community/AngouriMath/pull/2310) at the 2827th, so every step before that sat inside
the gate's 3% against the `v2.6.0` baseline without failing it.

- **`SolveHard` −4.72% and `SolveMediumHard` −3.51%** are #2310, a union of solves solving each equation once. Its
  description records −4.7% and −3.1% from the gate.
- **`SimplifyHard` +2.42%** came in three consecutive steps:
  157,879,560 at the 2738th, then 159,271,064 at the 2739th
  ([#2175](https://github.com/asc-community/AngouriMath/pull/2175), +0.88%), 160,942,296 at the 2740th
  ([#2176](https://github.com/asc-community/AngouriMath/pull/2176), +1.05%) and 161,669,968 at the 2741st
  ([#2177](https://github.com/asc-community/AngouriMath/pull/2177), +0.45%). The three make the trigonometric identities
  `sin² + cos²`, `1 + tan²` and the power reduction of `cos²` match anywhere in a sum, which is more searching on every
  sum `SimplifyHard` simplifies. Each step was inside the gate's 3%; together they are 2.4%.
- **`Derivate` +1.85%** is one step, 56,139 to 57,216 at the 2904th
  ([#2393](https://github.com/asc-community/AngouriMath/pull/2393), the power rule keeping an irrational exponent
  exact), inside the gate's 3%, and the baseline was not re-recorded after it. With the 2.6.0 pair's +5.65%, `Derivate`
  has gone up 7.5% since `v2.5.0` without the gate failing once.
- **`CompileEasy` +33% and `CompileHard` +42%** are not gated, because their allocation includes the runtime building a
  delegate and moves by a few hundred bytes between runs of one build. The steps are far larger than that, and they are
  the compilers learning new nodes: `CompileEasy` 10,971 to about 12,000 at the 2781st
  ([#2223](https://github.com/asc-community/AngouriMath/pull/2223), membership and divisibility compile), 12,874 at the
  2784th ([#2229](https://github.com/asc-community/AngouriMath/pull/2229)), 13,839 at the 2823rd
  ([#2305](https://github.com/asc-community/AngouriMath/pull/2305)), 14,496 at the 2933rd
  ([#2429](https://github.com/asc-community/AngouriMath/pull/2429), matrix arguments) and 14,593 at the 2937th
  ([#2435](https://github.com/asc-community/AngouriMath/pull/2435)). `CompileHard` moved at the same commits. Each
  step is attributed to its commit; what in each commit costs the bytes is not bisected.
- **The evaluation rows' −0.02% to −1.19%, `SolveEasy` −1.13% and the rows under 0.4%** are not attributed.

### Timings

The interleaving gives each change two independent readings, and each commit's two measurements give the runner's spread
on an unchanged kernel. In this run that spread reached 9.8% (`Derivate` at `v2.6.0`) and 8.9% (`SolveMediumHard`), far
less than the 30–57% between two separate Kernel Benchmark runs, but too much to read a single-digit move from one pair.
A timing is read as a change only where both pairs agree in sign and both are outside the spread:

| benchmark | v2.6.0 | 2943rd | change | first pair / second pair | allocation |
|---|--:|--:|--:|--:|--:|
| `CompileEasy` | 332,547 | 413,679 | +24.40% | +24.23% / +24.56% | +33.02% |
| `CompileHard` | 579,424 | 508,814 | −12.19% | −12.90% / −11.47% | +42.49% |
| `RunHard` | 570 | 501 | −12.03% | −12.50% / −11.56% | none |
| `RunMedium` | 316 | 302 | −4.27% | −4.11% / −4.43% | none |
| `SimplifyEasy` | 89,591 | 94,556 | +5.54% | +5.92% / +5.17% | +0.13% |

Nanoseconds per call, the mean of each commit's two measurements.

- **`CompileEasy`** moved with its allocation, which is the work: building a delegate for more node kinds.
- **`CompileHard`** moved against its allocation, faster while allocating 42% more; **`RunHard`** and **`RunMedium`**
  moved with no allocation in either column. All three reproduced in both pairs to within 1.5 points, so on this runner
  they are not the machine, and they are not attributed.
  [#2438](https://github.com/asc-community/AngouriMath/pull/2438), computing only the case a compiled piecewise takes, is
  the obvious candidate for `RunHard` and is untested.
- **`SimplifyEasy` +5.5%** is a timing moving alone, just outside this run's same-commit spread of 3.6%. One run does
  not say whether it is the code, so it is recorded as a question, not as a regression.

`SimplifyHard` (+10.5% and +2.8% in the two pairs), `SolveHard` (+2.8% / −7.8%) and `SolveMediumHard` (−3.3% / −11.7%)
disagree between their pairs or sit inside the spread, so only their allocation is read. The smaller rows are not quoted.

---

## The 1957th and the 2662nd, measured together on one machine — the pair for 2.6.0

The pair the release checklist owes: `v2.5.0`
([`ab7a8bd6`](https://github.com/asc-community/AngouriMath/commit/ab7a8bd6b92d9ae99b3e23e51b10dca5c0e8fe11), the 1957th)
re-measured beside
[`784f69d5`](https://github.com/asc-community/AngouriMath/commit/784f69d5b07d2f0c153c67791468b3754f2e3250), the 2662nd, which is the last change to
the kernel before the release. Both columns come from one run of `Sources/Utils/benchmark_key_commits.sh`, on
2026-10-22, on one machine with nothing else on it. The period is the whole of 2.6.0, 705 commits.

It replaces the pair measured on 2026-10-18 against the 2549th, which was then the last change to the kernel; 113
commits came after it. The `v2.5.0` column agrees with that run's on every row the gate measures, to within 0.02%, and
`Derivate` and `SimplifyEasy` agree to the byte. `CompileEasy` reads 11,003 here and 10,970 there, 0.30% apart. The
gate leaves it and `CompileHard` ungated because their allocation includes building a delegate.

### Allocation

| benchmark | v2.5.0 | 2662nd | change | since the 2549th |
|---|--:|--:|--:|--:|
| `CompileEasy` | 11,003 | 11,003 | 0.00% | 0.00% |
| `CompileHard` | 20,434 | 20,737 | +1.48% | +1.48% |
| `Derivate` | 53,239 | 56,247 | **+5.65%** | +0.10% |
| `EvalPolynomialFresh` | 650,888 | 12,536 | **−98.07%** | 0.00% |
| `EvalPolynomialFresh15Digits` | 265,816 | 12,064 | **−95.46%** | 0.00% |
| `EvalTranscendentalFresh` | 5,668,395 | 34,760 | **−99.39%** | 0.00% |
| `EvalTrig` | 1,341,457 | 69,944 | **−94.79%** | 0.00% |
| `EvalTrigPrecise` | 12,742,285 | 336,552 | **−97.36%** | 0.00% |
| `ParseEasy` | 18,258 | 18,400 | +0.78% | −0.11% |
| `ParseHard` | 3,584,841 | 60,023 | **−98.33%** | −0.13% |
| `SimplifyEasy` | 79,282 | 79,682 | +0.50% | +0.89% |
| `SimplifyHard` | 331,710,008 | 157,898,664 | **−52.40%** | **−10.39%** |
| `SolveEasy` | 8,865,936 | 453,396 | **−94.89%** | −0.14% |
| `SolveEasyMedium` | 65,650 | 66,664 | **+1.54%** | −0.12% |
| `SolveHard` | 11,884,016 | 9,998,784 | **−15.86%** | −0.03% |
| `SolveMedium` | 455,925 | 458,910 | +0.65% | −0.05% |
| `SolveMediumHard` | 1,449,394 | 1,412,412 | −2.55% | +0.06% |

Bytes allocated per call. `EvalEasy`, `RunEasy`, `RunMedium` and `RunHard` allocate nothing in both columns and are
left out. *Since the 2549th* compares with the 2549th column of the run of 2026-10-18.

- **The evaluation rows** are the numerical-evaluation work of the 2050th to the 2077th, recorded below.
- **`ParseHard`** is the 2203rd, the parser predicting in SLL first.
- **`SimplifyHard`** fell a further 10.4% after the 2549th. That is not attributed to any section of this file.

### The steps compose, and that was checked

`SolveEasy` was measured in six steps:
- −29.2% at the 2050th;
- −29.6% and −62.1% at the 2052nd and 2053rd;
- −41.4% and −20.2% at the 2054th to the 2075th;
- −42.1% at the 2077th.

Composed, they predict −94.9%, and the end-to-end measurement is −94.89%.

`SimplifyHard`'s recorded steps predict about −44.5%. It measured −46.88% at the 2549th and −52.40% here, and neither
gap is attributed.

### `Derivate` went up 5.65%, in steps none of which the gate could see

`Derivate` differentiates a parsed constant, so no parser change reaches it. Its gate baseline moved five times
across the release, each move within the gate's 3%:

| baseline recorded at | allocation | step |
|---|--:|--:|
| `v2.5.0`, re-measured here | 53,239 | |
| 2053rd | 53,111 | −0.2% |
| 2084th | 53,528 | +0.8% |
| 2117th | 54,567 | +1.9% |
| 2130th | 55,095 | +1.0% |
| 2203rd | 55,783 | +1.2% |
| 2549th, the run of 2026-10-18 | 56,190 | +0.7% |
| 2662nd, measured here | 56,247 | +0.1% |

A baseline is re-recorded by the pull request that moves it, so each step covers every commit since the step before it,
not one change. The largest step, +1,039 bytes, is the 2117th,
[#1426](https://github.com/asc-community/AngouriMath/pull/1426). It gives a sum, a product and the other flat operators
a cached list of their operands, and so makes each of those nodes larger. That is a candidate, not a finding:
**no step is bisected**, so the increase is recorded here as unattributed. This file has one other case of steps each free by the gate summing to a real cost, the
rule-set exchange's +13%. This is the same pattern at a smaller size, and it is why the pair is measured end to end.

`SolveEasyMedium` +1.54%, `ParseEasy` +0.78%, `SolveMedium` +0.65% and `SimplifyEasy` +0.50% are below the gate's
resolution for a single step and are not attributed either.

### Timings

These moved well outside this machine's run-to-run spread and agree in sign with the allocation beside them:

| benchmark | change |
|---|--:|
| `EvalTranscendentalFresh` | −99.51% |
| `EvalPolynomialFresh` | −98.83% |
| `ParseHard` | −98.54% |
| `EvalTrigPrecise` | −98.20% |
| `SolveEasy` | −96.62% |
| `EvalTrig` | −95.53% |
| `EvalPolynomialFresh15Digits` | −94.85% |
| `SimplifyHard` | −56.99% |
| `SolveHard` | −39.96% |

`Derivate` was +1.66% (14.5 → 14.7 µs). `SimplifyEasy` was −14.9% and `ParseEasy` −25.9% with their allocation flat or
up. One run does not say whether that is the code or the machine, so they are not read as either. The smaller rows from the same run are not
worth reading and are not quoted.

## The 1845th and the 1954th, measured together on one machine — the pair for 2.5.0

The pair the release checklist owes: `v2.4.0` (`de7189b1`, the 1845th) re-measured beside the
release commit (`6a97c071`, the 1954th), both on 2026-09-09, on one machine, with nothing else on
it, one entry per run. The period is the whole of 2.5.0 — 108 commits.

### Allocation

| benchmark | v2.4.0 | 1954th | change |
|---|--:|--:|--:|
| `CompileEasy` | 11,042 | 11,003 | −0.35% |
| `CompileHard` | 20,776 | 20,473 | −1.46% |
| `Derivate` | 52,824 | 53,240 | **+0.79%** |
| `EvalTrig` | 1,341,377 | 1,341,457 | +0.01% |
| `EvalTrigPrecise` | 12,742,205 | 12,742,285 | +0.00% |
| `ParseEasy` | 18,112 | 18,264 | **+0.84%** |
| `ParseHard` | 3,522,481 | 3,584,841 | **+1.77%** |
| `SimplifyEasy` | 128,100 | 79,282 | **−38.11%** |
| `SimplifyHard` | 3,632,019,136 | 331,756,152 | **−90.87%** |
| `SolveEasy` | 8,853,735 | 8,865,872 | +0.14% |
| `SolveEasyMedium` | 97,195 | 65,648 | **−32.46%** |
| `SolveHard` | 1,446,794,112 | 11,883,024 | **−99.18%** |
| `SolveMedium` | 661,994 | 455,925 | **−31.13%** |
| `SolveMediumHard` | 164,549,808 | 1,449,266 | **−99.12%** |

Bytes allocated. `EvalEasy`, `RunEasy`, `RunMedium` and `RunHard` allocate nothing in both columns
and are left out.

### The individually-measured steps compose, and that was checked rather than assumed

This file records a case where they did not: the rule-set exchange was measured step by step, each
step came back free or better, and the sum was **+13%**. So the five allocation changes in this
release are worth checking against where the release started rather than trusting their own
figures.

`SimplifyHard` was measured at −44% ([#1205](https://github.com/asc-community/AngouriMath/pull/1205)),
−64% ([#1207](https://github.com/asc-community/AngouriMath/pull/1207)),
−35% ([#1210](https://github.com/asc-community/AngouriMath/pull/1210)) and
−25% ([#1211](https://github.com/asc-community/AngouriMath/pull/1211)), each against the commit in
front of it. Composed, those predict 9.8% of the original surviving, or −90.2%. **Measured
end-to-end against `v2.4.0`: −90.87%**, 0.7 percentage points from the prediction. `SolveHard` was
−98.6% for [#1209](https://github.com/asc-community/AngouriMath/pull/1209) alone and is −99.18%
across the release. They compose here. That is a measurement, not an expectation, and the +13% case
is why it was taken.

### Three rows went up, and none is attributed

`ParseHard` +1.77%, `ParseEasy` +0.84% and `Derivate` +0.79%. The parser gained three alternatives
this release — the `divides` keyword, `#` for cardinality and the lambda arrow — which is consistent
with the two parse rows, and `Derivate` has no such candidate. **Neither is bisected**, so both are
recorded as unattributed rather than explained. A move nobody has attributed is a finding, not a
footnote.

### The determinism claim, held again — and its two exceptions confirmed

`v2.4.0` was measured on 2026-09-07 for the 1930th and again today, two days and one run apart.
Eleven of the fourteen rows reproduce to within **0.021%**, four of them to the byte —
`EvalTrig`, `EvalTrigPrecise`, `ParseEasy`, `ParseHard`, `SolveMediumHard`.

The only two rows that moved further are `CompileEasy` (+0.354%) and `CompileHard` (+1.674%), which
are exactly the two `performance-baseline.json` marks **ungated**, for the reason it records there:
their measured allocation includes the runtime building and JIT-compiling a delegate, which is not
reproducible, at a spread of 3.6% over three runs of one unchanged build. Today's spread sits inside
that. So this is an independent confirmation of a documented caveat rather than a new one — and it
is worth stating that the blanket sentence "the same commit measured twice gives the same bytes"
holds for every row this file gates and for neither of the two it does not.

### Timings

`SimplifyHard` −89.64%, `SolveHard` −87.29%, `SolveMediumHard` −83.83%, `SolveEasyMedium` −42.80%,
`SimplifyEasy` −57.15%. These are reproduced here, where the 1930th's deliberately were not,
because the 1844th section's objection is that this machine cannot resolve a move of a few per
cent — its measured run-to-run spread on mean time is up to 51.8%. A move of 84 to 90 per cent is
outside that band by a wide margin and agrees in sign and rough size with the allocation column
beside it. The small rows from the same run are still not worth reading, and are not quoted.

## The 2203rd: the parser predicts in SLL first

`ParseHard` allocated 3.8 MB to read a twenty-token expression, and every function added to the
grammar made it more: `Ei` and `li` cost it 24,272 B/op, and `Si`, `Ci`, `Shi` and `Chi` would
have cost another 48,544 -- 12,136 B for each literal token, exactly, which put the branch adding
the four trigonometric integrals over the gate's 3%. The bytes were ANTLR's full-context LL
prediction, which the default prediction mode falls back to wherever its context-free SLL
prediction sees a conflict, on every parse that reaches one. The parser now predicts in SLL, with
the bail error strategy, and parses again in LL only where that reports an error or an action
throws. ANTLR documents that SLL "will either return a correct parse tree (i.e. the same parse
tree that would be returned with the LL prediction mode), or it will report a syntax error", so
the tree is LL's either way, and only an input SLL cannot read pays for both. Of the 117,358
distinct expressions in Rubi's suite that the library can express, the only ones SLL could not read
were the 92 LL cannot either: it read every one LL read, to an equal entity.

| benchmark | 2202nd | 2203rd | allocation | time |
|---|--:|--:|--:|--:|
| `ParseHard` | 3,799,418 | 59,871 | **−98.4%** | 1,525 → 25.6 µs |
| `ParseEasy` | 18,301 | 18,373 | +0.4% | 6.27 → 5.58 µs |
| every other gated entry | | | within 64 bytes | within the noise |

Bytes allocated per call, same machine, both columns by the gate in one session. The 72 bytes on
`ParseEasy` are what the two stages cost a parse that never falls back. The baseline moves to this
run.

## The 2077th: the root of a negative real is exactly imaginary, and cheaper for it

A correctness fix ([#1378](https://github.com/asc-community/AngouriMath/issues/1378)) that
happens to be a large move on `SolveEasy`: a negative real to a
power that is a whole number of halves went through the polar form -- a hundred-digit
`Arctan2`, a `cos` and a `sin` of `pi/2`, and a real part of `6e-102` left over -- and is now
the modulus to that power times a power of `i`: an exact root where the numerator and
denominator are squares, the decimal square root and a whole power of it otherwise. The
solve of `x^2 + x + 1` evaluates `sqrt(-3)` four times.

| benchmark | 2075th | 2077th | allocation | time |
|---|--:|--:|--:|--:|
| `SolveEasy` | 783,622 | 453,364 | **−42.1%** | 390 → 222 µs |
| `SolveMediumHard` | 1,367,804 | 1,382,724 | +1.1% | |
| every other entry | | | within 1% | within the noise |

Bytes allocated per call, same machine, both columns by the gate in one session. The first
version of the fix took the modulus through the exact-base path, whose search over all the
roots of unity of `3` cost the same row **+17.4%** -- caught by the gate on the pull request,
which is what it is for. The baseline moves to this run.

## The 2075th: a cached evaluation follows the precision context

A correctness fix ([#1367](https://github.com/asc-community/AngouriMath/issues/1367)) with a
cost, recorded here because the cost is on every row: `pi` and `e` were worth the digits of
whichever precision was set when their registry was built, and an entity's cached `Evaled`
survived a change of `DecimalPrecisionContext`, so `pi` at three hundred digits after an
evaluation at a hundred came back with a hundred, and `e^x` came back correct to a hundred of
its three hundred. The constant's value is now looked up at the moment of evaluation, and the
precision setting advances an evaluation epoch on every `Set` and every scope's end, which an
entity records beside its `Evaled` and compares on each read -- one `int` on every entity and
one static read per evaluation.

| benchmark | 2074th | 2075th | allocation | time |
|---|--:|--:|--:|--:|
| `SolveMediumHard` | 1,367,804 | 1,381,260 | +1.0% | |
| `SimplifyHard` | 179,367,888 | 180,917,280 | +0.9% | |
| `Derivate` | 53,111 | 53,527 | +0.8% | |
| `EvalPolynomialFresh` | 12,328 | 12,424 | +0.8% | |
| every other entry | | | within 0.7% | within the noise |

Bytes allocated per call, same machine, both columns by the gate in one session: the eight
bytes an entity grew by, times the entities each row builds, all inside the gate's band, so
the baseline stands.

## The 2074th: the logarithm and the arctangent reduced by a table, the cosine from the sine, the square root an integer one

After the 2072nd the remaining factor to mpmath was the term count: eighty artanh terms for a
logarithm at a hundred digits, forty-seven for an arctangent after three square-root
halvings, and two series -- the sine's and the cosine's -- for either. The logarithm's
mantissa, already within `[1/sqrt 2, sqrt 2]`, is divided by the nearest `1 + j/64`, whose
logarithm the constant cache holds (built outward from `ln 1 = 0` when first asked, one short
series a step), so the series runs on a quotient within 1/128 of one: twenty-five terms. The
arctangent's argument, within `[0, 1]`, is brought within 1/128 of zero by the nearest `j/64`
the same way, `arctan x = arctan c + arctan((x - c)/(1 + xc))`: thirty terms and no square
root. The cosine at the sine's reduced argument, which is within a twentieth of zero, is
`sqrt(1 - sin^2)` and cancels nothing; the other way round is what lost half the digits in the
2050th. And the square root of a decimal is the integer square root of its mantissa padded to
twice the digits, with a sticky digit so the rounding to the context is the correct one in
every mode, as PeterO's is -- Newton from the root of the top half of the bits, three
divisions at the full width where a power of two above the root took one per bit of the
exponent. The hundred-digit values are the ones pinned before, to the last digit.

| benchmark | 2073rd | 2074th | allocation | time |
|---|--:|--:|--:|--:|
| `EvalTranscendentalFresh` | 59,136 | **34,608** | **−41.5%** | 33 → **16 µs** |
| `EvalTrig` | 82,104 | **69,792** | **−15.0%** | 37 → **32 µs** |
| `EvalTrigPrecise` | 525,289 | **336,360** | **−36.0%** | 617 → **406 µs** |
| `SolveEasy` | 981,996 | **783,622** | **−20.2%** | 491 → **390 µs** |
| `SolveHard` | 11,708,080 | **10,053,640** | **−14.1%** | 68 → 70 ms |
| every other entry | | | within 1.3% | |

Bytes allocated per call, same machine, both columns by the gate in one session; the gate's
baseline was taken from this run. Alone, one probe on both builds: at a hundred digits `ln`
18.0 → 8.4 µs, `arctan` 24.6 → 7.7, `sin` 15.2 → 13.1; at five hundred `ln` 777 → 241 µs,
`sin` 228 → 141, and the square root 41 → 22 (PeterO's against the integer one); at two
thousand digits the square root 441 → 174 µs. mpmath at a hundred digits is `ln` 4.5, `arctan`
3.9, `sin` 5.6.

## The 2073rd: the rational pre-check confirms a maybe, and reads two thousand digits

`EvalTrig` is `sin 1 + cos 1 + tan 1`, and after the 2072nd fifty of its eighty-three
microseconds were `Real.Create` deciding that sin 1 is not a small rational -- while cos 1 was
decided in a fifth of one. The cheap decision of the 2052nd runs the continued fraction in a
double first and says "may be" where a remainder is within the double's own error of the next
integer; for sin 1 that is the twelfth level, where the error has grown to a hundredth, and
the answer went straight to the exact hundred-digit search instead of to the double-double,
whose error there is ten to the minus twelve and which refuses in under a microsecond. A
double's yes is confirmed in the double-double now, always. And the reading of a value into
the double-double scaled by two halves of its binary shift, of which half of a thousand-digit
value's is `2^1608`, an infinity: every `Real.Create` past about six hundred and fifty digits
went to the exact search, 5.7 ms each at two thousand. The scaling is in as many pieces as
keep each factor in range, up to four thousand digits. Nothing a value downcasts to changes;
the twelve-thousand-rational agreement test and a two-thousand-digit one say so.

| benchmark | 2072nd | 2073rd | allocation | time |
|---|--:|--:|--:|--:|
| `EvalTrig` | 164,728 | **82,104** | **−50.2%** | 83 → **37 µs** |
| `EvalTrigPrecise` | 718,778 | **525,289** | **−26.9%** | 1.05 → **0.62 ms** |
| every other entry | | | 0.0% | |

Bytes allocated per call, same machine, both columns by the gate in one session; the gate's
baseline was taken from this run. Alone: `Real.Create` of sin 1's hundred digits 49 → 0.5 µs,
of two thousand digits 5,660 → 0.3 µs; `sin 1` now costs what `cos 1` does.

## The 2072nd: the fixed-point series on the BCL's integers

The 2054th and 2055th put the logarithm, the exponential, the sine, the cosine and the
arctangent into fixed point, on PeterO's `EInteger`. Measured against mpmath on the same
machine, the transcendental rows were still nine to seventeen times behind, and the reason is
the integer type: one multiply of two hundred-digit integers is 1,271 ns in `EInteger` and
109 ns in `System.Numerics.BigInteger` (at five hundred digits 8,087 against 1,364; at two
thousand 75,850 against 12,036), and the BCL's is at parity with CPython's `int`, which is
what mpmath's digits live in. The series run on `BigInteger` now, converted once on the way
in and once on the way out as two's-complement bytes, and the way out no longer multiplies by
`5^bits` and asks `EDecimal` to round a number of four times the digits: the fixed-point
integer is scaled to a power of ten six digits past the precision in `BigInteger`, and only
the rounding is `EDecimal`'s. The hundred-digit values are the ones pinned before, to the
last digit.

| benchmark | 2055th | 2072nd | allocation | time |
|---|--:|--:|--:|--:|
| `EvalTrig` | 260,728 | **164,728** | **−36.8%** | 153 → **83 µs** |
| `EvalTrigPrecise` | 2,525,707 | **718,778** | **−71.5%** | 2.16 → **1.05 ms** |
| `EvalTranscendentalFresh` | 128,664 | **59,136** | **−54.0%** | 83 → **33 µs** |
| `SolveEasy` | 1,675,487 | **981,996** | **−41.4%** | 1.19 → **0.49 ms** |
| `SimplifyHard` | 189,604,392 | **182,277,112** | **−3.9%** | 111 → 117 ms |
| every other entry | | | within 0.1% | |

Bytes allocated per call, same machine, both columns by the gate in one session; the gate's
baseline was taken from this run. Alone, by one probe on both builds, each row the best of
seven batches with tiered compilation off: at a hundred digits `sin(x)` is 15.2 µs where it
was 46.9, `ln(x)` 18.0 where it was 52.4, `e^x` 10.0 where it was 32.3, `arctan(x)` 24.6
where it was 48.8; at five hundred digits `sin(x)` 228 µs where it was 1,273, `ln(x)` 777
where it was 3,921, `e^x` 548 where it was 2,774; at two thousand digits `sin(x)` 11.7 ms
where it was 40.7 and `ln(x)` 26 where it was 121. mpmath at a hundred digits is 5.6, 4.5,
4.3 and 3.9 µs for the same four, so the remaining factor of three to six is the term count
-- eighty artanh terms for a logarithm, twenty-five for a sine -- which argument reduction
past what is done now would cut.

## The 2055th: the sine, cosine and arctangent in the same fixed point

The 2050th made the trigonometric functions argument reduction and short series, in
`EDecimal`; the 2054th found that a series in `EDecimal` pays two to three microseconds a term
for the alignment in each addition and moved the logarithm and the exponential to fixed point.
The sine, the cosine and the arctangent are there too now: the reduction to `[-pi, pi]` is an
integer division by `2 pi` in fixed point, the folds and halvings are subtractions and shifts,
the square root in the arctangent's halving `x/(1 + sqrt(1 + x^2))` is the integer square root
of the value shifted up, exactly floored, and the halvings come back as a shift. The arcsine
and arccosine are the arctangent's, and the tangent the quotient. The hundred-digit values
are the ones pinned before, to the last digit.

| benchmark | 2054th | 2055th | allocation | time |
|---|--:|--:|--:|--:|
| `EvalTrig` | 1,192,089 | **260,728** | **−78.1%** | 552 → **153 µs** |
| `EvalTrigPrecise` | 7,935,226 | **2,525,707** | **−68.2%** | 13.8 → **2.16 ms** |
| `EvalTranscendentalFresh` | 423,032 | **128,664** | **−69.6%** | 245 → **83 µs** |
| `SolveEasy` | 4,425,194 | **1,675,487** | **−62.1%** | 2.88 → **1.19 ms** |
| every other entry | | | within 1.5% | |

Bytes allocated per call, same machine, both columns by the gate in one session; the gate's
baseline was taken from this run. Alone, at a hundred digits: `sin(x)` is 45 µs where it was
176, `arctan(x)` 46 where it was 202, `arcsin(x)` 59 where it was 238; at five hundred digits
`arctan(x)` is 1.2 ms where it was 9.3. Against the 2049th, before any of this, `EvalTrig` is
709 → 153 µs and `EvalTrigPrecise` 22.5 → 2.2 ms.

## The 2054th: the logarithm and the exponential as series in fixed point

`EvalTranscendentalFresh` -- `ln(1 + x^2) arctan(x)` at a fresh point -- was 1.3 ms after the
2050th, and a `ln` alone 0.7 ms: PeterO's `Log` is seven hundred microseconds at a hundred
digits and its `Exp` three hundred, and `ln(x)` arrived as `log_e(x)`, which was two of them
and a division. A series in `EDecimal` pays for each term an alignment of exponents, an exact
sum and a rounding, two to three microseconds a term at a hundred digits, which is what those
are made of.

The logarithm and the exponential are their own series now, run in fixed point: an `EInteger`
holding the value times `2^bits`, with the context's digits in bits and forty-eight over, where
a product is a big-integer multiply and a shift, a division by a term's index an integer
division and a sum an addition -- a third of a microsecond a term -- and the value is a decimal
again exactly, by `5^bits` and a move of the point. `ln x` is `2 artanh((m - 1)/(m + 1))` with
`m` the mantissa brought between `1/sqrt(2)` and `sqrt(2)` by powers of ten and two, seventy
terms; `e^x` is `x = k ln 2 + r`, `r` halved ten times, twenty-five terms, ten squarings and the
power of two back. `ln 2` and `ln 10` are in the constant cache, per context. `log_b(x)` is
`ln x / ln b` with the base `e` recognised, a nonnegative real to a real power is
`exp(power ln base)` with eight guard digits, and `e` to any real power is the exponential of
the power -- `e^700` came back to ninety-seven digits as the constant's hundred raised to the
seven hundredth. The hyperbolic functions and the complex logarithm, exponential, power and
inverse trigonometric functions go through the same two.

| benchmark | 2053rd | 2054th | allocation | time |
|---|--:|--:|--:|--:|
| `EvalTranscendentalFresh` | 2,246,889 | **423,032** | **−81.2%** | 1.33 ms → **245 µs** |
| `SimplifyHard` | 302,423,792 | **189,726,120** | **−37.3%** | 172 → **109 ms** |
| `SolveEasy` | 6,284,124 | **4,425,194** | **−29.6%** | 3.55 → **2.88 ms** |
| every other entry | | | within 1.5% | |

Bytes allocated per call, same machine, both columns by the gate in one session; the gate's
baseline was taken from this run. The simplifier and the solver move because they evaluate
exponentials and logarithms numerically underneath, to compare candidates. Alone: `ln(x)` at
a hundred digits is 35 µs where it was 685, `e^x` 30 where it was 740, `sinh(x)` 62 where it
was 1,570, and at five hundred digits `ln(x)` is 2.9 ms where it was 15.4. Nineteen pinned
hundred-digit values moved, every one towards mpmath's; `BREAKING-CHANGES.md` has the table.

## The 2053rd: equality without the caches, and a rational's decimal written out when asked

Two things found under the evaluator's remaining cost, each measured alone and each reaching
further than the evaluator.

**Equality.** An `Entity` is a record, and a record compares every field: at this level of the
record the fields are caches -- the children, the variables, the evaluated value, eleven
`LazyPropertyA` slots and the sort keys -- each equal to every other by construction, so the
synthesized comparison was a dozen calls into `EqualityComparer<T>` that could only answer
yes, and the synthesized hash a dozen that could only add zero. The base record now declares
its own `Equals` and `GetHashCode`, the node's kind and nothing else; each node's own record
still compares and hashes its parts. `Equals` on two equal twenty-node trees is 0.085 µs where
it was 0.165, the hash 0.14 where it was 0.25 -- and the solver, which keeps its candidates in
sets keyed by the tree, is where that lands.

**A rational's decimal.** `Rational(ERational)` wrote its value out as a hundred-digit
`EDecimal` in the constructor, for every integer and fraction the simplifier and the solver
make and almost never ask the decimal of: `Integer.Create` was 0.11 µs and 872 bytes,
`Rational.Create(3, 7)` 0.83 µs -- a division at a hundred digits and then, because the
denominator turned out to be one, an `Integer` and the division again. The decimal is a lazy
property now, written out in the precision of the moment it is first asked for rather than
of the moment the rational was made; the tests pinning hundred-digit values see no change.
`Integer.Create` is 0.022 µs, `Rational.Create(3, 7)` 0.056, and a `Sumf` of two integers
evaluates in 0.19 µs where it was 0.38.

| benchmark | 2052nd | 2053rd | allocation | time |
|---|--:|--:|--:|--:|
| `SolveHard` | 12,285,384 | **11,714,880** | −4.6% | 116 → **70 ms** |
| `SolveMediumHard` | 1,414,142 | **1,365,686** | −3.4% | 14.5 → **7.9 ms** |
| `SolveMedium` | 452,357 | 444,046 | −1.8% | 392 → **267 µs** |
| `SimplifyHard` | 314,203,712 | **302,423,792** | −3.7% | 180 → 172 ms |
| `ParseEasy` | 18,456 | 18,125 | −1.8% | 6.18 → 5.93 µs |
| `SolveEasyMedium` | 65,630 | 64,974 | −1.0% | 19.3 → 18.5 µs |
| every other entry | | | within 1% | |

Bytes allocated per call, same machine, both columns by the gate in one session; the gate's
baseline was taken from this run. The timings are the gate's, one run each, so they were
re-measured both arms in one process: `SolveHard` 112 → 64 ms and `SolveMediumHard` 14.7 →
8.1 ms; and with the lazy decimal alone, `SolveHard` is 110 ms -- the solver's gain is the
equality, and the decimal's is in what makes numbers.

## The 2052nd: the rational search decided in a double-double where the double could not

The 2050th left `EvalPolynomialFresh` at 9.4 µs, of which the arithmetic is about one: the
polynomial `x^3 + 2x^2 - x + 1` at `0.37000000000001234` has a value near a short decimal at
every node -- `0.324453` and twenty-three units in the fourteenth place at `x^3 + 2x^2` -- and
a value near a rational whose denominator is within the bound is exactly what the double's
continued fraction cannot settle: its error, an ulp times the square of the convergent's
denominator, passes a tenth at a denominator of forty million, where the bound is a hundred.
Such a value went on to `System.Decimal`, where fifteen divisions of a `decimal` cost two
microseconds, and three of the polynomial's eight nodes paid them.

The second pass is now the same continued fraction in a double-double -- the value read as
two doubles, a hundred and six bits, from the leading bits of the mantissa and a table of
powers of ten, with Dekker's error-free sum and product and no fused multiply-add, since
netstandard2.0 has none. Its error starts at ten to the minus twenty-eight and the square of
any denominator within the bound leaves it below ten to the minus twelve, so it decides every
value the double could not, and the exact search runs only on a value it accepts. The
decisions are the exact search's on thirty-six thousand values -- twelve thousand rationals
within the bound, as many within ten to the minus ten to fifteen of one, as many random
hundred-digit values -- which is a test now.

| benchmark | 2050th | 2052nd | allocation | time |
|---|--:|--:|--:|--:|
| `EvalPolynomialFresh` | 14,000 | **12,328** | −11.9% | 9.41 → **4.09 µs** |
| `EvalPolynomialFresh15Digits` | 12,280 | **11,656** | −5.1% | 8.95 → **4.15 µs** |
| every other entry | | | within 1.1% | |

Bytes allocated per call, same machine, both columns by the gate in one session; a
`Real.Create` on such a value is 0.46 µs where it was 2.36, and 0.22 where it was 0.17 on a
value the double settles, the price of reading a hundred and six bits rather than sixty-two.
The gate's baseline was taken from this run. What is left of the 4.1 µs is the evaluator: six
operator nodes at 0.3 to 0.5 µs each -- `InnerSimplify` with its domain check and lazy caches
around a tenth of a microsecond of arithmetic -- and the substitution, 0.7 µs.

## The 2050th: numerical evaluation, four changes in the numbers themselves

[#1338](https://github.com/asc-community/AngouriMath/issues/1338) asked where an `EvalNumerical`
spends its time and whether the width of the numbers is the reason. Three rows were added for
the question (the 2049th, [#1341](https://github.com/asc-community/AngouriMath/pull/1341)): a
polynomial and `ln(1 + x^2) arctan(x)` evaluated at a fresh decimal point each call, and the
polynomial again at fifteen digits. Measured first, the width was not the reason -- fifteen
digits against a hundred bought four times, not the forty the digit count suggests -- and the
per-node cost was found in four places, each measured against these rows and against the
primitives alone:

- **The rational search.** Every arithmetic operation on a `Real` ends in `Real.Create`, which
  looks for a small rational the result might be by a continued fraction in hundred-digit
  arithmetic, fifteen divisions deep -- twenty microseconds a time, most of the cost of a node,
  and almost every result of an evaluation is not a small rational. The same continued fraction
  is run first in a double read off the leading bits of the mantissa, a third of a microsecond,
  carrying the error each level's inversion multiplies it by, which decides reliably that a value
  is *not* within the exact search's tolerance of a rational within the bound; where its error has
  grown past deciding, a value near a small rational, in `System.Decimal`; and only a value neither
  rules out reaches the exact search, which then decides as before.
- **Powers.** A cube was three multiplications and a tenth power nine, each with the rational
  search on its result, since the binary power computed its half twice; the half is squared
  now, a whole power of an inexact decimal is one `Pow` on the decimal, and a half power of a
  nonnegative one is its square root -- Newton's iteration, seventy times faster than the
  exponential of half its logarithm it was.
- **Sine and cosine.** The Taylor series at the argument as it came, sixty terms for an argument
  up to pi, and the sine as `sqrt(1 - cos^2)`, which loses half the digits where the sine is
  small; now the argument reduced to `[-pi/2, pi/2]` and halved below a twentieth, two series of
  a dozen terms, and the double-angle formulas back up with eight guard digits.
- **Arctangent and arcsine.** The arctangent was the arcsine of `x/sqrt(1 + x^2)`, a series
  that converges like `x^(2n)/n^(3/2)`: two milliseconds for an argument of a third. It is
  reduced to `[0, 1]`, halved by `2 arctan(x/(1 + sqrt(1 + x^2)))` below a twentieth, and summed
  as its own series; the arcsine is the arctangent of `x/sqrt((1 - x)(1 + x))`.

| benchmark | 2049th | 2050th | allocation | time |
|---|--:|--:|--:|--:|
| `EvalPolynomialFresh` | 650,888 | **14,000** | **−97.8%** | 349 → 9.4 µs |
| `EvalPolynomialFresh15Digits` | 265,816 | **12,280** | **−95.4%** | 83.7 → 9.0 µs |
| `EvalTranscendentalFresh` | 5,668,395 | **2,247,769** | **−60.3%** | 3.25 → 1.32 ms |
| `EvalTrig` | 1,341,457 | **1,190,217** | **−11.3%** | 709 → 555 µs |
| `EvalTrigPrecise` | 12,742,285 | **7,933,690** | **−37.7%** | 22.5 → 13.8 ms |
| `SolveEasy` | 8,865,872 | **6,280,444** | **−29.2%** | 4.83 → 3.54 ms |
| `SimplifyHard` | 331,683,360 | **314,496,408** | −5.2% | 190 → 178 ms |
| every other entry | | | within 1.5% | |

Bytes allocated per call, same machine, both columns measured by the gate in one session. The
solver and the simplifier move because both evaluate numerically underneath. What the last
digits do is stated, since twenty pinned hundred-digit values in `NumericDigits` changed in
their last one to three digits: against mpmath at a hundred and thirty digits, `sin(1)`,
`cos(1)` and `tan(1)` were two, three and one units off in the last place and are exact now, the
complex sines, cosines and tangents were about twenty units off and are within two, the complex
arcsine's real part is five units off where it was three, and the complex power is 289 off either
way, its own path; a new test holds eighteen values of the six functions to ninety-eight digits. The gate's baseline was taken from this run.
What is left of the cost is the evaluator's own overhead, `InnerSimplify` per node with its
domain check and its lazy caches, and PeterO's `Log` and `Exp` at three hundred microseconds a
call, which are the next things to measure.

## The 1930th, and every release beside it on one machine

The second column measured by `Sources/Utils/benchmark_key_commits.sh` reaching all five entries in
`key-commits.txt`, fifteen commits after the 1915th: the same benchmark against every release and
against `308384b2`, on one machine, on 2026-09-07, one entry per run with nothing else on the
machine. The period covers #1191–#1203 — the codomain in the e-node's identity, twenty-nine growth
declarations and the `Collects` contract, the constant fold on insertion, the perfect-square
collapse asking the rewrite graph before `Simplify`, and extraction as a fixed point from the
leaves up.

### Allocation

| benchmark | v2.1.0 | v2.2.0 | v2.3.0 | v2.4.0 | 1915th | 1930th |
|---|--:|--:|--:|--:|--:|--:|
| `CompileEasy` | 16,191 | 16,552 | 16,734 | 11,003 | 10,970 | 10,970 |
| `CompileHard` | 38,249 | 38,379 | 38,163 | 20,434 | 20,434 | 20,737 |
| `Derivate` | 52,823 | 52,823 | 52,823 | 52,814 | 52,823 | 52,814 |
| `EvalTrig` | 1,341,377 | 1,341,377 | 1,341,377 | 1,341,377 | 1,341,377 | 1,341,377 |
| `EvalTrigPrecise` | 12,742,205 | 12,742,205 | 12,742,205 | 12,742,205 | 12,742,205 | 12,742,205 |
| `ParseEasy` | 18,061 | 18,058 | 18,061 | 18,112 | 18,104 | 18,104 |
| `ParseHard` | 3,498,137 | 3,498,137 | 3,522,433 | 3,522,481 | 3,531,201 | 3,531,201 |
| `SimplifyEasy` | 128,564 | 128,564 | 128,100 | 128,099 | 128,100 | 128,460 |
| `SimplifyHard` | 3,733,175,832 | 3,749,409,536 | 3,620,748,264 | 3,632,015,744 | 3,655,695,192 | 3,662,375,240 |
| `SolveEasy` | 20,220,106 | 20,234,998 | 8,852,269 | 8,853,671 | 8,853,834 | 8,853,834 |
| `SolveEasyMedium` | 80,184 | 95,873 | 95,793 | 97,175 | 97,335 | 97,335 |
| `SolveHard` | 1,356,524,264 | 1,486,585,888 | 1,431,854,144 | 1,446,793,064 | 1,450,058,720 | 1,452,700,104 |
| `SolveMedium` | 554,368 | 658,259 | 658,099 | 661,963 | 662,923 | 662,923 |
| `SolveMediumHard` | 155,190,904 | 171,395,096 | 162,305,664 | 164,549,808 | 165,055,344 | 165,611,184 |

Bytes allocated. The four release columns are this run's own re-measurement, not the 1915th's
figures copied across; the 1915th column is that section's. `EvalEasy`, `RunEasy`, `RunMedium`
and `RunHard` allocate nothing in every column and are left out.

**Since the 1915th: `SimplifyHard` +0.18%, `SolveHard` +0.18%, `SolveMediumHard` +0.34%, and
those three figures are, to the digit, the cost recorded for #1192 when it repointed the three
`CanonicalOrder` sets at their data form.** Everything after #1192 — thirteen pull requests,
among them a new e-node identity, a constant fold and a rewrite of extraction — is therefore
allocation-neutral on these benchmarks *if* the whole movement is #1192's. That is a reading of
two measurements that agree, not a bisection: the two commits either side of #1192 were not
re-measured here (an older commit builds from scratch and one run of it overran the ten minutes
this session could give a single command). `SimplifyEasy` +360 bytes (+0.28%) is the one figure
that reading does not cover and is unattributed.

**Since 2.4.0, the pair a release publishes:** `SimplifyHard` +0.84%, `SolveMediumHard` +0.65%,
`SolveHard` +0.41%, `SimplifyEasy` +0.28%; `CompileEasy` −0.3% and `CompileHard` +1.5%, both
inside the compile benchmarks' 2% floor; every other row flat. All of it inside `PerformanceGate`'s
3%, and none of it a reason to hold a release; the 1915th's rider still applies — the period since
2.4.0 was not free, and the where is recorded above.

### The determinism claim, held again

`v2.4.0` was measured in the 1915th's run and again in this one: `SimplifyHard` 3,632,015,744 both
times, `SolveMediumHard` 164,549,808 both times, `SolveHard` 1,446,799,616 against 1,446,793,064
(0.0005%), `CompileEasy` 11,003 both times, `CompileHard` 20,396 against 20,434 (0.19%). The
large rows reproduce to the byte or nearly; the compile rows sit inside their floor.

Timings from the same run are in the generated table and are deliberately not reproduced here,
for the reason the 1844th section gives.

## The 1931st: a candidate registered once

The same afternoon, on the same machine. `Simplificator.Alternate` registered every candidate it
generated twice — once as it was and once as `expr.Rewrite(InvertNegativePowers)`, each
registration a `CanonicalOrder` rewrite, an inner simplification and two ratings — and on an
input with nothing to invert the second tree is the first tree, so the second registration
re-sorted an identical tree to offer the history set an entity it already held. Found by
attributing `SimplifyHard`'s bytes to the steps of one level: the registrations were 862 MB of
976. The second registration now runs only where the inversion produced a different tree, and
`CostModel.DefaultCost` costs a subtree once per instance instead of re-walking it for every
candidate that shares it. No answer moves — the same candidates reach a set that already
deduplicated the repeat, and every rate is the same double.

| benchmark | 1930th | 1931st | |
|---|--:|--:|--:|
| `SimplifyHard` | 3,662,375,240 | 2,027,771,744 | **−44.6%** |
| `SolveHard` | 1,452,700,104 | 1,057,996,016 | **−27.2%** |
| `SolveMediumHard` | 165,611,184 | 126,162,152 | **−23.8%** |
| `SimplifyEasy` | 128,460 | 115,763 | −9.9% |

Bytes allocated; every other row unchanged or inside its floor. The solvers move because `Solve`
simplifies inside. Timings, one run against one run and so carrying this file's usual rider:
`SimplifyHard` 1.60 s → 0.92 s, `SolveHard` 879 → 772 ms, `SolveMediumHard` 88 → 78 ms,
`SimplifyEasy` 110 → 89 µs. The gate's baseline was taken from this run, which is the case its
section above names as the one where a red gate means nothing is wrong.

What is left of a `SimplifyHard` level, for whoever comes next: the remaining registration
(431 MB), and the three expansion candidates — `res.Expand().Simplify(-level)`, the rule-based
factorisation at level 2, and the opened angles expanded — at 177, 170 and 82 MB, each a
recursive simplification of an expanded tree.

## The 1937th: a candidate is re-simplified at the default level

The same hook on `SimplifyHard`, attributed to the call sites that register a candidate. After
its level loop `Alternate` offers three more — the expansion, the rule-based factorisation and
the opened multiple angles — and re-simplifies each in full before the metric is asked, because
their cancellations only show once they are simplified. Each is a nested `Alternate` at the
caller's own level, so at level 4 the three were three nested four-level searches: **566 MB of
the run's 735**, 77%, before the sort-key cache above; and level by level, the third and fourth
levels of a nested run registered one or two new trees where the first two registered dozens,
for the same passes. The candidates are re-simplified at level 2 now, the default, whatever
level the run was asked for; the run's own loop keeps its level.

This is a policy and not a mechanical saving, so what it changes is stated: a caller at the
default level sees nothing different at all, since 2 is what it always was; a caller above it
gets a candidate simplified the way every default call simplifies, ranked by the same metric
against an outer run that still ran at the level asked for. The callers above the default in the
library are the solver's alternative spellings and polynomial long division's coefficients, and
no pinned answer of either moved; nor did any other in the suite.

| benchmark | 1936th | 1937th | allocation | time |
|---|--:|--:|--:|--:|
| `SimplifyHard` | 442,898,152 | **331,683,360** | **−25.1%** | 258 → 190 ms |
| `SimplifyEasy` | 106,643 | **79,282** | **−25.7%** | 68.8 → 47.4 µs |
| every other entry | | | identical to the byte | |

Bytes allocated per call, same machine, both columns measured by the gate in one session. Both
benchmarks call `Simplify(4)`, which is why both move and nothing else does. Since the 1930th:
`SimplifyHard` **−91%**. The gate's baseline was taken from this run.

## The 1936th: the sort key of a node is spelt once

The same hook, on `SimplifyHard`, attributed this time to the registration steps of
`Simplificator.Alternate`: the `CanonicalOrder` sort was 174 MB of the run's 735, some 2.6 MB per
sort of a tree a few hundred nodes wide. `Entity.SortHash` builds a node's key out of the keys of
every node below it and nothing kept them, so one sort re-spelt each subtree once per ancestor —
and the simplifier sorts what it registers at every pass of every level, in its own run and in
the three nested runs its candidates cost. The key is kept on the instance now: one reference per
node, an array made on the first sort, behind a struct that is equal to every other.

Four shapes were measured, and the three rejected ones are worth the lines. Three lazy slots
per node saved the same bytes and cost every node some fifty of its own, which the gate saw as
+4–6% on every solve and derivative entry. A dictionary kept for the run, keyed by reference,
saved the bytes and cost a lookup per node per sort: 369 ms against 266. And a bare array field
took part in the record's equality, so a tree that had been sorted stopped equalling its unsorted
twin and the simplifier, which recognises a repeated candidate by equality, never recognised one:
**30 GB and 57 s**, from a change whose unit test passed. HonkSharp's `LazyPropertyA` exists for
exactly that reason, and the struct here does what it does.

| benchmark | 1935th | 1936th | allocation | time |
|---|--:|--:|--:|--:|
| `SimplifyHard` | 680,457,400 | **442,898,152** | **−34.9%** | 339 → 258 ms |
| `SimplifyEasy` | 110,051 | 106,643 | −3.1% | 66.4 → 68.8 µs |
| every solve entry, `Derivate`, `ParseEasy` | | | +0.4% to +0.9% | within noise |

Bytes allocated per call, same machine, both columns measured by the gate in one session. The
fraction of a percent on the other rows is the one reference per node. Since the 1930th:
`SimplifyHard` **−88%**. Timings carry the usual rider; the gate's baseline was taken from this
run. No answer moves: the key is the same string, spelt once, and equality is untouched —
`SortKeyCacheTest` pins both, and that a `with` copy spells its own key.

## The 1935th: the solver tries the equation as written

The same hook, on `SolveHard` and attributed to the stages of `AnalyticalEquationSolver.Solve`
per recursion depth. Two things were done before any solving. The replacement machinery asked
`Entity.Alternate(4)` — a whole level-4 simplification of the equation, the search `Simplify`
runs — for every spelling before trying any, at every depth of its own recursion, and in the
ordinary case solved the first spelling it got, which was the equation itself: 268 MB of the
top-level call, 103 MB at the next depth, 20 MB below that. And the two polynomial-factoring
attempts every equation is offered — a rational root to split off, an irreducible factorisation —
each expanded the whole equation, a page of radicals included, to learn that a sine is not a
monomial: 3 MB a time, twice per equation visited, eighteen visits at one depth, 268 MB in all.

The loop now yields the equation as it stands and builds the alternatives only if it gets past
that — keeping a spelling that came back as a condition as a fallback while the next is tried —
and the coefficient extraction reads the tree before it expands anything.

| benchmark | 1933rd | 1935th | allocation | time |
|---|--:|--:|--:|--:|
| `SolveHard` | 859,268,416 | **11,818,936** | **−98.6%** | 700 → 109 ms |
| `SolveMediumHard` | 94,415,256 | **1,435,934** | **−98.5%** | 66.8 → 13.9 ms |
| `SolveMedium` | 662,923 | 452,901 | −31.7% | 456 → 387 µs |
| `SolveEasyMedium` | 97,335 | 65,232 | −33.0% | 29.7 → 19.2 µs |
| `SimplifyHard` | 733,188,536 | 680,457,400 | −7.2% | 371 → 339 ms |
| `SimplifyEasy` | 116,291 | 110,051 | −5.4% | 71.2 → 66.4 µs |

Bytes allocated per call, same machine, both columns measured by the gate in one session; every
other entry within 0.1%. The `Simplify` rows move through the same pre-check, which `Simplify`'s
own factoring candidates reach. Since the 1930th, this morning's column: `SolveHard` **−99.2%**,
`SolveMediumHard` −99.1%, `SimplifyHard` −81%. Timings carry the usual rider. The gate's baseline
was taken from this run.

One answer changes its spelling and nothing else moves: the condition on
`(x - b)/(x + a) + c/(x + a)`'s root has its terms in the order the equation had, since the
equation is solved as written rather than resimplified first. The `SolveHard` and
`SolveMediumHard` answers are the same size as before, 149,153 and 37,073 nodes.

## The 1933rd: a candidate registered once

The remaining registration, split by stage with the same hook: at level 4 on `SimplifyHard`,
363 registrations of which **298 were repeats** — a pass that changed nothing registered the same
tree again, and each registration was a `CanonicalOrder` rewrite (the `Sort` rules, some 2.4 MB a
time on this input) and an inner simplification before the history set declined the repeat:
874 MB of sorting and 718 MB of simplifying to add nothing. `Alternate` now keeps a set of what it
has registered and returns at once for a tree already in it. Same tree, same registration, so the
candidates are what they were.

| benchmark | 1931st | 1933rd | |
|---|--:|--:|--:|
| `SimplifyHard` | 2,027,771,744 | 733,188,536 | **−63.8%** |
| `SolveHard` | 1,057,996,016 | 859,268,416 | **−18.8%** |
| `SolveMediumHard` | 126,162,152 | 94,415,256 | **−25.2%** |
| `SimplifyEasy` | 115,763 | 116,291 | +0.5% |

Bytes allocated, same machine, same day as the two columns above. Since the 1930th, the morning's
column: `SimplifyHard` **−80%**, `SolveHard` −41%, `SolveMediumHard` −43%. `SimplifyEasy`'s 528
bytes more are the set itself, on an input with nothing to save. Timings, with the usual rider:
`SimplifyHard` 924 → 371 ms, `SolveHard` 772 → 700 ms, `SolveMediumHard` 78 → 67 ms,
`SimplifyEasy` 89 → 71 µs. The gate's baseline was taken from this run.

Two tests were re-pinned rather than loosened: they asserted the raw rewrite recording is twenty
times the derivation path and over a hundred steps, ratios calibrated to the re-sorting this
removes; it is 63 against 4 now, and the claim they make — the path is a small part of what
happened — is the same.

## The 1915th, and every release beside it on one machine

The first column measured by `Sources/Utils/benchmark_key_commits.sh` reaching all five entries in
`key-commits.txt`. Every figure below is the same benchmark against a different kernel, run on one
machine within one hour — which is what the four release-pair sections further down had to
approximate by measuring two commits at a time.

`v2.1.0` and `v2.2.0` are in it for the first time. They did not build before #1178: the benchmark
project carried cases reaching internals those kernels had not exposed, so the whole project failed
to compile and took the row with it, while the case that runs uses nothing but the public API.

### Allocation

| benchmark | v2.1.0 | v2.2.0 | v2.3.0 | v2.4.0 | 1915th |
|---|--:|--:|--:|--:|--:|
| `CompileEasy` | 16,519 | 16,309 | 16,206 | 11,003 | 10,970 |
| `CompileHard` | 38,542 | 38,089 | 38,355 | 20,396 | 20,434 |
| `Derivate` | 52,823 | 52,823 | 52,823 | 52,823 | 52,823 |
| `EvalTrig` | 1,341,377 | 1,341,377 | 1,341,377 | 1,341,377 | 1,341,377 |
| `EvalTrigPrecise` | 12,742,205 | 12,742,205 | 12,742,205 | 12,742,205 | 12,742,205 |
| `ParseEasy` | 18,061 | 18,061 | 18,061 | 18,109 | 18,104 |
| `ParseHard` | 3,498,137 | 3,498,137 | 3,522,409 | 3,522,457 | 3,531,201 |
| `SimplifyEasy` | 128,564 | 128,564 | 128,100 | 128,100 | 128,100 |
| `SimplifyHard` | 3,733,136,616 | 3,749,449,104 | 3,620,748,288 | 3,632,015,744 | 3,655,695,192 |
| `SolveEasy` | 20,220,106 | 20,234,998 | 8,852,269 | 8,853,671 | 8,853,834 |
| `SolveEasyMedium` | 80,184 | 95,873 | 95,791 | 97,193 | 97,335 |
| `SolveHard` | 1,356,525,608 | 1,486,580,536 | 1,431,859,752 | 1,446,799,616 | 1,450,058,720 |
| `SolveMedium` | 554,368 | 658,259 | 658,099 | 661,963 | 662,923 |
| `SolveMediumHard` | 155,191,480 | 171,395,096 | 162,305,664 | 164,549,808 | 165,055,344 |

Bytes allocated. `EvalEasy`, `RunEasy`, `RunMedium` and `RunHard` allocate nothing in every column
and are left out.

**Since 2.4.0, the Simplify and Solve family is up between 0.15% and 0.65%** — `SimplifyHard`
+0.65%, `SolveMediumHard` +0.31%, `SolveHard` +0.23%. Small, consistent in sign, and real: see the
next section for why 0.65% is far above the noise on those particular rows. It is well inside
`PerformanceGate`'s 3% and is not a reason to hold a release; it is a reason not to claim the period
was free. The likeliest cause is the definedness conditions added in #1176 and #1177, which give
every cancelled quotient a larger condition tree to carry.

`CompileEasy` and `CompileHard` are flat against 2.4.0 and roughly **half** what they were at 2.3.0.

### The determinism claim, measured

This file says allocation is deterministic — "the same commit measured twice gives the same bytes".
The five columns above were run **twice**, and that is true for the large benchmarks and false for
the small compile ones:

| same commit, two runs | run 1 | run 2 | apart |
|---|--:|--:|--:|
| `SolveHard` @ v2.1.0 | 1,356,524,520 | 1,356,525,608 | **0.00008%** |
| `SimplifyHard` @ v2.1.0 | 3,733,177,488 | 3,733,136,616 | **0.001%** |
| `CompileEasy` @ v2.1.0 | 16,280 | 16,519 | **1.47%** |
| `CompileEasy` @ v2.3.0 | 16,510 | 16,206 | **1.84%** |

So the rule this file states holds where it matters most and needs a rider: **the compile
benchmarks have an allocation floor of about 2%.** That is worth knowing precisely because
`PerformanceGate` fails at 3% — for `CompileEasy` that is one and a half noise-widths of margin, not
the comfortable threshold the number suggests. A `CompileEasy` allocation move under 2% is not
evidence; the same move on `SolveHard` is evidence a thousand times over.

Timings from the same run are in the generated table and are deliberately not reproduced here, for
the reason the next section gives.

## The 1844th: what this file's timings can and cannot support

The column for 2.4.0. Seventy-five commits from the 1769th, carrying the rule-set exchange --
twenty-seven of thirty sets running as data -- Hensel lifting along an evaluation homomorphism,
and four wrong answers found by writing rules out as data.

**Read the allocation column and treat the timings as indicative.** That is not the usual caution;
it was measured. The Kernel Benchmark was run **twice on this same commit**, nothing changed
between them:

| | run 1 | run 2 | apart |
|---|--:|--:|--:|
| ParseEasy | 12,593 ns | 6,846 ns | 46% |
| SimplifyEasy | 197,016 ns | 111,852 ns | 43% |
| SolveHard | 1,638,350,518 ns | 887,253,553 ns | 46% |
| RunEasy | 50 ns | 21 ns | 57% |
| *every one of eighteen* | | | **30-57%** |

Allocation over the same pair of runs: `SimplifyEasy` 128,098 B against 128,096 B, `SolveHard`
1,446,783,288 B against 1,446,779,520 B. **A thousandfold difference in reproducibility**, which
is exactly the argument `PerformanceGate` makes for gating allocation and not time -- restated
here because the columns above invite a comparison the numbers cannot carry.

The table takes run 2. Run 1 is recorded here so the choice is visible rather than quiet.

### What this means for the columns above

A timing difference between two columns of this file is evidence only if it is larger than about
50%. The 1769th's write-up reads `-7%`, `-10%`, `-15%` as improvements and concludes *"every row
is faster or flat"*; on this measurement none of those figures is distinguishable from the runner,
and only its `SolveEasy` row -- 21.3 ms to 8.5 ms, a 2.5x move with allocation dropping 2.3x
alongside -- survives. The rule it states is the right one and it is the allocation half that
carries it.

### The exchange cost 13% of `SimplifyEasy`, measured somewhere this file cannot measure

That is a real finding and it is **not** from the columns here. On one desktop, both arms built
and run the same way, BenchmarkDotNet reporting a standard deviation under half a per cent:

| | `SimplifyEasy` |
|---|--:|
| 1769th, before the rule sets became data | 82,676 ns |
| after the exchange, before the node-type index | 148,223 ns |
| after the index | 94,904 ns |
| 1843rd | 93,732 ns |

Every conversion was measured against the commit in front of it and every one came back free or
better. **Nothing was measured against the start**, and the sum of a run of individually-free
steps is +13%. The index recovered most of what the exchange spent; *most* is not *all*.

Where it goes, from counting rather than guessing: the work shape is identical -- 4,914 rule-set
invocations on this input at both commits -- so it is per-operation overhead, roughly 1.3 ns on
each invocation and 14 ns on each rule attempt. `InequalityEquality` alone is 26% of it and
`InvertNegativePowers`, a set with one rule, is 15%. Three attempts to make the dispatch cheaper
-- caching the per-type rule list one slot, eight slots, and replacing the dictionary with an
array index -- each measured, and none moved it. About 40% of the gap is still unattributed.

The lever, if it is wanted, is the invocation count rather than the dispatch: 4,914 for one
`Simplify`, of which 2,202 are the canonical order running on every node.

---

## The 1769th: ninety-eight commits, and nothing slower

Taken by the Kernel Benchmark workflow on `ubuntu-latest`, the same runner class as the 1620th and
the 1671st, so this trio is comparable in a way the earlier columns are not.

Ninety-eight commits separate it from the 1671st -- the same size of gap this file complains about
above -- and they carry the eight behaviour fixes recorded in `BREAKING-CHANGES.md` as unreleased,
along with the rule-registry work. **Every row is faster or flat.**

| Method | 1671st | 1769th | change |
|---|--:|--:|--:|
| ParseEasy | 10,857 ns | 10,080 ns | -7% |
| ParseHard | 2,493,337 ns | 2,235,852 ns | -10% |
| SimplifyEasy | 152,631 ns | 136,841 ns | -10% |
| SimplifyHard | 2,916,127,943 ns | 2,489,408,789 ns | -15% |
| Derivate | 25,629 ns | 23,674 ns | -8% |
| **SolveEasy** | 21,322,368 ns | 8,530,148 ns | **-60%** |
| SolveEasyMedium | 48,863 ns | 50,203 ns | +3% |
| SolveMedium | 796,814 ns | 696,969 ns | -13% |
| SolveMediumHard | 129,750,583 ns | 117,103,179 ns | -10% |
| SolveHard | 1,365,230,469 ns | 1,144,927,429 ns | -16% |
| EvalTrig | 1,344,115 ns | 1,280,227 ns | -5% |
| EvalTrigPrecise | 43,285,515 ns | 37,048,158 ns | -14% |
| CompileEasy | 352,449 ns | 314,521 ns | -11% |
| CompileHard | 596,664 ns | 585,717 ns | -2% |
| RunEasy | 42 ns | 28 ns | -34% |

**`SolveEasy` is the one worth reading.** 21.3 ms to 8.5 ms is not a few per cent, and its
allocation moves with it -- 20,213,448 B to 8,852,065 B, a 2.3x drop against a 2.5x speedup. A
timing that moves alone on this file's history has usually been the machine; a timing and an
allocation that move together by the same factor are the work.

`SolveEasyMedium` at +3% is the only row going the other way, and 3% is inside what the note at the
top of this file says to ignore. Its allocation *rose* 20% (79,613 B to 95,788 B), which is the one
thing here that is not explained; it is small in absolute terms -- 16 KB -- and worth a look if a
later column repeats it rather than chasing now.

---

## The 1671st and the 1697th, measured together on one machine

**Why a pair rather than a column.** 2.0.0 shipped without any performance measurement at all, so
there is no column for it: it is commit 1691 and the table above stops at the 1671st. Filling that gap
with a single new column would have been worse than leaving it. Measured on a developer machine, every
one of the sixteen rows came out at 0.5–0.6× of the 1671st — a uniform factor across the board, which
is a faster machine and not faster code. A reader comparing the two columns would have seen an
across-the-board 1.75× improvement that does not exist, which is exactly the mistake the note at the
top of this file warns about.

So the 1671st was re-measured on the same machine, minutes apart, with nothing else running. Only the
right-hand pair below may be read as a ratio; neither may be compared with the columns above.

The 1697th is `6476513b`, which is 2.0.0 plus the five corrections merged after it and the `abs`/`sgn`
fix — so this answers "did the release and the fixes after it cost anything", which is the question
2.0.1 needs answered.

| Method | [1671st](https://github.com/asc-community/AngouriMath/commit/253b5a8d598a9a1d721b777340bd53ae3be12f99) | [1697th](https://github.com/asc-community/AngouriMath/commit/6476513b) | change |
|---|--:|--:|--:|
| ParseEasy | 5,887 ns | 6,052 ns | +2.8% |
| ParseHard | 1,364,570 ns | 1,430,477 ns | +4.8% |
| SimplifyEasy | 79,832 ns | 86,744 ns | **+8.7%** |
| SimplifyHard | 1,646,740,009 ns | 1,697,337,871 ns | +3.1% |
| Derivate | 13,745 ns | 14,367 ns | +4.5% |
| SolveEasy | 11,302,282 ns | 11,883,543 ns | +5.1% |
| SolveEasyMedium | 24,545 ns | 24,438 ns | −0.4% |
| SolveMedium | 420,926 ns | 433,918 ns | +3.1% |
| SolveMediumHard | 74,401,542 ns | 76,053,093 ns | +2.2% |
| SolveHard | 751,607,244 ns | 766,020,069 ns | +1.9% |
| EvalEasy | 1.34 ns | 0.85 ns | see below |
| EvalTrig | 720,189 ns | 711,009 ns | −1.3% |
| EvalTrigPrecise | 22,870,710 ns | 23,086,271 ns | +0.9% |
| CompileEasy | 191,188 ns | 191,289 ns | +0.1% |
| CompileHard | 320,897 ns | 323,255 ns | +0.7% |
| RunEasy | 20.12 ns | 20.24 ns | +0.6% |
| RunMedium | 163.8 ns | 169.9 ns | +3.7% |
| RunHard | 290.6 ns | 296.9 ns | +2.2% |

Allocation over the same pair: `SimplifyHard` 3,710,538,664 → 3,737,029,480 B (+0.7%), `SolveHard`
1,341,724,616 → 1,356,514,512 B (+1.1%), `SolveMediumHard` 152,244,664 → 155,187,208 B (+1.9%);
everything else within a few tenths of a per cent.

**Read: no regression, with one row worth watching.** Most of the table moves 1–5%, which by this
file's own standard says nothing. `SimplifyEasy` at **+8.7%** is above that, and there is a plausible
cause rather than a mystery: the corrections merged in that range added guards to the simplification
patterns — `WithinHalfPi`, `WithinArccotanRange`, and the `abs`/`sgn` zero test — and each calls
`Evaled` inside a pattern's `when` clause on a path that previously matched structurally. `Evaled` is
cached per node, so the cost is a first-call computation and a lookup thereafter, but it is not free
and it is on the hot path. Worth a measurement of its own before more guards are written that way.

`EvalEasy` is not a real movement in either direction. It measures a cached lookup rather than
arithmetic — see the note above the main table — and at one nanosecond the figure is dominated by
whatever the harness itself costs.

## The 1691st and the 1707th, measured together on one machine — the pair for 2.1.0

`v2.0.0` itself against the 2.1.0 candidate: the 1691st is
[`f2594259`](https://github.com/asc-community/AngouriMath/commit/f2594259), tagged `v2.0.0`, and the
1707th is [`9446c762`](https://github.com/asc-community/AngouriMath/commit/9446c762), sixteen commits
later. Both measured minutes apart in one session on one machine, so **only these two columns may be
read against each other**; neither may be compared with anything above.

This is the question a release needs answered — what the version users have costs against the version
they are being offered — which the pair above could not answer, since it took the 1697th rather than
the tag.

| Method | [1691st](https://github.com/asc-community/AngouriMath/commit/f2594259) | [1707th](https://github.com/asc-community/AngouriMath/commit/9446c762) | change |
|---|--:|--:|--:|
| ParseEasy | 5,920 ns | 5,703 ns | −3.7% |
| ParseHard | 1,343,362 ns | 1,339,656 ns | −0.3% |
| SimplifyEasy | 81,620 ns | 83,340 ns | +2.1% |
| SimplifyHard | 1,574,777,032 ns | 1,614,280,250 ns | +2.5% |
| Derivate | 13,510 ns | 13,741 ns | +1.7% |
| SolveEasy | 11,139,199 ns | 11,383,594 ns | +2.2% |
| SolveEasyMedium | 23,437 ns | 23,733 ns | +1.3% |
| SolveMedium | 397,908 ns | 416,180 ns | +4.6% |
| SolveMediumHard | 71,646,943 ns | 71,756,037 ns | +0.2% |
| SolveHard | 770,417,841 ns | 761,848,527 ns | −1.1% |
| EvalEasy | 1.211 ns | 1.336 ns | +10.3% |
| EvalTrig | 701,704 ns | 703,612 ns | +0.3% |
| EvalTrigPrecise | 22,673,032 ns | 22,585,377 ns | −0.4% |
| CompileEasy | 186,889 ns | 187,557 ns | +0.4% |
| CompileHard | 319,797 ns | 319,365 ns | −0.1% |
| RunEasy | 20.36 ns | 19.99 ns | −1.8% |
| RunMedium | 163.9 ns | 168.6 ns | +2.9% |
| RunHard | 293.0 ns | 294.6 ns | +0.5% |

Allocation over the same pair: `SimplifyEasy` 127,964 → 128,564 B (+0.5%), `CompileEasy`
16,207 → 16,408 B (+1.2%), `ParseHard` 3,486,001 → 3,498,137 B (+0.3%), `SimplifyHard`
3,737,107,808 → 3,733,175,840 B (−0.1%). `Derivate`, `EvalTrig` and `EvalTrigPrecise` are
**byte-identical**, and the remaining rows move by tens of bytes on totals in the millions.

**Read: no regression.** The largest real move is `SolveMedium` at +4.6%, which by the standard at the
top of this file says nothing on its own; the rest sits inside ±3%. `EvalEasy` at +10.3% is 0.125 ns and
is the cached-lookup row again.

### It also corrects the section above

That pair reported `SimplifyEasy` at **+8.7%**, attributed it to guards added in that range calling
`Evaled` inside a pattern's `when` clause, and asked for "a measurement of its own before more guards
are written that way". Since then roughly six more guards of exactly that shape were added — the
interval tests of [#887](https://github.com/asc-community/AngouriMath/issues/887), the zero test of
[#892](https://github.com/asc-community/AngouriMath/issues/892), the sign read of
[#881](https://github.com/asc-community/AngouriMath/issues/881), the positivity check of
[#902](https://github.com/asc-community/AngouriMath/issues/902) and the sort guard of
[#897](https://github.com/asc-community/AngouriMath/issues/897).

`SimplifyEasy` is **+2.1%** here, and its allocation moves 0.5%. If the guards were the cause, that row
should have moved further and allocation should have followed, since `Evaled` allocates on its first
call per node. Neither happened. **So the +8.7% was mostly the machine rather than the guards, and the
attribution in the section above was wrong** — which is the same error that section itself was written
to catch, made one level down: a single number read as a cause without a second measurement to hold it
against. The action item it raised is discharged, and the answer is that this pattern is not
measurably expensive at this scale.

## The 1709th and the 1724th, measured together on one machine — the pair for 2.2.0

`v2.1.0` against the 2.2.0 candidate: the 1709th is
[`b8cf4dcd`](https://github.com/asc-community/AngouriMath/commit/b8cf4dcd), tagged `v2.1.0`, and the
1724th is [`03a0bf64`](https://github.com/asc-community/AngouriMath/commit/03a0bf64), fifteen commits
later. Measured minutes apart in one session on one machine, so **only these two columns may be read
against each other**; neither may be compared with anything above.

| Method | [1709th](https://github.com/asc-community/AngouriMath/commit/b8cf4dcd) | [1724th](https://github.com/asc-community/AngouriMath/commit/03a0bf64) | change | allocated, 1709th | allocated, 1724th |
|---|--:|--:|--:|--:|--:|
| ParseEasy | 5,746 ns | 5,747 ns | +0.0% | 18,061 B | 18,061 B |
| ParseHard | 1,325,558 ns | 1,369,170 ns | +3.3% | 3,498,137 B | 3,498,137 B |
| SimplifyEasy | 82,305 ns | 81,872 ns | −0.5% | 128,564 B | 128,564 B |
| SimplifyHard | 1,590.6 ms | 1,581.7 ms | −0.6% | 3,733,136,832 B | 3,749,448,792 B |
| Derivate | 13,461 ns | 13,565 ns | +0.8% | 52,811 B | 52,823 B |
| SolveEasy | 11,208,548 ns | 11,177,085 ns | −0.3% | 20,220,106 B | 20,234,998 B |
| **SolveEasyMedium** | 23,509 ns | 28,503 ns | **+21.2%** | 80,184 B | **95,873 B** |
| **SolveMedium** | 411,557 ns | 461,734 ns | **+12.2%** | 554,368 B | **658,259 B** |
| **SolveMediumHard** | 74.9 ms | 85.4 ms | **+14.0%** | 155,190,904 B | **171,395,096 B** |
| **SolveHard** | 800.2 ms | 858.0 ms | **+7.2%** | 1,356,524,520 B | **1,486,581,640 B** |
| EvalEasy | 1.334 ns | 1.332 ns | −0.1% | — | — |
| EvalTrig | 706,201 ns | 700,257 ns | −0.8% | 1,341,377 B | 1,341,377 B |
| EvalTrigPrecise | 22,548,629 ns | 22,583,915 ns | +0.2% | 12,742,205 B | 12,742,205 B |
| CompileEasy | 186,227 ns | 188,554 ns | +1.2% | 16,191 B | 16,309 B |
| CompileHard | 321,647 ns | 319,845 ns | −0.6% | 38,377 B | 37,755 B |
| RunEasy | 20.117 ns | 20.047 ns | −0.3% | — | — |
| RunMedium | 165.849 ns | 163.145 ns | −1.6% | — | — |
| RunHard | 293.908 ns | 291.365 ns | −0.9% | — | — |

### Four solver rows moved, and the allocation moved with them

Everything that is not the solver is flat — parse, simplify, evaluate and the compiled-call trio all
within 2%, with allocation byte-identical on most rows. Four `Solve*` rows are 7% to 21% slower and
allocate 10% to 20% more.

**Allocation moving with the timing is what makes this a real change rather than the machine.** The
pair above this one is the cautionary case: a row reported +8.7% with allocation flat, and
re-measuring put it at +2.1%. Here four related rows move together in both, which noise does not do.

### It is [#918](https://github.com/asc-community/AngouriMath/pull/918), and it is a price rather than a regression

Isolated by measuring the commits either side of it. Allocation steps exactly once, at the polynomial
layer, and is identical before and after within each half:

| SolveMedium, allocated | |
|---|--:|
| the 1709th, `v2.1.0` | 554,368 B |
| [`51194ce8`](https://github.com/asc-community/AngouriMath/commit/51194ce8), before #918 | 554,368 B |
| [`69f66da7`](https://github.com/asc-community/AngouriMath/commit/69f66da7), after #918 | 658,259 B |
| the 1724th | 658,259 B |

The same clean step appears in all four rows. `SolveEasy` — a quadratic, which never reaches the
factorisation path — is flat throughout, which is the mechanism corroborating itself.

#918 made the equation solver the polynomial layer's first consumer: where a polynomial factors over
`Q`, each factor is now solved as a lower-degree equation of its own. That is what turned
`x^5 + 2x^3 - 2x^2 - 4` from three roots, one of them a float, into all five, exact. An incomplete
solution set is not a partial answer but a false one, so **this cost buys a wrong answer being
right**, and by the first rule in [AGENTS.md](../../../../AGENTS.md) it is the correct trade. It is
recorded here rather than fixed.

### What the suite cannot see, which is worth more than the rows it can

**None of these ten solver benchmarks benefit from #918.** They are quadratics, a substituted
quadratic and a trigonometric substitution; not one of them factors into lower-degree pieces, so
every one pays the search and none collects the answer. The column therefore shows the change as
pure cost, which is true of these inputs and false of the change.

A benchmark whose polynomial *does* factor — the quintic from #918's own changelog entry is the
obvious candidate — is owed before the next column, or this row will keep reporting a correctness
fix as a slowdown for as long as anyone reads it.
