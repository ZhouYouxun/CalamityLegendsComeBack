using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Sounds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.Lazhar
{
    /// <summary>
    /// Lazhar Laser Projectile
    /// Features:
    /// - High velocity magic laser ray.
    /// - Smart homing: prioritizes enemies with the LazharTargetDebuff lock-on marker, offering 100% sharp tracking.
    /// - If no target is locked, tracks the nearest enemy in a 120-degree cone ahead.
    /// - Draws dual-layered gold and white primitive trails in IPixelatedPrimitiveRenderer.
    /// - Deals 50% extra damage to locked targets and spawns vertical satellite orbital strikes on hit.
    /// - Amplified size, damage, and trail width when spawned under Energy Overload.
    /// </summary>
    public class LazharLaser : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool hasSpawnedSound;
        private int trackingTimer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 220;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            // Expand size and hitbox if overloaded
            if (Projectile.ai[1] == 1f && Projectile.scale == 1f)
            {
                Projectile.scale = 1.6f;
                Projectile.width = 16;
                Projectile.height = 16;
            }

            // Play firing sound on spawn
            if (!hasSpawnedSound)
            {
                float pitch = Projectile.ai[1] == 1f ? -0.15f : 0.45f;
                float vol = Projectile.ai[1] == 1f ? 0.55f : 0.32f;
                SoundEngine.PlaySound(CommonCalamitySounds.LaserCannonSound with { Volume = vol, Pitch = pitch, PitchVariance = 0.1f }, Projectile.Center);
                hasSpawnedSound = true;
            }

            // Fade in opacity
            if (Projectile.alpha > 0)
                Projectile.alpha = Math.Max(0, Projectile.alpha - 35);

            float currentSpeed = Projectile.velocity.Length();
            if (currentSpeed < 26f) currentSpeed = 26f;

            // Homing steering logic
            NPC lockedTarget = ScanForLockedTarget();
            if (lockedTarget != null)
            {
                // Aggressive homing for locked targets
                Vector2 targetDir = (lockedTarget.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetDir * currentSpeed, 0.35f);
                trackingTimer++;
            }
            else
            {
                // Normal homing within front cone
                NPC normalTarget = ScanForNormalTarget(1000f, MathHelper.ToRadians(60f));
                if (normalTarget != null)
                {
                    Vector2 targetDir = (normalTarget.Center - Projectile.Center).SafeNormalize(Projectile.velocity);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, targetDir * currentSpeed, 0.08f);
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.65f, 0.52f, 0.15f);
        }

        private NPC ScanForLockedTarget()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.CanBeChasedBy(Projectile) && npc.HasBuff<LazharTargetDebuff>())
                {
                    return npc;
                }
            }
            return null;
        }

        private NPC ScanForNormalTarget(float maxRadius, float coneHalfAngle)
        {
            NPC closestNPC = null;
            float minDistance = maxRadius;
            Vector2 currentHeading = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.CanBeChasedBy(Projectile))
                {
                    float dist = Vector2.Distance(Projectile.Center, npc.Center);
                    if (dist < minDistance)
                    {
                        Vector2 toNPC = (npc.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                        float angleDifference = MathF.Abs(MathHelper.WrapAngle(toNPC.ToRotation() - currentHeading.ToRotation()));
                        
                        if (angleDifference <= coneHalfAngle)
                        {
                            minDistance = dist;
                            closestNPC = npc;
                        }
                    }
                }
            }
            return closestNPC;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Bonus damage on locked target
            if (target.HasBuff<LazharTargetDebuff>())
            {
                modifiers.SourceDamage *= 1.50f;
            }

            // Double damage on overload
            if (Projectile.ai[1] == 1f)
            {
                modifiers.SourceDamage *= 2.0f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Spawn orbital satellite strikes on locked targets
            if (target.HasBuff<LazharTargetDebuff>())
            {
                SoundEngine.PlaySound(CommonCalamitySounds.ExoPlasmaExplosionSound with { Volume = 0.55f, Pitch = -0.1f }, target.Center);

                if (Projectile.owner == Main.myPlayer)
                {
                    Vector2 strikeSpawnPos = new Vector2(target.Center.X + Main.rand.NextFloat(-15f, 15f), target.Center.Y - 1000f);
                    Vector2 strikeVel = new Vector2(0f, 32f);

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        strikeSpawnPos,
                        strikeVel,
                        ModContent.ProjectileType<LazharOrbitalStrike>(),
                        (int)(Projectile.damage * 0.95f),
                        Projectile.knockBack * 1.5f,
                        Projectile.owner,
                        target.whoAmI
                    );
                }
            }

            if (!Main.dedServ)
            {
                SpawnHitVisuals(target);
            }
        }

        private void SpawnHitVisuals(NPC target)
        {
            Vector2 contactPoint = Projectile.Center;
            Vector2 reflectDir = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            bool overloaded = Projectile.ai[1] == 1f;
            int sparkCount = overloaded ? 10 : 5;

            // Hitting sparks
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 sparkVel = reflectDir.RotatedByRandom(0.45f) * Main.rand.NextFloat(3f, 10f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    contactPoint,
                    sparkVel,
                    false,
                    overloaded ? 18 : 12,
                    Main.rand.NextFloat(0.3f, 0.65f),
                    Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.8f)),
                    true,
                    true
                ));
            }

            // Radial flash expansion
            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                contactPoint,
                Vector2.Zero,
                overloaded ? 1.0f : 0.6f,
                Color.Gold,
                overloaded ? 22 : 15
            ));
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 2; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center,
                        Main.rand.NextVector2Circular(2f, 2f),
                        false,
                        6,
                        0.25f,
                        Color.Gold,
                        true,
                        true
                    ));
                }
            }
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] points = BuildTrailPoints();
            if (points.Length < 2)
                return;

            // Outer golden glow trail
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak")
            );

            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(
                    WidthFunction,
                    ColorFunction,
                    OffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                points.Length * 2
            );

            // Inner white core trail
            Vector2[] corePoints = points.Take(Math.Min(12, points.Length)).ToArray();
            if (corePoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak")
            );

            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    CoreWidthFunction,
                    CoreColorFunction,
                    OffsetFunction,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]
                ),
                corePoints.Length * 2
            );
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] points = Projectile.oldPos
                .Where(pos => pos != Vector2.Zero)
                .Select(pos => pos + Projectile.Size * 0.5f)
                .ToArray();

            if (points.Length == 0)
                return new Vector2[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (points[0] != Projectile.Center)
                points = new[] { Projectile.Center }.Concat(points).ToArray();

            return points;
        }

        private Vector2 OffsetFunction(float completion, Vector2 _)
        {
            float waviness = (float)Math.Sin(completion * MathHelper.Pi * 1.5f + Main.GlobalTimeWrappedHourly * 16f) * 0.8f;
            return Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * waviness;
        }

        private float WidthFunction(float completion, Vector2 _)
        {
            float baseWidth = Projectile.scale * (Projectile.ai[1] == 1f ? 28f : 14f);
            float ratio = 0.15f;

            if (completion < ratio)
                return MathF.Sin(completion / ratio * MathHelper.PiOver2) * baseWidth + 0.1f;

            return Utils.Remap(completion, ratio, 1f, baseWidth, 0f);
        }

        private Color ColorFunction(float completion, Vector2 _)
        {
            Color startColor = Color.Lerp(Color.White, Color.Gold, 0.3f);
            Color endColor = Color.OrangeRed;
            float opacity = Projectile.Opacity;
            
            Color body = Color.Lerp(startColor, endColor, completion * 0.7f) * opacity;
            Color fade = Color.Lerp(body, Color.Transparent, Utils.GetLerpValue(0.7f, 1f, completion, true));
            fade.A = 0;
            return Color.Lerp(body, fade, completion);
        }

        private float CoreWidthFunction(float completion, Vector2 _)
        {
            float baseWidth = Projectile.scale * (Projectile.ai[1] == 1f ? 13f : 6.5f);
            float ratio = 0.15f;

            if (completion < ratio)
                return MathF.Sin(completion / ratio * MathHelper.PiOver2) * baseWidth + 0.1f;

            return Utils.Remap(completion, ratio, 1f, baseWidth, 0f);
        }

        private Color CoreColorFunction(float completion, Vector2 _)
        {
            float opacity = Projectile.Opacity;
            Color body = Color.Lerp(Color.White, Color.Gold, completion * 0.4f) * opacity;
            Color fade = Color.Lerp(body, Color.Transparent, Utils.GetLerpValue(0.75f, 1f, completion, true));
            fade.A = 0;
            return Color.Lerp(body, fade, completion);
        }
    }
}
