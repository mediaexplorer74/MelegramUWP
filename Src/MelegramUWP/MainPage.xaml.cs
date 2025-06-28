using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Popups;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace MelegramUWP
{
    public sealed partial class MainPage : Page
    {
        private readonly string MainUri = "https://mp.nnchan.ru/chats.php?theme=6";
        private readonly string Ct = "https://mp.nnchan.ru/contacts.php";
        private readonly string srch = "https://mp.nnchan.ru/chatsearch.php";
        private readonly string sg = "https://mp.nnchan.ru/login.php?logout=2";
        private readonly string settings = "https://mp.nnchan.ru//Html/index.html";
        private bool cookiesRestored = false;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            Browser.NavigationCompleted += Browser_NavigationCompleted;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Восстановление cookie в WebView
            if (!cookiesRestored)
            {
                var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string cookieString = localSettings.Values["mpgram_cookies"] as string;
                if (!string.IsNullOrEmpty(cookieString))
                {
                    var filter = new HttpBaseProtocolFilter();
                    var cookieManager = filter.CookieManager;
                    foreach (var part in cookieString.Split(';'))
                    {
                        var kv = part.Split('=');
                        if (kv.Length == 2)
                        {
                            var cookie = new Windows.Web.Http.HttpCookie(kv[0].Trim(), ".nnchan.ru", "/")
                            {
                                Value = kv[1].Trim()
                            };
                            cookieManager.SetCookie(cookie, false);
                        }
                    }
                    cookiesRestored = true;
                }
            }
            Browser.Navigate(new Uri("https://mp.nnchan.ru/login.php"));
        }

        private async void Browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            // Сохраняем cookie после любого успешного перехода
            if (args.IsSuccess)
            {
                var filter = new HttpBaseProtocolFilter();
                var cookieManager = filter.CookieManager;
                var cookies = cookieManager.GetCookies(new Uri("https://mp.nnchan.ru"));
                string cookieString = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["mpgram_cookies"] = cookieString;

                // CSS-инъекция для увеличения отступов между элементами
                string css = @"
                    .c { margin-bottom: 16px !important; }
                    .ctext { padding-bottom: 8px !important; }
                    .cm { margin-bottom: 8px !important; }
                    .hed, .hb { margin-bottom: 12px !important; }
                ";
                string script = $@"
                    var style = document.createElement('style');
                    style.type = 'text/css';
                    style.innerHTML = `{css}`;
                    document.head.appendChild(style);
                ";
                await sender.InvokeScriptAsync("eval", new[] { script });
            }
        }

        private void Browser_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            _ = new MessageDialog("Melegram Failed to start! Error: MG_NTERR").ShowAsync();
        }

        private void BackApplicationBar_Click(object sender, RoutedEventArgs e)
        {
            if (Browser.CanGoBack)
                Browser.GoBack();
        }

        private void ForwardApplicationBar_Click(object sender, RoutedEventArgs e)
        {
            Browser.Navigate(new Uri(settings));
        }

        private void HomeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Browser.Navigate(new Uri(MainUri));
        }

        private void Contacts_Click(object sender, RoutedEventArgs e)
        {
            Browser.Navigate(new Uri(Ct));
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            Browser.Navigate(new Uri(srch));
        }

        private void sg_Click(object sender, RoutedEventArgs e)
        {
            Browser.Navigate(new Uri(sg));
        }

        private void ab_Click(object sender, RoutedEventArgs e)
        {
            _ = new MessageDialog("Melegram BETA 2.0 Build 20250627").ShowAsync();
        }
    }
}
