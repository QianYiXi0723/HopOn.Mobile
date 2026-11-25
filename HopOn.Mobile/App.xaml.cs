namespace HopOn.Mobile;

public partial class App : Application
{
    public App(MainPage mainPage)
    {
        InitializeComponent();
        // 必须用 NavigationPage 包一层，否则 MainPage.OnPairClicked 的
        // Navigation.PushAsync 会抛 "PushAsync is not supported" 闪退。
        MainPage = new NavigationPage(mainPage);
    }
}
