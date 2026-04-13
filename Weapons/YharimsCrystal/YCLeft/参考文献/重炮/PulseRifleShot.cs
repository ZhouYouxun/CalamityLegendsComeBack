using System;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;


namespace CalamityMod.Projectiles.DraedonsArsenal
{
    public class PulseRifleShot : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Misc";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float time => ref Projectile.ai[0];
        public bool isBeam => Projectile.ai[1] == 0;
        public bool onSpawn = true;
        private NPC targeted = null;
        private NPC lastHitTarget = null;
        private int timesItCanHit = 3;
        public bool startAttackEffects = true;
        public bool dead = false;
        public int attackTime => (int)(180 + Projectile.ai[1]); // The amount of time an orb spends slowing before it attacks

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 7;
            Projectile.timeLeft = 900;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override void AI()
        {
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

            Lighting.AddLight(Projectile.Center, Effects.ArsenalEffects.ArsenalPulseColor.ToVector3() * 0.5f);

            if (onSpawn)
            {
                float fxPower = isBeam ? 2 : 0.5f;
                if (isBeam)
                {
                    Projectile.Center += Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(-0.05f * Math.Sign(Projectile.velocity.X)) * 60;
                    Projectile.extraUpdates = 100;
                    timesItCanHit = 1;
                    Owner.SetScreenshake(4f);
                }

                for (int i = 0; i <= 8 * fxPower; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Effects.ArsenalEffects.ArsenalPulseDust);
                    dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
                    dust.velocity = (Projectile.velocity.SafeNormalize(Vector2.UnitX)).RotateRandom(0.5f) * Main.rand.NextFloat(2f, 9f) * fxPower;
                    dust.noGravity = true;
                    dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
                    dust.fadeIn = 1;
                }
                for (int i = 0; i <= 6 * fxPower; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
                    dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
                    dust.velocity = (Projectile.velocity.SafeNormalize(Vector2.UnitX)).RotateRandom(0.3f) * Main.rand.NextFloat(4f, 15f) * fxPower;
                    dust.noGravity = true;
                    dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
                    dust.fadeIn = 0.3f;
                }

                onSpawn = false;
            }
            if (isBeam)
            {
                if (targetDist < 1400f && time > 0) // The main beam
                {
                    Particle mainBeam = new CustomSpark(Projectile.Center, Projectile.velocity * 0.01f, "CalamityMod/Particles/BloomCircle", false, 35, 0.55f * Projectile.scale, Effects.ArsenalEffects.ArsenalPulseColor * 0.5f, new Vector2(0.6f, 1f), true, true, shrinkSpeed: 0.2f, glowCenterScale: 0.7f, glowOpacity: 0.4f);
                    GeneralParticleHandler.SpawnParticle(mainBeam);
                    Projectile.scale += 0.007f;

                    if (time % 3 == 0)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(3 * Projectile.scale, 3 * Projectile.scale), Effects.ArsenalEffects.ArsenalPulseDust);
                        dust.scale = Main.rand.NextFloat(0.7f, 1.2f) * Projectile.scale;
                        dust.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(7f, 25f);
                        dust.noGravity = true;
                        dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
                        dust.fadeIn = 0.6f;
                    }
                }
            }
            else
            {
                if (time >= attackTime) // home in on enemy
                {
                    // Find a target
                    if (targeted == null)
                    {
                        Projectile.velocity *= 0.9f;

                        startAttackEffects = true;
                        NPC chosenTarget = null;
                        float distance = 2500;
                        for (int index = 0; index < Main.npc.Length; index++) // look for a target that isnt one it has already hit in the last two hits.
                        {
                            NPC searchedTarget = Main.npc[index];
                            if (searchedTarget.CanBeChasedBy(null, false))
                            {
                                if (Vector2.Distance(Projectile.Center, searchedTarget.Center) < distance && (lastHitTarget != null ? searchedTarget != lastHitTarget : true) && searchedTarget.active && searchedTarget.life > 0)
                                {
                                    distance = Vector2.Distance(Projectile.Center, searchedTarget.Center);
                                    chosenTarget = searchedTarget;
                                }
                            }
                        }
                        if (chosenTarget == null) // If no target was found, check again but accept last hit targets as viable.
                        {
                            if (lastHitTarget != null)
                                Projectile.localNPCImmunity[lastHitTarget.whoAmI] = 0;

                            for (int index = 0; index < Main.npc.Length; index++)
                            {
                                NPC searchedTarget = Main.npc[index];
                                if (searchedTarget.CanBeChasedBy(null, false))
                                {
                                    if (Vector2.Distance(Projectile.Center, searchedTarget.Center) < distance && searchedTarget.active && searchedTarget.life > 0)
                                    {
                                        distance = Vector2.Distance(Projectile.Center, searchedTarget.Center);
                                        chosenTarget = searchedTarget;
                                    }
                                }
                            }
                        }

                        targeted = chosenTarget;
                    }
                    else // Home in on selected target
                    {
                        // Add extra updates as it hits more times, this smoothy increases the speed without destroying velocity based visual effects
                        Projectile.extraUpdates = 7 + (int)(Projectile.numHits * 0.6f);

                        if (Projectile.timeLeft < 110 * Projectile.extraUpdates)
                            Projectile.timeLeft = 110 * Projectile.extraUpdates;
                        CalamityUtils.HomeInOnSelectedNPC(Projectile, targeted, true, 0.4f, 15, 0.97f);
                        if (startAttackEffects)
                        {
                            Projectile.velocity = Utils.DirectionTo(Projectile.Center, targeted.Center) * 10;
                            SoundStyle pulse = new("CalamityMod/Sounds/Item/PulseSound");
                            SoundEngine.PlaySound(pulse with { Volume = 0.35f, Pitch = Math.Max(0.5f, Main.rand.NextFloat(0.1f, 0.3f) + Projectile.numHits * 0.2f), MaxInstances = 5 }, Projectile.Center);

                            Particle pulse3 = new CustomSpark(Projectile.Center, Projectile.velocity * 0.5f, "CalamityMod/Particles/HighResHollowCircleHardEdgeAlt", false, 13, 0.05f * Projectile.scale, Effects.ArsenalEffects.ArsenalPulseColor, new Vector2(1.2f, 0.7f), shrinkSpeed: 0.4f);
                            GeneralParticleHandler.SpawnParticle(pulse3);

                            for (int k = 0; k < 6; k++)
                            {
                                Dust dust = Dust.NewDustPerfect(Projectile.Center, Effects.ArsenalEffects.ArsenalPulseDust);
                                dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
                                dust.velocity = Utils.DirectionTo(Projectile.Center, targeted.Center).RotatedByRandom(0.5f) * Main.rand.NextFloat(5f, 9f);
                                dust.noGravity = true;
                                dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
                                dust.fadeIn = 1;
                            }
                            startAttackEffects = false;
                        }
                        if (targeted.life <= 0 || !targeted.active || !targeted.CanBeChasedBy())
                            targeted = null;
                    }
                }
                else
                {
                    Projectile.velocity *= 0.985f;
                }

                float squash = Utils.GetLerpValue(1, 3, Projectile.velocity.Length(), true);
                if (targetDist < 1400f && squash > 0.1f && time > 5) // The trail
                {
                    Particle trail = new CustomSpark(Projectile.Center, Projectile.velocity * 0.01f, "CalamityMod/Particles/DualTrail", false, 13, 0.075f, Effects.ArsenalEffects.ArsenalPulseColor * 0.6f * squash, new Vector2(1 - 0.15f * squash, 1.5f), true, false, shrinkSpeed: 0.2f * squash);
                    GeneralParticleHandler.SpawnParticle(trail);
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            time++;
        }
        public override bool? CanHitNPC(NPC target) => ((target == targeted) || isBeam) ? null : false;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool onKill = (target.life <= 0 && target.realLife == -1);

            timesItCanHit--;

            float fxVel = isBeam ? 3f : 1f;
            for (int i = 0; i <= 5 * fxVel; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
                dust.scale = Main.rand.NextFloat(1.7f, 2.4f) * Projectile.scale;
                dust.velocity = (Projectile.velocity.SafeNormalize(Vector2.UnitX)).RotateRandom(0.3f) * Main.rand.NextFloat(4f, 9f) * fxVel;
                dust.noGravity = true;
                dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
                dust.fadeIn = 0.3f * fxVel;
            }

            // Set some values to get ready for it to home again for its next hit
            if (!isBeam)
            {
                Projectile.ai[1] = -50;
                lastHitTarget = target;
                targeted = null;
                time = 0;
                Projectile.velocity *= 0.9f;

                if (onKill)
                    timesItCanHit += 3;
            }
            else
            {
                if (Main.myPlayer == Projectile.owner)
                {
                    // Spawn the 4 energy orbs
                    // These should do a large fraction of the beam's damage so they will easily kill even some decently bulky enemies regular enemies in one hit
                    // This is so it can better proc its on kill effect
                    int numProj = 4;
                    int projectileDamage = (int)(Projectile.damage * 0.5f);
                    for (int i = 1; i < numProj + 1; i++)
                    {
                        Projectile orb = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.Zero) * (10 / numProj * i), Projectile.type, projectileDamage, Projectile.knockBack, Projectile.owner, 0f, 28 * i);
                        orb.scale = 1.4f - i * 0.2f;
                    }
                }
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Particle pulse3 = new CustomSpark(Projectile.Center, vel * 7, "CalamityMod/Particles/HighResHollowCircleHardEdgeAlt", false, 16, 0.065f, Effects.ArsenalEffects.ArsenalPulseColor, new Vector2(2f, 0.7f), shrinkSpeed: 0.2f);
                GeneralParticleHandler.SpawnParticle(pulse3);
                Particle pulse4 = new CustomSpark(Projectile.Center, vel * 13, "CalamityMod/Particles/HighResHollowCircleHardEdgeAlt", false, 12, 0.04f, Effects.ArsenalEffects.ArsenalPulseColor, new Vector2(2f, 0.7f), shrinkSpeed: 0.2f);
                GeneralParticleHandler.SpawnParticle(pulse4);
            }
            // If it's hit targeted enemies enough, kill it
            if (timesItCanHit <= 0)
                Projectile.Kill();
            
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (isBeam)
                return false;
            Asset<Texture2D> orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 squash = new Vector2(Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 0.6f), Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 2f));
            float timeleftFade = (float)Math.Pow(Utils.GetLerpValue(0, 40 * Projectile.extraUpdates, Projectile.timeLeft, true), 5);

            for (int i = 0; i < 7; i++)
            {
                Color orbColor = Color.Lerp(Effects.ArsenalEffects.ArsenalPulseColor, Color.White, i * 0.07f) with { A = 0 } * 0.5f;
                Vector2 scale = Projectile.scale * timeleftFade * squash * (0.05f + i * 0.01f) * 3;
                Main.EntitySpriteDraw(orb.Value, Projectile.Center - Main.screenPosition, null, orbColor, Projectile.rotation, orb.Size() * 0.5f, scale, SpriteEffects.None);
            }

            return false;
        }
    }
}
