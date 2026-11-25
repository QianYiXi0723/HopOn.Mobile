using HopOn.Mobile.Core;

namespace HopOn.Mobile;

public partial class PairPage : ContentPage
{
    private readonly PcRemoteDataSource _source; // 具体类型：需要 SetBaseUrl
    private IReadOnlyList<DiscoveredPc> _pcs = Array.Empty<DiscoveredPc>();
    private DiscoveredPc? _selected;
    private bool _busy;

    public PairPage(IHopOnDataSource source)
    {
        InitializeComponent();
        _source = (PcRemoteDataSource)source;
    }

    private async void OnDiscover(object? sender, EventArgs e)
    {
        if (_busy) return;
        _busy = true;
        BtnDiscover.IsEnabled = false;
        StatusLabel.Text = "正在搜索…";
        try
        {
            _pcs = await PcDiscovery.DiscoverAsync();
            PcList.ItemsSource = _pcs;
            _selected = null;
            StatusLabel.Text = _pcs.Count == 0
                ? "没找到电脑，确认同一 WiFi 且电脑已打开手机连接"
                : $"找到 {_pcs.Count} 台，点选一台后输入 6 位码";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"搜索失败：{ex.Message}";
        }
        finally
        {
            _busy = false;
            BtnDiscover.IsEnabled = true;
        }
    }

    private void OnPcSelected(object? sender, SelectionChangedEventArgs e)
    {
        _selected = e.CurrentSelection.Count > 0 ? e.CurrentSelection[0] as DiscoveredPc : null;
    }

    private async void OnPair(object? sender, EventArgs e)
    {
        if (_busy) return;
        if (_selected is null)
        {
            StatusLabel.Text = "请先搜索并点选一台电脑";
            return;
        }
        var code = CodeEntry.Text?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code))
        {
            StatusLabel.Text = "请输入电脑屏幕上的 6 位码";
            return;
        }
        _busy = true;
        BtnPair.IsEnabled = false;
        StatusLabel.Text = "正在连接…";
        try
        {
            _source.SetBaseUrl($"http://{_selected.Ip}:{_selected.Port}");
            var ok = await _source.RedeemCodeAsync(code);
            if (ok)
            {
                StatusLabel.Text = "连接成功";
                await Navigation.PopAsync();
            }
            else
            {
                StatusLabel.Text = "码不对或已过期，请看电脑屏幕重新输入";
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"连接失败：{ex.Message}";
        }
        finally
        {
            _busy = false;
            BtnPair.IsEnabled = true;
        }
    }
}
