# Workflow Engine

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green.svg)
![Build](https://img.shields.io/badge/build-passing-brightgreen.svg)

**A production-ready workflow execution engine for .NET that runs YAML-defined task pipelines with DAG-based parallel execution, webhooks, triggers, and scheduling.**

[Getting Started](#-getting-started) •
[Features](#-features) •
[Examples](#-examples) •
[Documentation](#-documentation) •
[Architecture](#-architecture)

</div>

---

## Overview

Workflow Engine is a powerful .NET 10 workflow automation system that enables you to define, execute, and monitor complex task pipelines using simple YAML configuration. It supports parallel execution via DAG scheduling, real-time webhook notifications, event-driven triggers from Telegram/Discord/Slack, and cron-based scheduling.

<a href="https://asciinema.org/a/w2fkdWaoMboJMAvn?autoplay=1">
<img src="https://asciinema.org/a//w2fkdWaoMboJMAvn.svg" alt="Workflow-engine demo" width="100%" />
</a>

```yaml
# Simple workflow example
name: Build and Deploy
tasks:
  - id: build
    run: dotnet build

  - id: test
    run: dotnet test
    dependsOn: [build]

  - id: deploy
    run: ./deploy.sh
    dependsOn: [test]
    if: ${{ success() }}
```

---

## Features

| Feature | Description |
|---------|-------------|
| **YAML Workflows** | Define workflows in human-readable YAML format |
| **DAG Scheduling** | Automatic parallel execution based on dependency graph |
| **Docker Execution** | Run tasks inside Docker containers via `docker exec` |
| **Data Piping** | Pass output from one task to another's stdin |
| **Conditional Execution** | `if` conditions with `success()`, `failure()`, `always()` |
| **Matrix Builds** | Run tasks across multiple configurations in parallel |
| **Webhook Notifications** | Send notifications to Discord, Slack, Telegram, or HTTP endpoints |
| **Trigger Service** | Event-driven workflow execution from chat commands |
| **Cron Scheduling** | Schedule workflows to run at specific times |
| **Retry Logic** | Configurable retry count and delay for transient failures |
| **Timeout Handling** | Per-task and workflow-level timeouts |
| **Rich Terminal UI** | Live progress table with Spectre.Console |
| **Multiple Shells** | Support for bash, sh, zsh, pwsh, and cmd |

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

### Installation

```bash
# Clone the repository
git clone https://github.com/your-org/workflow-engine.git
cd workflow-engine

# Build
dotnet build

# Run a workflow
dotnet run --project src/WorkflowEngine.Console -- examples/01-basic-workflow.yaml
```

### Quick Start

1. **Create a workflow file** (`my-workflow.yaml`):

```yaml
name: Hello World
description: My first workflow

tasks:
  - id: greet
    name: Say Hello
    run: echo "Hello, World!"

  - id: date
    name: Show Date
    run: date
    dependsOn: [greet]
```

2. **Run the workflow**:

```bash
dotnet run --project src/WorkflowEngine.Console -- my-workflow.yaml
```

---

## CLI Reference

### Commands

```bash
# Run a workflow
workflow-engine <workflow.yaml>
workflow-engine run <workflow.yaml>

# Validate workflow syntax
workflow-engine validate <workflow.yaml>

# Scheduling commands
workflow-engine schedule add <workflow.yaml> --cron "0 9 * * *" --name "daily-build"
workflow-engine schedule list
workflow-engine schedule remove <schedule-id>
workflow-engine schedule run

# Trigger service commands
workflow-engine trigger run --config triggers.yaml
workflow-engine trigger validate triggers.yaml
workflow-engine trigger list --config triggers.yaml
workflow-engine trigger test "/build my-api" --source telegram
```

### Run Options

| Option | Short | Description |
|--------|-------|-------------|
| `--verbose` | `-v` | Enable verbose logging output |
| `--dry-run` | `-n` | Validate and plan without executing |
| `--quiet` | `-q` | Minimal output, only show final result |
| `--timeout` | `-t` | Override default timeout (seconds) |
| `--working-dir` | `-C` | Set working directory |
| `--env` | `-e` | Set environment variables (NAME=VALUE) |
| `--json` | | Output results in JSON format |

### Examples

```bash
# Run with verbose logging
workflow-engine --verbose workflow.yaml

# Dry run (validate and plan only)
workflow-engine --dry-run workflow.yaml

# Set environment variables
workflow-engine --env API_KEY=secret --env DEBUG=true workflow.yaml

# Override timeout to 10 minutes
workflow-engine --timeout 600 workflow.yaml

# Run in specific directory
workflow-engine --working-dir /app workflow.yaml
```

---

## Workflow Schema

### Complete Schema Reference

```yaml
# Workflow metadata
name: Build Pipeline              # Required: Workflow name
description: Build and deploy     # Optional: Description
id: my-workflow-id                # Optional: Unique identifier

# Workflow-level settings
environment:                      # Global environment variables
  BUILD_CONFIG: Release
  NODE_ENV: production

defaultTimeoutMs: 300000          # Default task timeout (5 min)
defaultShell: bash                # Default shell for all tasks
workingDirectory: ./src           # Default working directory

# Matrix configuration (for parameterized workflows)
matrix:
  os: [ubuntu, macos, windows]
  node: [16, 18, 20]

# Webhook notifications
webhooks:
  - provider: discord
    url: https://discord.com/api/webhooks/...
    events: [workflow_completed, workflow_failed]

# Task definitions
tasks:
  - id: task-id                   # Required: Unique identifier
    name: Task Name               # Optional: Display name
    run: echo "Hello"             # Required: Command to execute
    shell: bash                   # Optional: bash, sh, zsh, pwsh, cmd
    workingDirectory: ./src       # Optional: Task working directory
    environment:                  # Optional: Task-specific env vars
      MY_VAR: value
    dependsOn: [other-task]       # Optional: Dependencies
    if: ${{ success() }}          # Optional: Condition
    input:                        # Optional: Stdin input
      type: pipe
      value: ${{ tasks.prev.output }}
    output:                       # Optional: Output capture
      type: string
      captureStderr: true
    timeoutMs: 60000              # Optional: Task timeout
    continueOnError: false        # Optional: Continue on failure
    retryCount: 3                 # Optional: Retry attempts
    retryDelayMs: 1000            # Optional: Delay between retries
    matrix:                       # Optional: Task-level matrix
      version: [1.0, 2.0]
```

---

## Task Configuration

### Basic Task

```yaml
tasks:
  - id: build
    name: Build Application
    run: dotnet build --configuration Release
```

### Task with Dependencies

```yaml
tasks:
  - id: install
    run: npm install

  - id: build
    run: npm run build
    dependsOn: [install]    # Runs after install completes

  - id: test
    run: npm test
    dependsOn: [install]    # Runs in parallel with build
```

### Task with Environment Variables

```yaml
tasks:
  - id: deploy
    run: ./deploy.sh
    environment:
      DEPLOY_ENV: production
      API_URL: https://api.example.com
```

### Task with Retry Logic

```yaml
tasks:
  - id: flaky-api-call
    run: curl https://api.example.com/health
    retryCount: 3
    retryDelayMs: 5000
    timeoutMs: 30000
```

### Task with Continue on Error

```yaml
tasks:
  - id: optional-task
    run: ./optional-script.sh
    continueOnError: true    # Workflow continues even if this fails

  - id: next-task
    run: echo "This runs regardless"
    dependsOn: [optional-task]
```

---

## Expression Syntax

### Variable Interpolation

Expressions use the `${{ ... }}` syntax:

```yaml
run: echo "Building ${{ env.BUILD_CONFIG }}"
run: echo "Previous output: ${{ tasks.build.output }}"
run: echo "Workflow: ${{ workflow.name }}"
```

### Available Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `env.{NAME}` | Environment variable | `${{ env.API_KEY }}` |
| `tasks.{id}.output` | Task stdout output | `${{ tasks.build.output }}` |
| `tasks.{id}.stderr` | Task stderr output | `${{ tasks.build.stderr }}` |
| `tasks.{id}.exitcode` | Task exit code | `${{ tasks.test.exitcode }}` |
| `tasks.{id}.status` | Task status | `${{ tasks.deploy.status }}` |
| `tasks.{id}.duration` | Task duration (ms) | `${{ tasks.build.duration }}` |
| `workflow.name` | Workflow name | `${{ workflow.name }}` |
| `workflow.id` | Workflow definition ID | `${{ workflow.id }}` |
| `workflow.runid` | Current run ID | `${{ workflow.runid }}` |
| `matrix.{key}` | Matrix variable | `${{ matrix.os }}` |

### Condition Functions

| Function | Description | Use Case |
|----------|-------------|----------|
| `success()` | True if all dependencies succeeded | Normal execution flow |
| `failure()` | True if any dependency failed | Error handling |
| `always()` | Always true | Cleanup tasks |
| `cancelled()` | True if workflow was cancelled | Cancellation handling |

### Examples

```yaml
# Run only if build succeeded
- id: deploy
  run: ./deploy.sh
  if: ${{ success() }}
  dependsOn: [build]

# Run cleanup regardless of outcome
- id: cleanup
  run: rm -rf ./temp
  if: ${{ always() }}

# Run error handler if tests failed
- id: notify-failure
  run: ./send-alert.sh
  if: ${{ failure() }}
  dependsOn: [test]

# Conditional based on exit code
- id: conditional
  run: ./process.sh
  if: ${{ tasks.check.exitcode == 0 }}

# Combine conditions
- id: deploy-prod
  run: ./deploy-prod.sh
  if: ${{ success() && env.DEPLOY_ENV == 'production' }}
```

---

## Input/Output

### Input Types

#### Text Input

```yaml
- id: echo-text
  run: cat
  input:
    type: text
    value: "Hello, World!"
```

#### File Input

```yaml
- id: process-file
  run: wc -l
  input:
    type: file
    filePath: ./data/input.txt
```

#### Pipe from Previous Task

```yaml
- id: get-version
  run: cat package.json | jq -r '.version'
  output:
    type: string

- id: use-version
  run: |
    echo "Version:"
    cat
  input:
    type: pipe
    value: ${{ tasks.get-version.output }}
  dependsOn: [get-version]
```

#### Binary Input (Base64)

```yaml
- id: process-binary
  run: ./process-image
  input:
    type: bytes
    value: iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB...
```

### Output Configuration

```yaml
# Capture stdout as string (default)
- id: get-output
  run: echo "result"
  output:
    type: string

# Capture both stdout and stderr
- id: capture-all
  run: ./script.sh
  output:
    type: string
    captureStderr: true

# Write output to file
- id: save-output
  run: ./generate-report.sh
  output:
    type: file
    filePath: ./reports/output.txt
```

---

## Docker Execution

Execute tasks inside Docker containers using `docker exec`. Perfect for workflows that need specific tools or isolated environments.

### Basic Docker Configuration

```yaml
name: Docker Workflow
docker:
  container: my-container     # Container name or ID
  user: hunter                # User to run as
  workingDirectory: /app      # Working directory in container

tasks:
  - id: build
    run: npm run build        # Runs inside container

  - id: test
    run: npm test
    dependsOn: [build]
```

### Task-Level Override

```yaml
docker:
  container: default-container

tasks:
  # Uses workflow-level Docker config
  - id: task1
    run: echo "Runs in default-container"

  # Override with different container
  - id: task2
    run: echo "Runs in other-container"
    docker:
      container: other-container

  # Disable Docker, run locally
  - id: local-task
    run: echo "Runs on host"
    docker:
      container: ""           # Empty = run locally
```

### Docker Configuration Options

| Field | Description |
|-------|-------------|
| `container` | Container name or ID (required) |
| `user` | User to run as (e.g., `root`, `1000:1000`) |
| `workingDirectory` | Working directory inside container |
| `environment` | Additional environment variables |
| `interactive` | Enable stdin (-i flag, default: true) |
| `tty` | Allocate TTY (-t flag, default: false) |
| `privileged` | Run privileged (default: false) |
| `host` | Docker host URL (e.g., `tcp://host:2375`) |
| `extraArgs` | Additional docker exec arguments |

---

## Matrix Builds

Matrix builds allow you to run workflows or tasks across multiple configurations in parallel.

### Workflow-Level Matrix

```yaml
name: Cross-Platform Build
matrix:
  os: [ubuntu, macos, windows]
  arch: [x64, arm64]

tasks:
  - id: build
    run: |
      echo "Building for ${{ matrix.os }}-${{ matrix.arch }}"
      ./build.sh --os ${{ matrix.os }} --arch ${{ matrix.arch }}
```

This creates 6 parallel workflow runs (3 OS × 2 architectures).

### Task-Level Matrix

```yaml
name: Test Suite
tasks:
  - id: test
    run: npm test -- --node-version ${{ matrix.node }}
    matrix:
      node: [16, 18, 20]

  - id: report
    run: ./generate-report.sh
    dependsOn: [test]
```

### Include/Exclude Combinations

```yaml
matrix:
  os: [ubuntu, windows]
  node: [16, 18]
  include:
    - os: ubuntu
      node: 20          # Add extra combination
  exclude:
    - os: windows
      node: 16          # Skip this combination
```

---

## Webhook Notifications

Webhook notifications allow you to send real-time updates about workflow execution to external services.

### Supported Providers

| Provider | Description |
|----------|-------------|
| `http` | Generic HTTP POST webhook |
| `discord` | Discord webhook with rich embeds |
| `slack` | Slack webhook with Block Kit |
| `telegram` | Telegram Bot API |

### Configuration

```yaml
name: Build Pipeline
webhooks:
  - provider: discord
    name: Discord Notifications
    url: https://discord.com/api/webhooks/123/abc
    events:
      - workflow_started
      - workflow_completed
      - workflow_failed
    options:
      username: "Build Bot"
      avatarUrl: "https://example.com/bot.png"

  - provider: slack
    name: Slack Alerts
    url: https://hooks.slack.com/services/T.../B.../xxx
    events: [workflow_failed, task_failed]
    options:
      channel: "#builds"
      username: "Workflow Engine"

  - provider: telegram
    name: Telegram Updates
    url: https://api.telegram.org/bot<token>/sendMessage
    events: [workflow_completed]
    options:
      chatId: "-1001234567890"
      parseMode: "HTML"

  - provider: http
    name: Custom Webhook
    url: https://api.example.com/webhooks/builds
    events: [workflow_completed, workflow_failed]
    headers:
      Authorization: "Bearer ${{ env.WEBHOOK_TOKEN }}"
      X-Custom-Header: "workflow-engine"
    timeoutMs: 10000
    retryCount: 3

tasks:
  - id: build
    run: dotnet build
```

### Event Types

| Event | Description |
|-------|-------------|
| `workflow_started` | Workflow execution started |
| `workflow_completed` | Workflow completed successfully |
| `workflow_failed` | Workflow failed |
| `workflow_cancelled` | Workflow was cancelled |
| `task_started` | Task execution started |
| `task_completed` | Task completed successfully |
| `task_failed` | Task failed |
| `task_skipped` | Task was skipped |
| `task_timed_out` | Task timed out |

### Webhook Payload

```json
{
  "eventType": "workflow_completed",
  "timestamp": "2024-01-15T10:30:00Z",
  "workflowId": "build-pipeline",
  "runId": "abc123",
  "workflowName": "Build Pipeline",
  "status": "Succeeded",
  "duration": "PT2M30S",
  "totalTasks": 5,
  "succeededTasks": 5,
  "failedTasks": 0,
  "skippedTasks": 0
}
```

---

## Trigger Service

The trigger service is a daemon that listens for messages from various platforms (Telegram, Discord, Slack, HTTP) and automatically executes workflows based on configurable rules.

### Configuration File

Create a `triggers.yaml` file:

```yaml
# Credentials for each platform
credentials:
  telegram:
    botToken: ${TELEGRAM_BOT_TOKEN}
  discord:
    botToken: ${DISCORD_BOT_TOKEN}
  slack:
    appToken: ${SLACK_APP_TOKEN}
    signingSecret: ${SLACK_SIGNING_SECRET}

# HTTP server for webhooks (Slack, generic HTTP)
httpServer:
  port: 8080
  host: "0.0.0.0"

# Trigger definitions
triggers:
  # Command trigger: "/build my-project"
  - name: build-project
    source: telegram
    type: command
    pattern: "/build {project}"
    workflow: ./workflows/build.yaml
    parameters:
      PROJECT_NAME: "{project}"
      TRIGGERED_BY: "{username}"
    responseTemplate: "Building {project}... Run ID: {runId}"
    cooldown: 30s
    enabled: true

  # Regex pattern trigger: "deploy auth-api v1.2.3"
  - name: deploy-version
    source: telegram
    type: pattern
    pattern: "deploy\\s+(?<service>\\w+)\\s+v(?<version>[\\d.]+)"
    workflow: ./workflows/deploy.yaml
    parameters:
      SERVICE_NAME: "{service}"
      VERSION: "{version}"
    responseTemplate: "Deploying {service} v{version}..."

  # Keyword trigger
  - name: help-command
    source: telegram
    type: keyword
    keywords: ["help", "status", "info"]
    workflow: ./workflows/help.yaml
    responseTemplate: "Processing your request..."
```

### Trigger Types

| Type | Description | Example |
|------|-------------|---------|
| `command` | Slash command with parameters | `/build {project}` |
| `pattern` | Regex pattern with named captures | `deploy (?<env>\w+)` |
| `keyword` | Keyword detection | `["help", "status"]` |

### Running the Trigger Service

```bash
# Set environment variables
export TELEGRAM_BOT_TOKEN=your-bot-token

# Run the trigger service
workflow-engine trigger run --config triggers.yaml

# Validate configuration
workflow-engine trigger validate triggers.yaml

# List configured triggers
workflow-engine trigger list --config triggers.yaml

# Test message matching
workflow-engine trigger test "/build my-api" --source telegram
```

### Available Variables in Templates

| Variable | Description |
|----------|-------------|
| `{username}` | Username of the person who sent the message |
| `{userId}` | User ID |
| `{chatId}` | Chat/Channel ID |
| `{source}` | Source platform (telegram, discord, etc.) |
| `{runId}` | Generated workflow run ID |
| `{...}` | Named captures from command/pattern |

---

## Scheduling

The scheduling system allows you to run workflows automatically based on cron expressions.

### Adding a Schedule

```bash
# Add a schedule with cron expression
workflow-engine schedule add ./workflows/backup.yaml \
  --cron "0 2 * * *" \
  --name "nightly-backup" \
  --description "Daily backup at 2 AM"

# Add with timezone
workflow-engine schedule add ./workflows/report.yaml \
  --cron "0 9 * * 1" \
  --name "weekly-report" \
  --timezone "America/New_York"

# Add with environment variables
workflow-engine schedule add ./workflows/deploy.yaml \
  --cron "0 6 * * *" \
  --name "morning-deploy" \
  --env ENVIRONMENT=staging
```

### Managing Schedules

```bash
# List all schedules
workflow-engine schedule list

# Remove a schedule
workflow-engine schedule remove nightly-backup

# Enable/disable a schedule
workflow-engine schedule enable nightly-backup
workflow-engine schedule disable nightly-backup
```

### Running the Scheduler

```bash
# Run the background scheduler
workflow-engine schedule run

# Run with verbose logging
workflow-engine schedule run --verbose
```

### Cron Expression Format

```
┌───────────── minute (0 - 59)
│ ┌───────────── hour (0 - 23)
│ │ ┌───────────── day of month (1 - 31)
│ │ │ ┌───────────── month (1 - 12)
│ │ │ │ ┌───────────── day of week (0 - 6) (Sunday = 0)
│ │ │ │ │
* * * * *
```

### Common Cron Examples

| Expression | Description |
|------------|-------------|
| `0 * * * *` | Every hour |
| `0 9 * * *` | Daily at 9 AM |
| `0 9 * * 1` | Every Monday at 9 AM |
| `0 0 1 * *` | First day of every month |
| `*/15 * * * *` | Every 15 minutes |
| `0 9,17 * * 1-5` | 9 AM and 5 PM, weekdays |

---

## Examples

The `examples/` directory contains sample workflows demonstrating various features:

| File | Description |
|------|-------------|
| `01-basic-workflow.yaml` | Simple sequential tasks |
| `02-environment-variables.yaml` | Using environment variables |
| `03-dependencies-parallel.yaml` | DAG scheduling with parallel execution |
| `04-conditional-execution.yaml` | Condition evaluation with `if` |
| `05-data-piping.yaml` | Task output piping |
| `06-retry-logic.yaml` | Retry configuration |
| `07-timeout-handling.yaml` | Timeout settings |
| `08-multi-shell.yaml` | Using different shells |
| `09-working-directories.yaml` | Working directory configuration |
| `10-output-capture.yaml` | Output capture options |
| `11-continue-on-error.yaml` | Error handling |
| `12-matrix-workflow.yaml` | Matrix builds |
| `13-complex-pipeline.yaml` | Complex multi-stage pipeline |
| `14-cleanup-tasks.yaml` | Always-run cleanup tasks |
| `15-json-processing.yaml` | JSON data processing |
| `16-scheduling.yaml` | Scheduled workflow example |
| `17-parallel-downloads.yaml` | Parallel download tasks |
| `18-webhooks.yaml` | Webhook notifications |

### Running Examples

```bash
# Run basic workflow
dotnet run --project src/WorkflowEngine.Console -- examples/01-basic-workflow.yaml

# Run with verbose output
dotnet run --project src/WorkflowEngine.Console -- examples/03-dependencies-parallel.yaml -v

# Dry run complex pipeline
dotnet run --project src/WorkflowEngine.Console -- examples/13-complex-pipeline.yaml --dry-run
```

---

## Documentation

For detailed documentation, see:

| Document | Description |
|----------|-------------|
| [YAML Schema Reference](docs/YAML-SCHEMA.md) | Complete reference for all YAML configuration fields |
| [Examples](examples/) | Sample workflows demonstrating various features |

---

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `WORKFLOW_ENGINE_TIMEOUT` | Default timeout (ms) | `300000` |
| `WORKFLOW_ENGINE_SHELL` | Default shell | `bash` |
| `WORKFLOW_ENGINE_WORKING_DIR` | Default working directory | Current dir |
| `WORKFLOW_ENGINE_LOG_LEVEL` | Log level | `Information` |

### Config File

Create `~/.workflow-engine/config.yaml`:

```yaml
defaults:
  timeoutMs: 300000
  shell: bash
  workingDirectory: .

logging:
  level: Information
  format: json

scheduling:
  storageType: file
  storagePath: ~/.workflow-engine/schedules.json
```

---

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Run tests (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Development Setup

```bash
# Clone
git clone https://github.com/your-org/workflow-engine.git
cd workflow-engine

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run specific test class
dotnet test --filter "DagSchedulerTests"
```

---

## License

MIT License - See [LICENSE](LICENSE) file for details.

---

<div align="center">
Made with .NET 10
</div>
