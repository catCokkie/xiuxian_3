using Godot;
using System.Collections.Generic;
using Xiuxian.Scripts.Services;

public partial class BookTabsController
{
    private readonly Dictionary<string, ProgressBar> _masteryProgressBars = new();
    private PanelContainer? _returnCard;
    private Label? _returnCardLabel;
    private ScrollContainer? _cultivationScroll;
    public void ShowReturnCard(string title, string body)
    {
        if (_cultivationRoot == null) return;
        if (_returnCard == null)
        {
            _returnCard = new PanelContainer();
            _returnCard.AddThemeStyleboxOverride("panel", PaperStyle());
            VBoxContainer content = new();
            _returnCard.AddChild(content);
            _returnCardLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            content.AddChild(_returnCardLabel);
            Button close = new() { Text = "收起结算" };
            close.Pressed += () => _returnCard.Hide();
            content.AddChild(close);
            _cultivationRoot.AddChild(_returnCard);
            _cultivationRoot.MoveChild(_returnCard, 0);
        }
        _returnCardLabel!.Text = $"{title}\n{body}";
        _returnCard.Show();
    }
}
