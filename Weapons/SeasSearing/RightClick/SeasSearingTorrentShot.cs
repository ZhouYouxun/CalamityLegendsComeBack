using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    // Rapid-fire pierce shot during AbyssalRupture. Pierces all; drops acid dust while travelling.
    internal sealed class SeasSearingTorrentShot : ModProjectile, ILocalizedModType
    {
        private static readonly Color TorrentHead = new(255, 180, 60);
        private static readonly Color TorrentTail = new(60, 160, 200);

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 10;
            Projectile.height         = 4;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 380;
            Projectile.extraUpdates   = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 8;
            Projectile.ArmorPenetration     = 22;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.22f, 0.12f, 0.04f));

            if (!Main.dedServ && (int)Projectile.localAI[0]++ % 8 == 0)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.GemEmerald,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.3f, 1.5f),
                    115,
                    Color.Lerp(TorrentTail, TorrentHead, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 0.18f;
            modifiers.DefenseEffectiveness   *= 0.55f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 4);
            target.AddBuff(BuffID.Venom, 200);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 200);
            SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 10, 3.5f, 5f, 0.9f);
        }

        public override void OnKill(int timeLeft) =>
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 8, 2.5f, 4f, 0.75f);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex    = TextureAssets.Projectile[Type].Value;
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   origin = tex.Size() * 0.5f;
            Vector2   bOrig  = bloom.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float   t   = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color   c   = Color.Lerp(TorrentTail, TorrentHead, t) * (0.05f + t * 0.48f); c.A = 0;
                Main.EntitySpriteDraw(bloom, pos, null, c * 0.70f, Projectile.rotation, bOrig,  new Vector2(0.10f, 0.032f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex,   pos, null, c,          Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            Color hc = (TorrentHead with { A = 0 }) * 0.65f;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, hc,          Projectile.rotation, bOrig,  new Vector2(0.14f, 0.04f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex,   Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }
}
