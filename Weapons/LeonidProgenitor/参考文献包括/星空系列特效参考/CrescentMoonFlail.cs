using System;
using System.Linq;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.DataStructures;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Summon;
using CalamityMod.Systems.Mechanic;
using Microsoft.Build.Evaluation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Melee
{
    public class CrescentMoonFlail : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public int moonCounter = 6;
        public int burstStage = -1;
        public override void SetDefaults()
        {
            Projectile.width = 50;
            Projectile.height = 50;
            Projectile.friendly = true;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.timeLeft = 6000;
            Projectile.MaxUpdates = 2;
        }


        bool hasFired = false;
        bool hasStarbits = false;
        StarburstEntity starburst1 = null;
        StarburstEntity starburst2 = null;
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            player.heldProj = Projectile.whoAmI;
            player.itemAnimation = 5;
            player.itemTime = 5;
                if (!player.channel && !hasFired)
            {
                Projectile.velocity = player.DirectionTo(player.Calamity().mouseWorld) * 25;
                Projectile.Center = player.Center + Projectile.velocity * 5;
                hasFired = true;
                Projectile.ai[0] = 0;
                hasStarbits = player.Calamity().AvaliableStarburst >= 20;
                if (hasStarbits)
                {
                    var star1 = player.Calamity().StarburstEntities.FirstOrDefault(x => x.AICooldown <= 0 && x.value == 10,null);
                    if (star1 != null)
                    {
                        star1.AICooldown = 1;
                        var star2 = player.Calamity().StarburstEntities.FirstOrDefault(x => x.AICooldown <= 0 && x.value == 10, null);
                        if (star2 != null)
                        {
                            star2.AICooldown = 1;
                            starburst1 = star1;
                            starburst2 = star2;
                        }
                    }
                }
            }
            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = player.DirectionTo(player.Calamity().mouseWorld);
            if (!hasFired)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(0.3f * player.direction).SafeNormalize(Vector2.One) * 50f;
                Projectile.Center = player.Center;
                Projectile.ai[0]++;

                if (player.miscCounter % 13 == 0 && Projectile.FinalExtraUpdate())
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.Zero) * 5f, ModContent.ProjectileType<CrescentMoonProj>(), (int)(Projectile.damage * 0.1f), 0f, Projectile.owner, 0f, 0f);
                if (player.miscCounter % 20 == 0 && Projectile.FinalExtraUpdate())
                {
                    player.Calamity().StratusStarburst++;
                }
            } else
            {
                Projectile.ai[0]++;
                if (Projectile.ai[0] == 30 && burstStage == -1 && hasStarbits && player.Calamity().StratusStarburst >= 20 && player.controlUseItem)
                {
                    burstStage = 10;
                }
                if (Projectile.ai[0] == 40)
                {
                    if (burstStage == -1)
                    {
                        for (var i = 0; i < 6; i++)
                        {
                            int moonDamage = (int)(Projectile.damage * 0.1f);
                            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.TwoPi * (i / 6f + 0.5f)) * 10f, ModContent.ProjectileType<CrescentMoonProj>(), moonDamage, 0f, Projectile.owner, 0f, 0f);
                        }
                    } else
                    {
                        burstStage = 0;
                        SoundEngine.PlaySound(SoundID.DD2_WitherBeastDeath);

                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ScorpiusConstellation>(), (int)(Projectile.damage * 3), 0f, Projectile.owner, 0f, 0f);
                        Projectile.position = Projectile.Center;
                        Projectile.Size *= 7;
                        Projectile.Center = Projectile.position;
                        Projectile.damage *= 3;
                        Projectile.Damage();
                        Projectile.damage = 0;
                        Projectile.position = Projectile.Center;
                        Projectile.Size *= 0.2f;
                        Projectile.Center = Projectile.position;
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.SkyBlue, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-15f, 15f), 0f, 0.25f, 12));
                        GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.DeepSkyBlue, "CalamityMod/Particles/ShatteredExplosion", Vector2.One, Main.rand.NextFloat(-15f, 15f), 0f, 0.2f, 12));
                        for (int i = 0; i < 30; i++)
                        {
                            int dustType = Utils.SelectRandom(Main.rand, new int[]
                            {
                                109,
                                111,
                                132
                            });

                            int dust = Dust.NewDust(Projectile.Center, 0, 0, dustType);
                            Main.dust[dust].noGravity = true;
                            Main.dust[dust].velocity *= 7;
                        }
                        player.Calamity().StratusStarburst -= 20;
                        if (starburst1 != null)
                            player.Calamity().StarburstEntities.Remove(starburst1);

                        if (starburst2 != null)
                            player.Calamity().StarburstEntities.Remove(starburst2);
                    }
                }
                if (burstStage > 0)
                    burstStage--;
                if (Projectile.ai[0] > 20 && Projectile.Distance(player.Center) < 100)
                    Projectile.ai[0] = 501;

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(player.Center) * 35, Projectile.ai[0] * 0.00075f);
                if (Projectile.Distance(player.Center) < 32 && Projectile.ai[0] > 20)
                {
                    Projectile.Kill();
                }
                if (hasStarbits)
                {
                    var rotation = Projectile.DirectionFrom(player.Center).ToRotation() - MathHelper.PiOver2;
                    float distanceMod = MathHelper.SmoothStep(0,1,MathHelper.Min(Projectile.ai[0] / 30f, 1));
                    if (burstStage > 0)
                        distanceMod = (burstStage * 0.1f);
                    float playerLerpMod = MathHelper.Min(Projectile.ai[0] / 40f, 1);
                    if (Projectile.ai[0] <= 40)
                    {
                        if (starburst1 != null)
                        {
                            var goal = Vector2.Lerp(Main.player[Projectile.owner].Center, Projectile.Center, playerLerpMod) + new Vector2(200, 0).RotatedBy(rotation) * distanceMod;
                            starburst1.Velocity = starburst1.Center.DirectionTo(goal) * MathHelper.Min(goal.Distance(starburst1.Center), 64f);
                            starburst1.AICooldown = 2;
                            starburst1.Velocity *= 0.95f;
                        }
                        if (starburst2 != null)
                        {
                            var goal = Vector2.Lerp(Main.player[Projectile.owner].Center, Projectile.Center, playerLerpMod) - new Vector2(200, 0).RotatedBy(rotation) * distanceMod;
                            starburst2.Velocity = starburst2.Center.DirectionTo(goal) * MathHelper.Min(goal.Distance(starburst2.Center), 64f);
                            starburst2.AICooldown = 2;
                            starburst2.Velocity *= 0.95f;
                        }
                    } else if (Projectile.ai[0] <= 45)
                    {
                        if (starburst2 != null)
                            starburst2.Velocity *= 0.8f;
                        if (starburst1 != null)
                            starburst1.Velocity *= 0.8f;
                    }
                }
                player.direction = player.DirectionTo(Projectile.Center + Projectile.velocity).X.DirectionalSign();
            }
            if (Projectile.FinalExtraUpdate() && burstStage != 0 && (Projectile.ai[0] <= 40 || !hasFired))
            {
                GeneralParticleHandler.SpawnParticle(new BloomParticle(Projectile.Center + Projectile.velocity, Vector2.Zero, Color.SkyBlue, 0.45f, 0.45f, 2, false));
            }
            player.SetCompositeArmFront(true,Player.CompositeArmStretchAmount.Full,player.DirectionTo(Projectile.Center + Projectile.velocity).ToRotation() - MathHelper.PiOver2);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.localAI[1] = 4f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var startPos = Main.player[Projectile.owner].Center + Main.player[Projectile.owner].DirectionTo(Projectile.Center) * 20f;
            var endPos = Projectile.Center;
            var rotation = Projectile.DirectionFrom(startPos).ToRotation() - MathHelper.PiOver2;
            var tex = TextureAssets.Projectile[Type].Value;

            for (var i = 1; i < 50; i++)
            {

                Main.EntitySpriteDraw(tex, Vector2.Lerp(startPos,endPos,i/50f) - Main.screenPosition, i % 5 == 2 ? new Rectangle(0, 60, 54, 20) : new Rectangle(0, 86, 54, 18), Color.White, rotation, ( i % 5 == 2 ? new Vector2(54, 20) : new Vector2(54, 18)) * 0.5f, Projectile.scale, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(tex, startPos - Main.screenPosition, new Rectangle(0, 0, 54, 56), Color.White, rotation, new Vector2(54, 56) * 0.5f, Projectile.scale, SpriteEffects.None);
            if (burstStage != 0)
            Main.EntitySpriteDraw(tex,Projectile.Center-Main.screenPosition,new Rectangle(0,108,54,50),Color.White, rotation, new Vector2(54,50) * 0.5f,Projectile.scale,SpriteEffects.None);
            return false;
        }
    }
}
