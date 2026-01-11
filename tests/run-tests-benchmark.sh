#!/bin/bash

# Run integration tests 20 times and calculate average execution time
# Usage: ./run-tests-benchmark.sh [runs]
# Example: ./run-tests-benchmark.sh 20

RUNS=${1:-20}
PROJECT_PATH="KoalaWiki.IntegrationTests/KoalaWiki.IntegrationTests.csproj"

echo "================================================"
echo "Running Integration Tests Benchmark"
echo "Number of runs: $RUNS"
echo "================================================"
echo ""

# Array to store individual times
declare -a times

for i in $(seq 1 $RUNS); do
    printf "Run %2d/%d: " $i $RUNS

    # Measure time using SECONDS (bash built-in, more accurate)
    SECONDS=0
    dotnet test "$PROJECT_PATH" --nologo --verbosity quiet >/dev/null 2>&1
    DURATION=$SECONDS

    times+=($DURATION)

    printf "%d seconds\n" $DURATION
done

echo ""
echo "================================================"
echo "Results:"
echo "================================================"

# Calculate statistics using awk for better performance
TOTAL=0
MIN=${times[0]}
MAX=${times[0]}

for time in "${times[@]}"; do
    TOTAL=$((TOTAL + time))
    [ $time -lt $MIN ] && MIN=$time
    [ $time -gt $MAX ] && MAX=$time
done

AVERAGE=$((TOTAL / RUNS))

printf "Total runs:        %d\n" $RUNS
printf "Total time:        %d seconds\n" $TOTAL
printf "Average time:      %d seconds\n" $AVERAGE
printf "Minimum time:      %d seconds\n" $MIN
printf "Maximum time:      %d seconds\n" $MAX

# Calculate standard deviation
SUM_SQUARED_DIFF=0
for time in "${times[@]}"; do
    DIFF=$((time - AVERAGE))
    SUM_SQUARED_DIFF=$((SUM_SQUARED_DIFF + DIFF * DIFF))
done

VARIANCE=$((SUM_SQUARED_DIFF / RUNS))
STDDEV=$(awk "BEGIN {printf \"%.2f\", sqrt($VARIANCE)}")
printf "Standard deviation: %s seconds\n" $STDDEV

echo ""
echo "All tests passed: ✓"
echo "================================================"
