namespace HarveyOverhaul.Core.UI;

/// <summary>UI-тексты окна «План Харви». Редактировать здесь.</summary>
internal static class HarveyPanelTexts
{
    public static bool IsInformal => HarveyToneHelper.IsInformal();

    public static string Tone(string formal, string informal) =>
        IsInformal ? informal : formal;

    public static string TalkToHarvey() =>
        Tone("Поговорите с Харви.", "Поговори с Харви.");

    public static string TalkToHarveySoon() =>
        Tone("Лучше не откладывать осмотр.", "Не заставляй его гадать, насколько тебе плохо.");

    public static string AfterAssignmentTalk() =>
        Tone("После этого поговорите с Харви.", "После этого поговори с Харви.");

    public static class Tabs
    {
        public const string Overview = "Обзор";
        public const string Stress = "Стресс";
        public const string Injuries = "Травмы";
        public const string Plan = "План";
        public const string Trust = "Доверие";
    }

    public static class Overview
    {
        public const string CalmHeadline = "Сегодня всё спокойно";
        public const string CalmBody =
            "Харви не видит повода для срочного осмотра. Можно заниматься делами, но без героизма.";

        public const string AssignmentHeadline = "Назначение Харви";
        public const string HarveyWaitingHeadline = "Харви ждёт контрольный разговор";

        public const string InjuryAttention = "Травма требует внимания.";

        public static string CalmAdvice =>
            Tone(
                "Харви наблюдает за состоянием. Небольшие шаги лучше, чем снова довести себя до срыва.",
                "Харви присматривает за тобой. Делай паузы — это тоже часть заботы.");

        public static string AssignmentAdvice =>
            Tone(
                "Харви просит не геройствовать: сначала выполните назначение, потом возвращайтесь к делам.",
                "Харви просит не геройствовать: сначала назначение, потом дела.");

        public static string ReviewAdvice =>
            Tone(
                "Харви ждёт вас — можно зайти в клинику или поговорить, когда он рядом.",
                "Он волнуется за тебя. Зайди к нему, когда сможешь.");

        public static string TrustedAdvice =>
            Tone(
                "Харви рядом, когда нужен. Делайте паузы — это тоже часть заботы.",
                "Он рядом, когда нужен. Не заставляй его гадать, как ты.");
    }

    public static class Stress
    {
        public const string NoAssignment =
            "Сейчас нет срочного назначения. Ниже — что Харви обычно советует при стрессе.";

        public const string ProgressPrefix = "Осталось:";

        public static string StageReadyForTalk => "Пора поговорить с Харви.";
        public static string StageInProgress => "Назначение в процессе";
        public static string StageCompleted => "Назначение выполнено";

        public static string RowActive => "Сейчас";
        public static string RowInactive => "Не беспокоит";
    }

    public static class Plan
    {
        public const string NoPlanTitle = "Плана восстановления нет";
        public const string ActiveTitle = "План Харви";
        public const string StressAssignmentTitle = "Назначение на сегодня";
    }
}
