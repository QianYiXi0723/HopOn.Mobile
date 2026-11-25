using HopOn.Mobile.Core;

namespace HopOn.Mobile;

public partial class MainPage : ContentPage
{
    // 用 SetAppThemeColor 绑定而非构造时按 RequestedTheme 求值——后者在切换系统主题
    // 后不会重画（用户真机实证：系统切深色，卡片仍是浅底）。绑定后主题切换自动更新。
    private static readonly Color LightCard = Color.FromArgb("#F2F4F8");
    private static readonly Color DarkCard = Color.FromArgb("#2A2D31");
    private static readonly Color LightFg = Color.FromArgb("#111111");
    private static readonly Color DarkFg = Color.FromArgb("#F1F3F5");
    private static readonly Color LightMuted = Color.FromArgb("#888888");
    private static readonly Color DarkMuted = Color.FromArgb("#9AA0A6");
    private readonly IHopOnDataSource _source;
    private AppSnapshot? _snap;

    public MainPage(IHopOnDataSource source)
    {
        InitializeComponent();
        _source = source;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Content.Clear();
        try
        {
            var snap = await _source.GetSnapshotAsync();
            if (snap.Version == "unpaired")
            {
                Content.Add(new Label { Text = "还没连接电脑。点右上角「连接电脑」，扫描电脑上的二维码。",
                    FontSize = 15 });
                return;
            }
            _snap = snap;
            foreach (var g in snap.Games)
            {
                Content.Add(GameCard(g));
            }
            Content.Add(new Label { Text = "自定义提醒", FontSize = 18, FontAttributes = FontAttributes.Bold });
            int shown = 0;
            foreach (var d in snap.CustomDailies)
            {
                if (!d.PlannedToday) continue;   // 非周计划日的提醒不显示、也不打卡（口径同 PC）
                Content.Add(DailyRow(d));
                shown++;
            }
            if (shown == 0)
                Content.Add(new Label { Text = "（无）" });
        }
        catch (Exception ex)
        {
            Content.Add(new Label { Text = $"连接失败：{ex.Message}" });
        }
    }

    private View GameCard(GameStatus g)
    {
        var status = g.Finished ? "已完成" : (g.Stale ? "稍旧" : "进行中");
        var box = new VerticalStackLayout { Padding = 14, Spacing = 4 };
        box.SetAppThemeColor(VisualElement.BackgroundColorProperty, LightCard, DarkCard);
        var title = new Label { Text = $"{g.Name}  ·  {status}", FontSize = 16, FontAttributes = FontAttributes.Bold };
        title.SetAppThemeColor(Label.TextColorProperty, LightFg, DarkFg);
        box.Children.Add(title);
        var stam = new Label { Text = $"{g.StaminaName} {g.Current}/{g.Max}", FontSize = 14 };
        stam.SetAppThemeColor(Label.TextColorProperty, LightMuted, DarkMuted);
        box.Children.Add(stam);
        // 标题已显示「已完成」，进度本身也表达了完成度——DailyDone/DailyTotal 是第三个
        // 重复的"完成"维度（用户原话「日常 1/1 是什么意思」），去掉只留实际进度。
        if (!string.IsNullOrWhiteSpace(g.DailyProgress))
        {
            var daily = new Label { Text = $"日常 {g.DailyProgress}", FontSize = 14 };
            daily.SetAppThemeColor(Label.TextColorProperty, LightMuted, DarkMuted);
            box.Children.Add(daily);
        }
        return box;
    }

    private View DailyRow(DailyItem d)
    {
        var row = new Grid { Padding = 12 };
        row.SetAppThemeColor(VisualElement.BackgroundColorProperty, LightCard, DarkCard);
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var name = new Label { Text = d.Done ? $"{d.Name} ✓" : d.Name,
            VerticalOptions = LayoutOptions.Center };
        name.SetAppThemeColor(Label.TextColorProperty, LightFg, DarkFg);
        row.Children.Add(name);
        var btn = new Button { Text = d.Done ? "撤销" : "打卡",
            FontSize = 13, VerticalOptions = LayoutOptions.Center };
        btn.Clicked += async (_, __) =>
        {
            btn.IsEnabled = false;
            var r = await _source.CheckInAsync(d.Id, "");
            btn.IsEnabled = true;
            if (r.Ok) await LoadAsync();
            else await DisplayAlert("打卡失败", r.Message, "知道了");
        };
        Grid.SetColumn(btn, 1);
        row.Children.Add(btn);
        return row;
    }

    private async void OnPairClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new PairPage(_source));
    }
}
