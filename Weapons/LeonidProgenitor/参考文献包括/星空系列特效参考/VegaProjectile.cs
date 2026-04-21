using System;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.CalPlayer;
using CalamityMod.Systems.Mechanic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Rogue
{
    public class VegaProjectile : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Rogue";
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/Vega";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.DamageType = RogueDamageClass.Instance;
            Projectile.MaxUpdates = 2;
        }

        private int SplitProjDamage => (int)(Projectile.damage * 0.6f);

        public override void AI()
        {
            if (Projectile.ai[0] == 0f)
                Projectile.rotation = (float)Math.Atan2(Projectile.velocity.Y, Projectile.velocity.X) + MathHelper.ToRadians(45);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {   
            OnHitEffects();
            var owner = Main.player[Projectile.owner];
            if (Projectile.ai[1] > 0)
            {
                foreach (var item in Main.ActiveProjectiles)
                {
                    if (item.type == ModContent.ProjectileType<VegaStar>() && item.owner == Projectile.owner)
                    {
                        item.ai[2] = target.whoAmI + 1;
                        item.timeLeft = Math.Max(300,item.timeLeft);
                        item.penetrate = 1;
                        item.usesIDStaticNPCImmunity = false;
                        item.usesLocalNPCImmunity = true;
                        item.localNPCHitCooldown = 10;
                        item.netUpdate = true;
                    }
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            OnHitEffects();
        }

        private void OnHitEffects()
        {
            int onHitCount = 6;
            float spread = 20f;
            int projectileDamage = SplitProjDamage;
            float kb = 5f;
            int sparkID = ModContent.ProjectileType<VegaSpark>();
            int starID = ModContent.ProjectileType<VegaStar>();
            if (Projectile.Calamity().stealthStrike)
                for (int i = 0; i < 1; i++)
                {
                    int projID = ModContent.ProjectileType<LyraConstellation>();
                        Vector2 velocity = Vector2.Zero;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity, projID, (int)(projectileDamage * 0.8f), kb, Projectile.owner);
                }
            for (int i = 0; i < onHitCount; i++)
            {
                int projID = i % 3 == 0 ? starID : sparkID;
                Vector2 velocity = Projectile.oldVelocity.RotateRandom(MathHelper.ToRadians(spread)) * 0.5f;
                float speed = Main.rand.NextFloat(1.5f, 2f);
                float moveDuration = Main.rand.Next(5, 15);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, velocity * speed, projID, projectileDamage, kb, Projectile.owner, 0f, moveDuration);
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = SoundID.Item62.Volume * 0.6f }, Projectile.position);
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = SoundID.Item68.Volume * 0.2f }, Projectile.position);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = SoundID.Item122.Volume * 0.4f }, Projectile.position);

            for (var i = 0; i < (Projectile.ai[1] == 0 && Projectile.Calamity().stealthStrike ? 3 : 1); i++)
            {
                Main.player[Projectile.owner].Calamity().StratusStarburst++;
                if (Main.player[Projectile.owner].Calamity().StratusStarburst <= CalamityPlayer.MaxStratusStarburst)
                    Main.player[Projectile.owner].Calamity().StarburstEntities.Add(new StarburstEntity(Projectile.Center));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
            Projectile.Kill();
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/VegaGlow").Value;
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustType = Utils.SelectRandom(Main.rand, new int[]
                {
                    109,
                    111,
                    132
                });

                int dust = Dust.NewDust(Projectile.Center, 1, 1, dustType, Projectile.velocity.X / 3f, Projectile.velocity.Y / 3f, 0, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
