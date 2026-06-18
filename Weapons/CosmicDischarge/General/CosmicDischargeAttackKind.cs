using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.Skies;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    internal enum CosmicDischargeAttackKind
    {
        WhipOver,
        WhipUnder,
        WhipThrust,
        SwordSwingOne,
        SwordSwingTwo,
        SwordFinisher,
        ChainKnifeSingle,
        ChainKnifeScatter,
        ChainKnifeBiteAll,
        QuickDraw
    }
}
