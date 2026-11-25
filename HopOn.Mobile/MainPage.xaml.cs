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
        try
        {
            var snap = await _source.GetSnapshotAsync();
            VersionLabel.Text = $"数据源版本：{snap.Version}";
        }
        catch (Exception ex)
        {
            // M1 接真实 HTTP 时这里显示网络隔离/PC 未开机等错误，不假装成功。
            VersionLabel.Text = $"连接失败：{ex.Message}";
        }
    }
}
