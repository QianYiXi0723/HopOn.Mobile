using HopOn.Mobile.Core;

namespace HopOn.Mobile;

public partial class MainPage : ContentPage
{
    private static readonly Color Muted = Color.FromArgb("#888888");
    private static readonly Color Card = Color.FromArgb("#F2F4F8");
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
                    TextColor = Muted, FontSize = 15 });
                return;
            }
            _snap = snap;
            foreach (var g in snap.Games)
            {
                Content.Add(GameCard(g));
            }
            Content.Add(new Label { Text = "自定义提醒", FontSize = 18, FontAttributes = FontAttributes.Bold });
            if (snap.CustomDailies.Count == 0)
                Content.Add(new Label { Text = "（无）", TextColor = Muted });
            foreach (var d in snap.CustomDailies)
            {
                Content.Add(DailyRow(d));
            }
        }
        catch (Exception ex)
        {
            Content.Add(new Label { Text = $"连接失败：{ex.Message}", TextColor = Muted });
        }
    }

    private View GameCard(GameStatus g)
    {
        var status = g.Finished ? "已完成" : (g.Stale ? "稍旧" : "进行中");
        var box = new VerticalStackLayout { Padding = 14, Spacing = 4, BackgroundColor = Card };
        box.Children.Add(new Label { Text = $"{g.Name}  ·  {status}", FontSize = 16, FontAttributes = FontAttributes.Bold });
        box.Children.Add(new Label { Text = $"{g.StaminaName} {g.Current}/{g.Max}", TextColor = Muted, FontSize = 14 });
        box.Children.Add(new Label { Text = $"日常 {g.DailyDone}/{g.DailyTotal}  {g.DailyProgress}", TextColor = Muted, FontSize = 14 });
        return box;
    }

    private View DailyRow(DailyItem d)
    {
        var row = new Grid { Padding = 12, BackgroundColor = Card };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.Children.Add(new Label { Text = d.Done ? $"{d.Name} ✓" : d.Name,
            VerticalOptions = LayoutOptions.Center });
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
