using Godot;
using GodotGameTemplate.Gameplay.Progression.Classes;
using GodotGameTemplate.Gameplay.Session;

namespace GodotGameTemplate.Game.UI.MainMenu;

/// <summary>
/// 主菜单：开始新游戏（选职业）/ 继续 / 退出。
/// </summary>
public partial class MainMenu : Control
{
    private const string TownScenePath = "res://Game/Scenes/Town/Town.tscn";

    private Button _newGameButton = default!;
    private Button _continueButton = default!;
    private VBoxContainer _classBox = default!;
    private GameSession _session = default!;

    public override void _Ready()
    {
        _session = GetNode<GameSession>("/root/GameSession");

        _newGameButton = GetNode<Button>("Center/NewGameButton");
        _continueButton = GetNode<Button>("Center/ContinueButton");
        _classBox = GetNode<VBoxContainer>("Center/ClassBox");

        _continueButton.Disabled = !_session.SaveService.ExistsAtDefaultPath();
        _continueButton.Pressed += OnContinuePressed;
        _newGameButton.Pressed += OnNewGamePressed;
        GetNode<Button>("Center/QuitButton").Pressed += () => GetTree().Quit();
        GetNode<Button>("Center/ClassBox/BarbarianButton").Pressed += () =>
            StartNewGame(ClassDatabase.Barbarian);
        GetNode<Button>("Center/ClassBox/SorcererButton").Pressed += () =>
            StartNewGame(ClassDatabase.Sorcerer);
        GetNode<Button>("Center/ClassBox/RogueButton").Pressed += () =>
            StartNewGame(ClassDatabase.Rogue);
    }

    private void OnNewGamePressed()
    {
        _newGameButton.Visible = false;
        _continueButton.Visible = false;
        _classBox.Visible = true;
    }

    private void StartNewGame(string classId)
    {
        _session.NewGame(classId);
        GetTree().ChangeSceneToFile(TownScenePath);
    }

    private void OnContinuePressed()
    {
        _session.Load();
        GetTree().ChangeSceneToFile(TownScenePath);
    }
}
