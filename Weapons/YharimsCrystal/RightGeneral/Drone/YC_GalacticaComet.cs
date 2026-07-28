using System;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral.Drone
{
    public class YC_GalacticaComet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Melee/GalacticaComet";

        public int time = 0;
        public int cometType = 0;
        public Color useColor = Color.White;
        public float baseSpeed = 14f;
        private bool Focused => Projectile.ai[0] >= 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 15;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 102;
            Projectile.height = 102;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 270;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (time == 0)
            {
                YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
                Projectile.scale = Main.rand.NextFloat(0.6f, 0.9f);
                cometType = Main.rand.Next(1, 4);
                useColor = cometType switch
                {
                    1 => Color.Cyan,
                    2 => Color.Gold,
                    _ => Color.HotPink,
                };
                baseSpeed = Projectile.velocity.Length();
                if (baseSpeed < 8f)
                    baseSpeed = 14f;
            }

            // 惰性弧形追踪 (Inertial smooth homing towards mouse)
            if (owner.active && !owner.dead)
            {
                NPC target = Focused ? null : Projectile.Center.ClosestNPCAt(2400f);
                Vector2 targetPos = Focused
                    ? NewLegendYharimsCrystal.GetMouseWorld(owner)
                    : target?.Center ?? NewLegendYharimsCrystal.GetMouseWorld(owner);
                Vector2 toTarget = targetPos - Projectile.Center;
                float dist = toTarget.Length();

                if (dist > 30f)
                {
                    float leadFrames = MathHelper.Clamp(dist / MathHelper.Max(baseSpeed, 1f), 5f, 34f);
                    if (target is not null)
                        targetPos += target.velocity * leadFrames;

                    float targetAngle = (targetPos - Projectile.Center).ToRotation();
                    float currentAngle = Projectile.velocity.ToRotation();

                    // 动态调整转向速率：在前几帧较软，随后具有平滑惯性，距离近时增强以离心弧线划过
                    float growth = Utils.GetLerpValue(0f, 150f, time, true);
                    float maxTurn = MathHelper.ToRadians(Focused ? 52f : MathHelper.Lerp(1.6f, 16f, growth));
                    float newAngle = currentAngle.AngleTowards(targetAngle, maxTurn);

                    // 加速与惯性保持
                    float targetSpeed = Focused ? 30f : MathHelper.Lerp(14f, 34f, growth);
                    baseSpeed = MathHelper.Lerp(baseSpeed, targetSpeed, Focused ? 0.22f : 0.045f);
                    Projectile.velocity = newAngle.ToRotationVector2() * baseSpeed;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            // 飞行流光与粒子
            if (Main.netMode != NetmodeID.Server && Vector2.Distance(owner.Center, Projectile.Center) < 1600f)
            {
                Vector2 tailPos = Projectile.Center - Projectile.velocity * 0.25f;
                Particle spark = new GlowSparkParticle(
                    tailPos + Main.rand.NextVector2Circular(8f, 8f),
                    -Projectile.velocity * Main.rand.NextFloat(0.15f, 0.35f),
                    false,
                    7,
                    Main.rand.NextFloat(0.08f, 0.14f) * Projectile.scale,
                    useColor * 0.55f,
                    new Vector2(1.2f, 0.35f),
                    true,
                    false,
                    1f);
                GeneralParticleHandler.SpawnParticle(spark);

                if (time % 2 == 0)
                {
                    Vector2 sparkVel = -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.1f, 0.4f);
                    Particle bloomSpark = new CustomSpark(
                        tailPos,
                        sparkVel,
                        "CalamityMod/Particles/SmallBloom",
                        false,
                        10,
                        0.35f * Projectile.scale,
                        Color.Lerp(useColor, Color.White, 0.3f) * 0.6f,
                        Vector2.One,
                        true,
                        false,
                        3,
                        false,
                        false);
                    GeneralParticleHandler.SpawnParticle(bloomSpark);
                }
            }

            time++;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = (int)(Projectile.damage * 0.9f);
            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.numHits == 0)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i <= 14; i++)
                    {
                        if (i < 9)
                        {
                            Dust dust = Dust.NewDustPerfect(
                                Projectile.Center,
                                ModContent.DustType<LightDust>(),
                                (Vector2.One * 9).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.8f),
                                0,
                                default,
                                Main.rand.NextFloat(1.3f, 1.8f));
                            dust.noGravity = true;
                            dust.color = useColor;
                        }
                        else
                        {
                            Dust dust = Dust.NewDustPerfect(
                                Projectile.Center,
                                DustID.FireworksRGB,
                                (Vector2.One * 9).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1.8f),
                                0,
                                default,
                                Main.rand.NextFloat(0.8f, 1.3f));
                            dust.noGravity = false;
                            dust.color = Color.Lerp(useColor, Color.White, 0.5f);
                        }
                    }
                }
                SoundEngine.PlaySound(SoundID.DD2_CrystalCartImpact with { Volume = 0.7f, PitchVariance = 0.3f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Volume = 0.9f, PitchVariance = 0.3f }, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            if (cometType == 1)
                tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/GalacticaComet").Value;
            else if (cometType == 2)
                tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/GalacticaComet2").Value;
            else if (cometType == 3)
                tex = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Melee/GalacticaComet3").Value;

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], Color.White, 2, tex);
            return false;
        }
    }
}
