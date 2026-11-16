#!/usr/bin/env bash
#
# Runs the inter-version CPU benchmark against every commit in key-commits.txt and writes one
# table of what it measured.
#
# https://github.com/asc-community/AngouriMath/issues/529
#
# The benchmark that runs is always the one at HEAD, overlaid onto each checked-out commit, so
# that what differs between rows is the kernel rather than the question. #529 calls this
# "replaces the Program.cs".
#
# Each commit is checked out *whole* rather than just Sources/AngouriMath, which was tried first
# and does not work: the kernel's rule sets are produced by a source generator that lives
# elsewhere in the tree, so a kernel on its own is missing `Patterns.SortRulesArms` and will not
# compile. Measured, not guessed.
#
# A commit that will not build is reported as such rather than skipped. The benchmark at HEAD
# calls the public API as it is now, so an old enough commit genuinely cannot be measured this
# way, and that is worth seeing rather than silently omitting.
#
# What stopped older commits building was not that case, though. It was the *other* cases in the
# same project -- MatchingEngine reaches internals that were internal until v2.3.0, and
# TransformationLayer names Transformation.Rationalization, which did not exist at v2.1.0 -- so
# the project failed to compile and took the row with it while the case that runs would have
# compiled fine. `-p:InterVersionOnly=true` leaves those out. It is passed for every row, so the
# columns still compare: the same file is compiled and the same file runs in each of them.
#
# Run from anywhere.

set -uo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repository="$(cd "$here/../.." && pwd)"
benchmark="Sources/Tests/DotnetBenchmark"
list="$repository/$benchmark/key-commits.txt"
work="${BENCHMARK_WORK_DIR:-$(mktemp -d)}"
out="${BENCHMARK_OUTPUT:-$repository/$benchmark/benchmark_results.csv/key-commits.md}"

if [ ! -f "$list" ]; then
    echo "no key-commits.txt at $list" >&2
    exit 1
fi

mkdir -p "$(dirname "$out")"
results="$work/results"
mkdir -p "$results"

commits=$(grep -vE '^\s*(#|$)' "$list")
echo "measuring:"; echo "$commits" | sed 's/^/  /'

# A result is keyed by the entry's position in the list, not by the entry. Naming one commit
# twice, interleaved with the change, is how the runner's spread is measured in the same run, and
# keyed by name the second measurement overwrote the first: the 2.8.0 pair's table showed the
# later one in both columns, identical to the nanosecond, and the control silently was not there
# (https://github.com/asc-community/AngouriMath/issues/2488). combine_key_commits.py reads the
# same positions.
position=0
for commit in $commits; do
    position=$((position + 1))
    key="$position"
    resolved=$(git -C "$repository" rev-parse --short "$commit" 2>/dev/null)

    # A branch name resolves to whatever this checkout's copy of it points at, which is not
    # necessarily the tip. In a worktree the local `master` belongs to whichever checkout last
    # moved it: this measured a `master` five days behind its remote and reported it as `master`,
    # and only the SHA printed beside the row gave it away. Say so rather than quietly measure
    # the wrong commit.
    upstream=$(git -C "$repository" rev-parse --short "origin/$commit" 2>/dev/null)
    if [ -n "$upstream" ] && [ "$upstream" != "$resolved" ]; then
        echo "=== $commit: local is $resolved, origin/$commit is $upstream -- measuring the local one" >&2
        echo "    put the SHA you mean in key-commits.txt if that is not what you want" >&2
    fi
    if [ -z "$resolved" ]; then
        echo "=== $commit: not a commit in this repository"
        echo "unresolved" > "$results/$key.status"
        continue
    fi

    echo "=== $commit ($resolved)"
    tree="$work/$key"
    rm -rf "$tree"; mkdir -p "$tree"
    git -C "$repository" archive "$commit" | tar -x -C "$tree" || { echo "archive failed" > "$results/$key.status"; continue; }

    # The benchmark from HEAD, so every row is asked the same question.
    rm -rf "$tree/$benchmark"
    mkdir -p "$tree/Sources/Tests"
    cp -r "$repository/$benchmark" "$tree/Sources/Tests/"
    rm -rf "$tree/$benchmark/bin" "$tree/$benchmark/obj" "$tree/$benchmark/benchmark_results.csv"

    # InterVersionOnly compiles the one case this run executes and leaves out the rest. A case
    # that reaches API an older kernel did not have stops the whole project compiling and takes
    # the row with it: MatchedRules and Patterns were internal until v2.3.0, and
    # Transformation.Rationalization did not exist until after v2.1.0. Passed for every row
    # including master, so what is compiled does not vary between columns.
    if ! (cd "$tree/$benchmark" && dotnet build -c Release -p:InterVersionOnly=true > "$results/$key.build.log" 2>&1); then
        echo "  did not build — see $results/$key.build.log"
        echo "did not build" > "$results/$key.status"
        continue
    fi
    if ! (cd "$tree/$benchmark" && dotnet run -c Release --no-build CommonFunctionsInterVersion > "$results/$key.run.log" 2>&1); then
        echo "  did not run — see $results/$key.run.log"
        echo "did not run" > "$results/$key.status"
        continue
    fi

    measured="$tree/$benchmark/benchmark_results.csv/measured-CommonFunctionsInterVersion.json"
    if [ -f "$measured" ]; then
        cp "$measured" "$results/$key.json"
        echo "measured $resolved" > "$results/$key.status"
        echo "  measured"
    else
        echo "ran but wrote nothing" > "$results/$key.status"
        echo "  ran but wrote no results file"
    fi
done

python3 "$here/combine_key_commits.py" "$list" "$results" "$out" || exit 1
echo
echo "wrote $out"
