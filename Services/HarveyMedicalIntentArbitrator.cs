using HarveyOverhaul.Core.Core;
using HarveyOverhaul.Core.Models;
using StardewModdingAPI;
using StardewValley;

namespace HarveyOverhaul.Core.Services;

/// <summary>
/// Единый арбитр медицинских интентов Харви. Injury и Stress пишут интенты сюда, не друг в друга.
/// </summary>
public sealed class HarveyMedicalIntentArbitrator
{
    private readonly IMonitor _monitor;
    private readonly HarveyMedicalConversationSettings _settings;
    private readonly object _lock = new();
    private readonly Dictionary<string, List<HarveyMedicalIntentRegistration>> _intentsByProvider = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Action> _intentPublishers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Action<HarveyMedicalIntentResolution>> _topicAppliers = new(StringComparer.Ordinal);
    private HarveyMedicalIntentResolution? _lastResolution;

    public HarveyMedicalIntentArbitrator(IMonitor monitor, HarveyMedicalConversationSettings settings)
    {
        _monitor = monitor;
        _settings = settings;
    }

    public HarveyMedicalConversationSettings Settings => _settings;

    public void RegisterMedicalIntents(string providerId, IReadOnlyList<HarveyMedicalIntentRegistration> intents)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return;

        lock (_lock)
        {
            _intentsByProvider[providerId] = intents.ToList();
        }
    }

    public void ClearMedicalIntents(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return;

        lock (_lock)
        {
            _intentsByProvider.Remove(providerId);
        }
    }

    public void RegisterIntentPublisher(string providerId, Action publishIntents)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return;

        lock (_lock)
        {
            _intentPublishers[providerId] = publishIntents;
        }
    }

    public void RegisterTopicApplier(string providerId, Action<HarveyMedicalIntentResolution> applyTopics)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return;

        lock (_lock)
        {
            _topicAppliers[providerId] = applyTopics;
        }
    }

    public void UnregisterIntentContributor(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            return;

        lock (_lock)
        {
            _intentPublishers.Remove(providerId);
            _topicAppliers.Remove(providerId);
            _intentsByProvider.Remove(providerId);
        }
    }

    /// <summary>Синхронизировать все интенты, выбрать победителя, применить topics.</summary>
    public HarveyMedicalIntentResolution PrepareHarveyClick(bool logDetails = false)
    {
        Action[] publishers;
        Action<HarveyMedicalIntentResolution>[] appliers;
        lock (_lock)
        {
            publishers = _intentPublishers.Values.ToArray();
            appliers = _topicAppliers.Values.ToArray();
        }

        foreach (var publish in publishers)
        {
            try
            {
                publish();
            }
            catch (Exception ex)
            {
                _monitor.Log($"[HarveyIntent] intent publisher failed: {ex}", LogLevel.Error);
            }
        }

        var resolution = Resolve(logDetails: logDetails);

        foreach (var apply in appliers)
        {
            try
            {
                apply(resolution);
            }
            catch (Exception ex)
            {
                _monitor.Log($"[HarveyIntent] topic applier failed: {ex}", LogLevel.Error);
            }
        }

        return resolution;
    }

    public HarveyMedicalIntentResolution Resolve(HarveyMedicalContext? context = null, bool logDetails = false)
    {
        context ??= HarveyMedicalContextHelper.BuildCurrent();

        List<HarveyMedicalIntentRegistration> all;
        lock (_lock)
        {
            all = _intentsByProvider.Values.SelectMany(v => v).ToList();
        }

        var rejected = new List<HarveyMedicalIntentRejection>();
        var eligible = new List<HarveyMedicalIntentRegistration>();

        foreach (var intent in all)
        {
            if (!IsIntentEligible(intent, context, out string? rejectReason))
            {
                rejected.Add(new HarveyMedicalIntentRejection
                {
                    Intent = intent,
                    Reason = rejectReason ?? "ineligible",
                });
                continue;
            }

            eligible.Add(intent);
        }

        HarveyMedicalIntentRegistration? selected = eligible
            .OrderByDescending(i => i.BasePriority)
            .ThenByDescending(i => i.DangerRank)
            .ThenByDescending(i => i.IsPhaseReady)
            .ThenByDescending(i => i.StateAgeTicks)
            .ThenBy(i => i.ProviderId, StringComparer.Ordinal)
            .FirstOrDefault();

        bool festivalBlocked = context.IsFestival
            && _settings.BlockLongTreatmentDuringFestivals
            && selected != null
            && !selected.AllowDuringFestival
            && !selected.IsEmergency;

        string? festivalDeferTopic = null;
        if (festivalBlocked && selected != null)
        {
            festivalDeferTopic = selected.FestivalDeferTopicKey;
            rejected.Add(new HarveyMedicalIntentRejection
            {
                Intent = selected,
                Reason = "festival: long treatment blocked — defer topic",
            });

            var emergency = eligible
                .Where(i => i.IsEmergency && i.AllowDuringFestival)
                .OrderByDescending(i => i.BasePriority)
                .ThenByDescending(i => i.DangerRank)
                .FirstOrDefault();

            selected = emergency;
        }

        var resolution = new HarveyMedicalIntentResolution
        {
            Selected = selected,
            Rejected = rejected,
            FestivalBlockedLongTreatment = festivalBlocked,
            ActiveFestivalDeferTopicKey = festivalDeferTopic,
            ResolvedAtTick = Context.IsWorldReady ? Game1.ticks : 0,
        };

        _lastResolution = resolution;

        if (logDetails)
            LogResolution(resolution);

        return resolution;
    }

    public HarveyMedicalIntentResolution? GetLastResolution() => _lastResolution;

    public bool IsMedicalIntentActive(string providerId, string topicKey, string stateId)
    {
        var resolution = _lastResolution ?? Resolve(logDetails: false);
        var selected = resolution.Selected;
        if (selected == null)
            return false;

        if (!string.Equals(selected.ProviderId, providerId, StringComparison.Ordinal))
            return false;

        if (!string.Equals(selected.StateId, stateId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (TopicMatchesSelectedIntent(selected, topicKey))
            return true;

        return false;
    }

    private static bool TopicMatchesSelectedIntent(
        HarveyMedicalIntentRegistration selected,
        string topicKey)
    {
        if (string.Equals(selected.TopicKey, topicKey, StringComparison.OrdinalIgnoreCase))
            return true;

        foreach (string alt in selected.AlternativeTopicKeys)
        {
            if (string.Equals(alt, topicKey, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool IsIntentSelectedForProvider(string providerId, string? stateId = null)
    {
        var resolution = _lastResolution ?? Resolve(logDetails: false);
        var selected = resolution.Selected;
        if (selected == null)
            return false;

        if (!string.Equals(selected.ProviderId, providerId, StringComparison.Ordinal))
            return false;

        if (stateId != null
            && !string.Equals(selected.StateId, stateId, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private bool IsIntentEligible(
        HarveyMedicalIntentRegistration intent,
        HarveyMedicalContext context,
        out string? reason)
    {
        reason = null;

        if (intent.RequiresClinic
            && _settings.RequireClinicForSevereInjuries
            && !context.IsAtClinic
            && !context.HarveyIsNearby
            && !context.IsSpouseMedicalContext)
        {
            reason = "requires clinic or Harvey nearby";
            return false;
        }

        if (!intent.AllowOutsideClinic
            && !context.IsAtClinic
            && !context.HarveyIsNearby
            && !context.IsSpouseMedicalContext)
        {
            reason = "outside clinic and Harvey not nearby";
            return false;
        }

        if (context.IsFestival && _settings.BlockLongTreatmentDuringFestivals)
        {
            if (!intent.AllowDuringFestival && !intent.IsEmergency)
            {
                reason = "festival blocks long treatment";
                return false;
            }
        }

        return true;
    }

    private void LogResolution(HarveyMedicalIntentResolution resolution)
    {
        if (resolution.Selected != null)
        {
            var s = resolution.Selected;
            _monitor.Log(
                $"[HarveyIntent] SELECTED provider={s.ProviderId} kind={s.Kind} state={s.StateId} " +
                $"priority={s.BasePriority} topic={s.TopicKey} action={s.ActionKey}",
                LogLevel.Info);
        }
        else
        {
            _monitor.Log("[HarveyIntent] SELECTED none", LogLevel.Info);
        }

        foreach (var reject in resolution.Rejected)
        {
            var i = reject.Intent;
            _monitor.Log(
                $"[HarveyIntent] REJECTED provider={i.ProviderId} kind={i.Kind} state={i.StateId} " +
                $"topic={i.TopicKey}: {reject.Reason}",
                LogLevel.Debug);
        }

        if (resolution.FestivalBlockedLongTreatment)
        {
            _monitor.Log(
                $"[HarveyIntent] FESTIVAL_BLOCK deferTopic={resolution.ActiveFestivalDeferTopicKey ?? "(none)"}",
                LogLevel.Info);
        }
    }
}
