# Xyk's Blessing Visual Effects Guide

This document explains how the Xyk's Blessing vanity accessory creates its wing and dash effects in Calamity Mod. It is written as teaching material: first the data flow, then the specific implementation ideas, then a simplified pattern you can reuse.

## 1. What The Item Actually Does

`Xyk's Blessing` is a vanity accessory with two variants:

- Blue: `Items/Accessories/Vanity/XyksBlessingBlue.cs`
- Orange: `Items/Accessories/Vanity/XyksBlessingOrange.cs`

The item itself does not directly draw wings. Its job is to set flags on the player's `CalamityPlayer` instance.

In the blue version:

```cs
public override void UpdateVanity(Player player)
{
    CalamityPlayer modPlayer = player.Calamity();
    modPlayer.XykVisualsBlue = true;
}

public override void UpdateAccessory(Player player, bool hideVisual)
{
    if (!hideVisual)
    {
        CalamityPlayer modPlayer = player.Calamity();
        modPlayer.XykVisualsBlue = true;
    }
}
```

Source: `Items/Accessories/Vanity/XyksBlessingBlue.cs`, lines 38-50.

The orange version is the same idea, except it sets `XykVisualsOrange`.

Important point for students: the accessory is only a switch. The actual visual effects are implemented elsewhere and run every frame while the switch is on.

## 2. Overall Effect Pipeline

The effect is split into three layers:

1. The accessory sets a player flag.
2. `CalamityPlayerMiscEffects.cs` checks the flag every frame.
3. If conditions are met, it spawns visual projectiles or particles.

For the wing effect, the pipeline is:

```text
Xyk's Blessing equipped
    -> XykVisualsBlue / XykVisualsOrange = true
    -> CalamityPlayerMiscEffects checks player flight state
    -> spawns XykWings projectiles
    -> each XykWings projectile updates its position in AI()
    -> each XykWings projectile draws wing textures in PreDraw()
```

For the dash effect, the pipeline is:

```text
Xyk's Blessing equipped
    -> XykVisualsBlue / XykVisualsOrange = true
    -> CalamityPlayerMiscEffects detects dashStart or active dash
    -> plays sound
    -> spawns pulse particles, spark particles, and dust
```

## 3. Shared Color Logic

Both the wing and dash effects use `XykFXColor`.

In `CalamityPlayerMiscEffects.cs`, lines 248-268:

```cs
bool Orange = XykVisualsOrange;
Color effectColor = Orange ? Color.Gold : Color.DodgerBlue;

float rate = Main.GlobalTimeWrappedHourly * 12;
List<Color> eColors = new List<Color>()
{
    Orange ? new Color(248, 117, 52) : Color.DodgerBlue,
    Orange ? Color.Gold : Color.Cyan,
    Orange ? Color.Orange : Color.RoyalBlue
};

int colorIndex = (int)(rate / 2 % eColors.Count);
Color currentColor = eColors[colorIndex];
Color nextColor = eColors[(colorIndex + 1) % eColors.Count];
effectColor = Color.Lerp(currentColor, nextColor, rate % 2f > 1f ? 1f : rate % 1f);
```

This creates a smoothly changing color by cycling through a small palette and interpolating between two neighboring colors.

Then special combat states can override the normal color:

```cs
Color attemptColor = rageAndAdren
    ? new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB)
    : Player.Calamity().adrenalineModeActive ? Color.MediumSpringGreen
    : Player.Calamity().rageModeActive ? Color.Crimson
    : effectColor;

XykFXColor = Color.Lerp(XykFXColor, attemptColor, rageOrAdren ? 0.05f : 0.25f);
```

Teaching point: `Color.Lerp(old, target, amount)` avoids sudden color jumps. A smaller amount changes more slowly.

## 4. How The Wing Effect Is Spawned

The wing pieces are spawned in `CalPlayer/CalamityPlayerMiscEffects.cs`, lines 271-290.

Key code:

```cs
int maxWingPieces = 7;
int numOfActiveWings = 0;

foreach (Projectile p in Main.ActiveProjectiles)
    if (p.type == ModContent.ProjectileType<XykWings>() && p.owner == Player.whoAmI && p.ai[1] == 0)
        numOfActiveWings++;

bool spawnWings =
    numOfActiveWings < maxWingPieces &&
    !Player.dead &&
    Player.wingsLogic > 0 &&
    ((!(Player.wingTime == Player.wingTimeMax && Player.velocity.Y == 0) && Player.wingTime > 0) || XykWingTimer >= 3);
```

This means:

- At most 7 wing projectiles can exist.
- The player must be alive.
- The player must have actual wings equipped: `Player.wingsLogic > 0`.
- The player must be flying or recently in a wing-active state.

When the timer reaches 3 frames, a new wing projectile is spawned:

```cs
Projectile wings = Projectile.NewProjectileDirect(
    Player.GetSource_FromThis(),
    Player.Center,
    Vector2.Zero,
    ModContent.ProjectileType<XykWings>(),
    0,
    0f,
    Player.whoAmI,
    wingCount
);
```

The important parameter is the final `wingCount`. It becomes `Projectile.ai[0]` in `XykWings.cs`, exposed as:

```cs
public ref float wingNum => ref Projectile.ai[0];
```

Source: `Projectiles/Typeless/XykWings.cs`, line 40.

Teaching point: each wing piece is the same projectile class, but `wingNum` gives each one a different index. That index changes spacing, animation phase, and texture choice.

## 5. XykWings Is An Invisible Projectile

`XykWings` uses an invisible projectile texture:

```cs
public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
```

Source: `Projectiles/Typeless/XykWings.cs`, line 14.

That means the projectile entity exists for logic, ownership, timing, and drawing hooks, but its default sprite is not used. The actual visuals are drawn manually in `PreDraw`.

The projectile cannot damage enemies:

```cs
public override bool? CanDamage() => false;
```

Source: `Projectiles/Typeless/XykWings.cs`, line 328.

Teaching point: this is a common modding pattern. Use a projectile as a small visual controller, not as a weapon.

## 6. Wing Lifetime And Fade-In

At the start of `AI()`, each wing stores initial positions and fades in:

```cs
float intendedScale = 0.85f;
float spawnAnimTime = 12 + wingNum;

if (time == 0)
{
    expectedWingPosition1 = Owner.Center;
    expectedWingPosition2 = Owner.Center;
    playerCenterPoint = Owner.MountedCenter;
}

if (time <= spawnAnimTime)
{
    spawnFade = Utils.GetLerpValue(0, spawnAnimTime, time, true);
    Projectile.scale = intendedScale * Math.Min(spawnFade + 0.5f, 1);
}
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 54-72.

`spawnAnimTime = 12 + wingNum` makes later wing pieces appear slightly later than earlier ones. This creates a chained unfolding effect.

The projectile keeps itself alive while the player still has the Xyk visual flag:

```cs
if (moddedOwner.XykVisualsBlue || moddedOwner.XykVisualsOrange)
    Projectile.timeLeft++;
else
    Projectile.Kill();
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 74-77.

Teaching point: increasing `timeLeft` every frame prevents the projectile from expiring naturally while the effect is active.

## 7. Wing Motion: Two Wing Points Per Projectile

Each `XykWings` projectile draws two wing parts:

- `expectedWingPosition1`
- `expectedWingPosition2`

They behave like front and back wing layers.

During normal flight, the code first reads player input:

```cs
bool isFlying = Owner.controlJump;
bool isFalling = Owner.controlDown && !isFlying && Owner.velocity.Y > 0;
bool isUpBoosting = Owner.wingTime > 0 && Owner.controlJump && Owner.controlUp;
bool isHovering = Owner.wingTime > 0 && Owner.controlDown && Owner.controlJump && !isUpBoosting;
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 170-173.

Then it builds a flap value using a sine wave:

```cs
float sine = (float)Math.Sin(
    (time * 0.35f * speedMultiplier + wingNum * phaseOffset) / MathHelper.Pi
);
```

The real code in line 181 combines several conditions into that expression. The idea is simple:

- `time` makes the wave move.
- `wingNum` offsets each wing piece so they do not flap exactly together.
- flying, hovering, and falling change the speed.

Then the code smooths the flap:

```cs
float flapSpeed = MathHelper.Lerp(slowSpeed, fastSpeed, flapStrength);
wingFlapHeight = MathHelper.Lerp(wingFlapHeight, sine, flapSpeed) * spawnFadePow;
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 203-205.

Teaching point: raw sine motion can look mechanical. Interpolating toward the sine result makes the motion feel softer.

## 8. Wing Destination And Follow Smoothing

The core positioning happens in the loop at `XykWings.cs`, lines 207-220.

The loop runs twice:

```cs
for (int i = -1; i <= 1; i += 2)
```

That gives two directions: `-1` and `1`, used for the two wing sides.

The code computes a `destination` around the player's mounted center:

```cs
Vector2 destination = playerCenterPoint + calculatedOffset + flightMovement;
```

The real expression is more complex because it rotates offsets based on falling, player direction, wing index, and flap height.

After calculating the destination, it does not teleport the wing there. It moves the wing position gradually:

```cs
expectedWingPosition1 += (target - expectedWingPosition1) / lerpSpeed;
expectedWingPosition2 += (target - expectedWingPosition2) / lerpSpeed;
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 214-219.

Teaching point: this is follow smoothing. Instead of setting position directly, the wing moves partway toward the target each frame. This makes it trail behind the player naturally.

## 9. Wing Rotation

After positions are updated, the wing's rotation is based on the direction from the player to the wing point:

```cs
Projectile.rotation = expectedWingPosition1.DirectionFrom(playerCenterPoint).ToRotation() - MathHelper.PiOver2;
backWingRot = expectedWingPosition2.DirectionFrom(playerCenterPoint).ToRotation() - MathHelper.PiOver2;
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 223-224.

Teaching point: if you know where the wing point is relative to the player, you can rotate the sprite so it points outward from the body.

## 10. Wing Boost And Dash Glow

Inside `XykWings.AI()`, `dashfx` is a visual intensity value:

```cs
if (Owner.dashDelay == -1 || isUpBoosting || isHovering || Owner.Calamity().adrenalineModeActive || Owner.Calamity().rageModeActive || isFalling && Owner.velocity.Y > 16)
{
    dashfx = MathHelper.Lerp(dashfx, 1, 0.25f);
}
else
{
    dashfx = MathHelper.Lerp(dashfx, 0, 0.03f);
}
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 183-200.

When `dashfx` is high enough, the wing spawns extra dust near the wing tips:

```cs
if (dashfx > 0.3f && (wingNum == checkActiveWings() - 1 || wingNum == 0))
{
    // Spawn dust near wing tips.
}
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 229-260.

Only the first and last active wing pieces create these dust bursts. That keeps the effect readable and avoids spawning too many particles.

## 11. How The Wings Are Drawn

Drawing happens in `PreDraw`, not through the default projectile sprite.

First the code picks a texture based on the wing index:

```cs
if (wingNum == 0)
    tex = orange ? XykWingOrange2 : XykWingBlue2;
else if (wingNum == topWingNum || iAmTopWing)
    tex = orange ? XykWingOrange1 : XykWingBlue1;
else
    tex = orange ? XykWingOrange3 : XykWingBlue3;
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 277-289.

This creates visual variety:

- first piece uses texture type 2
- top/final piece uses texture type 1
- middle pieces use texture type 3

Then the draw scale is calculated:

```cs
Vector2 scale = new Vector2(width, height * spawnFadePow) * 0.12f * Projectile.scale;
Vector2 scaleBack = new Vector2(width * 1.5f, height * 0.7f * spawnFadePow) * 0.07f * Projectile.scale;
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 293-294.

When `dashfx > 0`, it draws glow circles and afterimage copies:

```cs
Main.EntitySpriteDraw(glow, ...);
Main.EntitySpriteDraw(tex, ... drawColor * 0.3f * dashfx ...);
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 296-310.

Finally it draws the actual front and back wing parts:

```cs
Main.EntitySpriteDraw(
    tex,
    expectedWingPosition1 - Main.screenPosition,
    null,
    Color.Lerp(drawColor, Color.White, dashfx) with { A = 0 } * spawnFadePow,
    Projectile.rotation,
    new Vector2(tex.Width * 0.5f, 0),
    scale,
    lastDir == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
    0
);
```

Source: `Projectiles/Typeless/XykWings.cs`, lines 313-315.

Teaching point: `Main.screenPosition` converts world coordinates into screen coordinates. Most Terraria drawing code subtracts it before drawing.

## 12. How The Dash Effect Is Made

The dash effect is in `CalPlayer/CalamityPlayerMiscEffects.cs`, lines 561-625.

It has two phases:

1. On the first dash frame, play sound and spawn a large pulse.
2. While dash is active, continuously spawn sparks and dust trails.

The first-frame check:

```cs
if (dashStart)
{
    SoundStyle dash = new("CalamityMod/Sounds/Item/DashSound");
    SoundEngine.PlaySound(dash with { Volume = 0.7f, Pitch = ... }, Player.Center);

    // Spawn large pulse particles.
}
```

Source: `CalPlayer/CalamityPlayerMiscEffects.cs`, lines 565-585.

Blue and orange use different particle shapes:

- Orange uses `GlowSquareParticleBig`.
- Blue uses `BloomRing`.

During the dash:

```cs
if (Player.dashDelay == -1)
{
    float sparkscale1 = MathF.Min(Player.velocity.X * dir * 0.08f, 1.2f);
    Vector2 SparkVelocity1 = -Player.velocity.SafeNormalize(Vector2.UnitX) * 5;

    // Spawn CustomSpark particles.
    // Spawn Dust particles.
}
```

Source: `CalPlayer/CalamityPlayerMiscEffects.cs`, lines 586-625.

Important ideas:

- `Player.dashDelay == -1` means the player is currently dashing.
- Spark velocity points opposite the player's movement, so the trail appears behind the player.
- The effect scales with player speed using `sparkscale1`.

## 13. Why This Design Works

The implementation is effective because it separates responsibilities:

- The accessory only sets flags.
- The player effect file decides when visuals should exist.
- The wing projectile controls motion and drawing.
- Particles and dust are used for short-lived bursts.

This makes the system easier to maintain. You can change the item without rewriting wing physics, or change the wing rendering without touching the dash effect.

## 14. Simplified Version For Your Own Mod

Here is a simplified structure students can copy conceptually.

Accessory:

```cs
public override void UpdateVanity(Player player)
{
    player.GetModPlayer<MyPlayer>().myWingVisual = true;
}
```

Player effect:

```cs
if (myWingVisual && Player.wingsLogic > 0 && Player.wingTime > 0)
{
    if (countMyWingProjectiles() < 5)
    {
        Projectile.NewProjectile(
            Player.GetSource_FromThis(),
            Player.Center,
            Vector2.Zero,
            ModContent.ProjectileType<MyWingVisualProjectile>(),
            0,
            0,
            Player.whoAmI,
            currentWingIndex
        );
    }
}
```

Visual projectile:

```cs
public override void AI()
{
    Player owner = Main.player[Projectile.owner];

    float wave = MathF.Sin(Main.GameUpdateCount * 0.1f + Projectile.ai[0]);
    Vector2 target = owner.MountedCenter + new Vector2(owner.direction * -40, -20 + wave * 10);

    Projectile.Center += (target - Projectile.Center) * 0.15f;
    Projectile.rotation = Projectile.Center.DirectionFrom(owner.MountedCenter).ToRotation();
}
```

Manual drawing:

```cs
public override bool PreDraw(ref Color lightColor)
{
    Texture2D tex = ModContent.Request<Texture2D>("MyMod/Assets/MyWing").Value;

    Main.EntitySpriteDraw(
        tex,
        Projectile.Center - Main.screenPosition,
        null,
        Color.Cyan,
        Projectile.rotation,
        tex.Size() * 0.5f,
        Projectile.scale,
        SpriteEffects.None,
        0
    );

    return false;
}
```

## 15. Presentation Summary

If you need to explain this in class, use this short version:

Xyk's Blessing does not give the player a real wing item. It turns on visual flags on the custom player object. Every frame, Calamity checks those flags. If the player has wings and is flying, the mod spawns up to seven invisible visual projectiles. Each projectile stores a wing index, calculates two smoothed wing positions around the player, rotates the sprites outward, and manually draws glowing wing textures in `PreDraw`. The dash effect is separate: when the dash starts it plays a sound and spawns pulse particles; while the dash continues it spawns spark and dust trails behind the player. The whole effect is built from flags, timers, sine-wave motion, interpolation, manual drawing, and particles.
