using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using HtmlAgilityPack;
using System.Diagnostics;

namespace BackgroundTasks
{
    public sealed class NotificationTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            try
            {
                // Запрос к странице чатов
                Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                string cookieString = localSettings.Values["mpgram_cookies"] as string ?? "";
                HttpClient http = new HttpClient();
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
                Debug.WriteLine("[ex] NotificationTask - Run error: " + ex.Message);
            }
            finally
            {
                deferral.Complete();
            }
        }
    }
}
