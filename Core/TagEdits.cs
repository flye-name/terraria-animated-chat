using Microsoft.Xna.Framework;
using MonoMod.Cil;
using Terraria;
using Terraria.GameContent.UI.Chat;
using Terraria.ModLoader;
using Terraria.UI;

namespace AnimatedChat.Core;

public class TagEdits : ModSystem
{
	public override void Load()
	{
		IL_ItemTagHandler.ItemSnippet.UniqueDraw += EditItemTag;
		IL_GlyphTagHandler.GlyphSnippet.UniqueDraw += EditGlyphTag;
	}

	void EditItemTag(ILContext il)
	{
		ILCursor c = new(il);

		c.GotoNext(MoveType.After, i => i.MatchCall<ItemSlot>(nameof(ItemSlot.Draw)));
		c.GotoPrev(MoveType.After, i => i.MatchCall<Color>("get_White"));
		c.EmitPop();

		c.EmitLdarg(5); // passed color	
		c.EmitDelegate((Color color) => Color.White.MultiplyRGBA(new Color(color.A, color.A, color.A, color.A)));
	}

	void EditGlyphTag(ILContext il)
	{
		ILCursor c = new(il);

		ILLabel? skippedLabel = null;
		
		c.GotoNext(MoveType.After, i => i.MatchCall<Color>("get_Black"));
		c.GotoNext(MoveType.After, i => i.MatchBrfalse(out skippedLabel));

		c.EmitLdarg(5);
		c.EmitDelegate((Color color) => (color.R != 0 || color.G != 0 || color.B != 0));
		c.EmitBrfalse(skippedLabel);
	}
}