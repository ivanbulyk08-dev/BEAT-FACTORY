#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using System;
using Unity.Notifications;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// Real phone notifications (Unity Mobile Notifications package, unified API for Android and iOS).
    /// Permission is asked once, at a natural moment (after the tutorial), never at the very first launch.
    /// </summary>
    public class MobileNotificationProvider : INotificationProvider
    {
        const string ChannelId = "beatfactory_reminders";
        bool initialized;
        NotificationsPermissionRequest permission;

        public void Initialize()
        {
            if (initialized) return;
            NotificationCenter.Initialize(new NotificationCenterArgs
            {
                AndroidChannelId = ChannelId,
                AndroidChannelName = "Studio reminders",
                AndroidChannelDescription = "Offline earnings ready to collect",
                PresentationOptions = NotificationPresentation.Alert | NotificationPresentation.Sound,
            });
            initialized = true;
        }

        public bool PermissionGranted => permission != null && permission.Status == NotificationsPermissionStatus.Granted;

        public void RequestPermission()
        {
            if (!initialized) Initialize();
            if (permission == null) permission = NotificationCenter.RequestPermission();
        }

        public void Schedule(string id, string title, string body, TimeSpan delay)
        {
            if (!initialized) return;
            var n = new Notification { Title = title, Text = body };
            NotificationCenter.ScheduleNotification(n, new NotificationDateTimeSchedule(DateTime.Now.Add(delay)));
        }

        public void CancelAll()
        {
            if (!initialized) return;
            NotificationCenter.CancelAllScheduledNotifications();
            NotificationCenter.CancelAllDeliveredNotifications();
        }
    }
}
#endif
