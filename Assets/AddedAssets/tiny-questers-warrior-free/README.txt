================================================================
  TINY QUESTERS  —  Free Warrior
  A free sample from "Tiny Questers — Complete Pixel RPG
  Asset Pack" by Bobddadoo  (https://bobddadoo.itch.io)
================================================================

Thanks for downloading! This is a fully-animated top-down warrior
character, free for personal and commercial projects.

This Warrior is a free sample from the upcoming full pack:

      TINY QUESTERS  —  Complete Pixel RPG Asset Pack
      8 RPG classes, each with the same complete animation set:
        Knight  ·  Priestess  ·  Bard       ·  Dark Mage
        Warrior ·  Mage       ·  Rogue      ·  Elf Archer
      (+ VFX, launching on itch.io and the Unity Asset Store)

Follow Bobddadoo on itch.io to be notified the moment the full
pack drops — and to get launch-week discounts.


----------------------------------------------------------------
  FOLDER STRUCTURE
----------------------------------------------------------------

warrior_free/
  README.txt              ... this file
  LICENSE.txt             ... license terms (please read)

  png/
    single/               ... individual PNG frames
                              idle / walk / hit ... 64  x 49
                              die / die_no_shadow . 73  x 49
                              win_right / win_left  64  x 64
                              attack_*            . 128 x 128
      idle/
        down/             ...   down1.png   ~ down5.png
        up/               ...   up1.png     ~ up5.png
      walk/
        down/             ...   down1.png   ~ down4.png
        up/               ...   up1.png     ~ up4.png
        left/             ...   left1.png   ~ left4.png
        right/            ...   right1.png  ~ right4.png
      hit/                ...   hit1.png    ~ hit2.png
      die/                ...   die1.png    ~ die10.png
      die_no_shadow/      ...   die1.png    ~ die10.png  (shadow removed)
      win_right/          ...   win_right1.png ~ win_right10.png  (victory pose, 64 x 64)
      win_left/           ...   win_left1.png  ~ win_left10.png   (mirrored)
      attack_down/        ...   attack_down1.png        ~ attack_down20.png       (with built-in forward step)
      attack_down_stay/   ...   attack_down_stay1.png   ~ attack_down_stay20.png  (in place — drive motion in code)
      attack_up/          ...   attack_up1.png          ~ attack_up20.png         (with built-in forward step)
      attack_up_stay/     ...   attack_up_stay1.png     ~ attack_up_stay20.png    (in place)
      attack_left/        ...   attack_left1.png        ~ attack_left18.png       (with built-in forward step)
      attack_left_stay/   ...   attack_left_stay1.png   ~ attack_left_stay18.png  (in place)
      attack_right/       ...   attack_right1.png       ~ attack_right18.png      (mirrored from left)
      attack_right_stay/  ...   attack_right_stay1.png  ~ attack_right_stay18.png (mirrored from left)
    atlas/                ... horizontal sprite sheets (single PNG per animation)
      idle_down.png             ...   5 frames   (320 x 49)
      idle_up.png               ...   5 frames   (320 x 49)
      walk_down.png             ...   4 frames   (256 x 49)
      walk_up.png               ...   4 frames   (256 x 49)
      walk_left.png             ...   4 frames   (256 x 49)
      walk_right.png            ...   4 frames   (256 x 49)
      hit.png                   ...   2 frames   (128 x 49)
      die.png                   ...   10 frames  (730 x 49)   [73 px per frame]
      die_no_shadow.png         ...   10 frames  (730 x 49)   [73 px per frame]
      win_right.png             ...   10 frames  (640 x 64)   [64 px per frame]
      win_left.png              ...   10 frames  (640 x 64)   [64 px per frame]
      attack_down.png           ...   20 frames  (2048 x 256) [16 per row, 2 rows]
      attack_down_stay.png      ...   20 frames  (2048 x 256) [16 per row, 2 rows]
      attack_up.png             ...   20 frames  (2048 x 256) [16 per row, 2 rows]
      attack_up_stay.png        ...   20 frames  (2048 x 256) [16 per row, 2 rows]
      attack_left.png           ...   18 frames  (2048 x 256) [16 per row, 2 rows]
      attack_left_stay.png      ...   18 frames  (2048 x 256) [16 per row, 2 rows]
      attack_right.png          ...   18 frames  (2048 x 256) [16 per row, 2 rows]
      attack_right_stay.png     ...   18 frames  (2048 x 256) [16 per row, 2 rows]

    single_pot/           ... power-of-two padded versions of single/
                              (original art placed at top-left (0,0),
                              transparent padding on the right/bottom —
                              pivot and per-frame coordinates unchanged)
                              idle / walk / hit ... 64  x 64
                              die / die_no_shadow . 128 x 64
                              attack_*            . 128 x 128 (already POT, identical to single/)
      idle/{down,up}/, walk/{down,up,left,right}/, hit/, die/, die_no_shadow/,
      attack_down/, attack_down_stay/, attack_up/, attack_up_stay/,
      attack_left/, attack_left_stay/, attack_right/, attack_right_stay/
        (same filenames as single/)
    atlas_pot/            ... power-of-two padded versions of atlas/
                              (original sheet placed at top-left (0,0),
                              transparent padding on the right/bottom —
                              slice with the SAME pixel sizes as atlas/)
      idle_down.png             ...   5 frames   (512  x 64)
      idle_up.png               ...   5 frames   (512  x 64)
      walk_down.png             ...   4 frames   (256  x 64)
      walk_up.png               ...   4 frames   (256  x 64)
      walk_left.png             ...   4 frames   (256  x 64)
      walk_right.png            ...   4 frames   (256  x 64)
      hit.png                   ...   2 frames   (128  x 64)
      die.png                   ...   10 frames  (1024 x 64)  [73 px per frame]
      die_no_shadow.png         ...   10 frames  (1024 x 64)  [73 px per frame]
      win_right.png             ...   10 frames  (1024 x 64)  [64 px per frame]
      win_left.png              ...   10 frames  (1024 x 64)  [64 px per frame]
      attack_down.png           ...   20 frames  (2048 x 256) [identical to atlas/, already POT]
      attack_down_stay.png      ...   20 frames  (2048 x 256)
      attack_up.png             ...   20 frames  (2048 x 256)
      attack_up_stay.png        ...   20 frames  (2048 x 256)
      attack_left.png           ...   18 frames  (2048 x 256)
      attack_left_stay.png      ...   18 frames  (2048 x 256)
      attack_right.png          ...   18 frames  (2048 x 256)
      attack_right_stay.png     ...   18 frames  (2048 x 256)

  gif/                    ... preview GIFs, transparent background
    1x/                   ... original size
                              idle / walk / hit ... 64  x 49
                              die / die_no_shadow . 73  x 49
                              attack_*            . 128 x 128
      idle_down.gif, idle_up.gif, walk_down.gif, walk_up.gif, walk_left.gif,
      walk_right.gif, hit.gif, die.gif, die_no_shadow.gif,
      win_right.gif, win_left.gif,
      attack_down.gif, attack_up.gif, attack_left.gif, attack_right.gif
    5x/                   ... 5x upscaled, nearest-neighbor
                              idle / walk / hit ... 320 x 245
                              die / die_no_shadow . 365 x 245
                              win_right / win_left  320 x 320
                              attack_*            . 640 x 640
      (same filenames as 1x/)

  GIFs are provided only for the moving (non-_stay) variants, so
  the preview shows the full attack motion — including the
  forward step. _stay variants do not have a GIF preview.


----------------------------------------------------------------
  ANIMATION FRAMES
----------------------------------------------------------------

  Idle Down ....... 5 frames
  Idle Up ......... 5 frames
  Walk Down ....... 4 frames
  Walk Up ......... 4 frames
  Walk Left ....... 4 frames
  Walk Right ...... 4 frames
  Hit ............. 2 frames
  Die ............. 10 frames  (+ no-shadow variant)
  Win Right ....... 10 frames  (victory pose, 64 x 64 canvas)
  Win Left ........ 10 frames  (mirrored from Win Right)
  Attack Down ..... 20 frames  (+ _stay variant)
  Attack Up ....... 20 frames  (+ _stay variant)
  Attack Left ..... 18 frames  (+ _stay variant)
  Attack Right .... 18 frames  (+ _stay variant, mirrored from Left)

  All frames: transparent background (PNG). Idle / walk / hit
  use a consistent 64 x 49 size and pivot. Die / die_no_shadow
  use 73 x 49 — the sword extends further to the side as the
  warrior falls, so those animations need a slightly wider
  canvas. Attack animations use 128 x 128 so the swing arc and
  weapon trail fit fully inside the frame. Win uses 64 x 64 —
  the raised sword needs the extra height — but the warrior
  stands on the same ground line as idle / walk / hit, so a
  bottom-aligned sprite does not jump when you swap into it.
  The pivot (feet center) is consistent across every animation,
  so swapping clips at runtime won't shift the character.

  ATTACK — two variants per direction:
    attack_<dir>/         The warrior physically steps forward
                          inside the frame as part of the swing.
                          Use this if you want the artwork to
                          carry the motion (and let your
                          character's transform stay still during
                          the swing).
    attack_<dir>_stay/    The warrior stays planted on the spot.
                          Use this if you want to drive the
                          forward step in code (root motion,
                          dash, lunge, etc.) — the sprite won't
                          fight you for screen-space ownership.
    Pick ONE variant per direction; don't blend them.

  Both individual frames (png/single/) and horizontal sprite
  sheets (png/atlas/) are included — pick whichever your engine
  prefers. Power-of-two padded copies are also provided in
  png/single_pot/ and png/atlas_pot/ for engines / GPUs that
  require POT textures (mobile, older OpenGL ES, some compressed
  formats). The original art sits at top-left (0,0) with
  transparent padding on the right/bottom, so per-frame pixel
  sizes and the pivot are identical to the non-POT versions.
  Attack art is already 128 x 128 (POT) and 2048 x 256 (POT) so
  the _pot copies are byte-identical to the non-POT ones.

  Engine-agnostic — works in Unity, Godot, GameMaker, Construct,
  RPG Maker, and more.

  GIF previews of every animation are in gif/ (1x and 5x), with
  a transparent background, for quick visual reference and for
  store/itch.io page screenshots.


----------------------------------------------------------------
  HOW TO USE
----------------------------------------------------------------

  Unity:
    1. Drop png/single/ or png/atlas/ into Assets/.
    2. Set Texture Type = Sprite (2D and UI).
    3. Set Filter Mode = Point (no filter) and Compression = None
       to keep the pixel-art look crisp.
    4a. Single frames: select all PNGs of one animation and drag
        into the scene to auto-create an Animation clip.
    4b. Atlas: set Sprite Mode = Multiple, open Sprite Editor,
        Slice -> Grid By Cell Size.
          Idle / walk / hit  ->  Pixel Size 64  x 49
          Die / die_no_shadow -> Pixel Size 73  x 49
          Attack_*           ->  Pixel Size 128 x 128
            (2048 x 256 sheet, 16 frames in row 0, remaining
             frames in row 1 — Unity will pick them up in order)
        For atlas_pot/ use the SAME pixel sizes — extra empty
        cells at the right edge are transparent and can be left
        unused or deleted in the Sprite Editor.

  Godot:
    1. Import png/single/ (or atlas/) into your project.
    2. In the Import dock set Filter = Off and Mipmaps = Off.
    3. Use AnimatedSprite2D / AnimationPlayer — single frames as
       separate textures, or atlas via AtlasTexture / Region.

  General:
    - Loop: idle, walk_*.   One-shot: hit, die, attack_*.
    - For "left", you can either use the included left frames
      or flip the right frames horizontally — both work.
    - Same for attack_left / attack_right (right is provided
      pre-flipped from left, so they're already drop-in pairs).
    - Attack timing: ~6.67 fps (matches all other animations).
      20-frame attacks run ~3.0s, 18-frame attacks run ~2.7s.


----------------------------------------------------------------
  LICENSE — SHORT VERSION  (full text in LICENSE.txt)
----------------------------------------------------------------

  [OK] Free for personal and commercial projects
  [OK] Credit appreciated but not required
  [OK] Modify and edit freely to fit your game
  [NO] Do not resell or redistribute the assets on their own,
       or as part of another asset pack
  [NO] Do not use these assets to train AI / machine-learning
       models

  In short: use it in your games as much as you like — just
  don't repackage and sell the art itself.


----------------------------------------------------------------
  COMING SOON  —  THE OTHER 7 CLASSES
----------------------------------------------------------------

  Warrior is 1 of 8 classes in "Tiny Questers — Complete Pixel
  RPG Asset Pack". The other 7 are on the way — each with the
  same complete animation set (idle, 4-way walk, hit, die,
  attacks) and the same pivot, so they drop straight into the
  same animator setup you build with this Warrior.

    Knight       — heavy-armored sword & shield, the tank
    Priestess    — holy support, healing & buffs
    Bard         — lute-armed traveler, party utility
    Dark Mage    — forbidden magic, shadow & curse attacks
    Mage         — staff-wielding spellcaster, ranged elemental
    Rogue        — hooded daggerman, fast and lethal
    Elf Archer   — bow-wielding ranger, long-range DPS

  The full bundle (+ VFX add-ons: slashes, sparks, magic bursts,
  death poofs) will launch on itch.io and on the Unity Asset
  Store. Follow Bobddadoo on itch.io to be notified the moment
  it drops — followers get launch-week discounts.


----------------------------------------------------------------
  FEEDBACK & THE FULL PACK
----------------------------------------------------------------

  Spotted a misaligned frame or want a specific animation in
  the full set? Leave a comment on the itch.io page — early
  feedback directly shapes "Tiny Questers — Complete Pixel
  RPG Asset Pack".

  The full 8-class pack (plus VFX) will launch on itch.io and
  on the Unity Asset Store. Follow Bobddadoo on itch.io to get
  launch-week discounts.

  Thanks for checking it out — happy dev!  :)


================================================================
  (c) Bobddadoo — https://bobddadoo.itch.io
================================================================
