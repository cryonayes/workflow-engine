# Workflow Engine YAML Schema Reference

This document provides a complete reference for all YAML configuration fields supported by the Workflow Engine.

---

## Table of Contents

- [Workflow Schema](#workflow-schema)
  - [Root Fields](#root-fields)
  - [Docker Execution](#docker-execution)
  - [Tasks](#tasks)
  - [Task Input](#task-input)
  - [Task Output](#task-output)
  - [Matrix Configuration](#matrix-configuration)
  - [Webhooks](#webhooks)
- [Trigger Service Schema](#trigger-service-schema)
  - [Credentials](#credentials)
  - [HTTP Server](#http-server)
  - [Triggers](#triggers)
- [Expression Syntax](#expression-syntax)
- [Complete Examples](#complete-examples)

---

## Workflow Schema

### Root Fields

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `name` | string | **Yes** | - | Human-readable name of the workflow |
| `description` | string | No | `null` | Optional description of what this workflow does |
| `environment` | map | No | `{}` | Environment variables available to all tasks |
| `workingDirectory` | string | No | Current dir | Default working directory for task execution |
| `tasks` | array | **Yes** | - | List of tasks to execute |
| `defaultTimeoutMs` | integer | No | `300000` | Default timeout for tasks (5 minutes) |
| `maxParallelism` | integer | No | `-1` | Max concurrent tasks (-1 = unlimited) |
| `webhooks` | array | No | `[]` | Webhook notification configurations |
| `docker` | object | No | `null` | Docker container execution configuration |

```yaml
name: My Workflow
description: Builds and deploys the application
environment:
  BUILD_CONFIG: Release
  NODE_ENV: production
workingDirectory: ./src
defaultTimeoutMs: 600000
maxParallelism: 4
docker:
  container: my-container
webhooks: []
tasks: []
```

---

### Docker Execution

Execute tasks inside a Docker container using `docker exec`. Supports both workflow-level (all tasks) and task-level (per-task override) configuration.

#### Workflow-Level Docker Config

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `container` | string | **Yes** | - | Container name or ID to execute commands in |
| `user` | string | No | `null` | User to run commands as (e.g., `root`, `hunter`, `1000:1000`) |
| `workingDirectory` | string | No | `null` | Working directory inside the container |
| `environment` | map | No | `{}` | Additional environment variables for the container |
| `interactive` | boolean | No | `true` | Enable interactive mode (-i flag) for stdin support |
| `tty` | boolean | No | `false` | Allocate pseudo-TTY (-t flag) |
| `privileged` | boolean | No | `false` | Run in privileged mode (--privileged) |
| `host` | string | No | `null` | Docker host (DOCKER_HOST) for remote Docker |
| `extraArgs` | array | No | `[]` | Additional docker exec arguments |

```yaml
name: Docker Workflow
description: Execute tasks inside a Docker container

docker:
  container: hunting              # Container name
  user: hunter                    # Run as user
  workingDirectory: /home/hunter  # Working directory in container
  environment:
    CUSTOM_VAR: value
  interactive: true               # Enable stdin (-i)
  tty: false                      # No TTY needed
  privileged: false               # No privileged mode
  extraArgs:
    - "--cap-add=NET_ADMIN"       # Extra docker exec args

tasks:
  - id: scan
    run: nmap -sV target.com      # Runs inside container
```

#### Task-Level Docker Override

Tasks can override the workflow-level Docker config or disable Docker entirely:

```yaml
docker:
  container: default-container

tasks:
  # Uses workflow-level Docker config
  - id: task1
    run: echo "Runs in default-container"

  # Overrides with different container
  - id: task2
    run: echo "Runs in other-container"
    docker:
      container: other-container
      user: root

  # Disables Docker, runs locally
  - id: task3
    run: echo "Runs on host machine"
    docker:
      container: ""  # Empty string disables Docker
```

#### Remote Docker Host

Connect to a remote Docker daemon:

```yaml
docker:
  container: my-container
  host: "tcp://192.168.1.100:2375"  # Remote Docker host
  # Or use Unix socket: "unix:///var/run/docker.sock"
```

---

### Tasks

Each task in the `tasks` array supports the following fields:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `id` | string | **Yes** | - | Unique identifier for the task |
| `name` | string | No | Same as `id` | Display name shown in output |
| `run` | string | **Yes** | - | Shell command to execute |
| `shell` | string | No | `bash` | Shell to use: `bash`, `sh`, `zsh`, `pwsh`, `cmd` |
| `workingDirectory` | string | No | Workflow dir | Task-specific working directory |
| `environment` | map | No | `{}` | Task-specific environment variables |
| `dependsOn` | array | No | `[]` | Task IDs that must complete first |
| `if` | string | No | `null` | Condition expression for execution |
| `input` | object | No | `null` | Input configuration (stdin) |
| `output` | object | No | `null` | Output capture configuration |
| `timeoutMs` | integer | No | `300000` | Task timeout in milliseconds |
| `continueOnError` | boolean | No | `false` | Continue workflow if task fails |
| `retryCount` | integer | No | `0` | Number of retry attempts on failure |
| `retryDelayMs` | integer | No | `1000` | Delay between retries in milliseconds |
| `matrix` | object | No | `null` | Matrix configuration for parallel expansion |
| `docker` | object | No | `null` | Task-level Docker config (overrides workflow) |

```yaml
tasks:
  - id: build
    name: Build Application
    run: dotnet build --configuration ${{ env.BUILD_CONFIG }}
    shell: bash
    workingDirectory: ./src
    environment:
      DOTNET_CLI_TELEMETRY_OPTOUT: "1"
    dependsOn: [restore]
    if: ${{ success() }}
    timeoutMs: 120000
    continueOnError: false
    retryCount: 2
    retryDelayMs: 5000
```

#### Shell Options

| Shell | Description | Platform |
|-------|-------------|----------|
| `bash` | Bourne Again Shell (default) | Linux, macOS, Windows (WSL/Git Bash) |
| `sh` | POSIX Shell | Linux, macOS |
| `zsh` | Z Shell | Linux, macOS |
| `pwsh` | PowerShell Core | Cross-platform |
| `cmd` | Command Prompt | Windows only |

---

### Task Input

Configure stdin input for a task using the `input` field:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `type` | string | **Yes** | - | Input type: `none`, `text`, `bytes`, `file`, `pipe` |
| `value` | string | Conditional | - | Input value (for `text`, `bytes`, `pipe`) |
| `filePath` | string | Conditional | - | File path (for `file` type) |

#### Input Types

| Type | Description | Required Fields |
|------|-------------|-----------------|
| `none` | No input (default) | - |
| `text` | Plain text string | `value` |
| `bytes` | Base64-encoded binary data | `value` |
| `file` | Read input from file | `filePath` |
| `pipe` | Pipe from another task's output | `value` (expression) |

```yaml
# Text input
- id: echo-text
  run: cat
  input:
    type: text
    value: "Hello, World!"

# File input
- id: process-file
  run: wc -l
  input:
    type: file
    filePath: ./data/input.txt

# Pipe from previous task
- id: process-output
  run: grep "error"
  input:
    type: pipe
    value: ${{ tasks.fetch-logs.output }}
  dependsOn: [fetch-logs]

# Binary input (base64)
- id: decode-binary
  run: base64 --decode
  input:
    type: bytes
    value: SGVsbG8gV29ybGQh
```

---

### Task Output

Configure how task output is captured using the `output` field:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `type` | string | No | `string` | Output type: `string`, `bytes`, `file`, `stream` |
| `filePath` | string | Conditional | - | File path (for `file` type) |
| `captureStderr` | boolean | No | `true` | Also capture stderr separately |
| `maxSizeBytes` | integer | No | `10485760` | Max output size (10 MB) |

#### Output Types

| Type | Description | Use Case |
|------|-------------|----------|
| `string` | Capture as UTF-8 string (default) | Text output, logs |
| `bytes` | Capture as raw bytes | Binary data |
| `file` | Write directly to file | Large outputs |
| `stream` | Stream without buffering | Real-time processing |

```yaml
# String output (default)
- id: get-version
  run: cat package.json | jq -r '.version'
  output:
    type: string

# Capture stderr separately
- id: run-tests
  run: npm test
  output:
    type: string
    captureStderr: true

# Write to file
- id: generate-report
  run: ./generate-report.sh
  output:
    type: file
    filePath: ./reports/output.txt

# Limit output size
- id: large-output
  run: ./process-large-file.sh
  output:
    type: string
    maxSizeBytes: 52428800  # 50 MB
```

---

### Matrix Configuration

Matrix builds expand a single task into multiple parallel tasks based on combinations of values.

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `<dimension>` | array | Yes | List of values for this dimension |
| `include` | array | No | Additional combinations to include |
| `exclude` | array | No | Combinations to exclude |

```yaml
# Simple matrix
- id: test
  run: npm test -- --node-version ${{ matrix.node }}
  matrix:
    node: [16, 18, 20]

# Multi-dimensional matrix (creates 6 combinations)
- id: build
  run: |
    echo "Building for ${{ matrix.os }}-${{ matrix.arch }}"
    ./build.sh --os ${{ matrix.os }} --arch ${{ matrix.arch }}
  matrix:
    os: [ubuntu, macos, windows]
    arch: [x64, arm64]

# With include/exclude
- id: test-matrix
  run: ./test.sh
  matrix:
    os: [ubuntu, windows]
    python: ["3.9", "3.10", "3.11"]
    include:
      - os: ubuntu
        python: "3.12"
        experimental: "true"
    exclude:
      - os: windows
        python: "3.9"
```

#### Matrix Expansion

Given:
```yaml
matrix:
  os: [ubuntu, windows]
  version: [1, 2]
```

Creates 4 task instances:
- `test-ubuntu-1` (os=ubuntu, version=1)
- `test-ubuntu-2` (os=ubuntu, version=2)
- `test-windows-1` (os=windows, version=1)
- `test-windows-2` (os=windows, version=2)

---

### Webhooks

Configure webhook notifications for workflow events:

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `provider` | string | **Yes** | - | Provider: `http`, `discord`, `slack`, `telegram` |
| `url` | string | **Yes** | - | Webhook URL |
| `name` | string | No | `null` | Display name for this webhook |
| `events` | array | No | `[workflow_completed, workflow_failed]` | Events to trigger on |
| `headers` | map | No | `{}` | Additional HTTP headers |
| `options` | map | No | `{}` | Provider-specific options |
| `timeoutMs` | integer | No | `10000` | Request timeout (10 seconds) |
| `retryCount` | integer | No | `2` | Retry attempts on failure |

#### Webhook Events

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

#### Provider-Specific Options

**Discord:**
| Option | Description |
|--------|-------------|
| `username` | Override webhook username |
| `avatarUrl` | Override webhook avatar URL |

**Slack:**
| Option | Description |
|--------|-------------|
| `channel` | Override target channel |
| `username` | Override bot username |
| `iconEmoji` | Override bot icon emoji |
| `iconUrl` | Override bot icon URL |

**Telegram:**
| Option | Description |
|--------|-------------|
| `chatId` | Target chat/channel ID (required) |
| `parseMode` | Message format: `HTML` or `Markdown` |
| `disableNotification` | Send silently (`true`/`false`) |

```yaml
webhooks:
  # Discord
  - provider: discord
    name: Build Notifications
    url: https://discord.com/api/webhooks/123456/abcdef
    events: [workflow_started, workflow_completed, workflow_failed]
    options:
      username: "Build Bot"
      avatarUrl: "https://example.com/bot.png"

  # Slack
  - provider: slack
    name: Slack Alerts
    url: https://hooks.slack.com/services/T.../B.../xxx
    events: [workflow_failed, task_failed]
    options:
      channel: "#builds"
      username: "Workflow Engine"
      iconEmoji: ":robot_face:"

  # Telegram
  - provider: telegram
    name: Telegram Updates
    url: https://api.telegram.org/bot<TOKEN>/sendMessage
    events: [workflow_completed]
    options:
      chatId: "-1001234567890"
      parseMode: "HTML"

  # Generic HTTP
  - provider: http
    name: Custom Webhook
    url: https://api.example.com/webhooks/builds
    events: [workflow_completed, workflow_failed]
    headers:
      Authorization: "Bearer ${{ env.WEBHOOK_TOKEN }}"
      X-Custom-Header: "workflow-engine"
    timeoutMs: 15000
    retryCount: 3
```

---

## Trigger Service Schema

The trigger service uses a separate configuration file (`triggers.yaml`).

### Root Structure

```yaml
credentials:
  telegram: { ... }
  discord: { ... }
  slack: { ... }

httpServer:
  port: 8080
  host: "0.0.0.0"

triggers:
  - name: trigger-name
    ...
```

---

### Credentials

| Platform | Field | Type | Required | Description |
|----------|-------|------|----------|-------------|
| **Telegram** | `botToken` | string | Yes | Bot token from BotFather |
| **Discord** | `botToken` | string | Yes | Bot token from Discord Developer Portal |
| **Slack** | `appToken` | string | Yes | App-level token for Socket Mode |
| **Slack** | `signingSecret` | string | Yes | Signing secret for request verification |

```yaml
credentials:
  telegram:
    botToken: "${TELEGRAM_BOT_TOKEN}"

  discord:
    botToken: "${DISCORD_BOT_TOKEN}"

  slack:
    appToken: "${SLACK_APP_TOKEN}"
    signingSecret: "${SLACK_SIGNING_SECRET}"
```

---

### HTTP Server

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `port` | integer | No | `8080` | Port to listen on |
| `host` | string | No | `0.0.0.0` | Host to bind to |
| `enableHttps` | boolean | No | `false` | Enable HTTPS |
| `certificatePath` | string | Conditional | - | Path to HTTPS certificate |
| `certificatePassword` | string | Conditional | - | Certificate password |

```yaml
httpServer:
  port: 8080
  host: "0.0.0.0"
  enableHttps: true
  certificatePath: /path/to/cert.pfx
  certificatePassword: "${CERT_PASSWORD}"
```

---

### Triggers

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `name` | string | **Yes** | - | Unique trigger name |
| `source` | string | **Yes** | - | Source: `telegram`, `discord`, `slack`, `http` |
| `type` | string | **Yes** | - | Trigger type: `command`, `pattern`, `keyword` |
| `pattern` | string | Conditional | - | Pattern for `command` or `pattern` types |
| `keywords` | array | Conditional | - | Keywords for `keyword` type |
| `workflow` | string | **Yes** | - | Path to workflow file to execute |
| `parameters` | map | No | `{}` | Parameters to pass to workflow |
| `responseTemplate` | string | No | `null` | Reply template message |
| `cooldown` | string | No | `null` | Cooldown between triggers (e.g., `30s`, `5m`) |
| `enabled` | boolean | No | `true` | Whether trigger is active |

#### Trigger Types

| Type | Description | Required Field |
|------|-------------|----------------|
| `command` | Slash command with parameters | `pattern` |
| `pattern` | Regex pattern with named captures | `pattern` |
| `keyword` | Keyword detection | `keywords` |

#### Command Pattern Syntax

Use `{name}` for parameter placeholders:

```yaml
# "/build my-project" -> project = "my-project"
pattern: "/build {project}"

# "/deploy staging v1.2.3" -> env = "staging", version = "v1.2.3"
pattern: "/deploy {env} {version}"
```

#### Regex Pattern Syntax

Use named capture groups `(?<name>...)`:

```yaml
# "deploy auth-api v1.2.3" -> service = "auth-api", version = "1.2.3"
pattern: "deploy\\s+(?<service>\\w+)\\s+v(?<version>[\\d.]+)"
```

#### Available Template Variables

| Variable | Description |
|----------|-------------|
| `{username}` | Sender's username |
| `{userId}` | Sender's user ID |
| `{chatId}` | Chat/Channel ID |
| `{source}` | Source platform name |
| `{runId}` | Generated workflow run ID |
| `{...}` | Named captures from pattern |

```yaml
triggers:
  # Command trigger
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

  # Regex pattern trigger
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

---

## Expression Syntax

Expressions use the `${{ ... }}` syntax for variable interpolation and conditions.

### Variable References

| Syntax | Description | Example |
|--------|-------------|---------|
| `${{ env.NAME }}` | Environment variable | `${{ env.API_KEY }}` |
| `${{ tasks.ID.output }}` | Task stdout | `${{ tasks.build.output }}` |
| `${{ tasks.ID.stderr }}` | Task stderr | `${{ tasks.test.stderr }}` |
| `${{ tasks.ID.exitcode }}` | Task exit code | `${{ tasks.run.exitcode }}` |
| `${{ tasks.ID.status }}` | Task status | `${{ tasks.deploy.status }}` |
| `${{ tasks.ID.duration }}` | Task duration (ms) | `${{ tasks.build.duration }}` |
| `${{ workflow.name }}` | Workflow name | `${{ workflow.name }}` |
| `${{ workflow.id }}` | Workflow ID | `${{ workflow.id }}` |
| `${{ workflow.runid }}` | Current run ID | `${{ workflow.runid }}` |
| `${{ matrix.KEY }}` | Matrix value | `${{ matrix.os }}` |

### Condition Functions

| Function | Returns True When |
|----------|-------------------|
| `success()` | All dependencies succeeded |
| `failure()` | Any dependency failed |
| `always()` | Always (for cleanup tasks) |
| `cancelled()` | Workflow was cancelled |

### Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `==` | Equal | `${{ tasks.test.exitcode == 0 }}` |
| `!=` | Not equal | `${{ env.ENV != 'production' }}` |
| `&&` | Logical AND | `${{ success() && env.DEPLOY == 'true' }}` |
| `\|\|` | Logical OR | `${{ failure() \|\| cancelled() }}` |

### Examples

```yaml
# Run if build succeeded
if: ${{ success() }}

# Run if tests failed
if: ${{ failure() }}

# Always run (cleanup)
if: ${{ always() }}

# Check exit code
if: ${{ tasks.build.exitcode == 0 }}

# Check environment
if: ${{ env.DEPLOY_ENV == 'production' }}

# Combined conditions
if: ${{ success() && env.RUN_DEPLOY == 'true' }}

# Use in run command
run: echo "Building ${{ env.BUILD_CONFIG }} on ${{ matrix.os }}"
```

---

## Complete Examples

### Basic Workflow

```yaml
name: Basic Build
description: Simple build and test workflow

environment:
  BUILD_CONFIG: Release

tasks:
  - id: restore
    name: Restore Dependencies
    run: dotnet restore

  - id: build
    name: Build
    run: dotnet build --configuration ${{ env.BUILD_CONFIG }} --no-restore
    dependsOn: [restore]

  - id: test
    name: Run Tests
    run: dotnet test --no-build --verbosity normal
    dependsOn: [build]
```

### Parallel Execution with Dependencies

```yaml
name: Parallel Pipeline
description: Tasks with complex dependencies

tasks:
  - id: setup
    run: npm install

  - id: lint
    run: npm run lint
    dependsOn: [setup]

  - id: unit-tests
    run: npm run test:unit
    dependsOn: [setup]

  - id: integration-tests
    run: npm run test:integration
    dependsOn: [setup]

  - id: build
    run: npm run build
    dependsOn: [lint, unit-tests]

  - id: deploy
    run: ./deploy.sh
    dependsOn: [build, integration-tests]
    if: ${{ success() }}
```

### Data Piping

```yaml
name: Data Pipeline
description: Process data through multiple stages

tasks:
  - id: fetch-data
    run: curl -s https://api.example.com/data
    output:
      type: string

  - id: transform
    run: jq '.items[] | select(.active == true)'
    input:
      type: pipe
      value: ${{ tasks.fetch-data.output }}
    dependsOn: [fetch-data]
    output:
      type: string

  - id: save-results
    run: cat > ./results.json
    input:
      type: pipe
      value: ${{ tasks.transform.output }}
    dependsOn: [transform]
```

### Matrix Build

```yaml
name: Cross-Platform Build
description: Build for multiple platforms

tasks:
  - id: build
    run: |
      echo "Building for ${{ matrix.os }} with Node ${{ matrix.node }}"
      npm ci
      npm run build
    matrix:
      os: [ubuntu-latest, macos-latest, windows-latest]
      node: [18, 20]
    environment:
      CI: "true"

  - id: publish
    run: npm publish
    dependsOn: [build]
    if: ${{ success() }}
```

### Complete Trigger Configuration

```yaml
# triggers.yaml
credentials:
  telegram:
    botToken: "${TELEGRAM_BOT_TOKEN}"

httpServer:
  port: 8080
  host: "127.0.0.1"

triggers:
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

  - name: deploy-version
    source: telegram
    type: pattern
    pattern: "deploy\\s+(?<service>\\w+)\\s+v(?<version>[\\d.]+)"
    workflow: ./workflows/deploy.yaml
    parameters:
      SERVICE: "{service}"
      VERSION: "{version}"
    responseTemplate: "Deploying {service} v{version}..."

  - name: quick-commands
    source: telegram
    type: keyword
    keywords: ["help", "status", "info"]
    workflow: ./workflows/help.yaml
    responseTemplate: "Processing your request..."
```

### Workflow with Webhooks

```yaml
name: Build with Notifications
description: Build pipeline with webhook notifications

webhooks:
  - provider: discord
    url: https://discord.com/api/webhooks/123/abc
    events: [workflow_started, workflow_completed, workflow_failed]
    options:
      username: "Build Bot"

  - provider: slack
    url: https://hooks.slack.com/services/T.../B.../xxx
    events: [workflow_failed]
    options:
      channel: "#alerts"

tasks:
  - id: build
    name: Build Application
    run: npm run build

  - id: test
    name: Run Tests
    run: npm test
    dependsOn: [build]

  - id: deploy
    name: Deploy
    run: ./deploy.sh
    dependsOn: [test]
    if: ${{ success() }}
```

---

## Quick Reference Card

### Workflow File Structure
```yaml
name: string (required)
description: string
environment: map<string, string>
workingDirectory: string
defaultTimeoutMs: integer
maxParallelism: integer
webhooks: WebhookConfig[]
docker: DockerConfig             # Execute tasks in container
tasks: Task[] (required)
```

### Docker Config Structure
```yaml
docker:
  container: string (required)   # Container name/ID
  user: string                   # User to run as
  workingDirectory: string       # Working dir in container
  environment: map<string, string>
  interactive: boolean           # -i flag (default: true)
  tty: boolean                   # -t flag (default: false)
  privileged: boolean            # --privileged
  host: string                   # DOCKER_HOST
  extraArgs: string[]            # Additional args
```

### Task Structure
```yaml
id: string (required)
name: string
run: string (required)
shell: bash|sh|zsh|pwsh|cmd
workingDirectory: string
environment: map<string, string>
dependsOn: string[]
if: string
input: { type, value, filePath }
output: { type, filePath, captureStderr, maxSizeBytes }
timeoutMs: integer
continueOnError: boolean
retryCount: integer
retryDelayMs: integer
matrix: { dimensions, include, exclude }
docker: DockerConfig             # Task-level override
```

### Expression Syntax
```
${{ env.NAME }}              - Environment variable
${{ tasks.ID.output }}       - Task output
${{ tasks.ID.exitcode }}     - Task exit code
${{ workflow.name }}         - Workflow name
${{ matrix.KEY }}            - Matrix value
${{ success() }}             - All deps succeeded
${{ failure() }}             - Any dep failed
${{ always() }}              - Always true
${{ cancelled() }}           - Was cancelled
```
