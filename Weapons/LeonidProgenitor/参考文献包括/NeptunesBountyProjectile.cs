using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityMod.Projectiles.Melee
{
    public class NeptunesBountyProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Items/Weapons/Melee/NeptunesBounty";

        public int Time = 0;
        public int ChargeupTime = 25;
        public int Lifetime = 300;
        public int startDamage;
        public bool setDamage = false;
        public int dustType1 = 103;
        public int dustType2 = 172;
        public bool spinMode = false; // Initial spinning
        public bool spinMode2 = false; // After falling happens spin effect
        public Vector2 NPCDestination = new Vector2(0, 0);
        public float OverallProgress => 1 - Projectile.timeLeft / (float)Lifetime;
        public float ThrowProgress => 1 - Projectile.timeLeft / (float)(Lifetime);
        public float ChargeProgress => 1 - (Projectile.timeLeft - Lifetime) / (float)(ChargeupTime);

        public Player Owner => Main.player[Projectile.owner];
        public SlotId SpinSoundSlot;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 135;
            Projectile.height = 135;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime + ChargeupTime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition()
        {
            return ChargeProgress >= 1;
        }

        public override bool? CanDamage()
        {
            //We don't want the anticipation to deal damage.
            if (ChargeProgress < 1)
                return false;

            return base.CanDamage();
        }

        //Swing animation keys
        public CurveSegment pullback = new CurveSegment(EasingType.PolyOut, 0f, 0f, MathHelper.PiOver4 * -1.2f, 2);
        public CurveSegment throwout = new CurveSegment(EasingType.PolyOut, 0.7f, MathHelper.PiOver4 * -1.2f, MathHelper.PiOver4 * 1.2f + MathHelper.PiOver2, 3);
        internal float ArmAnticipationMovement() => PiecewiseAnimation(ChargeProgress, new CurveSegment[] { pullback, throwout });

        public override void AI()
        {
            if (SoundEngine.TryGetActiveSound(SpinSoundSlot, out var SpinSound) && SpinSound.IsPlaying)
                SpinSound.Position = Projectile.Center;

            float playerDist = Vector2.Distance(Owner.Center, Projectile.Center);

            Time++;
            Projectile.spriteDirection = Projectile.direction;
            Vector3 Light = spinMode2 ? new Vector3(0.200f, 0.200f, 0.255f) : new Vector3(0.070f, 0.070f, 0.250f);
            Lighting.AddLight(Projectile.Center, Light * 4);

            //Anticipation animation. Make the player look like theyre holding the depth crusher
            if (ChargeProgress < 1)
            {
                Owner.ChangeDir(MathF.Sign(Main.MouseWorld.X - Owner.Center.X));

                float armRotation = ArmAnticipationMovement() * Owner.direction;

                Owner.heldProj = Projectile.whoAmI;
                Projectile.spriteDirection = Owner.direction;
                Projectile.direction = Owner.direction;

                Projectile.Center = Owner.MountedCenter + Vector2.UnitY.RotatedBy(armRotation * Owner.gravDir) * -90f * Owner.gravDir + new Vector2(Owner.direction == 1 ? 10 : 3, 0);
                Projectile.rotation = (-MathHelper.PiOver4 * Projectile.direction + armRotation) * Owner.gravDir;

                Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, MathHelper.Pi + armRotation);

                return;
            }

            //Play the throw sound when the throw ACTUALLY BEGINS.
            //Additionally, make the projectile collide and set its speed and velocity
            if (Projectile.timeLeft == Lifetime)
            {
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
                Projectile.Center = Owner.MountedCenter + (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction) * 12;
                // 15NOV2024: Ozzatron: clamped mouse position unnecessary, only used for direction
                Projectile.velocity = (Main.MouseWorld - Owner.Center).SafeNormalize(Vector2.UnitX * Owner.direction) * 28;
                startDamage = Projectile.damage;
                Projectile.spriteDirection = Projectile.direction;
                SpinSoundSlot = SoundEngine.PlaySound(NeptunesBounty.SpinSound, Projectile.Center);
                Time = 0;
                spinMode = true;
            }

            if (Projectile.velocity.X > 0)
                Projectile.direction = 1;
            else
                Projectile.direction = -1;

            if (Time >= 75 && !spinMode2 && Projectile.velocity.Y >= 22)
            {
                Projectile.extraUpdates = 3;

                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/VividClarityBeamAppear") { Volume = 0.65f, PitchVariance = 0.3f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.ShimmerWeak1 with { Pitch = 0.15f }, Projectile.Center);

                Particle pulse = new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Aqua, new Vector2(2f, 2f), Main.rand.NextFloat(12f, 25f), 0.01f, 0.9f, 22);
                GeneralParticleHandler.SpawnParticle(pulse);
                Particle pulse2 = new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.DodgerBlue, new Vector2(2f, 2f), Main.rand.NextFloat(12f, 25f), 0.01f, 0.83f, 15);
                GeneralParticleHandler.SpawnParticle(pulse2);

                Projectile.damage = (int)(startDamage * 2); // Deals 200% damage after launching
                Time = 0;

                bool foundTarget = false;
                NPC target = Owner.ClampedMouseWorld().ClosestNPCAt(1000);
                if (target != null)
                {
                    NPCDestination = target.Center + target.velocity * 5f;
                    foundTarget = true;
                }
                else
                    foundTarget = false;

                if (!foundTarget)
                {
                    // 15NOV2024: Ozzatron: clamped mouse position unnecessary, only used for direction
                    Projectile.velocity = (Owner.Calamity().mouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * Projectile.direction) * 25;
                }
                else
                {
                    Projectile.velocity = (NPCDestination - Projectile.Center).SafeNormalize(Vector2.UnitX * Projectile.direction) * 25;
                }

                SpinSound?.Stop();

                spinMode2 = true;
                Projectile.numHits = 0;
            }

            if (spinMode)
            {
                if (!spinMode2)
                {
                    if (Projectile.velocity.Y < 22)
                        Projectile.velocity.Y += 0.42f;

                    if (Projectile.velocity.Y > 0)
                        Projectile.velocity.X *= 0.975f;

                    if (playerDist < 1400f)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 linePos = Projectile.Center + (i * MathHelper.Pi + Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 75f;
                            Particle spark = new CustomSpark(linePos, (i * MathHelper.Pi + Projectile.rotation * Math.Sign(Projectile.velocity.X)).ToRotationVector2().RotatedByRandom(0.4f) * Main.rand.NextFloat(5f, 22f), "CalamityMod/Particles/WaterFoam", false, Main.rand.Next(7, 16), Main.rand.NextFloat(0.4f, 0.7f), Color.DodgerBlue * 0.45f, new Vector2(1f, 1f), true, false, Main.rand.NextFloat(-10, 10));
                            GeneralParticleHandler.SpawnParticle(spark);
                        }
                    }

                    if (Time % 7 == 0)
                    {
                        Vector2 velDirection = new Vector2(80, 80).RotatedByRandom(100);
                        Vector2 location = Projectile.Center + velDirection;
                        Vector2 velocity;
                        bool foundTarget = false;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i].CanBeChasedBy(Projectile.GetSource_FromThis(), false))
                                NPCDestination = Main.npc[i].Center + Main.npc[i].velocity * 5f;

                            if (NPCDestination == new Vector2(0, 0))
                                foundTarget = false;
                            else
                                foundTarget = true;
                        }

                        if (!foundTarget)
                        {
                            velocity = velDirection.SafeNormalize(Vector2.UnitX * Projectile.direction) * 25;
                        }
                        else
                        {
                            velocity = (NPCDestination - location).SafeNormalize(Vector2.UnitX * Projectile.direction) * 25;
                        }

                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), location, velocity, ModContent.ProjectileType<NeptunesBountySplitProjectile>(), startDamage / 6, Projectile.knockBack / 4, Projectile.owner);
                        for (int i = 0; i < 6; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(location, Main.rand.NextBool(3) ? dustType1 : dustType2);
                            dust.noGravity = true;
                            dust.scale = Main.rand.NextFloat(1.5f, 1.8f);
                            dust.velocity = velocity.RotatedByRandom(0.7f) * Main.rand.NextFloat(0.1f, 0.5f);
                        }
                    }

                }
                else
                {
                    if (Time == 1)
                        SpinSoundSlot = SoundEngine.PlaySound(NeptunesBounty.SpinSound with { Pitch = 0.2f }, Projectile.Center);

                    if (playerDist < 1400f)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            Vector2 linePos = Projectile.Center + (i * MathHelper.Pi + Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * 75f;
                            Particle spark = new CustomSpark(linePos, (i * MathHelper.Pi + Projectile.rotation * Math.Sign(Projectile.velocity.X)).ToRotationVector2().RotatedByRandom(0.25f) * Main.rand.NextFloat(5f, 22f) - Projectile.velocity * 2f, "CalamityMod/Particles/SmallBloom", false, Main.rand.Next(10, 13), Main.rand.NextFloat(0.2f, 0.55f), Color.DodgerBlue * 0.3f, new Vector2(1f, 1f), true, false, Main.rand.NextFloat(-10, 10));
                            GeneralParticleHandler.SpawnParticle(spark);
                        }
                        if (Main.rand.NextBool(3))
                        {
                            Particle spark2 = new CustomSpark(Projectile.Center + Main.rand.NextVector2Circular(60, 60), -Projectile.velocity * Main.rand.NextFloat(0.2f, 1f), "CalamityMod/Particles/Sparkle", false, Main.rand.Next(24, 36), Main.rand.NextFloat(1.7f, 1.85f), Color.DodgerBlue, new Vector2(0.4f, 1f));
                            GeneralParticleHandler.SpawnParticle(spark2);
                        }
                    }
                }

                Projectile.spriteDirection = Projectile.direction;

                Projectile.rotation += (0.6f * (MathF.Abs(Projectile.velocity.Y) * 0.03f + 0.85f)) * Projectile.direction;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HadopelagicPressure>(), 300);

            if (spinMode2 && Projectile.numHits == 0)
            {
                Particle spark768 = new GlowSparkParticle(target.Center, -Projectile.velocity * 0.05f, false, 15, 0.13f, Color.DodgerBlue, new Vector2(1.5f, 1), true, false, 0.8f);
                GeneralParticleHandler.SpawnParticle(spark768);
                Particle spark763 = new GlowSparkParticle(target.Center, -Projectile.velocity * 0.05f, false, 15, 0.1f, Color.Aqua, new Vector2(1.5f, 1), true, false, 0.8f);
                GeneralParticleHandler.SpawnParticle(spark763);

                for (int i = 0; i <= 20; i++)
                {
                    Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(9, 9), Main.rand.NextBool() ? 307 : 180, Projectile.velocity * Main.rand.NextFloat(0.1f, 1.2f), 0, default, Main.rand.NextFloat(0.9f, 1.45f));
                    dust.noGravity = true;
                    Dust dust2 = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(9, 9), Main.rand.NextBool() ? 307 : 180, -Projectile.velocity * Main.rand.NextFloat(0.1f, 1.2f), 0, default, Main.rand.NextFloat(0.9f, 1.45f));
                    dust2.noGravity = true;
                }
                SoundStyle HitSound2 = new("CalamityMod/Sounds/Item/HellkiteSmallHit", 3) { Volume = 0.45f, Pitch = 0.2f };

                SoundEngine.PlaySound(HitSound2, Projectile.Center);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/SwiftSlice") { Volume = 0.65f, Pitch = -0.3f }, Projectile.Center);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(SpinSoundSlot, out var SpinSound))
                SpinSound?.Stop();

            for (int i = 0; i < 40; i++)
            {
                float dustMulti = Main.rand.NextFloat(0.3f, 1.5f);
                Vector2 dustPos = Projectile.Center;
                Dust dust = Dust.NewDustPerfect(dustPos, Main.rand.NextBool(3) ? dustType1 : dustType2);
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1.6f, 2.5f) - dustMulti;
                dust.velocity = new Vector2(5, 5).RotatedByRandom(100) * Main.rand.NextFloat(0.3f, 1f) * dustMulti;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (spinMode2)
            {
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Aqua with { A = 0 } * 0.6f, 1);
            }
            Asset<Texture2D> p = ModContent.Request<Texture2D>("CalamityMod/Particles/CircularSmearSmokey");
            Asset<Texture2D> p2 = ModContent.Request<Texture2D>("CalamityMod/Particles/SemiCircularSmearSwipe");
            Vector2 generalDrawPos = Projectile.Center - Main.screenPosition;
            if (spinMode)
            {
                Main.EntitySpriteDraw(p2.Value, generalDrawPos, null, (spinMode2 ? Color.Aqua : Color.DeepSkyBlue) with { A = 0 } * 0.55f, Projectile.rotation * Main.rand.NextFloat(1.6f, 1.7f), p2.Size() * 0.5f, (spinMode2 ? 1.6f : 1.4f) * Main.rand.NextFloat(0.8f, 1.15f), SpriteEffects.None);
                Main.EntitySpriteDraw(p.Value, generalDrawPos, null, (spinMode2 ? Color.Aqua : Color.DeepSkyBlue) with { A = 0 } * 0.75f, Projectile.rotation * Main.rand.NextFloat(1.2f, 1.3f), p.Size() * 0.5f, spinMode2 ? 1.4f : 1.2f, SpriteEffects.None);
            }
            return true;
        }
    }
}
