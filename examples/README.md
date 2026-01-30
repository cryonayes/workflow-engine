# Workflow Engine Examples

This directory contains example workflows demonstrating all features of the Workflow Engine.

## Running Examples

```bash
# Run any example
workflow-engine examples/01-basic-workflow.yaml

# Run with verbose output
workflow-engine examples/01-basic-workflow.yaml --verbose

# Validate without executing
workflow-engine validate examples/12-complex-pipeline.yaml

# Dry run (validate and show execution plan)
workflow-engine examples/03-dependencies-parallel.yaml --dry-run
```

## Example Index

| # | File | Features Demonstrated |
|---|------|----------------------|
| 01 | `01-basic-workflow.yaml` | Basic workflow structure, simple tasks |
| 02 | `02-environment-variables.yaml` | Workflow & task environment variables, interpolation |
| 03 | `03-dependencies-parallel.yaml` | `dependsOn`, parallel execution, DAG scheduling |
| 04 | `04-conditional-execution.yaml` | `if` conditions: `success()`, `failure()`, `always()` |
| 05 | `05-data-piping.yaml` | Input/output piping between tasks |
| 06 | `06-input-types.yaml` | Input types: `text`, `bytes`, `file`, `pipe` |
| 07 | `07-output-types.yaml` | Output capture: `string`, `bytes`, `file`, `stream` |
| 08 | `08-retry-and-timeout.yaml` | `retryCount`, `retryDelayMs`, `timeoutMs`, `continueOnError` |
| 09 | `09-shell-types.yaml` | Shell types: `bash`, `sh`, `zsh`, `pwsh` |
| 10 | `10-expression-evaluation.yaml` | Expressions: comparisons, logical operators |
| 11 | `11-working-directory.yaml` | Workflow & task `workingDirectory` |
| 12 | `12-complex-pipeline.yaml` | Full CI/CD pipeline combining all features |
| 13 | `13-diamond-dependency.yaml` | Diamond dependency pattern (A→B,C→D) |
| 14 | `14-error-handling.yaml` | Error handling patterns |
| 15 | `15-data-transformation.yaml` | ETL-style data processing pipeline |

## Feature Reference

### Workflow Configuration

```yaml
name: Workflow Name          # Required
description: Description     # Recommended
environment:                 # Workflow-level env vars
  VAR_NAME: value
workingDirectory: /path      # Default working directory
defaultTimeoutMs: 300000     # Default task timeout (5 min)
maxParallelism: 4            # Max concurrent tasks (-1 = unlimited)
```

### Task Configuration

```yaml
tasks:
  - id: task-id              # Required, unique identifier
    name: Display Name       # Optional, for UI display
    run: |                   # Required, command to execute
      echo "Hello"
    shell: bash              # bash, sh, zsh, pwsh, powershell, cmd
    workingDirectory: /path  # Task-specific working dir
    environment:             # Task-specific env vars
      VAR: value
    dependsOn:               # Task dependencies
      - other-task
    if: ${{ success() }}     # Conditional execution
    input:                   # Input configuration
      type: pipe
      value: ${{ tasks.prev.output }}
    output:                  # Output configuration
      type: string
      captureStderr: true
      maxSizeBytes: 10485760
    timeoutMs: 60000         # Task timeout
    retryCount: 3            # Retry on failure
    retryDelayMs: 1000       # Delay between retries
    continueOnError: false   # Continue workflow on failure
```

### Input Types

| Type | Description | Properties |
|------|-------------|------------|
| `none` | No input (default) | - |
| `text` | Plain text to stdin | `value` |
| `bytes` | Base64-encoded bytes | `value` |
| `file` | Read from file | `filePath` |
| `pipe` | Output from previous task | `value` (expression) |

### Output Types

| Type | Description |
|------|-------------|
| `string` | Capture as UTF-8 string (default) |
| `bytes` | Capture as raw bytes |
| `file` | Write to file |
| `stream` | Stream to next task |

### Expression Syntax

```yaml
# Environment variables
${{ env.VAR_NAME }}

# Task output
${{ tasks.taskId.output }}

# Task exit code
${{ tasks.taskId.exitcode }}

# Workflow metadata
${{ workflow.name }}

# Status functions
${{ success() }}     # All dependencies succeeded
${{ failure() }}     # Any dependency failed
${{ always() }}      # Always true (for cleanup)
${{ cancelled() }}   # Workflow was cancelled

# Comparisons
${{ env.VAR == 'value' }}
${{ env.VAR != 'other' }}
${{ tasks.build.exitcode == 0 }}

# Logical operators
${{ success() && env.DEPLOY == 'true' }}
${{ failure() || env.FORCE == 'true' }}
${{ !failure() }}
```

### Parallel Execution

Tasks are automatically parallelized based on dependencies:

```yaml
tasks:
  - id: a
    run: echo "A runs first"

  - id: b
    run: echo "B runs with C"
    dependsOn: [a]

  - id: c
    run: echo "C runs with B"
    dependsOn: [a]

  - id: d
    run: echo "D waits for B and C"
    dependsOn: [b, c]
```

Execution waves:
```
Wave 0: [a]
Wave 1: [b, c]  ← Parallel!
Wave 2: [d]
```

### Error Handling Patterns

```yaml
# Continue on error
- id: optional
  run: may-fail
  continueOnError: true

# Retry on failure
- id: flaky
  run: network-call
  retryCount: 3
  retryDelayMs: 2000

# Conditional on failure
- id: notify
  run: send-alert
  if: ${{ failure() }}

# Always run (cleanup)
- id: cleanup
  run: rm -rf /tmp/work
  if: ${{ always() }}
```
