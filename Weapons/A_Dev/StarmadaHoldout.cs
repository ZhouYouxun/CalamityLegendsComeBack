using System;
using System.Collections.Generic;
using System.IO;
using CalamityMod.Dusts;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace CalamityMod.Projectiles.Ranged
{
    public class StarmadaHoldout : BaseGunHoldoutProjectile
    {
        public override int AssociatedItemID => ItemType<Starmada>();
        public override Vector2 GunTipPosition => base.GunTipPosition + Projectile.velocity.RotatedBy(MathHelper.PiOver2 * Projectile.direction) * -5;
        public override float RecoilResolveSpeed => 0.1f;
        public override float MaxOffsetLengthFromArm => 32f;
        public override float OffsetXUpwards => -12f;
        public override float BaseOffsetY => -10f;
        public override float OffsetYDownwards => 10f;
        public override float WeaponTurnSpeed => (0.6f);

        public int time = 0;
        public int lastUseTime = 0;
        public static int perfectLeniancy = 3;
        public static int goodLeniancy = perfectLeniancy + 6;
        public static int starburstPerfectTime = 23;
        public float frontRecoil = 0;
        public ref float shootingCooldown => ref Projectile.ai[0];
        public ref float starburstTimer => ref Projectile.ai[1];
        public int extendedCooldown => (int)(lastUseTime * 1.2f);
        public int perfectCooldown => (int)(lastUseTime * 1.5f);
        public float recoilIntensity = 0;
        public int recoilTimerMax = 62;
        public Vector2 recoilDirection;
        public bool setVel = true;
        public float glowIntensity = 1;
        public float attackVisualMult = 0;
        public Color c1 = new Color(164, 47, 160);
        public Color c2 = new Color(227, 97, 72);
        public Color c3 = new Color(193, 255, 146);
        public Color shiftColor;
        public Vector2 gunBackPosition;
        public int gunPower = 1;
        public int lastGunPower = 1;
        public SlotId AudSlot1;
        public SlotId AudSlot2;
        public bool failedChain = false;
        public float shake = 0;
        public ref float starburstCooldown => ref Projectile.ai[2];
        public bool naildriver => ((starburstTimer <= starburstPerfectTime + perfectLeniancy) && (starburstTimer >= starburstPerfectTime - perfectLeniancy)); // if within perfect frame window
        public bool scattershot => !naildriver && ((starburstTimer <= starburstPerfectTime + goodLeniancy) && (starburstTimer >= starburstPerfectTime - goodLeniancy)); // If within early or late frame window
        public override void KillHoldoutLogic() { }
        public override void SendExtraAIHoldout(BinaryWriter writer)
        {
            writer.Write(lastUseTime);
            writer.Write(gunPower);
            writer.Write(lastGunPower);
        }

        public override void ReceiveExtraAIHoldout(BinaryReader reader)
        {
            lastUseTime = reader.ReadInt32();
            gunPower = reader.ReadInt32();
            lastGunPower = reader.ReadInt32();
        }
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
        }
        public override void HoldoutAI()
        {
            float rate = (time * 0.05f);
            List<Color> eColors = new List<Color>()
                {
                    c1,
                    c2,
                    c3,
                };
            int colorIndex = (int)(rate / 2 % eColors.Count);
            Color currentColor = eColors[colorIndex];
            Color nextColor = eColors[(colorIndex + 1) % eColors.Count];
            shiftColor = Color.Lerp(currentColor, nextColor, rate % 2f >= 1f ? 1f : rate % 1f);

            SetUsage = false;
            bool doingNothing = shootingCooldown == 0 && starburstCooldown == 0 && starburstTimer == 0;
            if (lastUseTime == 0 || doingNothing)
                lastUseTime = Owner.HeldItem.useAnimation;
            if (!doingNothing)
                Owner.itemTime = Owner.itemAnimation = 5;

            glowIntensity = MathHelper.Lerp(glowIntensity, (float)Math.Pow(Utils.GetLerpValue(recoilTimerMax, 0, shootingCooldown, true), 5), 0.2f);
            attackVisualMult = MathHelper.Lerp(attackVisualMult, (float)Math.Pow(Math.Min(Utils.GetLerpValue(0, starburstPerfectTime - 1, starburstTimer, true), 2) + (gunPower - 1) * 0.25f, 1) * glowIntensity, 0.2f);
            shake = MathHelper.Lerp(shake, 0, 0.05f);
            frontRecoil = MathHelper.Lerp(frontRecoil, 0, 0.11f);

            if ((Owner.HeldItem.type != ItemType<Starmada>() && doingNothing) || (doingNothing && (Main.mapFullscreen || Owner.mouseInterface)) || Owner.dead)
            {
                Projectile.Kill();
                return;
            }
            bool hasAmmo = Owner.PickAmmo(HeldItem, out _, out _, out _, out _, out _, true);
            bool leftShootChecks = Owner.whoAmI == Main.myPlayer && (Main.mouseLeft && !Main.mapFullscreen && !Owner.mouseInterface && shootingCooldown == 0) && hasAmmo;
            bool rightShootChecks = Owner.whoAmI == Main.myPlayer && Owner.Calamity().mouseRight && !Main.mapFullscreen && !Owner.mouseInterface && starburstCooldown == 0 && starburstTimer == 0;
            
            if (Owner.whoAmI == Main.myPlayer && Main.mouseLeft && !hasAmmo && shake < 0.1f)
            {
                shake = 0.8f;
                SoundStyle click = new("CalamityMod/Sounds/Item/DudFire");
                SoundEngine.PlaySound(click with { Volume = .6f, Pitch = -.2f}, Projectile.Center);
            }

            if (leftShootChecks)
                FireShotgun();
            if (rightShootChecks)
            {
                Projectile.ForceNetUpdate();
                SoundStyle blast1 = new("CalamityMod/Sounds/Item/StarfleetStarburst");
                AudSlot1 = SoundEngine.PlaySound(blast1 with { Volume = 0.7f + gunPower * 0.1f, Pitch = 0f, MaxInstances = 8 }, Projectile.Center);
                SoundStyle blast2 = new("CalamityMod/Sounds/Item/StarfleetStarburst");
                AudSlot2 = SoundEngine.PlaySound(blast2 with { Volume = 0.5f + gunPower * 0.1f, Pitch = -0.2f + gunPower * 0.2f, MaxInstances = 8 }, Projectile.Center);
                lastGunPower = gunPower;
                starburstTimer++;
            }
            if (starburstTimer > 0)
            {
                // Do wind up animation
                if (starburstTimer < starburstPerfectTime / 2)
                {
                    float up = (1 - (float)Math.Pow(Utils.GetLerpValue(starburstPerfectTime / 2 - 1, 0, starburstTimer, true), 2));
                    OffsetLengthFromArm = 32 - 7 * up;
                    frontRecoil = -25 * up;
                }
                else
                {
                    float down = ((float)Math.Pow(Utils.GetLerpValue(starburstPerfectTime / 2, starburstPerfectTime - 1, starburstTimer, true), 12));
                    OffsetLengthFromArm = 25 + 15 * down;
                    frontRecoil = -25 + 25 * down;
                }

                if (starburstTimer == starburstPerfectTime)
                    FireStarburst();
                
                starburstTimer++;
                if (starburstTimer > starburstPerfectTime + goodLeniancy + 1)
                    starburstTimer = 0;
            }
            if (shootingCooldown > 0)
            {
                if (lastGunPower != gunPower)
                {
                    if (SoundEngine.TryGetActiveSound(AudSlot1, out var s1) && s1.IsPlaying && failedChain)
                    {
                        s1.Pitch = MathHelper.Lerp(s1.Pitch, -0.7f, 0.07f);
                        s1.Volume = MathHelper.Lerp(s1.Volume, 0.2f, 0.07f);
                    }
                    if (SoundEngine.TryGetActiveSound(AudSlot2, out var s2) && s2.IsPlaying && failedChain)
                    {
                        s2.Pitch = MathHelper.Lerp(s2.Pitch, -0.7f, 0.07f);
                        s2.Volume = MathHelper.Lerp(s2.Volume, 0.2f, 0.07f);
                    }
                }
                shootingCooldown--;
            }
            else
                failedChain = false;
            if (starburstCooldown > 0)
                starburstCooldown--;
            if (recoilIntensity > 0 && (shootingCooldown > 0 || starburstCooldown > 0))
                ManageRecoil();

            time++;
        }
        public void ManageRecoil()
        {
            float slowdown = (float)Math.Pow(Utils.GetLerpValue(recoilTimerMax / 2, recoilTimerMax, Math.Max(shootingCooldown, starburstCooldown), true), 4);
            Vector2 movement = recoilDirection * (recoilIntensity) * slowdown;
            bool enableRecoil = false;
            if (!enableRecoil || Collision.SolidCollision(Owner.Center + movement, (int)(Owner.width * 1.1f), (int)(Owner.height * 1.1f)) || !Owner.Calamity().mouseRight)
            {
                recoilIntensity = 0;
                return;
            }
            if (slowdown > 0.1f)
            {
                if (setVel)
                {
                    Owner.velocity = movement * 0.25f;
                    setVel = false;
                }
                Projectile.Center += movement;
                Owner.Center += movement;
            }
            else
                setVel = true;
        }
        public void FireShotgun()
        {
            Projectile.ForceNetUpdate();
            // 50% chance to not consume ammo
            Owner.PickAmmo(HeldItem, out _, out _, out _, out _, out _, Main.rand.NextBool());

            
            if (!naildriver && gunPower > 1)
            {
                shake = 4;
                failedChain = true;
                SoundStyle oops = new("CalamityMod/Sounds/Item/TaserLaunch");
                SoundStyle oops2 = new("CalamityMod/Sounds/Item/LightMetal");
                for (int i = 0; i < 2; i++)
                    SoundEngine.PlaySound((i == 0 ? oops : oops2) with { Volume = 1f, Pitch = (i == 0 ? 0.5f : -0.3f), MaxInstances = 2 }, Projectile.Center);
            }
            else
            {
                SoundStyle shotgunFire = new("CalamityMod/Sounds/Item/StarmadaFire");
                for (int i = 0; i < (naildriver ? 2 : 1); i++)
                    SoundEngine.PlaySound(shotgunFire with { Volume = (naildriver && i == 0 ? 0.3f : 0.6f), Pitch = ((naildriver && i == 0) ? -0.2f : 0f), MaxInstances = 2 }, Projectile.Center);
                if (naildriver)
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HellkiteFullCharge") with { Volume = 0.7f, Pitch = 1.2f + 0.1f * gunPower, MaxInstances = 2 }, Projectile.Center);
                for (int b = 0; b < 24; b++)
                {
                    int parts2 = 4;
                    for (int i = 0; i < parts2; i++)
                    {
                        float power = Main.rand.NextFloat(0.2f, 1f);
                        Vector2 vel = (MathHelper.TwoPi * i / parts2).ToRotationVector2().RotatedBy(Projectile.rotation) * 12f;
                        float size = (0.8f + 0.35f * gunPower) * Main.rand.NextFloat(0.9f, 1.1f) * (1.1f - power);
                        int dustStyle = DustType<SquashDust>();
                        Dust dust = Dust.NewDustPerfect(gunBackPosition, dustStyle);
                        dust.scale = size;
                        dust.velocity = vel * power * (0.7f + gunPower * 0.2f);
                        dust.noGravity = true;
                        dust.color = GetRandomColor();
                        dust.fadeIn = naildriver ? -0.5f : 0f;

                        if (b == 0)
                        {
                            Particle aura = new CustomSpark(gunBackPosition, Vector2.Zero, "CalamityMod/Particles/BloomCircle", false, naildriver ? 35 : 20, (0.8f + 0.35f * gunPower) * 0.85f, shiftColor, new Vector2(0.65f, 1f), glowCenter: true, glowOpacity: 0.8f, glowCenterScale: 0.85f, extraRotation: Projectile.rotation + (i % 2 == 0 ? MathHelper.PiOver2 : 0), shrinkSpeed: 0.1f);
                            GeneralParticleHandler.SpawnParticle(aura);
                        }
                    }
                }
            }

            // Perfects have longer cooldown
            int cooldown = (naildriver ? perfectCooldown : lastUseTime);
            recoilTimerMax = cooldown;
            shootingCooldown = cooldown;
            recoilDirection = -Projectile.velocity;
            Owner.SetScreenshake(naildriver ? 9 + gunPower : scattershot ? 8 : 5);
            OffsetLengthFromArm = naildriver ? 0 : scattershot ? 7 : 15;
            frontRecoil = naildriver ? -25 : scattershot ? -18 : -10;

            float gunPowerMult = MathHelper.Lerp(gunPower, 1, 0.7f);

            if (Main.myPlayer == Projectile.owner)
            {
                int baseShotCount = 7;
                for (int i = 0; i < baseShotCount + gunPower; i++)
                {
                    float randomVel = Main.rand.NextFloat(0.8f, 1f);
                    float damageMult = ((naildriver || scattershot) ? 1.75f : 1f) / baseShotCount;
                    float spread = (i == 0 ? 0f : naildriver ? 0.06f : scattershot ? 0.9f : 0.25f) * MathHelper.Lerp(gunPower, 1, 0.75f);
                    int starExtraUpdates = naildriver ? 9 : scattershot ? 7 : 3;
                    Projectile shotgun = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), GunTipPosition, randomVel * Projectile.velocity.RotatedByRandom(spread) * 8, ProjectileType<StarmadaStar>(), (int)(Projectile.damage * damageMult), Projectile.knockBack, Projectile.owner, 0, starExtraUpdates, Main.rand.Next(0, 300 + 1));
                    shotgun.extraUpdates = starExtraUpdates;
                }
            }
            for (int i = 0; i < (int)(25 * gunPowerMult); i++)
            {
                float variance = Main.rand.NextFloat(-0.7f, 0.7f);
                int dustStyle = DustType<SquashDust>();
                Dust dust = Dust.NewDustPerfect(GunTipPosition, dustStyle);
                dust.scale = (Main.rand.NextFloat(1.4f, 1.8f) - Math.Abs(variance)) * 3f * gunPowerMult;
                dust.velocity = Projectile.velocity.RotatedBy(variance) * Main.rand.NextFloat(18f, 19f) * (float)Math.Pow(1 - Math.Abs(variance), 2) * gunPowerMult;
                dust.noGravity = true;
                dust.color = GetRandomColor();
                dust.fadeIn = 3.75f;
            }

            // You can uncomment this to check your timing
            /*if (naildriver)
            Main.NewText("naildriver: " + (starburstPerfectTime - starburstTimer), Color.DarkOrchid);
            if (scattershot)
            Main.NewText("scattershot: " + (starburstPerfectTime - starburstTimer), Color.Lime);*/

            if (naildriver && gunPower < 3)
                gunPower++;
            if (scattershot || (!naildriver && !scattershot))
                gunPower = 1;

            recoilIntensity = (naildriver ? 70f : scattershot ? 25f : 0);
            setVel = true;
        }
        public void FireStarburst()
        {
            Owner.SetScreenshake(8f);
            recoilDirection = -Projectile.velocity;
            if (recoilIntensity < 19)
                recoilIntensity = 19;
            if (recoilTimerMax < extendedCooldown)
                recoilTimerMax = extendedCooldown;
            if (starburstCooldown < extendedCooldown)
                starburstCooldown = extendedCooldown;
            setVel = true;

            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 blastCenter = GunTipPosition + Projectile.velocity * 10;
                float blastSize = 140 + 15 * lastGunPower;
                float minMultiplier = 0.1f;
                int hitsToMinMult = 8;
                int damage = (int)(Projectile.damage * (1 + lastGunPower * 0.5f));
                Projectile blast = Projectile.NewProjectileDirect(Owner.GetSource_FromThis(), blastCenter, Vector2.Zero, ProjectileType<BasicBurst>(), damage, -45, Owner.whoAmI, blastSize, minMultiplier, hitsToMinMult);
                blast.timeLeft = 15;
            }

            float gunPowerMult = MathHelper.Lerp(lastGunPower, 1, 0.7f);
            for (int i = 0; i < 20; i++)
            {
                float dist = Main.rand.NextFloat(1, 4);
                Particle forwardJet = new CustomSpark(GunTipPosition + Main.rand.NextVector2CircularEdge(dist * 3.5f, dist * 3.5f), Projectile.velocity * Main.rand.NextFloat(5, 6) * (8 - dist * 2) * gunPowerMult, "CalamityMod/Particles/ForwardSmear", false, (int)(Main.rand.Next(9, 15 + 1) + (dist * 3)), Main.rand.NextFloat(0.15f, 0.25f) * gunPowerMult, GetRandomColor(), new Vector2(1f, 1f), shrinkSpeed: 0.3f);
                GeneralParticleHandler.SpawnParticle(forwardJet);
            }
            for (int i = 0; i < 34; i++)
            {
                float rot = Main.rand.NextFloat(0.05f, 0.35f) * (Main.rand.NextBool() ? -1 : 1);
                Vector2 startVel = Projectile.velocity.RotatedBy(rot) * Main.rand.NextFloat(8, 18) * (Main.rand.NextBool(4) ? 2f : 1);
                Particle stars = new VelChangingSpark(GunTipPosition, startVel * gunPowerMult, startVel.RotatedBy(rot * 5) * gunPowerMult, "CalamityMod/Particles/PulseStar", Main.rand.Next(25, 45 + 1), Main.rand.NextFloat(0.1f, 0.35f) * gunPowerMult, GetRandomColor(), new Vector2(1f, 1f), shrinkSpeed: Main.rand.NextFloat(0.02f, 0.06f), lerpRate: 0.02f, glowCenter: true);
                GeneralParticleHandler.SpawnParticle(stars);
            }
            if (lastGunPower == 3)
            {
                int parts2 = 45;
                for (int i = 0; i < parts2; i++)
                {
                    Vector2 intenededVel = (MathHelper.TwoPi * i / parts2).ToRotationVector2() * 3f;
                    Vector2 fxVel = new Vector2(intenededVel.X, intenededVel.Y * 2.3f).RotatedBy(Projectile.velocity.ToRotation());
                    Vector2 fxVelEnd = new Vector2(intenededVel.X * 0.5f, intenededVel.Y * 6f).RotatedBy(Projectile.velocity.ToRotation());
                    Vector2 fxPlace = GunTipPosition + Projectile.velocity * 45 + fxVel.RotatedBy(Projectile.velocity.ToRotation());

                    float size = Utils.GetLerpValue(0, -3, intenededVel.X, true);
                    float width = Utils.GetLerpValue(0, 3 * Math.Sign(fxVel.X), fxVel.X, true);
                    Color clr = (size <= 0.5f ? Color.Lerp(c3, c2, size * 2) : Color.Lerp(c2, c1, size * 2 - 1f));

                    Particle aura = new CustomSpark(fxPlace, fxVel * 1.2f, "CalamityMod/Particles/BloomCircle", false, (int)(15 + size * 5), 0.35f + size * 0.2f, clr * 0.7f, new Vector2(1f + width * size, 1f), glowCenter: true, glowOpacity: size * 0.85f, glowCenterScale: 0.75f);
                    GeneralParticleHandler.SpawnParticle(aura);

                }
            }
            if (lastGunPower >= 2)
            {
                int parts = 85;
                for (int i = 0; i < parts; i++)
                {
                    Vector2 intenededVel = (MathHelper.TwoPi * i / parts).ToRotationVector2() * 5f;
                    Vector2 fxVel = new Vector2(intenededVel.X, intenededVel.Y * 2.3f).RotatedBy(Projectile.velocity.ToRotation());
                    Vector2 fxVelEnd = new Vector2(intenededVel.X * 0.5f, intenededVel.Y * 6f).RotatedBy(Projectile.velocity.ToRotation());
                    Vector2 fxPlace = GunTipPosition + fxVel.RotatedBy(Projectile.velocity.ToRotation());

                    float size = Utils.GetLerpValue(0, -5, intenededVel.X, true);
                    float width = Utils.GetLerpValue(0, 5 * Math.Sign(fxVel.X), fxVel.X, true);
                    Color clr = (size <= 0.5f ? Color.Lerp(c3, c2, size * 2) : Color.Lerp(c2, c1, size * 2 - 1f));

                    Particle aura = new CustomSpark(fxPlace, fxVel * 1.2f, "CalamityMod/Particles/BloomCircle", false, (int)(19 + size * 6), 0.45f + size * 0.23f, clr * 0.7f, new Vector2(1f + width * size, 1f), glowCenter: true, glowOpacity: size * 0.85f, glowCenterScale: 0.75f);
                    GeneralParticleHandler.SpawnParticle(aura);

                }
            }
            else
            {
                int parts = 70;
                for (int i = 0; i < parts; i++)
                {
                    Vector2 intenededVel = (MathHelper.TwoPi * i / parts).ToRotationVector2() * 4f;
                    Vector2 fxVel = new Vector2(intenededVel.X, intenededVel.Y * 2.3f).RotatedBy(Projectile.velocity.ToRotation());
                    Vector2 fxVelEnd = new Vector2(intenededVel.X * 0.5f, intenededVel.Y * 6f).RotatedBy(Projectile.velocity.ToRotation());
                    Vector2 fxPlace = GunTipPosition + fxVel.RotatedBy(Projectile.velocity.ToRotation());

                    float size = Utils.GetLerpValue(0, -4, intenededVel.X, true);
                    float width = Utils.GetLerpValue(0, 4 * Math.Sign(fxVel.X), fxVel.X, true);
                    Color clr = (size <= 0.5f ? Color.Lerp(c3, c2, size * 2) : Color.Lerp(c2, c1, size * 2 - 1f));

                    Particle aura = new CustomSpark(fxPlace, fxVel * 1.2f, "CalamityMod/Particles/BloomCircle", false, (int)(18 + size * 5), 0.35f + size * 0.23f, clr * 0.7f, new Vector2(1f + width * size, 1f), glowCenter: true, glowOpacity: size * 0.85f, glowCenterScale: 0.75f);
                    GeneralParticleHandler.SpawnParticle(aura);

                }
            }
        }
        public Color GetRandomColor()
        {
            Color useColor = Main.rand.Next(4) switch
            {
                0 => c1,
                1 => c2,
                _ => c3,
            };
            return useColor;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (time < 2)
                return false;

            gunBackPosition = Projectile.Center - Projectile.velocity * 38f + Projectile.velocity.RotatedBy(MathHelper.PiOver2 * Projectile.direction) * -2;

            Texture2D orb = Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D shine = Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;

            Texture2D front = Request<Texture2D>("CalamityMod/Projectiles/Ranged/StarmadaFront").Value;
            Texture2D frontGlow = Request<Texture2D>("CalamityMod/Projectiles/Ranged/StarmadaFrontGlow").Value;
            Texture2D back = Request<Texture2D>("CalamityMod/Projectiles/Ranged/StarmadaBack").Value;
            Texture2D backGlow = Request<Texture2D>("CalamityMod/Projectiles/Ranged/StarmadaBackGlow").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Main.rand.NextVector2Circular(8 * shake, 8 * shake);
            Color drawColor = Projectile.GetAlpha(lightColor);
            float drawRotation = Projectile.rotation + (Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f);
            SpriteEffects flipSprite = (Projectile.spriteDirection * Owner.gravDir == -1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            float glowMult = (float)Math.Pow(Utils.GetLerpValue(recoilTimerMax / 2, recoilTimerMax, Math.Max(shootingCooldown, starburstCooldown), true), 4);
            int draws = 14 + 4 * gunPower;
            float sine = (float)Math.Sin(time * 0.02f);
            float sine2 = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 55.5f / MathHelper.Pi);
            float fastSine = (float)Math.Sin(time * 0.2f);
            Color glowColor = Color.Lerp(Color.Gray * 0.15f, shiftColor with { A = 0 }, glowIntensity) * (0.1f * gunPower + 0.5f * glowMult);
            Vector2 frontRecoilPlace = Projectile.velocity * frontRecoil;

            if ((starburstTimer > 0 && starburstCooldown == 0) || gunPower > 1)
            {
                for (int i = 0; i < draws; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / draws).ToRotationVector2().RotatedBy(time * 2);
                    Main.EntitySpriteDraw(back, drawPosition + drawOffset * (6 + gunPower) * attackVisualMult, null, shiftColor with { A = 0 } * 0.7f * attackVisualMult, drawRotation, back.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
                    Main.EntitySpriteDraw(front, drawPosition + drawOffset * (6 + gunPower) * attackVisualMult + frontRecoilPlace, null, shiftColor with { A = 0 } * 0.7f * attackVisualMult, drawRotation, front.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
                }
            }

            // back
            Main.EntitySpriteDraw(back, drawPosition, null, drawColor, drawRotation, back.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
            for (int i = 0; i < draws; i++) // back glow
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / draws).ToRotationVector2().RotatedBy(time / 5) * (1.25f + (fastSine + 2f) * 0.2f + glowMult * 4) * MathHelper.Lerp(gunPower, 1, 0.75f);
                Main.EntitySpriteDraw(backGlow, drawPosition + drawOffset, null, glowColor, drawRotation, backGlow.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
                Main.EntitySpriteDraw(backGlow, drawPosition, null, Color.Lerp(Color.Gray * 0.15f, Color.White with { A = 0 }, glowIntensity), drawRotation, backGlow.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
            }
            // front
            Main.EntitySpriteDraw(front, drawPosition + frontRecoilPlace, null, drawColor, drawRotation, front.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
            for (int i = 0; i < draws; i++) // front glow
            {
                Vector2 drawOffset = (MathHelper.TwoPi * i / draws).ToRotationVector2().RotatedBy(time / 5) * (1.25f + (fastSine + 2f) * 0.2f + glowMult * 4) * MathHelper.Lerp(gunPower, 1, 0.75f);
                Main.EntitySpriteDraw(frontGlow, drawPosition + drawOffset + frontRecoilPlace, null, glowColor, drawRotation, frontGlow.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
                Main.EntitySpriteDraw(frontGlow, drawPosition + frontRecoilPlace, null, Color.Lerp(Color.Gray * 0.15f, Color.White with { A = 0 }, glowIntensity), drawRotation, frontGlow.Size() * 0.5f, Projectile.scale * Owner.gravDir, flipSprite);
            }
            
            if (starburstTimer > 0 && starburstCooldown == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    // The shining orb at the tip
                    Color orbColor = shiftColor with { A = 0 } * 0.5f;
                    Vector2 scale = new Vector2(Math.Abs(sine2 * 0.5f) + 0.1f, 1) * (0.05f + i * 0.01f) * attackVisualMult * Main.rand.NextFloat(0.9f, 1.1f) * 8.5f;
                    Main.EntitySpriteDraw(orb, GunTipPosition - Main.screenPosition, null, orbColor, Main.rand.NextFloat(-5, 5), orb.Size() * 0.5f, scale, SpriteEffects.None);
                }
            }

            Color powerColor = Color.Lerp(shiftColor, (gunPower == 1 ? c1 : gunPower == 2 ? c2 : c3), 0.7f);

            float scaleMod = (0.15f + gunPower * 0.1f) * attackVisualMult;
            float rand = Main.rand.NextFloat(0.7f, 1f);
            for (int i = 0; i < 4; i++)
            {
                for (int y = 0; y < 2; y++)
                    Main.EntitySpriteDraw(orb, gunBackPosition - Main.screenPosition, null, powerColor with { A = 0 }, y == 0 ? MathHelper.PiOver2 : 0, orb.Size() * 0.5f, new Vector2(0.5f, 1f) * scaleMod * 0.6f * rand, SpriteEffects.None);

                Vector2 offset = (MathHelper.TwoPi * i / 4).ToRotationVector2().RotatedBy(Projectile.rotation);
                for (int t = 0; t < 3; t++)
                    Main.EntitySpriteDraw(shine, gunBackPosition - Main.screenPosition, null, powerColor with { A = 0 }, offset.ToRotation(), new Vector2(shine.Width * 0.5f, 0), new Vector2((1f - t * 0.1f) * rand + gunPower * 0.05f, 0.5f + t * 0.15f) * Projectile.scale * Owner.gravDir * scaleMod, SpriteEffects.FlipVertically);
                Main.EntitySpriteDraw(shine, gunBackPosition - Main.screenPosition, null, Color.White with { A = 0 }, offset.ToRotation(), new Vector2(shine.Width * 0.5f, 0), new Vector2(0.5f, 0.75f) * Projectile.scale * Owner.gravDir * scaleMod, SpriteEffects.FlipVertically);
            }

            return false;
        }
    }
}
