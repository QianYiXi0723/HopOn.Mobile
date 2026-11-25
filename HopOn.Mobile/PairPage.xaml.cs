using HopOn.Mobile.Core;
using ZXing.Net.Maui;

namespace HopOn.Mobile;

public partial class PairPage : ContentPage
{
    private readonly PcRemoteDataSource _source; // 具体类型：需要 ApplyPairUrl
    private bool _busy;

    public PairPage(IHopOnDataSource source)
    {
        InitializeComponent();
        _source = (PcRemoteDataSource)source;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Android 6+ 相机是危险权限，ZXing 0.4.0 不会自行申请——必须显式请求，
        // 否则扫码页黑屏无回调（真机验证过的坑）。
        var status = await Permissions.RequestAsync<Permissions.Camera>();
        if (status == PermissionStatus.Granted)
        {
            Scanner.IsDetecting = true;   // 只在页面出现时开检测（生命周期纪律）
        }
        else
        {
            StatusLabel.Text = "需要相机权限才能扫码，请在系统设置里允许";
        }
    }

    protected override void OnDisappearing()
    {
        Scanner.IsDetecting = false;
        base.OnDisappearing();
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_busy || e.Results is null || e.Results.Length == 0) return;
        var value = e.Results[0].Value;
        if (string.IsNullOrWhiteSpace(value)) return;
        _busy = true;
        Scanner.IsDetecting = false;
        StatusLabel.Text = "正在连接…";
        try
        {
            var ok = await _source.ApplyPairUrl(value!);
            if (ok)
            {
                StatusLabel.Text = "连接成功";
                await Navigation.PopAsync();
            }
            else
            {
                StatusLabel.Text = "配对码无效或已过期，请重新扫描";
                _busy = false;
                Scanner.IsDetecting = true;
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"连接失败：{ex.Message}";
            _busy = false;
            Scanner.IsDetecting = true;
        }
    }
}
