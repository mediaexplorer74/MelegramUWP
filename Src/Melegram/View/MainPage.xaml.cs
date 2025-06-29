using HtmlAgilityPack;
using Melegram.Control;
using Melegram.Helpers;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.Web.Http.Filters;
using UltraTimer;

namespace Melegram.View
{
    public sealed partial class MainPage : Page
    {
        private readonly string MainUri = "https://mp.nnchan.ru/chats.php?theme=6";
        private readonly string Ct = "https://mp.nnchan.ru/contacts.php";
        private readonly string srch = "https://mp.nnchan.ru/chatsearch.php";
        private readonly string sg = "https://mp.nnchan.ru/login.php?logout=2";
        private readonly string settings = "https://mp.nnchan.ru/sets.php";
        //"https://mp.nnchan.ru//Html/index.html";

        private bool cookiesRestored = false;

        public MainPage()
        {
            this.InitializeComponent();
            this.Loaded += MainPage_Loaded;
            Browser.NavigationCompleted += Browser_NavigationCompleted;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            ShowNotifications();

            //TODO: Show Tile Notification...

            try
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Cookies handling bug: " + ex.Message);
            }
            Browser.Navigate(new Uri("https://mp.nnchan.ru/login.php"));
        }

        private async void ShowNotifications()
        {
            try
            {
                // Запрос к странице чатов
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string cookieString = localSettings.Values["mpgram_cookies"] as string ?? "";
                System.Net.Http.HttpClient http = new System.Net.Http.HttpClient();
                if (!string.IsNullOrEmpty(cookieString))
                    http.DefaultRequestHeaders.Add("Cookie", cookieString);
                string response = await http.GetStringAsync("https://mp.nnchan.ru/chats.php?theme=6");

                // Парсим все <b class="unr">+N </b> и суммируем значения
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(response);
                HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//b[contains(@class,'unr')]");
                int notifCount = 0;
                if (nodes != null)
                {
                    foreach (HtmlNode node in nodes)
                    {
                        string text = node.InnerText.Trim();
                        if (text.StartsWith("+") && int.TryParse(text.Substring(1), out int n))
                            notifCount += n;
                    }
                }

                // Получить сохранённое значение (например, из LocalSettings)
                //var 
                localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                object lastCountObj = localSettings.Values["LastNotifCount"];
                int lastCount = lastCountObj is int ? (int)lastCountObj : 0;

                //DEBUG
                if (true)//(notifCount > lastCount)
                {
                    // Показать toast-уведомление
                    string toastXml = $@"<toast><visual><binding template='ToastGeneric'>
                        <text>Новые сообщения</text>
                        <text>У вас {notifCount - lastCount} новых сообщений!</text>
                    </binding></visual></toast>";
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(toastXml);
                    ToastNotification notif = new ToastNotification(xmlDoc);
                    ToastNotificationManager.CreateToastNotifier().Show(notif);
                }

                // Сохраняем новое значение
                localSettings.Values["LastNotifCount"] = notifCount;
            }
            catch (Exception ex)
            {
                // Логировать ошибку при необходимости
                Debug.WriteLine("[ex] ShowNotifications error: " + ex.Message);
            }
        }

        private async void Browser_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            try
            {
                // Сохраняем cookie после любого успешного перехода
                if (args.IsSuccess)
                {
                    var filter = new HttpBaseProtocolFilter();
                    var cookieManager = filter.CookieManager;
                    var cookies = cookieManager.GetCookies(new Uri("https://mp.nnchan.ru"));
                    string cookieString = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
                    Windows.Storage.ApplicationData.Current.LocalSettings.Values["mpgram_cookies"] 
                        = cookieString;

                    // CSS-инъекция для увеличения отступов между элементами
                    /*string css = @"
                    .c { margin-bottom: 16px !important; }
                    .ctext { padding-bottom: 8px !important; }
                    .cm { margin-bottom: 8px !important; }
                    .hed, .hb { margin-bottom: 12px !important; }
                    ";*/
                    string css = @"
                    .c { margin-bottom: 36px !important; }
                    .ctext { padding-bottom: 18px !important; }
                    .cm { margin-bottom: 8px !important; }
                    .hed, .hb { margin-bottom: 42px !important; }
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
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Css enjection bug: " + ex.Message);
            }
            //Browser.Navigate(new Uri("https://mp.nnchan.ru/login.php"));
        }

        private void Browser_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e)
        {
            try
            {
                Windows.Foundation.IAsyncOperation<IUICommand> 
                    r = new MessageDialog("Melegram Failed to start! Error: MG_NTERR").ShowAsync();
            }
            catch { }
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
            Windows.Foundation.IAsyncOperation<IUICommand> r 
                = new MessageDialog("Melegram BETA 2.0 Build 20250627").ShowAsync();
        }

        // Experimental **********************
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ReviewHelper.TryRequestReview();
            AnimateProgressBar();
            if (App.startupHelper.shouldAskForTilePinning)
            {
                try
                {
                    await new AskForLiveTilePinDialog().ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ex] MainPage - AskForLiveTilePinDialog error: " + ex.Message);
                }
            }
        }



        private void AnimateProgressBar()
        {

            const int numberOfIntervals = 25;

            int percentageToLoad = 0;// YPViewModel.YearProgress;
            int animationTimeInMiliseconds = 500;
            double intervalAmount = (double)percentageToLoad / numberOfIntervals;
            int intervalTime = animationTimeInMiliseconds / numberOfIntervals;

            Timer progressTimer = new Timer(new TimeSpan(0, 0, 0, 0, animationTimeInMiliseconds), new TimeSpan(0, 0, 0, 0, intervalTime));
            progressTimer.TimerTicked += (s, e) =>
            {
                MyProgressBar.Value += intervalAmount;
            };
            progressTimer.StartTimer();

        }
        // ***********************************
    }
}
