using HtmlAgilityPack;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace Notifications
{
    public sealed class Tiles: IBackgroundTask
    {
        int msgCount = 0;
        
        public Tiles()
        {
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            msgCount = await GetMessagesCountAsync();
        }

        
        private static async Task<int> GetMessagesCountAsync()
        {
            int notifCount = 0;
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
                                

                // Сохраняем новое значение
                localSettings.Values["LastNotifCount"] = notifCount;
            }
            catch (Exception ex)
            {
                // Логировать ошибку при необходимости
                Debug.WriteLine("[ex] NotificationTask - Run error: " + ex.Message);
            }
            
            return notifCount;
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            SendTileNotification();
        }

        public void SendTileNotification()
        {
            var tileContent = new TileContent()
            {
                Visual = new TileVisual()
                {
                    TileSmall = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                {
                    new AdaptiveText()
                    {
                        Text = $"{msgCount}", 
                        HintStyle = AdaptiveTextStyle.Base,
                        HintAlign = AdaptiveTextAlign.Center
                    },
                    //new AdaptiveText()
                    //{
                    //    Text = $"{dateCalculation.currentDate.Year}",
                    //    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                    //    HintAlign = AdaptiveTextAlign.Center
                    //}
                }
                        }
                    },
                    TileMedium = new TileBinding()
                    {
                        Content = new TileBindingContentAdaptive()
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                {
                    new AdaptiveText()
                    {
                        Text = $"{msgCount}",
                        HintStyle = AdaptiveTextStyle.Title,
                        HintWrap = true,
                        HintAlign = AdaptiveTextAlign.Center
                    },
                    new AdaptiveText()
                    {
                        Text = "Complete",
                        HintStyle = AdaptiveTextStyle.Base,
                        HintAlign = AdaptiveTextAlign.Center
                    },
                    //new AdaptiveText()
                    //{
                    //    Text =  $"{dateCalculation.currentDate.Year}",
                    //    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                    //    HintAlign = AdaptiveTextAlign.Center
                    //}
                }
                        }
                    },
                    TileWide = new TileBinding()
                    {
                        Branding = TileBranding.NameAndLogo,
                        Content = new TileBindingContentAdaptive()
                        {
                            TextStacking = TileTextStacking.Center,
                            Children =
                            {
                                new AdaptiveText()
                                {
                                    Text = $"{msgCount} Unreaded Messages", 
                                    HintStyle = AdaptiveTextStyle.Title,
                                    HintAlign = AdaptiveTextAlign.Center
                                },
                                //new AdaptiveText()
                                //{
                                //    Text =  $"{dateCalculation.currentDate.Year} Year Progress",
                                //    HintStyle = AdaptiveTextStyle.CaptionSubtle,
                                //    HintAlign = AdaptiveTextAlign.Center
                                //}
                            }
                        }
                    }
                }
            };

            // Create the tile notification
            var tileNotif = new TileNotification(tileContent.GetXml());

            // And send the notification to the primary tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
        }
    }
}
