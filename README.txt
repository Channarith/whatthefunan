================================================================================
WHAT THE FUNAN
================================================================================
A children's RPG adventure set in the ancient Funan Kingdom of Southeast Asia

Version: 0.2.0
Platform: Android & iOS
Engine: Unity 2022 LTS (URP)
Repository: https://github.com/Channarith/whatthefunan
================================================================================

ABOUT
================================================================================
"What the Funan" is a family-friendly RPG game featuring cute animal characters
in an ancient Southeast Asian fantasy setting. Players explore temples, solve
puzzles, engage in melee combat, and learn about the rich cultural heritage of
the Funan Kingdom (1st-9th Century CE).

FEATURES
- Fortnite-style cute 3D graphics
- Multiple combat modes (free-flow, paired animation, automated)
- 7 playable characters based on mythological creatures
- 5 mini-games (fishing, cooking, racing, rhythm, puzzles)
- Educational codex with real Funan history
- Seasonal events and daily rewards
- Photo mode with filters and sharing
- Full accessibility support
- Multi-language localization (English, Khmer, Thai, Lao)

================================================================================

QUICK START
================================================================================
1. Open project in Unity 2022.3 LTS or later
2. Unity will automatically import packages from manifest.json
3. Open Assets/Scenes/MainMenu.unity
4. Press Play to test

BUILD INSTRUCTIONS
--------------------------------------------------------------------------------
Android:
- File > Build Settings > Android
- Use "What the Funan/Build/Android APK" menu for debug builds
- Use "What the Funan/Build/Android AAB" for release builds

iOS:
- File > Build Settings > iOS
- Use "What the Funan/Build/iOS Xcode Project" menu
- Open generated Xcode project to build

================================================================================

PROJECT STRUCTURE
================================================================================
Assets/
├── Editor/              # Unity Editor tools and build scripts
├── Plugins/             # Third-party SDKs (Firebase, etc.)
├── Resources/           # Runtime-loaded assets
├── Scenes/              # Unity scenes
├── Scripts/             # C# game code
│   ├── Accessibility/   # Accessibility features
│   ├── Achievements/    # Achievement system
│   ├── Backend/         # Firebase integration
│   ├── Characters/      # Player controller
│   ├── Cinematics/      # Cutscene management
│   ├── Codex/           # Educational encyclopedia
│   ├── Collection/      # Collectibles album
│   ├── Combat/          # Combat systems
│   ├── Core/            # Core managers
│   ├── Data/            # ScriptableObject databases
│   ├── Economy/         # Currency management
│   ├── Features/        # Photo mode, emotes
│   ├── Gameplay/        # Mounts, weather, bosses
│   ├── LiveOps/         # Daily rewards, events
│   ├── Localization/    # Multi-language support
│   ├── MiniGames/       # Mini-game systems
│   ├── Monetization/    # IAP, ads
│   ├── Notifications/   # Push/local notifications
│   ├── ParentalControls/# COPPA-compliant controls
│   ├── RPG/             # Quests, dialogue, inventory
│   ├── Social/          # Friends, gifts, referrals
│   ├── Tutorial/        # Onboarding system
│   ├── UI/              # UI management
│   └── Utils/           # Helper utilities
├── Prefabs/             # Reusable game objects
├── Materials/           # Shaders and materials
├── Audio/               # Music and SFX
├── Textures/            # Images and sprites
└── Animations/          # Animation clips and controllers

================================================================================

LEGAL COMPLIANCE
================================================================================
IMPORTANT: Before releasing, complete LEGAL_COMPLIANCE_CHECKLIST.txt

Key requirements:
- No Thai royal imagery (lèse-majesté law - prison risk)
- Buddha images must be respectful (no combat zones)
- All temples must be fictional (not real UNESCO sites)
- COPPA compliance for children's data
- Thailand PDPA compliance for users under 20
- Parental controls and age gates required

See .cursor/rules/legal-compliance.mdc for development rules.

================================================================================

AI ART GENERATION
================================================================================
Use AI_ART_PROMPTS.txt for generating character and environment art with:
- Midjourney
- Leonardo.ai
- DALL-E
- Stable Diffusion

For 3D models:
- Meshy.ai
- Tripo3D
- Luma AI

================================================================================

THIRD-PARTY PACKAGES
================================================================================
Required (install via Package Manager):
- Universal Render Pipeline (URP)
- TextMeshPro
- Unity Input System
- Unity Localization
- Unity Purchasing (IAP)
- Unity Mobile Notifications
- Cinemachine
- Timeline
- Addressables

External (add via .unitypackage or Package Manager):
- Firebase SDK (Auth, Firestore, Analytics, Messaging)
- LeanTween (animation)
- DOTween (optional alternative)

================================================================================

CONTACTS
================================================================================
Repository: https://github.com/Channarith/whatthefunan
Issues: https://github.com/Channarith/whatthefunan/issues

================================================================================

LICENSE
================================================================================
Copyright (c) 2024 What the Funan Studio
All rights reserved.

================================================================================

