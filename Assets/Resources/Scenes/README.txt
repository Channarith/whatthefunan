================================================================================
WHAT THE FUNAN - SCENE STRUCTURE
================================================================================

This folder should contain the following Unity scenes:

CORE SCENES
-----------
1. Splash.unity        - Company logo, age gate
2. MainMenu.unity      - Title screen, play, settings, shop
3. Loading.unity       - Loading screen between scenes

GAMEPLAY SCENES
---------------
4. HubWorld.unity      - Funan City main hub, NPCs, shops
5. JungleZone.unity    - Jungle exploration area
6. TempleRuins.unity   - Ancient temple dungeon
7. WaterKingdom.unity  - Naga underwater realm
8. MountainPath.unity  - Mountain climbing zone
9. RoyalPalace.unity   - Final story area

MINI-GAME SCENES
----------------
10. Fishing.unity      - Mekong fishing spot
11. Cooking.unity      - Kitchen cooking game
12. Racing.unity       - Elephant racing track
13. RhythmDance.unity  - Apsara dance stage
14. Puzzles.unity      - Temple puzzle room

BOSS ARENAS
-----------
15. Boss_SerpentKing.unity   - Chapter 1 boss
16. Boss_ShadowDragon.unity  - Chapter 2 boss
17. Boss_AncientGuardian.unity - Final boss

CINEMATICS
----------
18. Cinematic_Intro.unity    - Game intro cutscene
19. Cinematic_Chapter1.unity - Chapter transitions
20. Cinematic_Ending.unity   - Ending sequence

================================================================================
SCENE LOADING ORDER
================================================================================

1. Splash (auto-loads MainMenu after 3s)
2. MainMenu (player choice)
3. Loading (brief, async load next scene)
4. [Target Scene]

================================================================================
NOTES
================================================================================

- All gameplay scenes should include:
  * GameManager (persistent singleton)
  * Player spawn point
  * Environment lighting (URP)
  * Audio zones
  * Navigation mesh

- Hub world is the default scene after login

- Use SceneController.LoadScene("SceneName") for transitions
================================================================================

