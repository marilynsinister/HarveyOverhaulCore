using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using HarveyOverhaul.Core.Models;
using HarveyOverhaul.Core.Services;
using HarveyOverhaul.Core.UI;
using StardewUI.Framework.Dom;
using StardewUI.Framework.Grammar;

internal static class Program
{
    private static int Main(string[] args)
    {
        string workspace = Path.GetFullPath(args.Length > 0 ? args[0] : @"..\..\..");
        string game = Environment.GetEnvironmentVariable("STARDEW_GAME_PATH")
            ?? @"D:\Games\Steam\steamapps\common\Stardew Valley";
        AssemblyLoadContext.Default.Resolving += (_, name) =>
        {
            foreach (string dir in new[] { game, Path.Combine(game, "Mods", "StardewUI"), Path.Combine(game, "smapi-internal") })
            {
                string path = Path.Combine(dir, name.Name + ".dll");
                if (File.Exists(path)) return AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            }
            return null;
        };
        try { Run(workspace); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Run(string workspace)
    {
        int assertions = 0;
        void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }
        HarveyFamilyChecks.Run(Check);
        HarveyRuntimeChecks.Run(Check);
        HarveyOutingsChecks.Run(Check);
        HarveyOutingMapChecks.Run(workspace, Check);
        HarveyCareDirective Directive(string id, string type, string state = HarveyCareDirectiveState.Active,
            bool canFail = false, string priority = HarveyCareDirectivePriority.Normal) => new()
        {
            Id = id, Type = type, State = state, Title = id, CanFailDay = canFail, Priority = priority,
        };
        HarveyPlanDashboardViewModel Dashboard(params HarveyCareDirective[] directives) =>
            HarveyPlanDashboardBuilder.Build(new() { AllDirectives = directives });

        var empty = Dashboard();
        Check(!empty.HasProgress && !empty.HasMetrics && !empty.HasNotice, "An empty plan must not invent progress or alerts.");
        var done = Directive("bandage", HarveyCareDirectiveType.ImmediateAction, HarveyCareDirectiveState.Done);
        var visit = Directive("visit", HarveyCareDirectiveType.Appointment);
        var rule = Directive("sleep", HarveyCareDirectiveType.TodayRule);
        var avoid = Directive("mines", HarveyCareDirectiveType.Avoid);
        var mixed = Dashboard(done, visit, rule, avoid);
        Check(mixed.Metrics[0].Value == "1 / 2" && mixed.ProgressFraction == 0.5f, "All-day rules must not dilute action progress.");
        Check(mixed.Metrics[1].Value == "1" && mixed.Metrics[2].Value == "2", "Remaining actions and rules have independent counts.");
        Check(mixed.MainActionTitle == "visit", "A pending visit must be the next action.");
        var routineOnly = Dashboard(rule, avoid);
        Check(!routineOnly.HasProgress && routineOnly.MainActionTitle == "Соблюдай режим до сна", "Routine-only plans must not show 0/0 or claim everything is done.");
        var duplicate = Dashboard(visit, Directive("VISIT", HarveyCareDirectiveType.Appointment));
        Check(duplicate.Metrics[1].Value == "1", "Provider IDs must be deduplicated without case sensitivity.");
        var completed = Dashboard(done, Directive("sleep", HarveyCareDirectiveType.TodayRule, HarveyCareDirectiveState.Done));
        Check(completed.ProgressFraction == 1 && completed.Metrics[2].Value == "0", "Completed rules must not be counted as active.");
        var informational = Dashboard(Directive("info", HarveyCareDirectiveType.ImmediateAction, HarveyCareDirectiveState.Info));
        Check(!informational.HasProgress && !informational.HasMetrics, "Information must not become an assignment.");
        var failed = Dashboard(Directive("failed", HarveyCareDirectiveType.TodayRule, HarveyCareDirectiveState.Failed));
        Check(failed.HasNotice && !failed.NoticeText.Contains("не засчитается"), "An unrelated failure must not claim the treatment day is lost.");
        var failedDay = Dashboard(Directive("failed", HarveyCareDirectiveType.TodayRule, HarveyCareDirectiveState.Failed, canFail: true));
        Check(failedDay.NoticeText.Contains("не засчитается"), "A treatment-day failure must remain visible.");
        var providerFailure = Dashboard(Directive("reason", HarveyCareDirectiveType.FailureReason, HarveyCareDirectiveState.Failed));
        Check(providerFailure.NoticeText.Contains("не засчитается"), "Injury's explicit FailureReason reports a failed day even without CanFailDay.");
        var urgent = Dashboard(Directive("urgent", HarveyCareDirectiveType.ImmediateAction, priority: HarveyCareDirectivePriority.Critical));
        Check(urgent.StatusText == "Нужно внимание сейчас", "Critical actions must be identified explicitly.");
        var urgentWarning = Dashboard(Directive("urgent warning", HarveyCareDirectiveType.Warning, priority: HarveyCareDirectivePriority.Critical));
        Check(urgentWarning.MainActionTitle == "urgent warning", "Critical warnings must not be followed by a claim that nothing urgent exists.");
        var warning = Dashboard(Directive("warning", HarveyCareDirectiveType.TodayRule, HarveyCareDirectiveState.Warning));
        Check(warning.Metrics[2].Value == "1", "An at-risk rule still applies until the end of the day.");
        var uiBuilder = typeof(HarveyPanelService).Assembly.GetType("HarveyOverhaul.Core.Services.HarveyPlanUiBuilder")!;
        var buildSections = uiBuilder.GetMethod("BuildPlanTabSections", BindingFlags.Public | BindingFlags.Static)!;
        IReadOnlyList<HarveyPanelSectionViewModel> Sections(HarveyPlanSnapshot snapshot) =>
            (IReadOnlyList<HarveyPanelSectionViewModel>)buildSections.Invoke(null,new object[] { snapshot })!;
        var routineSections = Sections(new()
        {
            AllDirectives = new[] { rule }, PrimaryAction = rule, TodayRules = new() { rule },
        });
        Check(routineSections.All(s => s.Headline != "Сначала"), "A bedtime rule must never be presented as something to do immediately.");
        Check(routineSections.Any(s => s.Headline == "Режим на сегодня" && s.Items.Count == 1), "Routine-only plans must retain the rule in the all-day section.");
        var doneSections = Sections(new() { AllDirectives = new[] { done } });
        Check(doneSections.Any(s => s.Headline == "Выполнено сегодня" && s.Collapsible && s.Items.Count == 1), "Completed actions must be available in their own collapsed section.");
        foreach (var (width, height) in new[] { (1920,1080), (1280,720), (960,540), (640,480), (480,360) })
        {
            var size = HarveyPanelLayout.Compute(width,height);
            Check(size.Width <= width && size.Height <= height, $"Panel must fit {width}x{height}.");
        }

        var views = Path.Combine(workspace,"HarveyOverhaulCore","assets","views");
        var assembly = typeof(Document).Assembly;
        var tags = new Dictionary<string,string>
        {
            ["frame"]="Frame", ["lane"]="Lane", ["panel"]="Panel", ["grid"]="Grid", ["image"]="Image",
            ["label"]="Label", ["banner"]="Banner", ["scrollable"]="ScrollableView", ["expander"]="Expander",
        };
        foreach (string file in Directory.GetFiles(views,"*.sml"))
        {
            var document = Document.Parse(File.ReadAllText(file));
            var root = document.Root ?? throw new InvalidOperationException("View has no root: " + file);
            foreach (var node in Walk(root))
            {
                if (node.Tag == "include")
                {
                    string name = node.Attributes.First(a => a.Name == "name").Value;
                    Check(File.Exists(Path.Combine(views,name.Split('/').Last()+".sml")), "Missing include: "+name);
                    continue;
                }
                Check(tags.ContainsKey(node.Tag), "Unknown UI tag: "+node.Tag);
                var type = assembly.GetExportedTypes().Single(t => t.Name == tags[node.Tag]);
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.Name is "if" or "repeat" or "context" or "outlet") continue;
                    string property = string.Concat(attribute.Name.Split('-').Select(p => char.ToUpperInvariant(p[0])+p.Substring(1)));
                    Check(type.GetProperty(property) != null, $"{Path.GetFileName(file)}: {type.Name} has no property {property}.");
                }
            }
            var contexts = new Dictionary<string,Type>
            {
                ["HarveyPanel"] = typeof(HarveyPanelViewModel), ["HarveyAdvice"] = typeof(HarveyPanelViewModel),
                ["PlanSection"] = typeof(HarveyPanelSectionViewModel), ["PlanSectionHeader"] = typeof(HarveyPanelSectionViewModel),
                ["PlanSectionContent"] = typeof(HarveyPanelSectionViewModel), ["PlanDashboard"] = typeof(HarveyPlanDashboardViewModel),
                ["PlanItem"] = typeof(HarveyPlanItemViewModel), ["HandbookCard"] = typeof(HandbookRow),
            };
            CheckBindings(root, new[] { contexts[Path.GetFileNameWithoutExtension(file)] }, file, Check);
            Console.WriteLine("Parsed and checked: "+Path.GetFileName(file));
        }
        Console.WriteLine($"PASS: {assertions} checks against installed StardewUI {assembly.GetName().Version}.");
    }

    private static void CheckBindings(SNode node, IReadOnlyList<Type> parents, string file, Action<bool,string> check)
    {
        var contexts = parents.ToList();
        foreach (var attribute in node.Attributes.Where(a => a.Name is "repeat" or "context"))
        {
            Type value = ResolveBinding(contexts, attribute.Value, attribute.ContextRedirect, file, check);
            if (attribute.Name == "repeat")
                value = value.GetInterfaces().Concat(new[] { value })
                    .First(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>)).GetGenericArguments()[0];
            contexts.Add(value);
        }
        foreach (var attribute in node.Attributes)
        {
            if (attribute.Name is "repeat" or "context") continue;
            if (attribute.ValueType is AttributeValueType.InputBinding or AttributeValueType.TwoWayBinding
                or AttributeValueType.OneTimeBinding or AttributeValueType.OutputBinding)
                ResolveBinding(contexts, attribute.Value, attribute.ContextRedirect, file, check);
        }
        foreach (var evt in node.Element.Events)
        {
            Type context = Redirect(contexts, evt.ContextRedirect);
            check(context.GetMethod(evt.HandlerName) != null, $"{Path.GetFileName(file)}: missing handler {context.Name}.{evt.HandlerName}");
        }
        foreach (var child in node.ChildNodes) CheckBindings(child,contexts,file,check);
    }

    private static Type Redirect(IReadOnlyList<Type> contexts, ContextRedirect? redirect) =>
        contexts[contexts.Count-1-(redirect is ContextRedirect.Distance distance ? checked((int)distance.Depth) : 0)];

    private static Type ResolveBinding(IReadOnlyList<Type> contexts, string path, ContextRedirect? redirect,
        string file, Action<bool,string> check)
    {
        Type current = Redirect(contexts,redirect);
        if (path == "this") return current;
        foreach (string segment in path.Split('.'))
        {
            var property = current.GetProperty(segment);
            check(property != null, $"{Path.GetFileName(file)}: missing binding {current.Name}.{segment}");
            current = property!.PropertyType;
        }
        return current;
    }

    private static IEnumerable<SNode> Walk(SNode node)
    {
        yield return node;
        foreach (var child in node.ChildNodes)
            foreach (var descendant in Walk(child)) yield return descendant;
    }
}
