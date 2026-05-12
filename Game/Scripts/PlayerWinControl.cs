using Godot;

namespace TankDestroyer.Scripts;

public partial class PlayerWinControl : Control
{
	[Export] public RichTextLabel Label { get; set; }
	[Export] public Button PlayAgain { get; set; }

	public void Setup(string text, string buttonText)
	{
		Label.Text = text;
		Label.AddThemeFontSizeOverride("normal_font_size", 32);
		Label.AddThemeColorOverride("default_color", new Color("FFD700"));

		PlayAgain.Text = buttonText;
		PlayAgain.AddThemeFontSizeOverride("font_size", 20);
		PlayAgain.CustomMinimumSize = new Vector2(200, 50);
	}

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		Label.FitContent = true;
		Label.AutowrapMode = TextServer.AutowrapMode.Off;
		PlayAgain.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
		PlayAgain.Pressed += ClickPlayAgainButton;

		CallDeferred(MethodName.CenterOnScreen);
	}

	private void CenterOnScreen()
	{
		// Centreer alleen de VerticalWinContainer
		var screenSize = GetViewport().GetVisibleRect().Size;
		var containerSize = GetNode<Control>("VerticalWinContainer").Size;
		GetNode<Control>("VerticalWinContainer").Position = (screenSize - containerSize) / 2;
	}

	private void ClickPlayAgainButton()
	{
		GetParent().QueueFree();
	}
}
