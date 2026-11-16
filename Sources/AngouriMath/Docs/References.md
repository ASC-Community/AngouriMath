# References

The books, papers, corpora and libraries this code and its tests draw on, with what each is used
for. A citation in a comment, a test summary or a changelog entry names the work as it is named
here — *Sullivan and Mackey's Ex 5.3.2*, *Bronstein §2.2* — and this page carries the full title
and the link. Add a work here when it is first cited.

## Textbooks

- **Sullivan, Brendan W., with Mackey, John.** *Everything You Always Wanted To Know About
  Mathematics (But didn't even know to ask): A Guided Journey Into the World of Abstract
  Mathematics and the Writing of Proofs.* Carnegie Mellon University, 2013.
  <https://www.math.cmu.edu/~jmackey/151_128/bws_book.pdf>.
  The proofs book of [#1409](https://github.com/asc-community/AngouriMath/issues/1409): sets,
  subsets and power sets, indexed unions and intersections, the integer range `[n]`, quantified
  statements, congruences and residue classes, image and pre-image, injectivity and surjectivity,
  binomial identities, and induction — its examples and exercises are the rows of
  `QuantifierTest`, `SubsetTest`, `IndexedSetOperationTest`, `ImageAndPreImageTest`,
  `BinomialIdentityTest`, `InductionTest` and `ThresholdSetTest`.
- **Modenov, P. S.** *Problems in Geometry.* Translated from the Russian by George Yankovsky.
  Mir, Moscow, 1981. <https://archive.org/details/ModenovProblemsInGeometryMir1981>.
  Read for [#2131](https://github.com/asc-community/AngouriMath/issues/2131): vector algebra,
  analytic geometry, complex numbers in plane geometry, inversion. Its computations, and the
  errors found in it by computing, are the rows of the `ModenovCorpus` harness
  ([#2153](https://github.com/asc-community/AngouriMath/issues/2153)), cited by chapter, section
  and problem.
- **Bronstein, Manuel.** *Symbolic Integration I: Transcendental Functions.* 2nd ed., Springer,
  2005. The Hermite reduction, the rational and logarithmic parts, and the Risch structure the
  integrator follows.
- **von zur Gathen, Joachim, and Gerhard, Jürgen.** *Modern Computer Algebra.* 3rd ed., Cambridge
  University Press, 2013. The polynomial layer: gcds, factorisation, resultants.
- **Gruntz, Dominik.** *On Computing Limits in a Symbolic Manipulation System.* PhD thesis, ETH
  Zürich, 1996. The limit algorithm.
- **Timofeev, A. F.** *Integration of Functions* (Интегрирование функций). 1948. Its integrals are
  a family of Rubi's test suite, and are cited by his name where one of them is the row.

## Corpora

- **Rubi — Rule-based Integration** (Albert D. Rich et al.), <https://rulebasedintegration.org/>,
  and its test problems, <https://rulebasedintegration.org/testProblems.html>: the integration
  corpus the integrator is measured against
  ([#718](https://github.com/asc-community/AngouriMath/issues/718)).
- **Wester, Michael.** *A Critique of the Mathematical Abilities of CA Systems*, in *Computer
  Algebra Systems: A Practical Guide*, Wiley, 1999. The Wester problems of `Docs/Usage/Comparison.md`.

## Reference works

- **NIST Digital Library of Mathematical Functions (DLMF)**, <https://dlmf.nist.gov/>: the
  identities and branch cuts of the special functions, cited by section (`DLMF 4.23`).
- **Wolfram MathWorld**, <https://mathworld.wolfram.com/>: conventions and the names of things,
  where a convention is chosen and the choice recorded.
- **mathlib4**, <https://leanprover-community.github.io/mathlib4_docs/>: the hypotheses of an
  identity, machine-checked, which is what a rewrite rule's contract is checked against
  (`Contributing/SimplificationContract.md`).
- **SymPy**, <https://www.sympy.org/>: readable reference implementations of Risch, Gruntz and
  Gröbner, with the papers cited in the docstrings; the target of `ToSympyCode`.
