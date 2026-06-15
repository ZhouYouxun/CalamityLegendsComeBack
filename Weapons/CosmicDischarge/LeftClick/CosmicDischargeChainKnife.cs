using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeChainKnife : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        private Player Player => Main.player[Projectile.owner];

        private int State
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        private int TargetNPCIndex
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        private int latchTimer;
        private int hitTicker;
        private const int MaxLatchTime = 36;
        private const float MaxRange = 520f;

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            if (!Player.active || Player.dead)
            {
                Projectile.Kill();
                return;
            }

            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.FrostGlowColor.ToVector3() * 0.35f);

            // State 0: Fly Out
            if (State == 0)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
                float dist = Vector2.Distance(Player.MountedCenter, Projectile.Center);
                if (dist > MaxRange)
                {
                    State = 2; // Transition to retracting
                }
            }
            // State 1: Latch / Bite
            else if (State == 1)
            {
                NPC target = Main.npc[TargetNPCIndex];
                if (!target.active || target.friendly || target.dontTakeDamage)
                {
                    State = 2; // Return if target dies or becomes invalid
                    return;
                }

                // Snap to target center
                Projectile.Center = target.Center;
                Projectile.rotation += 0.22f; // Spin rapidly while biting

                latchTimer++;
                hitTicker++;

                // Deal biting tick damage every 6 frames
                if (hitTicker >= 6)
                {
                    hitTicker = 0;
                    // Trigger damage on the target
                    Projectile.localNPCImmunity[target.whoAmI] = 0;
                    Player.Calamity().GeneralScreenShakePower = Math.Max(Player.Calamity().GeneralScreenShakePower, 1.8f);
                    
                    if (Main.myPlayer == Projectile.owner)
                    {
                        var modPlayer = Player.GetModPlayer<CosmicDischargePlayer>();
                        bool ultActive = modPlayer.UltimateFieldActive;
                        bool empActive = modPlayer.FrozenEmperorActive;
                        float damageMult = 1.0f;
                        if (ultActive) damageMult += 0.5f;
                        if (empActive) damageMult += 0.4f;

                        int biteDamage = (int)(Projectile.damage * damageMult * 0.48f);
                        target.StrikeNPC(target.CalculateHitInfo(biteDamage, Math.Sign(target.Center.X - Player.Center.X), false, 0f));

                        // Frost Mark detonation
                        int markType = ModContent.BuffType<CosmicDischargeFrostMarkDebuff>();
                        if (target.HasBuff(markType))
                        {
                            target.DelBuff(target.FindBuffIndex(markType)); // Consume mark
                            for (int i = 0; i < 3; i++)
                            {
                                Vector2 speed = Main.rand.NextVector2Circular(4f, 4f) + new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-3.5f, -1.8f));
                                Projectile.NewProjectile(
                                    Projectile.GetSource_FromThis(),
                                    target.Center,
                                    speed,
                                    ModContent.ProjectileType<CosmicDischargeIceShard>(),
                                    (int)(Projectile.damage * 0.52f),
                                    0f,
                                    Projectile.owner
                                );
                            }
                            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.65f, Pitch = -0.1f }, Projectile.Center);
                        }

                        // Spawn tearing particles and play tearing sound
                        SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
                        for (int i = 0; i < 4; i++)
                        {
                            Dust d = Dust.NewDustPerfect(
                                target.Center + Main.rand.NextVector2Circular(20f, 20f),
                                DustID.Blood,
                                Main.rand.NextVector2Circular(3f, 3f),
                                0,
                                Color.Red,
                                Main.rand.NextFloat(0.8f, 1.3f)
                            );
                            d.noGravity = true;
                        }

                        if (ultActive || empActive)
                        {
                            // Spawn mini CosmicIceBurst at target center
                            Projectile.NewProjectile(
                                Projectile.GetSource_FromThis(),
                                target.Center,
                                Vector2.Zero,
                                ModContent.ProjectileType<CalamityMod.Projectiles.Melee.CosmicIceBurst>(),
                                (int)(Projectile.damage * (empActive ? 0.55f : 0.42f)),
                                0f,
                                Projectile.owner,
                                0f,
                                empActive ? 0.95f : 0.72f
                            );

                            // Spawn 6 radial line particles
                            for (int i = 0; i < 6; i++)
                            {
                                Vector2 vel = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * Main.rand.NextFloat(3f, 5.5f);
                                GeneralParticleHandler.SpawnParticle(new LineParticle(
                                    target.Center,
                                    vel,
                                    false,
                                    Main.rand.Next(10, 15),
                                    Main.rand.NextFloat(0.35f, 0.55f),
                                    Main.rand.NextBool() ? CosmicDischargeCommon.FrostCoreColor : CosmicDischargeCommon.FrostWhiteColor
                                ));
                            }
                        }
                    }
                }

                // Pull effect: Drag non-boss targets toward the player
                var cosPlayer = Player.GetModPlayer<CosmicDischargePlayer>();
                bool isUltimateActive = cosPlayer.UltimateFieldActive;
                bool isEmpActive = cosPlayer.FrozenEmperorActive;
                if (!target.boss && target.knockBackResist > 0f)
                {
                    Vector2 pullDir = Player.MountedCenter - target.Center;
                    float distance = pullDir.Length();
                    if (distance > 60f)
                    {
                        float pullSpeed = MathHelper.Clamp(distance / 12f, 5f, 18f);
                        target.velocity = pullDir.SafeNormalize(Vector2.Zero) * pullSpeed;
                        target.netUpdate = true;
                    }
                }
                else if (target.boss)
                {
                    if (isEmpActive)
                    {
                        // Shackling bosses: even heavier slow under Frozen Emperor!
                        target.velocity *= 0.4f; // 60% slow
                        target.netUpdate = true;
                    }
                    else if (isUltimateActive)
                    {
                        // Shackling bosses: slow them down significantly under Ultimate Field
                        target.velocity *= 0.6f; // 40% slow
                        target.netUpdate = true;
                    }
                }

                if (latchTimer >= MaxLatchTime)
                {
                    State = 2; // Latch expired, return
                }
            }
            // State 2: Retract
            else if (State == 2)
            {
                Vector2 returnDir = Player.MountedCenter - Projectile.Center;
                float dist = returnDir.Length();
                if (dist < 30f)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.velocity = returnDir.SafeNormalize(Vector2.Zero) * 22f;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            // Only allow hitting and latching if we are in Fly Out state
            return State == 0 ? base.CanHitNPC(target) : false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyColdDebuffs(target, 120);

            if (State == 0)
            {
                State = 1;
                TargetNPCIndex = target.whoAmI;
                latchTimer = 0;
                hitTicker = 0;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
                
                SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.52f, Pitch = 0.25f }, Projectile.Center);
                
                // Spawn pulse ring on bite
                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new StrongBloom(
                        target.Center,
                        Vector2.Zero,
                        CosmicDischargeCommon.FrostCoreColor * 0.45f,
                        0.5f,
                        16
                    ));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // Draw the lock chain from player back to the knife tip
            Vector2 tipPos = Projectile.Center;
            if (State == 1)
            {
                // Under stress / tension shake jitter
                tipPos += Main.rand.NextVector2Circular(2.2f, 2.2f);
            }

            CosmicDischargeCommon.DrawChain(Main.spriteBatch, Player.MountedCenter, tipPos, lightColor, Projectile.scale, false, Player.gfxOffY);

            // Draw the knife tip itself
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            SpriteEffects effects = Player.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 origin = texture.Size() * 0.5f;

            Main.EntitySpriteDraw(
                texture,
                tipPos - Main.screenPosition + Vector2.UnitY * Player.gfxOffY,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                effects,
                0
            );

            return false;
        }
    }
}
