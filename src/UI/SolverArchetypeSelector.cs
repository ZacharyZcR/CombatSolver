using Godot;

namespace CombatSolver;

internal sealed partial class SolverArchetypeSelector : VBoxContainer
{
    private readonly Label _label;
    private readonly OptionButton _choices = new() { Name = "ArchetypePreference", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private string _character = string.Empty;
    private Action<ArchetypePreference>? _changed;
    private const string Help = "流派倾向温和影响奖励、商店评分与战斗铺垫；保留生存优先和收益限制。自动识别保留多体系评分。仅列出已建模机制。";

    internal SolverArchetypeSelector()
    {
        Name = "CombatSolverArchetypeSelector";
        CustomMinimumSize = new Vector2(250, 0);
        MouseFilter = MouseFilterEnum.Stop;
        _label = SolverUiTokens.CreateLabel(string.Empty, SolverUiTokens.Type.Body, SolverUiTokens.Palette.TextPrimary);
        AddChild(_label);
        AddChild(_choices);
        _choices.ItemSelected += index => _changed?.Invoke((ArchetypePreference)_choices.GetItemId((int)index));
        SolverLocaleRefresh.Bind(this, RefreshText);
    }

    internal void Refresh(string character, ArchetypePreference selected, bool disabled, Action<ArchetypePreference> changed)
    {
        _changed = changed;
        if (_character != character || _choices.ItemCount == 0)
        {
            _character = character;
            _choices.Clear();
            foreach (var value in Enum.GetValues<ArchetypePreference>())
                if (ArchetypeCatalog.Supports(character, value))
                    _choices.AddItem(SolverText.Get(ArchetypeCatalog.Name(value)), (int)value);
        }
        _choices.Select(_choices.GetItemIndex((int)ArchetypeCatalog.Normalize(character, selected)));
        _choices.Disabled = disabled || _choices.ItemCount <= 1;
        RefreshText();
    }

    private void RefreshText()
    {
        _label.Text = SolverText.Get("流派倾向");
        _choices.TooltipText = SolverText.Get(Help);
        for (int i = 0; i < _choices.ItemCount; i++)
            _choices.SetItemText(i, SolverText.Get(ArchetypeCatalog.Name((ArchetypePreference)_choices.GetItemId(i))));
    }
}
