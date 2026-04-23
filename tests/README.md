# Test Infrastructure — 影子回忆 (Shadow Memory)

**Engine**: Unity 2022.3.62f2 LTS
**Test Framework**: Unity Test Framework (NUnit)
**CI**: `.github/workflows/tests.yml`
**Setup date**: 2026-04-22
**Coverage target**: 70% for gameplay logic systems (per `technical-preferences.md`)

## Directory Layout

```
tests/                          # Docs, specs, manual evidence (repo root)
  unit/                         # Design notes for unit test scope
  integration/                  # Design notes for integration test scope
  smoke/                        # Critical path checklist for /smoke-check
  evidence/                     # Screenshot logs and manual test sign-off records

src/MyGame/ShadowGame/Assets/Tests/   # Actual Unity test code (inside project)
  EditMode/                     # Unity EditMode test assembly (.asmdef + tests)
  PlayMode/                     # Unity PlayMode test assembly (.asmdef + tests)
```

## Running Tests

### From Unity Editor
`Window → General → Test Runner` — Run EditMode and PlayMode tests.

### From Command Line
```bash
# EditMode tests (headless)
Unity -batchmode -nographics -projectPath src/MyGame/ShadowGame \
  -runTests -testPlatform EditMode -testResults test-results/editmode.xml

# PlayMode tests (requires display or virtual framebuffer)
Unity -batchmode -projectPath src/MyGame/ShadowGame \
  -runTests -testPlatform PlayMode -testResults test-results/playmode.xml
```

### Via CI
Push to `main` or open a Pull Request — GitHub Actions runs both suites automatically.

## Test Naming

- **Files**: `[System]_[Feature]_Test.cs` (PascalCase per Unity convention)
- **Classes**: `[System][Feature]Tests`
- **Methods**: `[MethodUnderTest]_[Scenario]_[ExpectedResult]`
- **Example**: `ShadowPuzzle_MatchScore_Test.cs` → `CalculateMatchScore_AllAnchorsAligned_ReturnsPerfectMatch()`

## Story Type → Test Evidence

| Story Type | Required Evidence | Location |
|---|---|---|
| Logic | Automated unit test — must pass | `Assets/Tests/EditMode/` |
| Integration | Integration test OR playtest doc | `Assets/Tests/PlayMode/` or `tests/integration/` |
| Visual/Feel | Screenshot + lead sign-off | `tests/evidence/` |
| UI | Manual walkthrough OR interaction test | `tests/evidence/` |
| Config/Data | Smoke check pass | `production/qa/smoke-*.md` |

## Assembly References

### EditMode Tests (`src/MyGame/ShadowGame/Assets/Tests/EditMode/EditModeTests.asmdef`)
- References: `GameLogic` (gameplay code), `GameProto` (Luban configs)
- Does NOT reference: UnityEngine.UI, DOTween (no runtime dependencies)

### PlayMode Tests (`src/MyGame/ShadowGame/Assets/Tests/PlayMode/PlayModeTests.asmdef`)
- References: `GameLogic`, `GameProto`, plus any runtime assemblies needed
- Runs in a real scene context with MonoBehaviour lifecycle

## CI

Tests run automatically on every push to `main` and on every pull request.
A failed test suite blocks merging. See `.github/workflows/tests.yml`.

**Note**: Unity CI requires a `UNITY_LICENSE` secret in GitHub repository settings.
See [game-ci/unity-test-runner](https://github.com/game-ci/unity-test-runner) for setup.
