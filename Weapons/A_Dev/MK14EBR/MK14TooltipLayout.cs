using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR;

// Keep each displayed row in a separate tooltip entry, matching vanilla's layout.
// In particular, a multiline replacement ending in '\n' must not reserve an empty row.
public sealed class MK14TooltipLayout : GlobalItem
{
    public override bool AppliesToEntity(Item entity, bool lateInstantiation) =>
        entity.ModItem is NewLegendMK14EBR or NewLegendM14;

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        for (int i = 0; i < tooltips.Count; i++)
        {
            TooltipLine line = tooltips[i];
            string text = line.Text.Replace("\\r\\n", "\n").Replace("\\n", "\n")
                .Replace("\r\n", "\n").Replace('\r', '\n');
            string[] rows = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (rows.Length == 0)
            {
                tooltips.RemoveAt(i--);
                continue;
            }

            line.Text = rows[0];
            for (int row = 1; row < rows.Length; row++)
            {
                var extra = new TooltipLine(Mod, $"{line.Name}_Row{row}", rows[row])
                {
                    IsModifier = line.IsModifier,
                    IsModifierBad = line.IsModifierBad,
                    OverrideColor = line.OverrideColor
                };
                if (!line.Visible)
                    extra.Hide();
                tooltips.Insert(++i, extra);
            }
        }
    }
}
