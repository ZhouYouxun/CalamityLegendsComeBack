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
    // Enhanced left-click fired during VentCooldown. On hit spawns 2 diverging SeasSearingSatellites.
    internal sealed class SeasSearingVentShot : ModProjectile, ILocalizedModType
    {
        private static readonly Color VentHead = new(200, 240, 255);
        private static readonly Color VentTail = new(40, 90, 200);

        private int  Stage => (int)Projectile.ai[1];
        private bool orbsSpawned;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 12;
            Projectile.height         = 6;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.penetrate      = 4;
            Projectile.timeLeft       = 480;
            Projectile.extraUpdates   = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 10;
            Projectile.ArmorPenetration     = 28;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                int stage = Stage;
                Projectile.penetrate = stage >= 3 ? 6 : (stage >= 1 ? 5 : 4);
                Projectile.netUpdate = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.20f, 0.32f));

            if (Projectile.localAI[1]++ < 2f) return;

            if (!Main.dedServ && Main.rand.NextBool())
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(4f, 20f),
                    DustID.GemEmerald,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.18f) * Main.rand.NextFloat(0.6f, 2.2f),
                    100,
                    Color.Lerp(VentTail, VentHead, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float dist = Vector2.Distance(Main.player[Projectile.owner].Center, target.Center);
            modifiers.FinalDamage             *= 1f + Utils.GetLerpValue(380f, 1050f, dist, true) * 0.30f;
            modifiers.ScalingArmorPenetration += 0.10f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int stacks = hit.Crit ? 7 : 5;
            if (Stage >= 2) stacks++;
            if (Stage >= 3) stacks++;
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, stacks);
            target.AddBuff(BuffID.Venom, 260);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 260);
            SpawnPressureOrbs(target.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 20, 5f, 6f, 1.1f);
        }

        public override void OnKill(int timeLeft)
        {
            if (!orbsSpawned) SpawnPressureOrbs(Projectile.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 12, 3f, 5f, 0.8f);
        }

        private void SpawnPressureOrbs(Vector2 center)
        {
            if (orbsSpawned || Main.myPlayer != Projectile.owner) return;
            orbsSpawned = true;

            Vector2 dir    = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int     orbDmg = Math.Max(1, (int)(Projectile.damage * 0.45f));
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 orbVel = dir.RotatedBy(MathHelper.ToRadians(35f * i)) * 10f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), center, orbVel,
                    ModContent.ProjectileType<SeasSearingSatellite>(),
                    orbDmg, 1f, Projectile.owner);
            }
        }

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
                Color   c   = Color.Lerp(VentTail, VentHead, t) * (0.06f + t * 0.52f); c.A = 0;
                Main.EntitySpriteDraw(bloom, pos, null, c * 0.80f, Projectile.rotation, bOrig,  new Vector2(0.14f, 0.042f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex,   pos, null, c,          Projectile.rotation, origin, Projectile.scale * (0.85f + t * 0.3f), SpriteEffects.None, 0);
            }
            Color hc = (VentHead with { A = 0 }) * 0.75f;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, hc,          Projectile.rotation, bOrig,  new Vector2(0.18f, 0.052f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex,   Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale * 1.12f, SpriteEffects.None, 0);
            return false;
        }
    }
}
