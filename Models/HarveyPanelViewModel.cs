using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HarveyOverhaul.Core.UI;

namespace HarveyOverhaul.Core.Models;

public sealed class HarveyPanelViewModel : INotifyPropertyChanged
{
    private readonly Dictionary<string, IReadOnlyList<HarveyPanelSectionViewModel>> _sectionsByTab = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _tabTitles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _adviceByTab = new(StringComparer.Ordinal);

    private string _selectedTabKey = nameof(HarveyPanelTab.Overview);
    private string _activeTabTitle = "";
    private string _harveyAdviceText = "";
    private Action? _requestClose;

    public string Title { get; init; } = "План Харви";

    public ObservableCollection<HarveyPanelTabButtonViewModel> Tabs { get; } = new();

    public ObservableCollection<HarveyPanelSectionViewModel> ActiveSections { get; } = new();

    public string SelectedTabKey
    {
        get => _selectedTabKey;
        private set => SetField(ref _selectedTabKey, value);
    }

    public string SelectedTab => SelectedTabKey;

    public string ActiveTabTitle
    {
        get => _activeTabTitle;
        private set => SetField(ref _activeTabTitle, value);
    }

    public string HarveyAdviceText
    {
        get => _harveyAdviceText;
        private set => SetField(ref _harveyAdviceText, value);
    }

    public HandbookViewModel Handbook { get; init; } = new();

    public bool ShowStressHandbook => string.Equals(SelectedTabKey, nameof(HarveyPanelTab.Stress), StringComparison.Ordinal);

    public string OverviewStateLine { get; init; } = "";
    public string OverviewAssignmentLine { get; init; } = "";
    public string OverviewProgressLine { get; init; } = "";
    public string OverviewAfterLine { get; init; } = "";
    public string OverviewStressLine { get; init; } = "";
    public string OverviewInjuriesLine { get; init; } = "";
    public string OverviewAdviceLine { get; init; } = "";

    public string StressAssignmentTitle { get; init; } = "";
    public string StressAssignmentProgress { get; init; } = "";
    public string StressAssignmentObjective { get; init; } = "";
    public string StressAssignmentAfter { get; init; } = "";
    public string StressNoAssignmentLine { get; init; } = "";

    public string InjuriesBody { get; init; } = "";

    public string PlanTitle { get; init; } = "";
    public string PlanBody { get; init; } = "";

    public string TrustLevelLine { get; init; } = "";
    public string TrustDescriptionLine { get; init; } = "";
    public string TrustPermissionsLine { get; init; } = "";
    public string TrustPlaceholder { get; init; } = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ConfigureTabContent(
        IReadOnlyDictionary<string, IReadOnlyList<HarveyPanelSectionViewModel>> sectionsByTab,
        IReadOnlyDictionary<string, string> tabTitles,
        IReadOnlyDictionary<string, string> adviceByTab)
    {
        _sectionsByTab.Clear();
        _tabTitles.Clear();
        _adviceByTab.Clear();

        foreach (var (key, sections) in sectionsByTab)
            _sectionsByTab[key] = sections;

        foreach (var (key, title) in tabTitles)
            _tabTitles[key] = title;

        foreach (var (key, advice) in adviceByTab)
            _adviceByTab[key] = advice;
    }

    public void SetCloseHandler(Action? requestClose)
        => _requestClose = requestClose;

    public bool SelectTab(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        SelectedTabKey = key;

        foreach (var tab in Tabs)
            tab.Active = string.Equals(tab.Key, key, StringComparison.Ordinal);

        RefreshActiveTabContent();
        OnPropertyChanged(nameof(SelectedTab));
        OnPropertyChanged(nameof(ShowStressHandbook));
        return true;
    }

    public bool Close()
    {
        _requestClose?.Invoke();
        return true;
    }

    private void RefreshActiveTabContent()
    {
        ActiveSections.Clear();

        if (_sectionsByTab.TryGetValue(SelectedTabKey, out var sections))
        {
            foreach (var section in sections)
                ActiveSections.Add(section);
        }

        if (ActiveSections.Count == 0)
        {
            ActiveSections.Add(new HarveyPanelSectionViewModel
            {
                Title = HarveyPanelTexts.Overview.CalmHeadline,
                Body = "Данных от модов стресса/травм пока нет.",
            });
        }

        ActiveTabTitle = _tabTitles.TryGetValue(SelectedTabKey, out var tabTitle)
            ? tabTitle
            : SelectedTabKey;

        HarveyAdviceText = _adviceByTab.TryGetValue(SelectedTabKey, out var advice)
            ? advice
            : OverviewAdviceLine;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }
}
