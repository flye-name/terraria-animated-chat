using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using log4net.Core;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Chat;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace AnimatedChat.Core;

public class ChatEdit : ModSystem
{
	public List<float> Progress = new();
	public List<float> Exit = new();
	public int MaxMessages;
	
	public override void Load()
	{
		IL_RemadeChatMonitor.DrawChat += EditDraw;

		On_RemadeChatMonitor.AddNewMessage += OnNew;
		On_RemadeChatMonitor.Update += OnUpdate;
	}

	void EditDraw(ILContext il)
	{
		ILCursor c = new(il);

		ILLabel loopStartLabel = null;
		ILLabel loopEndLabel = null;

		int hoveredSnippetIndex = -1;
		int snippetIndex = -1;
		
		#region replace position, keeping the call but rendering off-screen
		c.GotoNext(MoveType.After, i => i.MatchLdcR4(88f));
		c.GotoNext(MoveType.After, i => i.MatchNewobj<Vector2>());
		c.EmitPop();
		c.EmitDelegate(FakePosition);
		#endregion
		
		#region fade in and out
		c.GotoNext(MoveType.After, i => i.MatchPop());
		c.GotoNext(MoveType.Before, i => i.MatchLdloc(out hoveredSnippetIndex));
		int cur = c.Index;
		c.GotoPrev(MoveType.After, i => i.MatchStloc(out snippetIndex));
		c.Goto(cur);

		c.EmitLdloca(hoveredSnippetIndex);
		c.EmitLdloc(snippetIndex);
		c.EmitLdarg0();
		c.EmitLdfld(typeof(RemadeChatMonitor).GetField(nameof(RemadeChatMonitor._startChatLine), BindingFlags.NonPublic | BindingFlags.Instance));
		c.EmitLdloc3(); // loop iteration / num5
		c.EmitLdloc1(); // message index / num2
		c.EmitDelegate(DrawMessage);
		#endregion
		
		#region invalidate the first loop condition (num5 < _showCount)
		c.GotoNext(MoveType.After, i => i.MatchLdfld<RemadeChatMonitor>(nameof(RemadeChatMonitor._showCount)));
		c.EmitPop();
		c.EmitLdcI4(int.MaxValue);
		#endregion
		
		#region new condition
		c.GotoNext(MoveType.After, i => i.MatchBge(out loopEndLabel));
		c.GotoNext(MoveType.After, i => i.MatchBlt(out loopStartLabel));

		c.GotoLabel(loopStartLabel);

		c.EmitLdarg0();
		c.EmitLdloc3();
		c.EmitLdloc1();
		c.EmitLdarg0();
		c.EmitLdfld(typeof(RemadeChatMonitor).GetField(nameof(RemadeChatMonitor._messages), BindingFlags.NonPublic | BindingFlags.Instance));
		c.EmitLdarg0();
		c.EmitLdfld(typeof(RemadeChatMonitor).GetField(nameof(RemadeChatMonitor._showCount), BindingFlags.NonPublic | BindingFlags.Instance));
		
		c.EmitDelegate(NewWhileCondition);
		
		c.EmitBrfalse(loopEndLabel);
		#endregion
	}

	void DrawMessage(ref int hoveredSnippet, TextSnippet[] snippetWithInversedIndex, int _startChatLine, int loopIteration, int index)
	{
		float opacity = Progress[index] * (1f - Exit[index]);
		
		RemadeChatMonitor monitor = (RemadeChatMonitor)Main.chatMonitor;
		if (!monitor._messages[index].CanBeShownWhenChatIsClosed || Main.drawingPlayerChat)
			opacity = 1f;
		
		DrawMethods.DrawColorCodedStringWithShadow(Main.spriteBatch, FontAssets.MouseText.Value, snippetWithInversedIndex, 
			NewPosition(_startChatLine, loopIteration, index), 0f, Vector2.Zero, Vector2.One, out hoveredSnippet, opacity: opacity);
	}
	bool NewWhileCondition(RemadeChatMonitor self, int loopIteration, int index, List<ChatMessageContainer> _messages, int _showCount)
	{
		bool result = true;

		if (_messages[index].CanBeShownWhenChatIsClosed && Progress[index] < 0.999f && MaxMessages < self._showCount + 6)
			MaxMessages++;
		
		else if (_messages[index].CanBeShownWhenChatIsClosed && Progress[index] > 0.999f && MaxMessages > self._showCount)
			MaxMessages--;
		
		return loopIteration < MaxMessages;
	}
	Vector2 NewPosition(int _startChatLine, int loopIteration, int index) => new(88f - Exit[index] * 44 * (!Main.drawingPlayerChat).ToInt(), Main.screenHeight - 58 - loopIteration * Progress[index] * 21 + 12 * (1f - Progress[index]));

	Vector2 FakePosition() => new(-Main.screenWidth * 3, -Main.screenHeight * 3);
	
	void OnNew(On_RemadeChatMonitor.orig_AddNewMessage orig, RemadeChatMonitor self, string text, Color color, int widthLimitInPixels)
	{
		orig(self, text, color, widthLimitInPixels);
	
		Progress.Insert(0, 0);
		while (Progress.Count > 500)
			Progress.RemoveAt(Progress.Count - 1);
	
		Exit.Insert(0, 0);
		while (Exit.Count > 500)
			Exit.RemoveAt(Exit.Count - 1);
	}

	void OnUpdate(On_RemadeChatMonitor.orig_Update orig, RemadeChatMonitor self)
	{
		orig(self);

		RemadeChatMonitor monitor = (RemadeChatMonitor)Main.chatMonitor;

		if (MaxMessages < monitor._showCount)
			MaxMessages = monitor._showCount;
		
		List<ChatMessageContainer> messages = monitor._messages;

		for (int i = 0; i < messages.Count; i++)
		{
			Progress[i] = MathHelper.Lerp(Progress[i], 1, 0.1f);

			if (Progress[i] > 0.998f)
				Progress[i] = 1f;
			
			if (messages[i]._timeLeft > 50)
				continue;
			
			Exit[i] = MathHelper.Lerp(Exit[i], 1, 0.1f);

			if (Exit[i] > 0.998f)
				Exit[i] = 1f;
		}
	}
}