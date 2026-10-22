using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.NPCs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BF.FairyDanceSeries
{
    // Adapted from Calamity AerialTrackerLaser: retain its laser-burn behavior,
    // while anchoring the beam to a fairy and using its pink/green/blue color.
    public class BFFairyTrackerLaser : ModProjectile
    {
        private Color BeamColor => (int)Projectile.ai[0] switch
        {
            1 => Color.LimeGreen,
            2 => Color.RoyalBlue,
            _ => Color.HotPink
        };
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public float Time
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }
        public Projectile disk;
        public Vector2 laserStart;
        public Vector2 laserEnd;
        public int lifetime = 15;
        public NPC targeted;
        public float fade => (float)Math.Pow(Utils.GetLerpValue(0, lifetime, Projectile.timeLeft), 3);
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = lifetime;
            Projectile.ArmorPenetration = 10;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player Owner = Main.player[Projectile.owner];
            float targetDist = Vector2.Distance(Owner.Center, Projectile.Center);
            if (Time == 0)
            {
                disk = Projectile.ai[1] >= 0 && Projectile.ai[1] < Main.maxProjectiles ? Main.projectile[(int)Projectile.ai[1]] : null;
                targeted = Projectile.ai[2] >= 0 && Projectile.ai[2] < Main.maxNPCs ? Main.npc[(int)Projectile.ai[2]] : null;
                if (disk != null)
                    laserStart = disk.Center;
                else
                    laserStart = Projectile.Center;
                if (targeted != null)
                    laserEnd = targeted.Center;
                else
                    laserEnd = Projectile.Center;
            }
            if (targeted != null && targeted.active && targeted.life > 0)
            {
                laserEnd = targeted.Center;
                Projectile.Center = targeted.Center;
            }
            
            Time++;
        }
        public override bool? CanHitNPC(NPC target)
        {
            return (Projectile.numHits > 0 ? false : null);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // Handles giving the NPC the laser burn effect
            CalamityGlobalNPC modNPC = target.Calamity();
            if (!modNPC.laserBurnMarked)
            {
                modNPC.laserBurnMarked = true;
                modNPC.laserBurnType = 1;
                modNPC.laserBurnTimer = CalamityGlobalNPC.laserBurnTime;
            }

            modNPC.laserBurnTimer -= modNPC.laserBurnStacks * 2;
            modNPC.laserBurnDamage += (int)(Projectile.damage * 0.2f);

            modNPC.laserBurnStacks++;

            modifiers.SourceDamage *= 0;
            modifiers.FinalDamage.Flat = 0.1f;
            modifiers.HideCombatText();
            
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            // So unless placed here the position of projectile updates is inconsistent and will cause offsets
            if (disk != null && disk.active && disk.owner == Projectile.owner && disk.type == ModContent.ProjectileType<FairyDanceWisp>())
            {
                laserStart = disk.Center;
            }
            else
            {
                disk = null;
            }

            if (laserStart == Vector2.Zero || laserEnd == Vector2.Zero)
                return false;

            Player player = Main.player[Projectile.owner];
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            Texture2D lineTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            float distance = Utils.Distance(laserStart, laserEnd);
            int drawSeperation = 10;
            int startSeperation = drawSeperation;
            Vector2 toPoint = Utils.DirectionTo(laserStart, laserEnd);
            for (int i = startSeperation; i < distance - drawSeperation; i += drawSeperation)
            {
                float completion = MathHelper.Lerp(0.8f, 3f, 1 - (i / distance));
                for (int y = 0; y < 2; y++)
                    Main.EntitySpriteDraw(lineTex, laserStart - Main.screenPosition + toPoint * i, null, (y == 1 ? Color.White : BeamColor) with { A = 0 } * fade, toPoint.ToRotation() + MathHelper.PiOver2, lineTex.Size() * 0.5f, new Vector2((y == 1 ? 0.3f : 1) * MathHelper.Max(fade, 0.3f) * completion, 1.3f) * Projectile.scale * 0.01f, SpriteEffects.None);
            }
            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Main.EntitySpriteDraw(glowTex, laserStart - Main.screenPosition, null, BeamColor with { A = 0 }, Projectile.rotation, glowTex.Size() * 0.5f, 0.3f * fade, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glowTex, laserStart - Main.screenPosition, null, Color.White with { A = 0 } * 0.65f, Projectile.rotation, glowTex.Size() * 0.5f, 0.15f * fade, SpriteEffects.None, 0);

            return false;
        }
    }
}
