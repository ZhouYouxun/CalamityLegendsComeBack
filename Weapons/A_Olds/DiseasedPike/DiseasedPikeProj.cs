using System;
using CalamityMod;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.DiseasedPike
{
    public class DiseasedPikeProj : BaseSwordHoldoutProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override bool useAttackSpeed => true;
        public override bool useMeleeSize => true;
        public override int swingWidth => 240;
        public override Item BaseItem => ModContent.GetModItem(ModContent.ItemType<DiseasedPike>()).Item;
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Olds/DiseasedPike/瘟疫长枪（弹幕）";

        public override int StartupTime { get; set; }
        public override int CooldownTime { get; set; }
        public override int swingTime { get; set; }

        public override float lineCollisionLength => 160f;

        private int combo => (int)Projectile.ai[0];
        private float spinRotation = 0f;

        public override void Defaults()
        {
            Projectile.width = Projectile.height = 90;
            Projectile.extraUpdates = 3;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void Spawn()
        {
            Player player = Main.player[Projectile.owner];
            AlternateSwings = false;

            OffsetDistance = 60;
            RotateInStartup = 0f;
            RotateInCooldown = 0f;

            if (combo == 0 || combo == 1) // Slashes
            {
                StartupTime = 6;
                CooldownTime = 8;
                swingTime = player.HeldItem.useTime - StartupTime - CooldownTime;
                OffsetDistance = 60;
                UseSound = SoundID.Item71;
                Projectile.localNPCHitCooldown = -1;
            }
            else if (combo == 2) // Thrust
            {
                StartupTime = 8;
                CooldownTime = 10;
                swingTime = player.HeldItem.useTime - StartupTime - CooldownTime;
                OffsetDistance = 30;
                UseSound = SoundID.Item73;
                Projectile.localNPCHitCooldown = -1;
            }
            else if (combo == 3) // Spin
            {
                StartupTime = 4;
                CooldownTime = 4;
                swingTime = 20;
                OffsetDistance = 60;
                UseSound = SoundID.Item71 with { Volume = 0.5f, Pitch = 0.2f };
                Projectile.localNPCHitCooldown = 12; // Hit repeatedly
            }
        }

        public override float SwingFunction()
        {
            if (combo == 0) // Upward slash
            {
                if (inStartup)
                    return MathHelper.ToRadians(MathHelper.Lerp(0f, 60f, StartupCompletion));
                if (inCooldown)
                    return MathHelper.ToRadians(MathHelper.Lerp(-60f, -80f, CooldownCompletion));
                return MathHelper.ToRadians(MathHelper.SmoothStep(60f, -60f, SwingCompletion));
            }
            else if (combo == 1) // Downward slash
            {
                if (inStartup)
                    return MathHelper.ToRadians(MathHelper.Lerp(0f, -60f, StartupCompletion));
                if (inCooldown)
                    return MathHelper.ToRadians(MathHelper.Lerp(70f, 90f, CooldownCompletion));
                return MathHelper.ToRadians(MathHelper.SmoothStep(-60f, 70f, SwingCompletion));
            }
            return 0f;
        }

        public override void AdditionalAI()
        {
            Player player = Main.player[Projectile.owner];
            var armCenter = player.MountedCenter - new Vector2(5 * player.direction, 2);

            if (player.itemAnimation > 0)
            {
                player.direction = (player.Center.X - Main.MouseWorld.X < 0) ? 1 : -1;
            }

            if (combo == 2)
            {
                if (inStartup)
                    OffsetDistance = (int)MathHelper.Lerp(30, 10, StartupCompletion);
                else if (inCooldown)
                    OffsetDistance = (int)MathHelper.Lerp(130, 70, CooldownCompletion);
                else
                    OffsetDistance = (int)MathHelper.Lerp(10, 130, MathF.Pow(SwingCompletion, 1.5f));

                if (timer == StartupTime && Projectile.owner == Main.myPlayer)
                {
                    Vector2 shootVel = -angle * 12f;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        shootVel,
                        ModContent.ProjectileType<CalamityMod.Projectiles.Melee.VirulentWave>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner
                    );
                }
            }
            else if (combo == 3)
            {
                if (player.channel && player.altFunctionUse == 2 && !player.noItems && !player.CCed)
                {
                    Projectile.timeLeft = 2;
                    player.itemTime = 2;
                    player.itemAnimation = 2;
                    if (timer >= StartupTime + swingTime - 1)
                    {
                        timer = StartupTime + swingTime - 2;
                    }
                }

                spinRotation += 0.22f;
                float currentAngle = spinRotation * player.direction;
                
                Projectile.Center = armCenter + currentAngle.ToRotationVector2() * OffsetDistance * Projectile.scale;
                Projectile.rotation = currentAngle + (player.direction == 1 ? MathHelper.ToRadians(225) : MathHelper.ToRadians(-45));

                if (timer % 30 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.4f, Pitch = 0.3f }, player.Center);
                }

                Vector2 tipPos = armCenter + currentAngle.ToRotationVector2() * (OffsetDistance + 80f) * Projectile.scale;
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(tipPos, DustID.GemEmerald, currentAngle.ToRotationVector2().RotatedBy(MathHelper.PiOver2 * player.direction) * Main.rand.NextFloat(1f, 3f), 100, default, 1f);
                    dust.noGravity = true;
                }
            }

            if (inSwing && combo != 3)
            {
                Vector2 dir = player.MountedCenter.DirectionTo(Projectile.Center);
                Vector2 tipPos = player.MountedCenter + dir * (OffsetDistance + 80f) * Projectile.scale;
                
                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(tipPos, DustID.GemEmerald, dir.RotatedByRandom(0.2f) * Main.rand.NextFloat(1f, 3f), 100, default, Main.rand.NextFloat(0.8f, 1.2f));
                    dust.noGravity = true;
                }
                if (Main.rand.NextBool(3))
                {
                    Dust dust = Dust.NewDustPerfect(tipPos, DustID.GreenTorch, dir.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 5f), 100, default, Main.rand.NextFloat(1f, 1.5f));
                    dust.noGravity = true;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(target, ref modifiers);
            
            if (combo == 0 || combo == 1)
            {
                modifiers.SourceDamage *= 3.0f;
            }
            else if (combo == 3)
            {
                modifiers.SourceDamage *= 0.7f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.DamageOverTime.Plague>(), 300);

            if ((combo == 0 || combo == 1) && Projectile.owner == Main.myPlayer)
            {
                SpawnBees(target, hit.Damage);
            }
        }

        private void SpawnBees(NPC target, int strikeDamage)
        {
            Player player = Main.player[Projectile.owner];
            int beeCount = (player.strongBees ? 9 : 7) + 3;
            
            for (int i = 0; i < beeCount; i++)
            {
                float delayFactor = Main.rand.NextFloat(0.7f, 1.4f);
                float initialHomingCounter = 30f - 30f * delayFactor;
                Vector2 velocity = (MathHelper.TwoPi * i / beeCount + Main.rand.NextFloat(-0.14f, 0.14f)).ToRotationVector2() * Main.rand.NextFloat(3.5f, 8f);
                int beeDamage = Math.Max(1, (int)(strikeDamage * 0.08f));
                
                int bee = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    velocity,
                    ModContent.ProjectileType<CalamityMod.Projectiles.Typeless.BasicPlagueBee>(),
                    beeDamage,
                    0f,
                    Projectile.owner,
                    initialHomingCounter,
                    120f,
                    1.5f
                );

                if (Main.projectile.IndexInRange(bee))
                {
                    Projectile plagueBee = Main.projectile[bee];
                    plagueBee.DamageType = DamageClass.Melee;
                    plagueBee.penetrate = 1;
                    plagueBee.scale *= 1.35f;
                    plagueBee.light = MathHelper.Max(plagueBee.light, 0.35f);
                }
            }
        }
    }
}
