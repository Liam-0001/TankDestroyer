using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Godot;
using TankDestroyer.API;
using TankDestroyer.Engine;
using TankDestroyer.Engine.Services.Instantiate;
using TankDestroyer.Scripts;

namespace TankDestroyer;

public partial class UINode : Control
{
	[Export] public Button StartButton { get; set; }
	[Export] public Button PlayButton { get; set; }
	[Export] public Button NextButton { get; set; }
	[Export] public Button PreviousButton { get; set; }
	[Export] public Button PauseButton { get; set; }
	[Export] public OptionButton MapButton { get; set; }
	[Export] public Control PlayersContainer { get; set; }
	[Export] public Control PlayerScoresContainer { get; set; }
	[Export] public GameNode GameNode { get; set; }
	[Export] public HSlider TurnSlider { get; set; }
	[Export] public PackedScene PlayerWinControlScene { get; set; }
	private bool _winShown = false;

	public List<BotSelection> BotSelections { get; set; } = new();

	public Type[] BotTypes { get; set; }
	public World[] Maps { get; set; }

	public int PlayerCount { get; set; }
	private Dictionary<int, Label> _tankHealthMapping = new();
	private readonly ICollectBotService _collectBotService = new CollectBotsService();
	private readonly ICollectMapsService _collectMapsService = new CollectMapsService();


	public override void _Ready()
	{
		ConfigFile file =
			System.Text.Json.JsonSerializer.Deserialize<ConfigFile>(System.IO.File.ReadAllText("config.json"));
		BotTypes = _collectBotService.LoadBots(Path.GetFullPath(file.BotFolder));
		Maps = _collectMapsService.LoadMaps(Path.GetFullPath(file.MapFolder));
		base._Ready();
		MapButton.AddItem("Random generated", 0);
		for (int i = 0; i < Maps.Length; i++)
		{
			MapButton.AddItem(Maps[i].Name, i + 1);
		}

		MapButton.ItemSelected += MapButtonOnItemSelected;
		MapButtonOnItemSelected(0);

		StartButton.Pressed += StartButtonOnPressed;
		PauseButton.Pressed += PauseButtonPressed;
		PlayButton.Pressed += PlayButtonPressed; /*        TurnSlider.MaxValue = _gameRunner.GetTurns().Length;
		TurnSlider.Value = TurnSlider.MaxValue;*/
		GameNode.ChangedTurn += OnGameNodeChangedTurn;

		NextButton.Pressed += NextButtonOnPressed;
		PreviousButton.Pressed += PreviousButtonOnPressed;

		TurnSlider.ValueChanged += TurnSliderOnValueChanged;
	}


	private void TurnSliderOnValueChanged(double value)
	{
		if (GameNode.IsPlaying())
		{
			return;
		}

		GameNode.SetStepIndex((int)value);
	}

	private void PreviousButtonOnPressed()
	{
		if (GameNode.IsPlaying())
		{
			return;
		}

		GameNode.StepBack();
	}

	private void NextButtonOnPressed()
	{
		if (GameNode.IsPlaying())
		{
			return;
		}

		GameNode.DoTurn();
	}

	private void PauseButtonPressed()
	{
		GameNode.PausePlay();
	}

	private void OnGameNodeChangedTurn()
	{
		TurnSlider.MaxValue = GameNode.GetRunner().GetTurns().Length - 1;
		TurnSlider.Value = GameNode.GetCurrentTurnIndex();
		OnGameFinished();
	}

	private void OnGameFinished()
	{
		if (!GameNode.GetRunner().Finished) return;
		if (_winShown) return;
		_winShown = true;

		var tank = GameNode.GetPlayers()
			.Select(p=> p.Tank)
			.FirstOrDefault(t=>!t.Destroyed);
		var text = "We got a Tie!";
		if (tank != null) text = GameNode.GetRunner().GetWinnerText(tank);

		var canvasLayer = new CanvasLayer();
		GetTree().Root.AddChild(canvasLayer);

		var scene = PlayerWinControlScene.Instantiate();
		canvasLayer.AddChild(scene);

		var control = scene as PlayerWinControl;
		GD.Print("Control cast: " + (control != null ? "OK" : "NULL")); // debug
	
		control?.Setup(text,"Play again");
	}

	private void PlayButtonPressed()
	{
		GameNode.StartPlay();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
		if (!GameNode.HasGame || _tankHealthMapping.Keys.Count <= 0)
		{
			return;
		}

		var players = GameNode.GetPlayers();
		for (int i = 0; i < players.Length; i++)
		{
			var player = players[i];
			_tankHealthMapping[player.Tank.OwnerId]?.Text = player.Tank.Health + "%";
		}
	}

	private void StartButtonOnPressed()
	{
		_tankHealthMapping.Clear();
		List<IPlayerBot> bots = new();

		for (int i = 0; i < PlayerCount; i++)
		{
			if (BotSelections[i].SelectedBot == null)
			{
				continue;
			}

			bots.Add((IPlayerBot)Activator.CreateInstance(BotSelections[i].SelectedBot));
		}

		World world = null;
		if (MapButton.Selected == 0)
		{
			world = World.GenerateRandom(20, 20);
		}
		else
		{
			world = Maps[MapButton.Selected - 1];
		}

		var runner = new GameRunner(world, bots.ToArray());
		GameNode.StartGame(runner);

		PlayerScoresContainer.ClearChilderen();
		foreach (var player in GameNode.GetPlayers())
		{
			var hexColor = player.Color;

			var nameLabel = new Label()
			{
				Text = player.Name,
			};

			var creatorLabel = new Label()
			{
				Text = player.Creator
			};

			var healthLabel = new Label()
			{
				Text = player.Tank.Health + "%"
			};

			var color = Color.FromHtml(hexColor);

			nameLabel.AddThemeColorOverride("font_color", color);
			creatorLabel.AddThemeColorOverride("font_color", color);
			healthLabel.AddThemeColorOverride("font_color", color);

			PlayerScoresContainer.AddChild(nameLabel);
			PlayerScoresContainer.AddChild(creatorLabel);
			PlayerScoresContainer.AddChild(healthLabel);
			_tankHealthMapping.Add(player.Tank.OwnerId, healthLabel);
			_winShown = false;
		}
	}

	private void MapButtonOnItemSelected(long index)
	{
		if (index == 0)
		{
			PlayerCount = 2;
		}
		else
		{
			PlayerCount = Maps[index - 1].SpawnPoints.Length;
		}

		while (BotSelections.Count < PlayerCount)
		{
			BotSelections.Add(new BotSelection());
		}

		InitializeBotSelection();
	}

	private void InitializeBotSelection()
	{
		PlayersContainer.ClearChilderen();
		for (int i = 0; i < PlayerCount; i++)
		{
			var botSelection = BotSelections[i];
			var playerIndex = new Label() { Text = (i + 1).ToString() };

			PlayersContainer.AddChild(playerIndex);
			OptionButton optionButton = new();
			optionButton.AddItem("<geen>");

			if (botSelection.SelectedBot == null)
			{
				optionButton.AddThemeColorOverride("font_color", Colors.White);
			}
			else
			{
				var hexColor = botSelection.SelectedBot.GetCustomAttribute<BotAttribute>()?.Color;
				optionButton.AddThemeColorOverride("font_color", Color.FromHtml(hexColor));
			}

			foreach (var botType in BotTypes)
			{
				var name = botType.GetCustomAttribute<BotAttribute>()?.Name;
				var creator = botType.GetCustomAttribute<BotAttribute>()?.Creator;

				var label = $"{name} ({creator})";
				optionButton.AddItem(label);
			}

			optionButton.ItemSelected += index =>
			{
				if (index == 0)
				{
					botSelection.SelectedBot = null;
					optionButton.AddThemeColorOverride("font_color", Colors.White);
					return;
				}

				botSelection.SelectedBot = BotTypes[index - 1];
				var hexColor = botSelection.SelectedBot.GetCustomAttribute<BotAttribute>()?.Color;
				optionButton.AddThemeColorOverride("font_color", Color.FromHtml(hexColor));
			};

			if (botSelection.SelectedBot == null)
			{
				optionButton.Selected = 0;
			}
			else
			{
				optionButton.Selected = BotTypes.IndexOf(botSelection.SelectedBot) + 1;
			}

			PlayersContainer.AddChild(optionButton);
		}
	}
}
