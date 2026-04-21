using System;
using System.Collections.Generic;
using System.IO;
using CalamityMod.Buffs.Summon;
using CalamityMod.CalPlayer;
using CalamityMod.DataStructures;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.Particles;
using CalamityMod.Systems.Graphic.PixelationSystem;
using CalamityMod.Systems.Mechanic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using rail;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Summon
{
    public class SiriusMinion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Summon";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public Player Owner => Main.player[Projectile.owner];

        public CalamityPlayer moddedOwner => Owner.Calamity();

        public ref float TimerForShooting => ref Projectile.ai[0];

        public bool CheckForSpawning = false;

        public List<StarburstEntity> starburstsToFire = new();

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionSacrificable[Type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 38;
            Projectile.height = 48;

            Projectile.minionSlots = 1f;
            Projectile.penetrate = -1;

            Projectile.netImportant = true;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
        }

        public int MinionSlotsToAdd
        {
            get { return (int)Projectile.ai[1]; }
            set { Projectile.ai[1] = value; }
        }

        public override void AI()
        {
            NPC target = Projectile.Center.MinionHoming(5000f, Owner); // Constantly tries to find a target.
            #region Add Minion Slots
            if (MinionSlotsToAdd > 0)
            {
                float minionSlotsAvaliable = Owner.maxMinions;
                foreach (var item in Main.ActiveProjectiles)
                {
                    if (item.owner == Projectile.owner)
                        minionSlotsAvaliable -= item.minionSlots;
                }
                while (minionSlotsAvaliable >= 1 && MinionSlotsToAdd > 0)
                {

                    Projectile.minionSlots++;
                    minionSlotsAvaliable--;
                    MinionSlotsToAdd--;
                    Projectile.netUpdate = true;
                }
                MinionSlotsToAdd = 0;
            }
            #endregion
            CheckMinionExistince(); // Checks if the minion can still exist.
            SpawnEffect(); // Does a dust spawn effect.
            ShootTarget(target); // If there's a target, shoot at the target.

            if (target is not null)
            {
                moddedOwner.StarburstSpawnFrameCounter += Projectile.minionSlots / (float)CalamityUtils.SecondsToFrames(3f); //0.33 starbursts per seconds per minion slot
                while (moddedOwner.StarburstSpawnFrameCounter >= 1 && moddedOwner.StratusStarburst <= CalamityPlayer.MaxStratusStarburst)
                {
                    moddedOwner.StratusStarburst++;
                    moddedOwner.StarburstEntities.Add(new StarburstEntity(Projectile.Center));
                    moddedOwner.StarburstSpawnFrameCounter--;
                }
                
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.5f, 1f); // Passively makes blue light.

            // The timer for the minion shooting.
                TimerForShooting++;

            // Makes the star oscillate.
            Projectile.scale = MathHelper.Lerp(0.3f,0.33f, (1+MathF.Sin((Projectile.frameCounter* 0.01f))*0.5f));
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 31415)
                Projectile.frameCounter = 0;
            Projectile.spriteDirection = Owner.direction;


            Projectile.Center = Owner.oldPosition + Owner.Size * 0.5f - new Vector2(64 * Projectile.spriteDirection, 96f - Owner.gfxOffY); // Stays above and behindthe player.
            Projectile.velocity = Owner.velocity * 0f;

            var SiriusPos = Projectile.Center + Projectile.velocity;
            var value = 0f;
            //Count the value of starbursts in Sirius firing animation
            if (Projectile.ai[2] > 2) foreach (var item in starburstsToFire)
            {
                value += item.value;
            }
            var SiriusScale = 0.055f + (0.001f * (moddedOwner.AvaliableStarburst + value));
            void SpawnStar(float SlotRequirement, Vector2 offset, float intensity, int flashOffset = 0, int flashMod = 100)
            {
                if (SlotRequirement > 0 && Projectile.minionSlots < SlotRequirement)
                    return;
                offset.X *= Projectile.spriteDirection;
                var star = new BloomParticle(SiriusPos + offset * Projectile.scale - (Owner.oldVelocity * Math.Clamp(offset.Length() * 0.001f,0,1) ), Vector2.Zero, Color.SlateBlue * ((Owner.miscCounter + flashOffset) % flashMod < 5 ? 0.75f : 1f), 2*SiriusScale * intensity, 2*SiriusScale * intensity, 2, false);
                var star2 = new CustomSpark(SiriusPos + offset * Projectile.scale - (Owner.oldVelocity * Math.Clamp(offset.Length() * 0.001f, 0, 1)), Vector2.UnitX.RotatedBy(MathHelper.Pi * (Owner.miscCounter/300f)) * 0.1f, "CalamityMod/Particles/Sparkle", false, 2, 10*SiriusScale * intensity, Color.SkyBlue, Vector2.One);
                GeneralParticleHandler.SpawnParticle(star,false,Enums.GeneralDrawLayer.AfterProjectiles); 
                GeneralParticleHandler.SpawnParticle(star2, false, Enums.GeneralDrawLayer.AfterProjectiles);
            }
            SpawnStar(0,new Vector2(0f, 0f), 1.5f, 0,300); //Sirius
            SpawnStar(2, new Vector2(-118f, 217f), 0.75f, 40); //bottom
            SpawnStar(3, new Vector2(-67f, 272f), 0.75f, 120); //bakc foot
            SpawnStar(4,new Vector2(119f,32f),0.75f,5); //Front foot
            SpawnStar(5, new Vector2(-192f, 284f), 0.75f, 10); //tail
            SpawnStar(6, new Vector2(-62f, 11f), 0.5f, 75); //neck
            SpawnStar(7, new Vector2(-50f, -103f), 0.5f, 130); //nose
            SpawnStar(8, new Vector2(-101f, -23f), 0.5f, 20); //head
            SpawnStar(9,new Vector2(46f, 59f),0.5f,100); // Front Leg
            SpawnStar(10,new Vector2(-49f, 166f), 0.5f,60); // belly
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.minionSlots);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.minionSlots = reader.ReadSingle();
        }

        #region Methods

        public void CheckMinionExistince()
        {
            Owner.AddBuff(ModContent.BuffType<SiriusBuff>(), 3600);
            if (Projectile.type == ModContent.ProjectileType<SiriusMinion>())
            {
                if (Owner.dead)
                    moddedOwner.sirius = false;
                if (moddedOwner.sirius)
                {
                    Projectile.timeLeft = 2;
                    moddedOwner.StratusStarburstResetTimer = (int)MathHelper.Max(moddedOwner.StratusStarburstResetTimer, 60);
                }
            }
        }

        public void SpawnEffect()
        {
            if (CheckForSpawning == false)
            {
                int dustAmt = 50;
                for (int d = 0; d < dustAmt; d++)
                {
                    float angle = MathHelper.TwoPi / dustAmt * d;
                    Vector2 dustVelocity = angle.ToRotationVector2() * 20f;
                    Dust spawnDust = Dust.NewDustPerfect(Owner.Center - Vector2.UnitY * 60f, DustID.PurificationPowder, dustVelocity);
                    spawnDust.noGravity = true;
                }
                CheckForSpawning = true;
            }
        }

        public void ShootTarget(NPC target)
        {
            if (target is not null)
            {
                float timer = 90f * (10f / (10f + Projectile.minionSlots));
                if (TimerForShooting >= timer && Projectile.owner == Main.myPlayer)
                {
                    TimerForShooting = 0;
                    // Makes a dust effect on the minion, to make a better effect of it shooting.
                    SoundEngine.PlaySound(FrigidflashBolt.UseSound with { Volume = 1f, Pitch = -0.15f }, Projectile.Center);
                    int dustAmt = 50;
                    for (int d = 0; d < dustAmt; d++)
                    {
                        float angle = MathHelper.TwoPi / dustAmt * d;
                        Vector2 dustVelocity = angle.ToRotationVector2() * 20f;
                        Dust spawnDust = Dust.NewDustPerfect(Projectile.Center, DustID.PurificationPowder, dustVelocity);
                        spawnDust.noGravity = true;
                    }

                    // Shoots the beam.
                    for (var i = 0; i < 2; i++)
                    {
                        Vector2 velocity = new Vector2(25, 0).RotatedByRandom(MathHelper.Pi);
                        float damageMod = 1 + MathF.Pow(0.2f * Projectile.minionSlots, 1.5f);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity, velocity, ModContent.ProjectileType<SiriusBeam>(), (int)(Projectile.damage * damageMod), Projectile.knockBack, Projectile.owner);
                    }
                }
                if (moddedOwner.AvaliableStarburst >= CalamityPlayer.MaxStratusStarburst)
                {
                    Projectile.ai[2]++;
                }
                if (Projectile.ai[2] > 0)
                {
                    var value = 0f;
                    //Count the value of starbursts in Sirius firing animation
                    foreach (var item in starburstsToFire)
                    {
                        value += item.value;
                    }
                    //Add starbursts to the animation to equal the shot starburst cost
                    foreach (var star in moddedOwner.StarburstEntities)
                    {
                        if (value >= 50)
                            break;
                        value += star.value;
                        starburstsToFire.Add(star);
                    }
                    //Animate the starbursts in the animation
                    foreach (var star in starburstsToFire)
                    {
                        star.Center = Vector2.Lerp(star.Center, Projectile.Center + Projectile.velocity, Projectile.ai[2] / 15f);
                        star.AICooldown = 2;
                    }
                    Projectile.ai[2]++;
                    if (Projectile.ai[2] > 15) {
                    if (Main.LocalPlayer.whoAmI == Projectile.owner)
                            for (var i = 0; i < 2; i++)
                            {
                                Vector2 velocity = Projectile.Center.DirectionTo(target.Center) * 10;
                                float damageMod = 40;
                                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity, velocity, ModContent.ProjectileType<SiriusQuasar>(), (int)(Projectile.damage * damageMod), Projectile.knockBack, Projectile.owner, 1);
                            }
                        SoundEngine.PlaySound(Exoblade.BeamHitSound, Owner.Center);
                        Particle explosion = new DetailedExplosion(Projectile.Center, Vector2.Zero, Color.SkyBlue, Vector2.One, Main.rand.NextFloat(-5, 5), 0f, 0.65f + 0.1f, Main.rand.Next(15, 22));
                        GeneralParticleHandler.SpawnParticle(explosion);
                        Particle explosion2 = new DetailedExplosion(Projectile.Center, Vector2.Zero, Color.SlateBlue, Vector2.One, Main.rand.NextFloat(-5, 5), 0f, 0.45f + 0.1f, Main.rand.Next(10, 19), false);
                        GeneralParticleHandler.SpawnParticle(explosion2);
                        Particle explosion3 = new DetailedExplosion(Projectile.Center, Vector2.Zero, Color.SlateBlue, Vector2.One, Main.rand.NextFloat(-5, 5), 0f, 0.30f + 0.1f, Main.rand.Next(10, 19), false);
                        GeneralParticleHandler.SpawnParticle(explosion3);

                        for (int i = 0; i < 4; i++)
                        {
                            Particle blastRing = new CustomPulse(Projectile.Center, Vector2.Zero, Color.SkyBlue, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10, 10), 0, 0.5f + 0.05f, 25);
                            GeneralParticleHandler.SpawnParticle(blastRing);
                        }
                        moddedOwner.StratusStarburst -= 50;
                        Projectile.ai[2] = 0;
                        foreach (var item in starburstsToFire)
                        {
                            moddedOwner.StarburstEntities.Remove(item);
                        }
                        starburstsToFire = new();
                    }
                }
            }
        }

        public override Color? GetAlpha(Color lightColor) => new Color(200, 200, 200, 200);

        public override bool PreDraw(ref Color lightColor)
        {
            var SiriusPos = Projectile.Center;
            void ConnectStars(float SlotRequirement, Vector2 point1, Vector2 point2)
            {
                if (SlotRequirement > 0 && Projectile.minionSlots < SlotRequirement)
                    return;
                point1.X *= Projectile.spriteDirection;
                point2.X *= Projectile.spriteDirection;
                var color = Color.SkyBlue * 0.75f * ((MathF.Sin(Main.GlobalTimeWrappedHourly) + 1) * 0.25f + 0.5f);
                CalamityUtils.DrawLineBetter(Main.spriteBatch, SiriusPos+point1 * Projectile.scale - (Owner.oldVelocity * Math.Clamp(point1.Length() * 0.001f, 0, 1)), SiriusPos+point2* Projectile.scale - (Owner.oldVelocity * Math.Clamp(point2.Length() * 0.001f, 0, 1)), color, 2f);
            }
                ConnectStars(4, new Vector2(0f, 0f), new Vector2(119f, 32f)); //Sirius - Front Foot
                ConnectStars(2, new Vector2(0f, 0f), new Vector2(-118f, 217f)); //Sirius - Bottom

                ConnectStars(6, new Vector2(0f, 0f), new Vector2(-62f, 11f)); //Sirius - neck

                ConnectStars(9, new Vector2(119f, 32f), new Vector2(46f, 59f)); // Front Foot - Front Leg
                ConnectStars(10, new Vector2(46f, 59f), new Vector2(-49f, 166f)); // Frong Leg - Belly
                ConnectStars(10, new Vector2(-49f, 166f), new Vector2(-67f, 272f)); // Belly - Back Foot
                ConnectStars(3, new Vector2(-67f, 272f), new Vector2(-118f, 217f)); // Back Foot - Bottom
                ConnectStars(5, new Vector2(-118f, 217f), new Vector2(-192f, 284f)); // Bottom - Tail
                ConnectStars(8, new Vector2(-62f, 11f), new Vector2(-101f, -23f)); // Neck - Head
                ConnectStars(8, new Vector2(-101f, -23f), new Vector2(-50f, -103f)); // Head - Nose
                ConnectStars(7, new Vector2(-50f, -103f), new Vector2(-62f, 11f)); // Nose - Neck
            return false;
        }
        #endregion
    }
}
