using System;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.UI.Chat;

namespace AnimatedChat.Core;

public class DrawMethods
{
	public static Vector2 DrawColorCodedStringWithShadow(SpriteBatch spriteBatch, DynamicSpriteFont font, TextSnippet[] snippets, Vector2 position, float rotation, Vector2 origin, Vector2 baseScale, out int hoveredSnippet, float maxWidth = -1f, float spread = 2f, float opacity = 1f)
	{
		DrawColorCodedStringShadow(spriteBatch, font, snippets, position, Color.Black, rotation, origin, baseScale, maxWidth, spread, opacity);
		return DrawColorCodedString(spriteBatch, font, snippets, position, Color.White, rotation, origin, baseScale, out hoveredSnippet, maxWidth /*, ignoreColors: true*/, opacity: opacity);
	}
	
	public static void DrawColorCodedStringShadow(SpriteBatch spriteBatch, DynamicSpriteFont font, TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, float maxWidth = -1f, float spread = 2f, float opacity = 1f)
	{
		for (int i = 0; i < ChatManager.ShadowDirections.Length; i++) {
			DrawColorCodedString(spriteBatch, font, snippets, position + ChatManager.ShadowDirections[i] * spread, baseColor, rotation, origin, baseScale, out var _, maxWidth, ignoreColors: true, opacity: opacity);
		}
	}
	
	public static Vector2 DrawColorCodedString(SpriteBatch spriteBatch, DynamicSpriteFont font, TextSnippet[] snippets, Vector2 position, Color baseColor, float rotation, Vector2 origin, Vector2 baseScale, out int hoveredSnippet, float maxWidth, bool ignoreColors = false, float opacity = 1f)
	{
		int num = -1;
		Vector2 vec = new Vector2(Main.mouseX, Main.mouseY);
		Vector2 vector = position;
		Vector2 result = vector;
		float x = font.MeasureString(" ").X;
		Color color = baseColor;
		float num2 = 1f;
		float num3 = 0f;
		for (int i = 0; i < snippets.Length; i++) {
			TextSnippet textSnippet = snippets[i];
			textSnippet.Update();
			if (!ignoreColors)
				color = textSnippet.GetVisibleColor();

			num2 = textSnippet.Scale;

			/*
			if (textSnippet.UniqueDraw(justCheckingString: false, out var size, spriteBatch, vector, color, num2)) {
			*/
			if (textSnippet.UniqueDraw(justCheckingString: false, out Vector2 size, spriteBatch, vector, color * opacity, baseScale.X * num2)) {
				if (vec.Between(vector, vector + size))
					num = i;

				/*
				vector.X += size.X * baseScale.X * num2;
				*/
				vector.X += size.X;

				result.X = Math.Max(result.X, vector.X);
				continue;
			}

			string[] array = textSnippet.Text.Split('\n');
			array = Regex.Split(textSnippet.Text, "(\n)");
			bool flag = true;
			foreach (string text in array) {
				string[] array2 = Regex.Split(text, "( )");
				array2 = text.Split(' ');
				if (text == "\n") {
					vector.Y += (float)font.LineSpacing * num3 * baseScale.Y;
					vector.X = position.X;
					result.Y = Math.Max(result.Y, vector.Y);
					num3 = 0f;
					flag = false;
					continue;
				}

				for (int k = 0; k < array2.Length; k++) {
					if (k != 0)
						vector.X += x * baseScale.X * num2;

					if (maxWidth > 0f) {
						float num4 = font.MeasureString(array2[k]).X * baseScale.X * num2;
						if (vector.X - position.X + num4 > maxWidth) {
							vector.X = position.X;
							vector.Y += (float)font.LineSpacing * num3 * baseScale.Y;
							result.Y = Math.Max(result.Y, vector.Y);
							num3 = 0f;
						}
					}

					if (num3 < num2)
						num3 = num2;

					spriteBatch.DrawString(font, array2[k], vector, color * opacity, rotation, origin, baseScale * textSnippet.Scale * num2, SpriteEffects.None, 0f);
					Vector2 vector2 = font.MeasureString(array2[k]);
					if (vec.Between(vector, vector + vector2))
						num = i;

					vector.X += vector2.X * baseScale.X * num2;
					result.X = Math.Max(result.X, vector.X);
				}

				if (array.Length > 1 && flag) {
					vector.Y += (float)font.LineSpacing * num3 * baseScale.Y;
					vector.X = position.X;
					result.Y = Math.Max(result.Y, vector.Y);
					num3 = 0f;
				}

				flag = true;
			}
		}

		hoveredSnippet = num;
		return result;
	}
}