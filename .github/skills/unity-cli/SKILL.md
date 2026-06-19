---
name: unity-cli
description: 'Control the Unity Editor from the terminal with the `unity-cli` binary (youngwoocho02/unity-cli). USE FOR: entering/stopping play mode, running arbitrary C# inside Unity via `exec`, reading/clearing the console, running EditMode/PlayMode tests, executing menu items, reserializing assets after text edits, capturing screenshots, reading the profiler, and listing/calling custom tools. Works against a Unity instance that has the `com.youngwoocho02.unity-cli-connector` package installed (already present in this project). Prefer this over MCP when a shell command is enough.'
argument-hint: 'Describe the Unity action (e.g. "enter play mode", "run C# to get scene name", "read errors")'
---

# Unity CLI

Control the Unity Editor from the command line using the `unity-cli` binary. The
Unity-side connector (`com.youngwoocho02.unity-cli-connector`) listens
automatically when the Editor is open — there is no server to start. The CLI
discovers Unity instances by scanning per-project instance files in
`~/.unity-cli/instances/`.

## When to Use

- Enter / stop / pause play mode, refresh or recompile assets
- Run one-off C# code inside Unity (`exec`) — full access to UnityEngine/UnityEditor
- Read, filter, or clear console logs
- Run EditMode / PlayMode tests
- Execute any Unity menu item by path
- Reserialize assets (`.prefab`, `.unity`, `.asset`, `.mat`) after text edits
- Read the profiler hierarchy or capture screenshots
- List or call project-defined custom tools

## Prerequisites

1. **Unity Editor open** with this project (the connector starts automatically).
2. **`unity-cli` binary installed** and on PATH. Verify with `unity-cli status`.
   - Windows (PowerShell): `irm https://raw.githubusercontent.com/youngwoocho02/unity-cli/master/install.ps1 | iex`
   - macOS / Linux: `curl -fsSL https://raw.githubusercontent.com/youngwoocho02/unity-cli/master/install.sh | sh`
   - Any platform with Go: `go install github.com/youngwoocho02/unity-cli@latest`
3. The Unity package is already in this project's `Packages/manifest.json`:
   `"com.youngwoocho02.unity-cli-connector": "https://github.com/youngwoocho02/unity-cli.git?path=unity-connector"`.
4. Recommended: set Edit → Preferences → General → Interaction Mode to **No
   Throttling** for responsive background commands.

Always run `unity-cli status` first to confirm Unity is reachable. The CLI also
auto-waits when Unity is compiling or reloading before sending a command.

## Common Commands

```bash
# Connection / status
unity-cli status

# Editor control
unity-cli editor play            # enter play mode
unity-cli editor play --wait     # enter play mode and wait until fully loaded
unity-cli editor stop            # stop play mode
unity-cli editor pause           # toggle pause (only during play mode)
unity-cli editor refresh         # refresh assets (blocked in play mode unless --force)
unity-cli editor refresh --compile   # refresh + recompile scripts
unity-cli editor refresh --force     # force refresh while in play mode

# Console logs
unity-cli console                              # errors + warnings (default)
unity-cli console --lines 20 --filter error,warning,log
unity-cli console --type error                 # errors only
unity-cli console --stacktrace user            # user code stack traces (or: full)
unity-cli console --clear                      # clear console

# Run C# inside Unity (most powerful command)
unity-cli exec "return Application.dataPath;"
unity-cli exec "return EditorSceneManager.GetActiveScene().name;"
unity-cli exec "return World.All.Count;" --usings Unity.Entities
echo 'Debug.Log("hello"); return null;' | unity-cli exec   # pipe via stdin to avoid escaping

# Menu items (File/Quit is blocked)
unity-cli menu "File/Save Project"
unity-cli menu "Assets/Refresh"

# Reserialize after editing asset YAML as text
unity-cli reserialize                                  # whole project
unity-cli reserialize Assets/Prefabs/Player.prefab
unity-cli reserialize Assets/Scenes/Main.unity Assets/Scenes/Lobby.unity

# Tests (Unity Test Framework required)
unity-cli test                          # EditMode (default)
unity-cli test --mode PlayMode
unity-cli test --filter MyTestClass

# Profiler
unity-cli profiler hierarchy --depth 3
unity-cli profiler enable / disable / status / clear

# Screenshot
unity-cli screenshot                    # capture scene/game view as PNG

# Tools discovery / custom tools
unity-cli list                          # all tools + parameter schemas
unity-cli my_custom_tool --params '{"key":"value"}'
```

## exec Notes

- Use `return` to get output back. Common namespaces are included by default; add
  project-specific ones with `--usings Namespace` (repeatable, or comma-separated).
- `exec` **blocks async/coroutine/deferred callbacks by default** because the
  command returns before those complete. Pass `--allow-async` only when the
  delayed behavior is intentional.
- Pipe complex code via stdin (`echo '...' | unity-cli exec`) to avoid shell
  escaping headaches.

## Reserialize Notes

After editing a Unity asset file as plain-text YAML, always run `unity-cli
reserialize <path>` so Unity rewrites it through its own serializer. This
prevents silent corruption from a missing field, wrong indent, or stale
`fileID`.

## Global Options

| Option | Purpose | Default |
|---|---|---|
| `--project <path>` | Select Unity instance by project path | auto |
| `--timeout <ms>` | HTTP request timeout | 120000 |
| `--ignore-version-mismatch` | Skip CLI/connector version check | false |

When multiple Unity Editors are open, disambiguate with `--project`:
`unity-cli --project gemcafe editor play`. Use `--help` on any command for
detailed usage (e.g. `unity-cli exec --help`).

## Reference

- Repo: https://github.com/youngwoocho02/unity-cli
- Built-in commands: `editor`, `console`, `exec`, `test`, `menu`, `reserialize`,
  `screenshot`, `profiler`, `list`, `status`, `update`.
