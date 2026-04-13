using System.IO;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
namespace CalamityMod.Projectiles.Ranged
{
    public class ScorchedEarthRocket : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";
        public int time = 0;
        public bool BonusEffectMode;
        public bool HasHit = false;
        public bool SetLifetime = false;
        public static readonly SoundStyle RocketExplosion = new("CalamityMod/Sounds/Item/AnomalysNanogunMPFBExplosion");
        public ref float RocketID => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 10;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Ranged;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(HasHit);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            HasHit = reader.ReadBoolean();
        }

        public override void AI()
        {
            //Rotation
            Projectile.rotation = Projectile.velocity.ToRotation();
            //If the frame ever goes over 10, return it to 6
            if (Projectile.frame >= Main.projFrames[Type])
            {
                Projectile.frame = 6;
            }
            //For the first 20 frames, the rocket constantly loses velocity and stays on frame 0
            if (time <= 20)
            {
                Projectile.velocity *= 0.9f;
                Projectile.frame = 0; 
                Projectile.frameCounter = 0; 
            }
            //Ensures the prime sound only plays once
            if (time == 24)
            {
                SoundStyle PrimeSound = new("CalamityMod/Sounds/Item/ScorchedEarthShot", 3) { Volume = 0.25f, MaxInstances = 8 };
                SoundEngine.PlaySound(PrimeSound with { Pitch = -0.1f }, Projectile.Center);
            }
            //Be VERY CAREFUL changing this. The rocket quickly gains speed for a very limited amount of time to make sure it doesn't go too fast
            if (time > 24 && time < 34)
            {
                Projectile.velocity *= 1.5f;
            }
            //All of these effects only trigger once the rocket is speeding up
            if (time >= 24)
            {
                //Lighting
                Lighting.AddLight(Projectile.Center, 1f, 0.79f, 0.3f);
                Projectile.frameCounter++;
                if (Projectile.frameCounter >= 5)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;
                    if (Projectile.frame >= Main.projFrames[Type])
                    {
                        Projectile.frame = 6;
                    }
                }
            }
            //After the rocket has reached full speed, give it homing for half a second
            if (time >= 37 && time < 67)
            {
                CalamityUtils.HomeInOnNPC(Projectile, true, 350f, 20f, 6f);
            }
            //Handling Tile Breaking/Liquid Rockets
            BonusEffectMode = Projectile.ai[2] == 2;
            if (BonusEffectMode)
            {
                if (!SetLifetime)
                {
                    Projectile.timeLeft = 60;
                    Projectile.extraUpdates = 0;
                    Projectile.damage = 0;
                    Projectile.alpha = 255;
                    SetLifetime = true;
                }
                if (RocketID == ItemID.RocketII || RocketID == ItemID.RocketIV || RocketID == ItemID.MiniNukeII)
                {
                    var info = new CalamityUtils.RocketBehaviorInfo((int)RocketID);
                    int blastRadius = (int)(Projectile.RocketBehavior(info) * 3f);
                    if (time % 5 == 0)
                    {
                        Projectile.ExplodeTiles((int)(blastRadius * Utils.Remap(time, 60f, 1f, 1f, 0f, true)), info.respectStandardBlastImmunity, info.tilesToCheck, info.wallsToCheck);
                    }
                }
                else
                {
                    Point center = Projectile.Center.ToTileCoordinates();
                    var info = new CalamityUtils.RocketBehaviorInfo((int)RocketID);
                    int blastRadius = Projectile.RocketBehavior(info);
                    if (RocketID == ItemID.DryRocket)
                    {
                        DelegateMethods.f_1 = 10.5f * Utils.Remap(time, 60f, 1f, 1f, 0f, true);
                        if (time == 0)
                        {
                            Utils.PlotTileArea(center.X * blastRadius, center.Y * blastRadius, DelegateMethods.SpreadDry);
                        }
                    }
                    if (RocketID == ItemID.WetRocket)
                    {
                        DelegateMethods.f_1 = 10.5f * Utils.Remap(time, 60f, 1f, 1f, 0f, true);
                        if (time == 0)
                        {
                            Utils.PlotTileArea(center.X * blastRadius, center.Y * blastRadius, DelegateMethods.SpreadWater);
                        }
                    }
                    if (RocketID == ItemID.LavaRocket)
                    {
                        DelegateMethods.f_1 = 10.5f * Utils.Remap(time, 60f, 1f, 1f, 0f, true);
                        if (time == 0)
                        {
                            Utils.PlotTileArea(center.X * blastRadius, center.Y * blastRadius, DelegateMethods.SpreadLava);
                        }
                    }
                    if (RocketID == ItemID.HoneyRocket)
                    {
                        DelegateMethods.f_1 = 10.5f * Utils.Remap(time, 60f, 1f, 1f, 0f, true);
                        if (time == 0)
                        {
                            Utils.PlotTileArea(center.X * blastRadius, center.Y * blastRadius, DelegateMethods.SpreadHoney);
                        }
                    }
                }
            }
            time++;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            HasHit = true;
            Projectile.netUpdate = true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            HasHit = true;
            Projectile.netUpdate = true;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            HasHit = true;
            Projectile.netUpdate = true;
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (HasHit && !BonusEffectMode && Projectile.ai[2] == 0)
            {
                // Visual effects
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                SoundEngine.PlaySound(RocketExplosion with { MaxInstances = 2 }, Projectile.Center);
                for (int i = 0; i < 15; i++)
                {
                    Vector2 randVel = new Vector2(12, 12).RotatedByRandom(100) * Main.rand.NextFloat(0.8f, 1.6f);
                    Particle smoke = new HeavySmokeParticle(Projectile.Center + randVel, randVel, Color.Black, Main.rand.Next(20, 25 + 1), Main.rand.NextFloat(0.9f, 2.3f), 0.7f);
                    GeneralParticleHandler.SpawnParticle(smoke);
                }

                for (int i = 0; i < 2; i++)
                {
                    //Explosion effect
                    Particle blastRing = new CustomPulse(Projectile.Center, Vector2.Zero, Main.rand.NextBool() ? Color.OrangeRed : Color.DarkGoldenrod * 0.8f, "CalamityMod/Particles/ShineExplosion1", Vector2.One, Main.rand.NextFloat(-10, 10), 0f, 0.2f, 20, true, 1.4f);
                    GeneralParticleHandler.SpawnParticle(blastRing);
                    Particle blastRing2 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.OrangeRed, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0f, 0.18f, 20, true, 1f);
                    GeneralParticleHandler.SpawnParticle(blastRing2);
                    Particle blastRing3 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.Red, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10, 10), 1f, 1.8f, 25, true);
                    GeneralParticleHandler.SpawnParticle(blastRing3);
                    Particle blastRing4 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.OrangeRed, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10, 10), 0.5f, 0.8f, 25, true);
                    GeneralParticleHandler.SpawnParticle(blastRing4);
                    Particle blastRing5 = new CustomPulse(Projectile.Center, Vector2.Zero, Color.OrangeRed, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10, 10), 0f, 0.13f, 20, true, 1f);
                    GeneralParticleHandler.SpawnParticle(blastRing5);
                }

                // Below is projectile spawning logic, so it should only run on one instance
                if (Main.myPlayer != Projectile.owner)
                    return;

                bool isClusterRocket = RocketID == ItemID.ClusterRocketI || RocketID == ItemID.ClusterRocketII;
                // If using a rocket with extra effects, spawn an invisible copy that handes the tile breaking/liquid spawning logic
                if (RocketID == ItemID.RocketII || RocketID == ItemID.RocketIV || RocketID == ItemID.MiniNukeII || RocketID == ItemID.DryRocket || RocketID == ItemID.WetRocket || RocketID == ItemID.LavaRocket || RocketID == ItemID.HoneyRocket)
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ScorchedEarthRocket>(), 0, 0f, Projectile.owner, RocketID, 0f, 2f);
                // Create blast
                float blastSize = 300;
                float minMultiplier = 0.25f;
                int hitsToMinMult = 4;
                int debuff1 = BuffID.Daybreak;
                int debuff2 = BuffID.Oiled;
                int debuffTime = 360;
                // The explosion has a different damage scaling depending on which rocket type you have. Left is Cluster Rocket, right is Non-Cluster.
                Projectile blast = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BasicBurst>(), (int)(Projectile.damage * (isClusterRocket ? 0.75f : 1)), Projectile.knockBack, Projectile.owner, blastSize, minMultiplier, hitsToMinMult);
                blast.localAI[0] = debuff1;
                blast.localAI[2] = debuff2;
                blast.localAI[1] = debuffTime;
                blast.timeLeft = 15;
                blast.DamageType = DamageClass.Ranged;
                for (int j = 0; j < (isClusterRocket ? 9 : 5); j++)
                {
                    Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f, 10f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, ModContent.ProjectileType<ScorchedEarthClusterBomb>(), (int)(Projectile.damage * 0.25), Projectile.knockBack * 0.25f, Projectile.owner);
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (time < 1)
                return false;
            Texture2D Texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Rectangle frame = Texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
            Vector2 drawPosition;
            Vector2 origin = frame.Size() * 0.5f;

            drawPosition = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(Texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
