using System;
using UnityEngine;

namespace BeatFactory
{
    /// <summary>
    /// What the game needs from a local-notification system. To make real phone notifications:
    /// install the "Mobile Notifications" package (com.unity.mobile.notifications), implement this
    /// interface with it (AndroidNotificationCenter / iOSNotificationCenter) and return it from
    /// NotificationManager.CreateProvider().
    /// </summary>
    public interface INotificationProvider
    {
        void Initialize();
        void Schedule(string id, string title, string body, TimeSpan delay);
        void CancelAll();
    }

    /// <summary>Placeholder provider: only writes to the Console, so you can see what would be sent.</summary>
    public class LogNotificationProvider : INotificationProvider
    {
        public void Initialize() { }

        public void Schedule(string id, string title, string body, TimeSpan delay) =>
            Debug.Log($"[Notifications] In {NumberFormatter.FormatDuration(delay)}: \"{title}\" — {body}");

        public void CancelAll() { }
    }

    /// <summary>
    /// When the player leaves, schedules friendly reminders ("Your producers created 245 new beats!").
    /// When the player comes back, cancels them. Respects the Notifications setting.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        [Tooltip("First reminder after this many hours away.")]
        [SerializeField] float reminderHours = 2f;

        INotificationProvider provider;
        GameManager gm;

        bool permissionAsked;
        float checkTimer;

        void Start()
        {
            gm = GameManager.Instance;
            provider = CreateProvider();
            provider.Initialize();
            provider.CancelAll(); // the player is here, nothing to remind about
            gm.Settings.Changed += OnSettingsChanged;
        }

        void OnDestroy()
        {
            if (gm != null) gm.Settings.Changed -= OnSettingsChanged;
        }

        /// <summary>Real notifications on phones; Console messages in the Editor.</summary>
        INotificationProvider CreateProvider()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            return new MobileNotificationProvider();
#else
            return new LogNotificationProvider();
#endif
        }

        // The system permission dialog is shown once, at a calm moment: after the tutorial (or when the player
        // turns Notifications on in Settings) — never right at the first launch.
        void Update()
        {
            if (permissionAsked) { enabled = false; return; }
            checkTimer += Time.unscaledDeltaTime;
            if (checkTimer < 5f) return;
            checkTimer = 0f;
            if (gm != null && gm.Settings.notificationsOn && gm.TutorialStep >= TutorialUI.StepCount) AskPermission();
        }

        void OnSettingsChanged()
        {
            if (gm.Settings.notificationsOn && !permissionAsked) AskPermission();
        }

        void AskPermission()
        {
            permissionAsked = true;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            (provider as MobileNotificationProvider)?.RequestPermission();
#endif
        }

        void OnApplicationPause(bool paused)
        {
            if (provider == null) return;
            if (paused) ScheduleReminders();
            else provider.CancelAll();
        }

        void OnApplicationQuit()
        {
            if (provider != null) ScheduleReminders();
        }

        void ScheduleReminders()
        {
            provider.CancelAll();
            if (gm == null || !gm.Settings.notificationsOn) return;
            if (gm.Producers.BeatsPerSecond <= 0) return; // nothing is being produced

            double maxSeconds = gm.Offline.MaxOfflineSeconds;
            double reminder = Math.Min(reminderHours * 3600.0, maxSeconds);

            var early = gm.Offline.Calculate(reminder);
            provider.Schedule("reminder", Loc.T("push.busy_title"),
                Loc.P("push.busy_body", early.Beats),
                TimeSpan.FromSeconds(reminder));

            if (maxSeconds > reminder)
            {
                var full = gm.Offline.Calculate(maxSeconds);
                provider.Schedule("full", Loc.T("push.full_title"),
                    Loc.F("push.full_body", "money", NumberFormatter.FormatMoney(full.Money)),
                    TimeSpan.FromSeconds(maxSeconds));
            }
        }
    }
}
