# Labyrinth Algorithm

Unity 6.6 (6000.6.2f1) project. Gameplay and generation code lives in `Assets/Scripts`, EditMode tests in `Assets/Tests`. It depends on `com.ryanslibrary`, which is loaded from `../../SharedPackages/com.ryanslibrary`, outside this repo.

## Unity Editor access

This project is set up for the Unity CLI (`unity`) and its MCP server:

- `com.unity.pipeline` is already in `Packages/manifest.json`, so a running Editor exposes its commands to the CLI.
- `.mcp.json` registers `unity mcp` as the `unity` MCP server. It targets the Editor that has this project open.
- `.claude/settings.json` enables Unity's official plugin (`unity@unity-agent-plugin`), which carries the `unity-cli` skill.

When an Editor is open, run `unity status` first and drive the Editor with `unity command` instead of hand-editing `.unity`, `.prefab` or `.asset` YAML. Run `unity command` with no arguments to list what the Editor exposes; don't guess command names.

If the CLI can't connect while the Editor is open, the project has probably failed to compile and the Editor is in Safe Mode. Confirm with `unity pipeline list`, then fix the compile errors.

## One-time setup per machine

1. Install the Unity CLI (beta):
   - macOS / Linux: `curl -fsSL https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.sh | UNITY_CLI_CHANNEL=beta bash`
   - Windows (PowerShell): `$env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex`
2. Open a new shell and check `unity --version`.
3. Open this project in Unity 6000.6.2f1 and check that `unity status` shows it as `ready`.
4. Start Claude Code in the project root. Approve the `unity` MCP server and the Unity plugin when prompted.
5. Optional: `unity skill install claude-code --local` adds the Pipeline package's own `unity-pipeline` skill under `.claude/skills/`.
