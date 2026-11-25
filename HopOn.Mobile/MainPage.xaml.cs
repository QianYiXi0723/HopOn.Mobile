using HopOn.Mobile.Core;

namespace HopOn.Mobile;

public partial class MainPage : ContentPage
{
    private readonly IHopOnDataSource _source;

    public MainPage(IHopOnDataSource source)
    {
        InitializeComponent();
        _source = source;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object? sender, EventArgs e)
    {
        var snap = await _source.GetSnapshotAsync();
        VersionLabel.Text = $"数据源版本：{snap.Version}";
    }
}
