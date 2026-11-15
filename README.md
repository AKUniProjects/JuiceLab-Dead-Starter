# JuiceLab — Improving Game Feel (SWE402 Assignment 3)

**Student:** Ali Kamal Aloryd  
**Student ID:** 202322750 
**Course:** SWE402 — Game Development  
**Engine:** Unity 6.3 (`6000.3.22f1`, development editor)  
**Project:** Brick Breaker / JuiceLab — Before & After  

## Overview and intended feel

This assignment improves the *game feel* of the supplied Brick Breaker starter while preserving the original board, scoring values, paddle controls, ball physics, and lives. My intended style is **playful, responsive, and clear**: every important action should be easy to recognize through brief, readable visual and audio feedback.

The repository contains two scenes for comparison:

- **Before:** `Assets/_Project/Scenes/Before.unity` — the untouched, deliberately unpolished starter.
- **After:** `Assets/_Project/Scenes/After.unity` — the same core game with event-driven feedback and additional optional interface features.

The starter awards **10 points per brick** and contains **28 bricks**. The maximum score when all are cleared is **280 points**. The additions were made around the starter's existing gameplay rather than changing its collision physics or point values.

## Five required game-feel improvements

| Improvement | Trigger | Implementation file / Unity object | Purpose |
|---|---|---|---|
| **1. Brick impact particles** | A brick is destroyed (`GameEvents.BrickDestroyed`) | `Assets/_Project/Scripts/Feedback/BrickImpactFeedback.cs`, `BrickImpactFX.cs`; **Brick Impact Feedback** object; `BrickImpactFX.prefab` | Fast, colorful sparks and brick-colored fragments make a successful hit immediately recognizable. |
| **2. Reactive paddle animation** | Ball contacts paddle (`GameEvents.BallHitPaddle`) | `Assets/_Project/Scripts/Feedback/PaddleImpactFeedback.cs`; **Paddle** visual child | Squash/stretch, recoil, and cyan flash emphasize impact without modifying the paddle's collider or bounce physics. |
| **3. Event-based audio** | Paddle hit, brick destruction, or ball loss | `Assets/_Project/Scripts/Feedback/GameAudioFeedback.cs`; **Game Audio Feedback** object | Distinct sounds identify the three required events; independent volume and duration controls keep them balanced. |
| **4. Animated score interface** | Score increases (`GameEvents.ScoreChanged`) | `Assets/_Project/Scripts/Feedback/ScoreFeedback.cs`; **Canvas** | A 130% score pop, brief gold flash, and floating volley-point display make scoring noticeable while keeping the real total score correct. |
| **5. Ball trail and loss feedback** | Ball moves; ball is lost (`GameEvents.BallLost`) | `Assets/_Project/Scripts/Feedback/BallTrailFeedback.cs`, `LifeLossFeedback.cs`; **Ball** and **Canvas** | A readable cyan trail helps track fast movement; a brief red flash emphasizes losing a life. |

**Complementary techniques on the same event:** Brick destruction triggers both the particle impact and `BrickBreak.wav`, combining visual and audio feedback.

### Effect tuning and extra polish

**Brick impact:** Two independently switchable effects — 15 warm-colored sparks and 8 larger fragments tinted to the brick's color. Temporary particles fade and clean up after the hit.

**Paddle feedback:** Visual-only squash/stretch, approximately 0.22-unit recoil, and a short cyan flash. Each can be enabled independently; the visual returns to its original position and scale.

**Audio:** `PaddleHit.wav`, `BrickBreak.wav`, and `BallLost.wav` are event-specific, with separate toggles, volume sliders, and **Duration (%)** controls. Shortened sounds fade out rather than changing pitch or playback speed.

**Score:** The total score is displayed at the top center; lives appear at the bottom right. A centered gold `+Points` label shows the *accumulated points in the current volley*. For example, three brick hits before a paddle touch display `+10`, `+20`, `+30`. Touching the paddle resets the **volley counter only**, not the actual total score. The pop, gold flash, and floating label each have independent toggles.

**Ball trail:** Cyan `#45E6F5`, **0.32 world units** wide, **85% opacity**, tapering to zero over **0.15 seconds**. The trail clears on loss, respawn/teleport, menu pause, victory, and restart so it never draws a long line across the board.

**Life-loss flash:** Red `#FF4545`, maximum **25% opacity**, fading over **0.18 seconds**. The non-interactive overlay does not block HUD or buttons and continues fading even when the game is paused.

**Additional collision pulse:** `Assets/_Project/Scripts/Feedback/BallCollisionPulseFeedback.cs` creates an expanding cyan ring at the *real collision contact point*. The ring grows from **0.30 to 0.95 world units** over **0.22 seconds** and fades out. Wall pulses are enabled by default; separate paddle and brick pulse toggles are available.

These effects have independent Inspector settings and were tuned for a **640 × 360, 16:9** Game view. They do not add forces, modify hitboxes, or change scoring rules.

## Optional start, win, and loss presentation

The **After** scene also includes optional game-flow presentation, separate from the five required upgrades:

- **PLAY:** A centered start overlay before gameplay begins.
- **GAME OVER:** A final-score panel and **PLAY AGAIN** button when all three lives are lost. `GameOver.wav` is used for the final loss instead of layering another `BallLost.wav` on top of it.
- **YOU WIN!:** Clearing the last brick shows the final score, a **PLAY AGAIN** button, colorful fireworks, and separate `Win.wav` and `Fireworks.wav` cues.
- **PLAY AGAIN:** Restarts the **After** scene with the original 28-brick board, three lives, and zero score.

The start, Game Over, Victory, fireworks, and their respective sounds are separately configurable in the Inspector. **These menus/restart behaviors are additional presentation features, not part of the original starter's end-state behavior.** When evaluating strict baseline equivalence, disable the optional game-flow screens and compare the feedback on unchanged core collisions, controls, point values, and lives.

## Before vs. After

| Situation | Before scene | After scene |
|---|---|---|
| Brick destroyed | Brick disappears and score increases | Sparks, colored fragments, brick-break sound, and score animation |
| Paddle contact | Ball bounces | Ball bounces with visual squash, recoil, cyan flash, and paddle sound |
| Ball moving / wall contact | Basic moving ball and bounce | Thicker cyan trail; wall-contact ring pulse |
| Score gained | Text value updates | Text pops/flashes, with a floating volley total |
| Life lost | Life count decreases | Ball-loss cue plus a short red flash; dedicated Game Over cue on final life when enabled |
| All bricks cleared | Starter's original state | Optional victory panel, sound, and fireworks |

`Before.unity` was reported **byte-identical** after implementation and regression testing. Shared gameplay scripts and physics settings were preserved. Visual effects were implemented in `After.unity` and its dedicated feedback components.

## Playtest and revision

**Tester:** My brother (one person other than the developer; name intentionally omitted).

**Observation and feedback:** During his playtest, he noticed that reaching **GAME OVER** did not have its own distinctive sound. He suggested adding a sound to the loss screen because that outcome felt noticeably quieter than the other events.

**Revision based on feedback:** I generated `GameOver.wav` with ElevenLabs and integrated it into `GameAudioFeedback.cs` and the existing Game Over flow. Losing the first two lives continues to play `BallLost.wav`; the third/final loss now opens Game Over and plays `GameOver.wav` **once instead**, avoiding two overlapping loss cues. Its enable switch, volume, and duration percentage can be adjusted in the Inspector.

**Observed result after the revision:** Unity Play Mode testing confirmed the final-loss audio played once, continued while the Game Over UI paused gameplay, stopped on **PLAY AGAIN**, and remained independent of the victory audio. The tested full-length Game Over cue was approximately **1.49 seconds**. This addresses the specific gap the tester identified. This post-change verification was performed in Unity; no second human playtest is claimed.

## How to run

1. Clone or download this repository.
2. In **Unity Hub**, select **Add project from disk** and open the folder that directly contains `Assets/`, `Packages/`, and `ProjectSettings/`. In this repository layout, that is the inner `JuiceLab-Dead-Starter/` folder, not necessarily the Git repository root.
3. Open the project using **Unity 6.3**. The project was developed with editor `6000.3.22f1`; if using a different Unity 6.3 patch, review Unity's upgrade/import prompts.
4. Open either `Assets/_Project/Scenes/Before.unity` or `Assets/_Project/Scenes/After.unity`.
5. Set the **Game** tab to a **16:9** aspect ratio (640 × 360 is the reference test resolution), then press **Play**.
6. Focus the Game view. Move the paddle with the **mouse**, **A/D**, or the **left/right arrow** keys. The starter launches the ball automatically; with the optional **After** start overlay enabled, click **PLAY** first.
7. For a fresh baseline comparison, stop Play Mode and reopen the desired scene. The After scene's **PLAY AGAIN** button is also available when the optional end screen appears.

### Where to adjust the feedback

| Unity object | Inspector controls |
|---|---|
| **Brick Impact Feedback** | Sparks and fragments toggles / particle settings |
| **Paddle** | Squash/stretch, recoil, and flash toggles |
| **Game Audio Feedback** | Event sounds, Game Over sound, volume, and duration (%) |
| **Canvas** | Score effects and Life Loss Feedback |
| **Ball** | Ball Trail Feedback and Ball Collision Pulse Feedback |
| **Game Flow UI** | Optional Start, Game Over, and Victory screens |
| **Victory Feedback** | Fireworks count/intensity/duration and Win/Fireworks audio |

## Technical approach and verification

The added feedback responds to the starter's `GameEvents` where suitable, including `BrickDestroyed`, `BallHitPaddle`, `BallLost`, and `ScoreChanged`. The wall pulse uses actual ball collision contact data. Visual movement effects operate on renderers and visual objects rather than changing gameplay Rigidbody2D or Collider2D behavior. Temporary feedback is cleared or reused on reset, pause, and scene reload.

Codex's Unity 6.3 Play Mode verification reports covered real brick and paddle hits; all combinations of the new Step 2.5 toggles; repeated wall pulses; all three life losses; Game Over/Victory transitions; replay; duration and volume controls; and UI captures at 640 × 360. The tests also checked that `Before.unity` remained unchanged. These are implementation/test reports, not a claim that every effect was independently retested by the README author afterward.

## Assets, tools, attribution, and AI use

- **Course starter:** SWE402 *Assignment 3: Improving Game Feel*, supplied `JuiceLab-Dead-Starter.zip`. The original starter is credited to its authors and retains its **MIT license** and existing source attribution. Do not remove the starter's license file or notices.
- **Audio assets:** `PaddleHit.wav`, `BrickBreak.wav`, `BallLost.wav`, `GameOver.wav`, `Win.wav`, and `Fireworks.wav` were generated for this project with **ElevenLabs Sound Effects**. Check the applicable ElevenLabs account/license terms before reuse or public redistribution beyond course submission.
- **Visual assets:** Brick sparks/fragments, ball trail shader/material, collision ring, life-loss overlay, score animation, and victory fireworks were created/configured as Unity project feedback assets rather than imported from a finished game-feel solution.
- **Development tools:** Unity 6.3, C#, TextMeshPro/Unity UI, Unity Particle System, URP 2D, Git/GitHub.
- **AI assistance:**
  - **ChatGPT:** Wrote implementation prompts and refined tuning and documentation.
  - **OpenAI Codex:** Assisted with C# scripts, Unity scene configuration, and debugging.
  - **ElevenLabs:** Used to generate sound effects.

## Submission

- **GitHub repository:** https://github.com/AKUniProjects/JuiceLab-Dead-Starter
- **Assignment:** [SWE402 — Improving Game Feel](https://gamedevkfupm.github.io/swe402/individual%20assignments/game-feel/)
