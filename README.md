# Camp Badgewood — gameplay update

Open with **Unity 6000.6.4f1**. Open **Assets/Scenes/CampgroundLayout.unity** and press Play. The main menu appears automatically; choose **Start Adventure** to enter the campground.

This update uses the supplied **CMPM125FinalProject-main(2).zip** as its base. Existing campground placements, campfire objects, bird assets, imported nature packs, art, prefabs, package versions, and the original standalone cliff prototype are retained.

## Menus

The main menu provides Start Adventure, Options, Help & Controls, Credits, and Quit Game. After starting, Escape opens a pause menu with Resume, Options, Help & Controls, Main Menu, and Quit. Returning to the main menu preserves the current session; Start becomes Continue Adventure.

Options include mouse sensitivity, master audio volume, field of view, invert vertical look, and fullscreen. Sensitivity, volume, FOV, and invert look are saved with PlayerPrefs. Fullscreen is a desktop-build setting. Quit closes a desktop build; in the Unity editor it stops Play Mode. Browsers control their own tabs, so browser builds cannot be closed by Application.Quit.

Menus use Unity uGUI and the Input System UI module. The menu Canvas, panels, buttons, sliders, toggles, crosshair, and HUD are created automatically by **CampGameFlow** at runtime. They appear under the **Game - menus, badges, and cliff progression** object during Play Mode. No editor generation or manual button wiring is required.

## First-person controls

| Control | Action |
| --- | --- |
| WASD / arrows | Walk |
| Mouse | Look |
| Space | Jump; dive when standing on the yellow launch pad |
| Shift | Run |
| F or left click | Pick up campfire supplies; build the campfire when ready |
| Left click on a bird under the crosshair | Observe a bird |
| Escape | Pause and release the mouse |
| R during a dive or its result screen | Retry the current stage |
| Enter on a dive result screen | Next stage after success, or retry after failure |

Click the Game view to give it keyboard/mouse focus when testing in the editor. The free camera component is disabled in this scene. The new first-person player uses a CharacterController, gravity, ground detection, jumping with a short input buffer/coyote window, collision-limited walking, and no free flight or viewpoint teleports. Movement runs in small time steps to reduce collision and ring-skipping issues.

Falling off camp outside an active dive returns the player to the entrance. This prevents becoming stranded at the bottom of the cliff.

## Connected cliff diving

Walk from the common ground along the **CLIFF DIVING** trail. The activity takes place in the existing cliff environment in the same campground scene. Stand on the yellow launch pad and press Space.

Forward movement during a dive is automatic. A / D steers across the course. W / S fine-tunes forward speed after the walking key has been released, so holding W while approaching the pad does not accidentally accelerate the first part of a dive. Mouse look stays available so you can follow the rings below.

Pass through every ring **in order**, then land within the lake. A result panel provides Next Stage after success, Retry Stage, and Return to Camp. Three completed stages award the Cliff Diver Badge. Returning to camp after a successful stage queues the next one. Progress and badges are kept for the current Play session; a save-game system has not been added.

| Stage | Course | Rings | Opening radius | Course changes |
| --- | --- | ---: | ---: | --- |
| 1 | First Leap | 4 | 1.50 m | Broad openings and gentle lateral adjustments |
| 2 | Woodland Weave | 5 | 1.32 m | Tighter alternating turns and faster descent |
| 3 | Eagle Run | 6 | 1.14 m | Smaller openings, more direction changes, and the fastest descent |

The stages share the cliff and lake, with separate saved ring layouts. They are not separate scene loads. Look under **10 - Existing cliff diving environment > Dive stage ...** in the Hierarchy. Stage one is initially visible; the activity swaps layouts as you progress.

### Ring hit detection

The new activity replaces spherical proximity pickups with an ordered **swept crossing of each ring's opening plane**. The player's body center must cross forward through the opening. The permitted radius accounts for the player's 0.32 m body radius and 0.10 m forgiveness. Touching a ring from the side or passing outside its opening does not collect it. Continuous segment checks handle movement spanning the plane between frames. Collection stops at the lake landing.

The actual torus mesh has an inner radius of 0.91 times the stage's ring scale; hit detection uses that same radius. The ring graphics have no solid collider that could snag the player. The original **Assets/Scenes/CliffDivePrototype.unity** and its legacy scripts remain available separately for reference and retain their earlier behaviour. Use CampgroundLayout for the new three-stage first-person activity.

## Existing group activities

- The campfire activity still uses the assigned campfire and its surrounding rocks/sticks. Collect five of each, then aim at the fire and press F or click to build it. Its progress and contextual hints are displayed in the shared HUD.
- The bird watching counter is connected to first-person center-screen interaction. The existing Living Birds prefabs use trigger hitboxes, so the interaction ray includes triggers. Each distinct ambient bird can be counted once; existing birdclick interactions also feed the shared counter. Observing five birds awards the Bird Watching Badge.
- Activity input is blocked during menus, pause screens, dive results, and active dives. Menu clicks therefore do not also collect supplies or observe birds. Existing quest HUDs are replaced by the shared HUD only while the new game flow is present.

## Main scene objects and scripts

- **Player - first person**: CharacterController and CampFirstPerson; the former free camera is now its head camera.
- **Main menu scenic camera**: fixed overview used behind the title menu.
- **Game - menus, badges, and cliff progression**: CampGameFlow, CliffDiveActivity, the shared bird counter, and TerrainBoundsRefresh.
- **Assets/Badgewood/Runtime/CampGameFlow.cs**: menu design, button actions, settings, cursor/pause state, and shared HUD.
- **Assets/Badgewood/Runtime/CampFirstPerson.cs**: walking, jumping, looking, bird raycasts, and dive movement.
- **Assets/Badgewood/Runtime/CliffDiveActivity.cs**: launch, stage swapping, scoring, retries, badge progress, and camp return.
- **Assets/Badgewood/Runtime/DiveAperture.cs**: ring-opening intersection calculations.
- **Assets/Badgewood/Runtime/TerrainBoundsRefresh.cs**: makes fresh runtime copies of generated terrain meshes and recomputes their bounds. This keeps saved terrain assets/placements intact while avoiding reliance on their old imported LOD/bounds metadata.

The earlier **Tools > Campground > Close-up Scene View** helper remains available. Terrain bounds refreshing and this helper address different parts of the earlier visibility issue. The issue has not been reproduced in Unity in this environment, so its visual resolution still needs checking in the editor.

## Install in the group repository

The full ZIP contains a **Badgewood** folder with Assets, Packages, ProjectSettings, .gitignore, and this README. Add that folder as a project through Unity Hub, or copy its contents into the existing Unity project on your working Git branch. Keep every .meta file. Use GitHub Desktop to commit the extracted files, then push/publish the branch.

The optional **Campground-Gameplay-Update.zip** contains only changed/new assets and this README. Apply it to the exact uploaded main-project version by merging its Assets folder into that project's Assets folder, then reopen CampgroundLayout. It is an update, not a complete standalone project.

## Verification and remaining checks

See **GAMEPLAY-VALIDATION.json** for reference checks, preservation checks, C# syntax checks, ring-boundary tests, and nine numerical trajectory simulations across 30, 60, and 144 FPS. All three designed routes pass every ring and land inside the lake in those simulations.

**Unity is not installed in the preparation environment.** These are data, syntax, and numerical geometry checks, not Unity compilation, automated Play Mode tests, or confirmation of the game feel. Open the project in Unity to complete the following checks before merging:

1. Confirm the Console has no compile errors and the title menu appears.
2. Test Start, Options, Back, Help, Credits, Pause/Resume, Main Menu/Continue, and Quit.
3. Walk around the new group placements, jump, run, and approach terrain from multiple angles.
4. Collect campfire materials, build it, and observe five distinct birds.
5. Test a missed ring, a successful landing, retry, stage progression, pause during a dive, and return to camp.
6. Test all three courses for difficulty and camera comfort; tune the serialized course fields if needed after playtesting.
7. In a desktop build, check fullscreen, audio volume, and quitting.

## Credits

- CMPM 125 project group; Florian Ionescu and teammates.
- Campfire activity, bird-watching work, and updated scenery from the supplied group main branch.
- Low-Poly Simple Nature Pack by JustCreate: https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-simple-nature-pack-162153
- Low Poly: Woods Lifestyle by Rad-Coders: https://assetstore.unity.com/packages/3d/environments/low-poly-woods-lifestyle-65306
- Living Birds asset pack, retained from the uploaded project.

Retain the imported assets' original license and attribution files with the group project.
