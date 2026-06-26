using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingPressureBolt : ModProjectile, ILocalizedModType
    {
        private static readonly Color HeadColor = new(210, 255, 252);
        private static readonly Color TrailColor = new(30, 100, 200);

        private bool Strong => Projectile.ai[0] > 0f;
        private bool satellitesSpawned;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 22;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 18;
            Projectile.height         = 8;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.penetrate      = 3;
            Projectile.timeLeft       = 420;
            Projectile.extraUpdates   = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 12;
            Projectile.ArmorPenetration     = 24;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.penetrate  = Strong ? 5 : 3;
                Projectile.netUpdate  = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            Color glow = Strong ? SeasSearingPalette.PressureBlue : SeasSearingPalette.RadioactiveCyan;
            Lighting.AddLight(Projectile.Center, glow.ToVector3() * 0.42f);

            if (!Main.dedServ && (int)Projectile.localAI[1]++ % 3 == 0)
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - dir * Main.rand.NextFloat(4f, 16f) + Main.rand.NextVector2Circular(2f, 2f),
                    Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                    -dir.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.5f, 2.2f),
                    110,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.55f, 0.95f + (Strong ? 0.15f : 0f)));
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 0.12f;
            if (Strong) modifiers.FinalDamage *= 1.18f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, Strong ? 9 : 5);
            target.AddBuff(BuffID.Venom, 240);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
            SpawnSatellites(target.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 18, 4.5f, 8f, 1.1f);
        }

        public override void OnKill(int timeLeft)
        {
            if (!satellitesSpawned)
                SpawnSatellites(Projectile.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 12, 3.2f, 6f, 0.9f);
            SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 3.5f, 10f, 18, SeasSearingPalette.PressureBlue);
        }

        private void SpawnSatellites(Vector2 center)
        {
            if (satellitesSpawned || Main.myPlayer != Projectile.owner) return;
            satellitesSpawned = true;

            int count  = Strong ? 8 : 5;
            int satDmg = Math.Max(1, Projectile.damage / 3);
            for (int i = 0; i < count; i++)
            {
                float   angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(0.2f);
                Vector2 vel   = angle.ToRotationVector2() * Main.rand.NextFloat(3.5f, 6.5f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center + angle.ToRotationVector2() * 18f, vel,
                    ModContent.ProjectileType<SeasSearingSatellite>(),
                    satDmg, 1.5f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex    = TextureAssets.Projectile[Type].Value;
            Texture2D bloom  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   origin = tex.Size() * 0.5f;
            Vector2   bOrig  = bloom.Size() * 0.5f;
            Color     head   = Strong ? SeasSearingPalette.PressureBlue : HeadColor;
            Color     tail   = TrailColor;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float   t   = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color   c   = Color.Lerp(tail, head, t) * (0.06f + t * 0.55f); c.A = 0;
                Main.EntitySpriteDraw(bloom, pos, null, c * 0.78f, Projectile.rotation, bOrig, new Vector2(0.18f, 0.055f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex,   pos, null, c,          Projectile.rotation, origin, Projectile.scale * (0.9f + t * 0.25f), SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, (head with { A = 0 }) * 0.7f,  Projectile.rotation, bOrig,  new Vector2(0.22f, 0.06f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex,   Projectile.Center - Main.screenPosition, null, Color.White,                    Projectile.rotation, origin, Projectile.scale * 1.15f, SpriteEffects.None, 0);
            return false;
        }
    }
}
