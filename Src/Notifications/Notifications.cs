using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using Windows.Data.Xml.Dom;
using HtmlAgilityPack;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;


namespace Notifications
{
    public sealed class Notifications : IBackgroundTask
    {
        //int savedProgress;
        //bool hasSavedProgressChanged = false;
        //DateCalc dateCalculation = new DateCalc();
        //SettingsHelper settingsHelper = new SettingsHelper();



        /*public void Run(IBackgroundTaskInstance taskInstance)
        {

            int yearProgress = dateCalculation.yearProgressPercentage;

            hasSavedProgressChanged = isDifferentToSavedProgress(yearProgress);

            if (hasSavedProgressChanged)
            {
                SendAMilestoneNotification(yearProgress);
            }

            StoreYearProgressIfNew(yearProgress);

        }*/


        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            /*int yearProgress = 50;//dateCalculation.yearProgressPercentage;

            hasSavedProgressChanged = isDifferentToSavedProgress(yearProgress);

            if (true)//(hasSavedProgressChanged)
            {
                SendAMilestoneNotification(yearProgress);
            }

            StoreYearProgressIfNew(yearProgress);*/


            //BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
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
                    /*string toastXml = $@"<toast><visual><binding template='ToastGeneric'>
                        <text>Новые сообщения</text>
                        <text>У вас {notifCount} непрочитанных сообщений!</text>
                    </binding></visual></toast>";
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(toastXml);
                    ToastNotification notif = new ToastNotification(xmlDoc);
                    ToastNotificationManager.CreateToastNotifier().Show(notif);*/

                    SendCountNotification(notifCount);
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
                //deferral.Complete();
            }
        }

        /*
        private void StoreYearProgressIfNew(int yearProgress)
        {
            if (hasSavedProgressChanged)
            {
                settingsHelper.SetYearProgress(yearProgress);
            }
        }
        */

        /*
        private bool isDifferentToSavedProgress(int yearProgress)
        {
            savedProgress = settingsHelper.GetStoredYearProgress();
            hasSavedProgressChanged = yearProgress != savedProgress;
            return hasSavedProgressChanged;
        }
        */

        private void SendCountNotification(int count)
        {

            var text = $"New {count}% messages detected!";
            SendRegularCountNotification(text);

        }



        public void SendRegularCountNotification(string text)
        {
            var toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = text
                            }

                        }
                    }
                }
            };

            // Create the toast notification
            var toastNotif = new ToastNotification(toastContent.GetXml());

            // And send the notification
            ToastNotificationManager.CreateToastNotifier().Show(toastNotif);

        }//SendRegularCountNotification

    }
}
