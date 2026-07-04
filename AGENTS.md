# AGENTS.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 (6000.5.2f1), URP 17.5.0, Linear color space. A 3D action game in early development — no custom gameplay code yet. One scene: `Assets/Scenes/SampleScene.unity`.

## Working with the Editor

Unity MCP (`com.coplaydev.unity-mcp`) is installed. Always invoke the `unity-mcp-skill` skill before automating editor operations via Claude.

Tests run via **Window → General → Test Runner** in the editor, or via `unity -runTests` on the CLI.

Build via **File → Build Settings** — no custom build scripts exist yet.

## Packages (non-obvious)

| Package | Purpose |
|---|---|
| `com.coplaydev.unity-mcp` | Unity MCP server — drives editor from Claude Code |
| `com.unity.ai.navigation` 2.0.13 | NavMesh-based AI pathfinding |
| `com.unity.inputsystem` 1.19.0 | New Input System; actions in `Assets/InputSystem_Actions.inputactions` |
| `com.unity.multiplayer.center` | Multiplayer planning hub (installed, not yet wired up) |
| `com.unity.timeline` | Cutscene / sequencer support |
| `com.unity.visualscripting` | Bolt visual scripting |
| `com.unity.test-framework` 1.7.0 | Unit/integration tests via Test Runner |

## Input Map

`Assets/InputSystem_Actions.inputactions` defines the **Player** action map and reveals the intended game shape:

| Action | Keyboard | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Look | Mouse delta | Right stick |
| Attack | Left mouse button | West button |
| Interact | E (**Hold**) | North button (hold) |
| Jump | Space | South button |
| Crouch | C | East button |
| Sprint | Left Shift | Left stick press |
| Previous / Next | 1 / 2 | D-pad left / right |

`Previous`/`Next` suggest an item or weapon wheel. `Interact` uses a Hold interaction — player must hold the key.

## Folder Structure

No project-specific folders established yet. Structure should grow under `Assets/` as gameplay systems are added.

## Behavioral Guidelines

Guidelines to reduce common LLM coding mistakes. Bias toward caution over speed; for trivial tasks, use judgment.

### Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.
