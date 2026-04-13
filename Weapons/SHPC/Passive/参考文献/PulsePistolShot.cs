//using System;
//using CalamityMod.Dusts;
//using CalamityMod.Particles;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
//using ReLogic.Content;
//using Terraria;
//using Terraria.Audio;
//using Terraria.ID;
//using Terraria.ModLoader;

//namespace CalamityMod.Projectiles.DraedonsArsenal
//{
//    public class PulsePistolShot : ModProjectile, ILocalizedModType
//    {
//        public new string LocalizationCategory => "Projectiles.SHPC";
//        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
//        public ref float time => ref Projectile.ai[0];
//        public bool onSpawn = true;
//        private NPC targeted = null;
//        private NPC lastHitTarget = null;
//        private int timesItCanHit = 1;
//        public bool startAttackEffects = true;
//        public int attackTime = 160; // The amount of time an orb spends slowing before it attacks
//        public override void SetDefaults()
//        {
//            Projectile.width = 16;
//            Projectile.height = 16;
//            Projectile.friendly = true;
//            Projectile.DamageType = DamageClass.Magic;
//            Projectile.penetrate = -1;
//            Projectile.extraUpdates = 5;
//            Projectile.timeLeft = 600;
//            Projectile.tileCollide = false;
//            Projectile.usesLocalNPCImmunity = true;
//            Projectile.localNPCHitCooldown = 20;
//        }

//        public override void AI()
//        {
//            Lighting.AddLight(Projectile.Center, Effects.ArsenalEffects.ArsenalPulseColor.ToVector3() * 0.5f);
//            Player Owner = Main.player[Projectile.owner];
//            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);

//            if (onSpawn)
//            {
//                if (Projectile.ai[1] == 0)
//                    SoundEngine.PlaySound(SoundID.DD2_DarkMageCastHeal with { Volume = 1.7f, Pitch = 0.3f }, Projectile.Center);

//                for (int i = 0; i <= 8; i++)
//                {
//                    Dust dust = Dust.NewDustPerfect(Projectile.Center, Effects.ArsenalEffects.ArsenalPulseDust);
//                    dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
//                    dust.velocity = (Projectile.velocity.SafeNormalize(Vector2.UnitX)).RotateRandom(0.5f) * Main.rand.NextFloat(5f, 7f);
//                    dust.noGravity = true;
//                    dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
//                    dust.fadeIn = 1;
//                }
//                for (int i = 0; i <= 6; i++)
//                {
//                    Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
//                    dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
//                    dust.velocity = (Projectile.velocity.SafeNormalize(Vector2.UnitX)).RotateRandom(0.3f) * Main.rand.NextFloat(7f, 9f);
//                    dust.noGravity = true;
//                    dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
//                    dust.fadeIn = 0.3f;
//                }

//                onSpawn = false;
//            }

//            if (time >= attackTime) // home in on enemy
//            {
//                // Find a target
//                if (targeted == null)
//                {
//                    Projectile.velocity *= 0.9f;
                    
//                    startAttackEffects = true;
//                    NPC chosenTarget = null;
//                    float distance = 2500;
//                    for (int index = 0; index < Main.npc.Length; index++) // look for a target that isnt one it has already hit in the last two hits.
//                    {
//                        NPC searchedTarget = Main.npc[index];
//                        if (searchedTarget.CanBeChasedBy(null, false))
//                        {
//                            if (Vector2.Distance(Projectile.Center, searchedTarget.Center) < distance && (lastHitTarget != null ? searchedTarget != lastHitTarget : true) && searchedTarget.active && searchedTarget.life > 0)
//                            {
//                                distance = Vector2.Distance(Projectile.Center, searchedTarget.Center);
//                                chosenTarget = searchedTarget;
//                            }
//                        }
//                    }
//                    if (chosenTarget == null) // If no target was found, check again but accept last hit targets as viable.
//                    {
//                        if (lastHitTarget != null)
//                            Projectile.localNPCImmunity[lastHitTarget.whoAmI] = 0;

//                        for (int index = 0; index < Main.npc.Length; index++)
//                        {
//                            NPC searchedTarget = Main.npc[index];
//                            if (searchedTarget.CanBeChasedBy(null, false))
//                            {
//                                if (Vector2.Distance(Projectile.Center, searchedTarget.Center) < distance && searchedTarget.active && searchedTarget.life > 0)
//                                {
//                                    distance = Vector2.Distance(Projectile.Center, searchedTarget.Center);
//                                    chosenTarget = searchedTarget;
//                                }
//                            }
//                        }
//                    }

//                    targeted = chosenTarget;
//                }
//                else // Home in on selected target
//                {
//                    // Add extra updates as it hits more times, this smoothy increases the speed without destroying velocity based visual effects
//                    Projectile.extraUpdates = 5 + (int)(Projectile.numHits * 0.6f);

//                    if (Projectile.timeLeft < 110 * Projectile.extraUpdates)
//                        Projectile.timeLeft = 110 * Projectile.extraUpdates;
//                    CalamityUtils.HomeInOnSelectedNPC(Projectile, targeted, true, 0.5f, 15, 0.97f);
//                    if (startAttackEffects)
//                    {
//                        Projectile.velocity = Utils.DirectionTo(Projectile.Center, targeted.Center) * 10;
//                        SoundStyle pulse = new("CalamityMod/Sounds/Item/PulseSound");
//                        SoundEngine.PlaySound(pulse with { Volume = 0.25f, Pitch = Math.Max(0.6f, Main.rand.NextFloat(0.3f, 0.4f) + Projectile.numHits * 0.1f), MaxInstances = 5 }, Projectile.Center);
//                        for (int k = 0; k < 6; k++)
//                        {
//                            Dust dust = Dust.NewDustPerfect(Projectile.Center, Effects.ArsenalEffects.ArsenalPulseDust);
//                            dust.scale = Main.rand.NextFloat(1.2f, 1.9f) * Projectile.scale;
//                            dust.velocity = Utils.DirectionTo(Projectile.Center, targeted.Center).RotatedByRandom(0.5f) * Main.rand.NextFloat(5f, 9f);
//                            dust.noGravity = true;
//                            dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
//                            dust.fadeIn = 1;
//                        }
//                        startAttackEffects = false;
//                    }
//                    if (targeted.life <= 0 || !targeted.active || !targeted.CanBeChasedBy())
//                        targeted = null;
//                }
//            }
//            else
//            {
//                Projectile.velocity *= Projectile.numHits > 0 ? 0.955f : 0.97f;
//            }

//            float squash = Utils.GetLerpValue(1, 3, Projectile.velocity.Length(), true);
//            if (targetDist < 1400f && squash > 0.2f && (Projectile.ai[1] == 0 || time > 5)) // The trail
//            {
//                Particle trail = new CustomSpark(Projectile.Center, Projectile.velocity * 0.01f, "CalamityMod/Particles/DualTrail", false, 13, 0.075f * Projectile.scale, Effects.ArsenalEffects.ArsenalPulseColor * 0.6f * squash, new Vector2(1 - 0.15f * squash, 1.5f), true, false, shrinkSpeed: 0.2f * squash);
//                GeneralParticleHandler.SpawnParticle(trail);
//            }

//            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
//            time++;
//        }

//        public override bool? CanHitNPC(NPC target) => (target == targeted) ? null : false;

//        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
//        {
//            bool onKill = (target.life <= 0 && target.realLife == -1);
            
//            // Set some values to get ready for it to home again for its next hit
//            lastHitTarget = target;
//            targeted = null;
//            time = 0;
//            timesItCanHit--;
//            Projectile.velocity *= 0.8f;

//            for (int i = 0; i <= 5; i++)
//            {
//                Dust dust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<SquashDust>());
//                dust.scale = Main.rand.NextFloat(1.7f, 2.4f) * Projectile.scale;
//                dust.velocity = (Projectile.velocity.SafeNormalize(Vector2.UnitX)).RotateRandom(0.3f) * Main.rand.NextFloat(4f, 9f);
//                dust.noGravity = true;
//                dust.color = Effects.ArsenalEffects.ArsenalPulseColor;
//                dust.fadeIn = 0.3f;
//            }

//            if (onKill)
//                timesItCanHit += 1;

//            // If it's hit targeted enemies enough, kill it
//            if (timesItCanHit <= 0)
//            {
//                if (Projectile.ai[1] == 0 && Main.myPlayer == Projectile.owner)
//                {
//                    Projectile pulseOrb1 = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, (Projectile.velocity.SafeNormalize(Vector2.UnitX) * 10).RotatedBy(0.3f) * 0.5f, Projectile.type, Projectile.damage / 4, Projectile.knockBack / 2, Projectile.owner, 0, 1);
//                    Projectile pulseOrb2 = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, (Projectile.velocity.SafeNormalize(Vector2.UnitX) * 10).RotatedBy(-0.3f) * 0.5f, Projectile.type, Projectile.damage / 4, Projectile.knockBack / 2, Projectile.owner, 0, -1);
//                    pulseOrb1.penetrate = 1;
//                    pulseOrb1.scale = 0.7f;
//                    pulseOrb2.penetrate = 1;
//                    pulseOrb2.scale = 0.7f;
//                }
//                Projectile.Kill();
//            }
//        }
//        public override bool PreDraw(ref Color lightColor)
//        {
//            Asset<Texture2D> orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
//            Vector2 squash = new Vector2(Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 0.6f), Utils.Remap(Projectile.velocity.Length(), 5, 10, 1, 2f));
//            float timeleftFade = (float)Math.Pow(Utils.GetLerpValue(0, 40 * Projectile.extraUpdates, Projectile.timeLeft, true), 5);

//            for (int i = 0; i < 6; i++)
//            {
//                Color orbColor = Color.Lerp(Effects.ArsenalEffects.ArsenalPulseColor, Color.White, i * 0.07f) with { A = 0 } * 0.5f;
//                Vector2 scale = Projectile.scale * timeleftFade * squash * (0.05f + i * 0.01f) * 3;
//                Main.EntitySpriteDraw(orb.Value, Projectile.Center - Main.screenPosition, null, orbColor, Projectile.rotation, orb.Size() * 0.5f, scale, SpriteEffects.None);
//            }

//            return false;
//        }
//    }
//}
