# Smoke Test: Critical Paths — 影子回忆 (Shadow Memory)

**Purpose**: Run these checks in under 15 minutes before any QA hand-off.
**Run via**: `/smoke-check` (which reads this file)
**Update**: Add new entries when new core systems are implemented.

## Core Stability (always run)

1. [ ] Game launches to MainScene without crash (BootScene → ProcedureMain → MainScene)
2. [ ] Main menu panel displays correctly (HUD layer, safe area respected)
3. [ ] Main menu responds to touch input without freezing

## Core Mechanic (update per sprint)

4. [ ] [Sprint 1] Object can be selected via tap (Raycast hit detection)
5. [ ] [Sprint 1] Object can be dragged with single finger (position updates smoothly)
6. [ ] [Sprint 1] Object snaps to grid on release (grid snap animation plays)
7. [ ] [Sprint 2] Shadow projection updates in real-time when object moves
8. [ ] [Sprint 2] Match score updates and NearMatch feedback triggers

## Data Integrity

9. [ ] Save game completes without error (auto-save on puzzle state change)
10. [ ] Load game restores correct chapter and puzzle state
11. [ ] Settings persist across app restart (PlayerPrefs)

## Performance

12. [ ] No visible frame rate drops below 30fps on target device (60fps target)
13. [ ] No memory growth over 5 minutes of core loop play
14. [ ] Scene transition completes without hang (fade + load + fade < 5s)

## Chapter Transition

15. [ ] Chapter complete → narrative sequence plays → next chapter loads
