# CLAUDE.md

The instructions for this repository are in **[AGENTS.md](AGENTS.md)**. Read it before doing
anything else here; this file exists only so that tools looking for `CLAUDE.md` find their way there.

Four things from it that are most often learned the hard way, so that they are visible even if
nothing else is read:

1. **Be a mathematician first.** When correctness and backward compatibility disagree, correctness
   wins — and every changed answer is recorded in [BREAKING-CHANGES.md](BREAKING-CHANGES.md), measured
   on a build of each version rather than read off a diff.
2. **Not answering is a legitimate answer; answering wrongly is not.** Unevaluated means "I could not
   settle this", `NaN` means "this does not exist", and confusing them ships a wrong answer.
3. **Read [#746](https://github.com/asc-community/AngouriMath/issues/746) before a release or a
   version number.** Its `v1.0`–`v9.0` are capability tiers, not shipping versions, and it names
   conditions — measured performance, deliberate package boundaries — that a release has to meet. See
   *Read the roadmap before you release anything* in AGENTS.md.
4. **Before adding or changing a simplification rule**, read
   [`Contributing/SimplificationContract.md`](Sources/AngouriMath/Docs/Contributing/SimplificationContract.md).
   A rule states the assumptions under which it holds, or it is asserting there are none.

Ten measurement harnesses live in `Sources/Tests/Harnesses` and run in CI on every change to the
library: the boundary checker, root completeness, the self-verifying solver corpus, the property
checker, the simplification sweep, the rule-set checker, a crash harness that survives a stack
overflow, the canonical-form checker, the arm-order checker, and a checker for the code samples in
the wiki and on the website. Each fails on a defect, or the canonical-form and arm-order checkers on
a change to the findings they list; the README says which. The rest are still in the analysis
workspace one directory up (`work/`). Run them before claiming anything is fixed.

There is also a *gate*, which is not a harness:
`Sources/Tests/UnitTests/Corpus` runs forty problems on every commit and reports **solved / unsolved
/ wrong / error / timeout**. It fails on a wrong answer, and on any case that stops matching its
recorded verdict — including one that improves, which is a prompt to record the improvement rather
than a complaint. See *The corpus runs on every commit* in [AGENTS.md](AGENTS.md).
