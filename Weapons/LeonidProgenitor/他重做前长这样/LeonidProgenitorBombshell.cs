using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.Projectiles.Ranged;
using CalamityMod.Projectiles.Rogue;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.他重做前长这样
{
    public class LeonidProgenitorBombshell : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Rogue";
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/LeonidProgenitor";

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.DamageType = RogueDamageClass.Instance;
        }

        public override void AI()
        {
            int randomDust = Utils.SelectRandom(Main.rand, new int[]
            {
                ModContent.DustType<AstralOrange>(),
                ModContent.DustType<AstralBlue>()
            });
            int astral = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, randomDust, 0f, 0f, 100, CalamityUtils.ColorSwap(LeonidProgenitor.blueColor, LeonidProgenitor.purpleColor, 1f), 0.8f);
            Main.dust[astral].noGravity = true;
            Main.dust[astral].velocity *= 0f;

            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 10f)
            {
                Projectile.ai[0] = 10f;
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    if (Projectile.velocity.X > -0.01f && Projectile.velocity.X < 0.01f)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y += 0.2f / (float)Projectile.MaxUpdates;
            }
            Projectile.rotation += Projectile.velocity.X * 0.1f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Items/Weapons/Rogue/LeonidProgenitorGlow").Value;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, tex.Size() / 2, Projectile.scale, SpriteEffects.None, 0);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item62, Projectile.position);
            if (Main.myPlayer == Projectile.owner)
            {
                int flash = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Flash>(), Projectile.damage, 0f, Projectile.owner, 0f, 1f);
                if (flash.WithinBounds(Main.maxProjectiles))
                    Main.projectile[flash].DamageType = RogueDamageClass.Instance;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
            SpawnExtraProjectiles(target);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<AstralInfectionDebuff>(), 240);
            SpawnExtraProjectiles(target);
        }

        private void SpawnExtraProjectiles(Entity target)
        {
            Vector2 pos = new Vector2(target.Center.X + Main.rand.Next(-201, 201), Main.screenPosition.Y - 600f - Main.rand.Next(50));
            Vector2 meteorVel = CalamityUtils.CalculatePredictiveAimToTargetMaxUpdates(pos, target, 20f, 3);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, meteorVel, ModContent.ProjectileType<LeonidCometBig>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner, 0f, 0.5f + Main.rand.NextFloat() * 0.3f);
        
            if (Projectile.Calamity().stealthStrike)
            {
                for (int i = 0; i < 5; i++)
                {
                    Vector2 cometPos = new Vector2(target.Center.X + Main.rand.Next(-100, 101), target.Center.Y - 150f - Main.rand.Next(30));
                    Vector2 cometVel = CalamityUtils.CalculatePredictiveAimToTargetMaxUpdates(pos, target, 18f, 2);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), cometPos, cometVel, ModContent.ProjectileType<LeonidCometSmall>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, -1f);
                }
            }
        }
    }
}
