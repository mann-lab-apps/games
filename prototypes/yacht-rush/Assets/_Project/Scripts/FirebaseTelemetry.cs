using System;
using System.Collections.Generic;
#if !UNITY_WEBGL
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
#endif
using UnityEngine;

namespace MannLab.Games.YachtRush
{
    public static class FirebaseTelemetry
    {
        private static readonly Dictionary<string, string> EmptyParameters = new Dictionary<string, string>();
        private static readonly List<PendingEvent> PendingEvents = new List<PendingEvent>();
        private static readonly Dictionary<string, string> PendingContext = new Dictionary<string, string>();
        private static bool initialized;
        private static bool handlingExceptionLog;
#if !UNITY_WEBGL
        private static bool dependencyCheckStarted;
        private static bool firebaseReady;
#endif

        public static bool IsReady
        {
            get
            {
#if !UNITY_WEBGL
                return firebaseReady;
#else
                return false;
#endif
            }
        }

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            Application.logMessageReceived += HandleLogMessageReceived;
            StartFirebaseDependencyCheck();
        }

        public static void LogEvent(string eventName)
        {
            LogEvent(eventName, EmptyParameters);
        }

        public static void LogEvent(string eventName, IDictionary<string, string> parameters)
        {
            Initialize();

            var safeParameters = parameters ?? EmptyParameters;
            var parameterText = FormatParameters(safeParameters);
            Debug.Log(string.IsNullOrEmpty(parameterText)
                ? $"[Telemetry] {eventName}"
                : $"[Telemetry] {eventName} {parameterText}");

            if (!IsReady)
            {
                PendingEvents.Add(new PendingEvent(eventName, safeParameters));
                return;
            }

            SendEvent(eventName, safeParameters);
        }

        public static void SetContext(string key, string value)
        {
            Initialize();

            if (!IsReady)
            {
                PendingContext[key] = value ?? string.Empty;
                return;
            }

            SetCrashlyticsCustomKey(key, value);
        }

        public static void LogException(Exception exception)
        {
            Initialize();
            Debug.LogException(exception);
            SendException(exception);
        }

        public static void ForceCrashForTesting()
        {
            Initialize();
            FlushPendingTelemetry();
            LogCrashlyticsMessage("Crashlytics forced test crash requested.");
            throw new InvalidOperationException("Crashlytics forced test crash fallback.");
        }

        private static void StartFirebaseDependencyCheck()
        {
#if !UNITY_WEBGL
            if (dependencyCheckStarted)
            {
                return;
            }

            dependencyCheckStarted = true;

            try
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    if (task.IsCanceled)
                    {
                        Debug.LogWarning("[Telemetry] Firebase dependency check was canceled.");
                        return;
                    }

                    if (task.IsFaulted)
                    {
                        Debug.LogWarning($"[Telemetry] Firebase dependency check failed: {task.Exception?.GetBaseException().Message}");
                        return;
                    }

                    if (task.Result != DependencyStatus.Available)
                    {
                        Debug.LogWarning($"[Telemetry] Firebase dependencies are unavailable: {task.Result}");
                        return;
                    }

                    _ = FirebaseApp.DefaultInstance;
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    firebaseReady = true;
                    LogCrashlyticsMessage("Firebase telemetry initialized.");
                    FlushPendingTelemetry();
                    Debug.Log("[Telemetry] Firebase dependencies available.");
                });
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Telemetry] Firebase dependency check could not start: {exception.GetType().Name}");
            }
#else
            Debug.Log("[Telemetry] Firebase dependency check skipped on WebGL.");
#endif
        }

        private static void FlushPendingTelemetry()
        {
            if (!IsReady)
            {
                return;
            }

            foreach (var pair in PendingContext)
            {
                SetCrashlyticsCustomKey(pair.Key, pair.Value);
            }

            PendingContext.Clear();

            foreach (var pendingEvent in PendingEvents)
            {
                SendEvent(pendingEvent.Name, pendingEvent.Parameters);
            }

            PendingEvents.Clear();
        }

        private static void SendEvent(string eventName, IDictionary<string, string> parameters)
        {
#if !UNITY_WEBGL
            FirebaseAnalytics.LogEvent(eventName);
#endif
            var parameterText = FormatParameters(parameters);
            LogCrashlyticsMessage(string.IsNullOrEmpty(parameterText)
                ? eventName
                : $"{eventName} {parameterText}");
        }

        private static void LogCrashlyticsMessage(string message)
        {
#if !UNITY_WEBGL
            Crashlytics.Log(message);
#endif
        }

        private static void SetCrashlyticsCustomKey(string key, string value)
        {
#if !UNITY_WEBGL
            Crashlytics.SetCustomKey(key, value ?? string.Empty);
#endif
        }

        private static void SendException(Exception exception)
        {
#if !UNITY_WEBGL
            Crashlytics.LogException(exception);
#endif
        }

        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.ExceptionObject is Exception exception)
            {
                SendException(exception);
            }
        }

        private static void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (handlingExceptionLog || type != LogType.Exception)
            {
                return;
            }

            try
            {
                handlingExceptionLog = true;
                LogCrashlyticsMessage($"{condition}\n{stackTrace}");
            }
            finally
            {
                handlingExceptionLog = false;
            }
        }

        private static string FormatParameters(IDictionary<string, string> parameters)
        {
            if (parameters == null || parameters.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            foreach (var pair in parameters)
            {
                parts.Add($"{pair.Key}={pair.Value}");
            }

            return string.Join(", ", parts);
        }

        private readonly struct PendingEvent
        {
            public PendingEvent(string name, IDictionary<string, string> parameters)
            {
                Name = name;
                Parameters = parameters == null
                    ? EmptyParameters
                    : new Dictionary<string, string>(parameters);
            }

            public string Name { get; }
            public IDictionary<string, string> Parameters { get; }
        }
    }
}
