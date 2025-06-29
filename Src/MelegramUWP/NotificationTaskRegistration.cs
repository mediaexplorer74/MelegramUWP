using System;
using System.Diagnostics;
using Windows.ApplicationModel.Background;

namespace MelegramUWP
{
    public static class NotificationTaskRegistration
    {
        public static async void Register()
        {
            // Проверяем, зарегистрирована ли задача
            foreach (System.Collections.Generic.KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == "NotificationTask")
                    return;
            }
            // Запрашиваем разрешение
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if (status == BackgroundAccessStatus.DeniedByUser 
                || status == BackgroundAccessStatus.DeniedBySystemPolicy)
            {
                // Пользователь отказал в доступе либо система не позволяет использовать фоновые задачи
                Debug.WriteLine("[!] Background tasks are not allowed.");
                return;
            }

            try
            {
                // Регистрируем задачу
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder
                {
                    Name = "NotificationTask",
                    TaskEntryPoint = "BackgroundTasks.NotificationTask"
                };
                builder.SetTrigger(new TimeTrigger(15, false)); // Каждые 15 минут...
                builder.Register();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] NotificationTaskRegistration error: " 
                    + ex.Message);
            }
        }
    }
}
