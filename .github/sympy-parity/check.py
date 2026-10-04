# Re-runs the SymPy half of the parity survey against whatever SymPy pip installs today and
# compares it with the snapshot in sympy-baseline.json.
#
# Only SymPy runs here. The survey's other half asks AngouriMath the same questions, and that
# half is not in this repository, so nothing below compares the two libraries or restates a
# verdict about either. What it answers is the narrower question that the published snapshot
# cannot answer about itself: is SymPy still the version, and still giving the answers, that
# the snapshot was taken from.
#
# Exit code 1 means the snapshot no longer describes SymPy, and a human has to re-take it.
# https://github.com/asc-community/AngouriMath/issues/746 item 73.

import json
import os
import signal
import sys

import sympy
from sympy import *                                    # noqa: F401,F403  the probes are written against the flat namespace
from sympy.abc import x, y, k, n, s, a, b, c           # noqa: F401

# Every import below serves at least one probe. A release that moves or removes one of these is
# itself a finding, so an import failure is recorded and left to surface as the probes that need
# it failing, rather than taking the whole run down before anything is measured.
FAILED_IMPORTS = []
for statement in (
    "import sympy.ntheory.modular",
    "from sympy.stats import Die, P",
    "from sympy.logic.boolalg import to_cnf",
    "from sympy.logic.inference import satisfiable",
    "from sympy.combinatorics import PermutationGroup",
    "from sympy.vector import CoordSys3D",
    "from sympy.tensor.tensor import TensorIndexType",
    "from sympy.diffgeom import Manifold",
    "from sympy.holonomic import expr_to_holonomic",
    "from sympy.physics.units import convert_to",
    "from sympy.printing.mathml import mathml",
    "from sympy.discrete import convolution, fft",
    "from sympy.simplify.sqrtdenest import sqrtdenest",
    "from sympy.polys.numberfields import minimal_polynomial",
    "from sympy.ntheory.continued_fraction import continued_fraction_iterator",
    "from sympy.sets.conditionset import ConditionSet",
    "from sympy.integrals.transforms import laplace_transform, fourier_transform",
):
    try:
        exec(statement)
    except Exception as e:                             # noqa: BLE001  any import failure is data
        FAILED_IMPORTS.append(f"{statement} -- {type(e).__name__}: {e}")


class Timeout(Exception):
    pass


def _alarm(sig, frm):
    raise Timeout()


signal.signal(signal.SIGALRM, _alarm)

here = os.path.dirname(os.path.abspath(__file__))
probes = json.load(open(os.path.join(here, "probes.json")))
baseline = json.load(open(os.path.join(here, "sympy-baseline.json")))

answers = []
for p in probes:
    signal.alarm(20)
    try:
        value = str(eval(p["sympy"]))
    except Timeout:
        value = "*** timeout (>20s) ***"
    except Exception as e:                             # noqa: BLE001  an exception is the answer
        value = f"!!! {type(e).__name__}: {e}".split("\n")[0]
    finally:
        signal.alarm(0)
    answers.append({"area": p["area"], "what": p["what"], "sympy": value})

current = {"sympy": sympy.__version__, "answers": answers}
with open(os.path.join(here, "sympy-current.json"), "w") as f:
    json.dump(current, f, indent=1)

was = {(r["area"], r["what"]): r["sympy"] for r in baseline["answers"]}
now = {(r["area"], r["what"]): r["sympy"] for r in answers}

moved = [(k2, was[k2], now[k2]) for k2 in now if k2 in was and was[k2] != now[k2]]
added = sorted(k2 for k2 in now if k2 not in was)
gone = sorted(k2 for k2 in was if k2 not in now)
version_moved = baseline["sympy"] != sympy.__version__


def cell(t, at=70):
    t = t.replace("|", "\\|").replace("\n", " ")
    return t if len(t) <= at else t[: at - 1] + "…"


lines = [
    "## SymPy parity watch",
    "",
    f"- baseline: **SymPy {baseline['sympy']}**, taken {baseline['measured']}",
    f"- installed today: **SymPy {sympy.__version__}**",
    f"- {len(answers)} probes run, **{len(moved)}** answer{'' if len(moved) == 1 else 's'} moved",
    "",
]
if FAILED_IMPORTS:
    lines += ["Imports that no longer resolve:", ""] + [f"- `{i}`" for i in FAILED_IMPORTS] + [""]
if moved:
    lines += ["| area | capability | was | is now |", "|---|---|---|---|"]
    lines += [
        f"| {area} | {what} | `{cell(old)}` | `{cell(new)}` |" for (area, what), old, new in sorted(moved)
    ]
    lines.append("")
if added or gone:
    lines += [f"Probes added since the baseline: {added}", f"Probes gone since the baseline: {gone}", ""]

summary = "\n".join(lines)
print(summary)

step_summary = os.environ.get("GITHUB_STEP_SUMMARY")
if step_summary:
    with open(step_summary, "a") as f:
        f.write(summary + "\n")

if not (version_moved or moved or added or gone):
    print(f"The snapshot still describes SymPy {sympy.__version__}.")
    sys.exit(0)

# A version bump on its own is a failure because the published comparison names the version it
# measured; a version it does not name is a version it did not measure, whether or not any of
# these 80 answers happens to have moved with it.
print(
    "\nThe snapshot no longer describes SymPy. Re-run work/sympyparity end to end against "
    f"SymPy {sympy.__version__}, republish the comparison, and replace sympy-baseline.json "
    "with the sympy-current.json this run uploaded.",
    file=sys.stderr,
)
sys.exit(1)
