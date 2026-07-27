using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MannLab.Games.Game10000
{
    public static class FirebaseTelemetry
    {
        private static readonly Dictionary<string, string> EmptyParameters = new Dictionary<string, string>();
        private static bool initialized;
        private static bool firebaseAvailable;
        private static Type analyticsType;
        private static Type crashlyticsType;

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            analyticsType = FindType("Firebase.Analytics.FirebaseAnalytics");
            crashlyticsType = FindType("Firebase.Crashlytics.Crashlytics");
            firebaseAvailable = analyticsType != null || crashlyticsType != null;

            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            Application.logMessageReceived += HandleLogMessageReceived;

            if (firebaseAvailable)
            {
                LogCrashlyticsMessage("Firebase telemetry initialized.");
                Debug.Log("[Telemetry] Firebase Analytics/Crashlytics SDK detected.");
                return;
            }

            Debug.Log("[Telemetry] Firebase SDK not installed yet. Events will only be written to Unity logs.");
        }

        public static void LogEvent(string eventName)
        {
            LogEvent(eventName, EmptyParameters);
        }

        public static void LogEvent(string eventName, IDictionary<string, string> parameters)
        {
            Initialize();

            var parameterText = FormatParameters(parameters);
            Debug.Log(string.IsNullOrEmpty(parameterText)
                ? $"[Telemetry] {eventName}"
                : $"[Telemetry] {eventName} {parameterText}");

            InvokeAnalyticsLogEvent(eventName);
            LogCrashlyticsMessage(string.IsNullOrEmpty(parameterText)
                ? eventName
                : $"{eventName} {parameterText}");
        }

        public static void LogException(Exception exception)
        {
            Initialize();
            Debug.LogException(exception);
            InvokeCrashlyticsLogException(exception);
        }

        private static void InvokeAnalyticsLogEvent(string eventName)
        {
            if (analyticsType == null)
            {
                return;
            }

            var method = analyticsType.GetMethod(
                "LogEvent",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

            method?.Invoke(null, new object[] { eventName });
        }

        private static void LogCrashlyticsMessage(string message)
        {
            if (crashlyticsType == null)
            {
                return;
            }

            var method = crashlyticsType.GetMethod(
                "Log",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string) },
                null);

            method?.Invoke(null, new object[] { message });
        }

        private static void InvokeCrashlyticsLogException(Exception exception)
        {
            if (crashlyticsType == null)
            {
                return;
            }

            var method = crashlyticsType.GetMethod(
                "LogException",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(Exception) },
                null);

            method?.Invoke(null, new object[] { exception });
        }

        private static void HandleUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            if (args.ExceptionObject is Exception exception)
            {
                InvokeCrashlyticsLogException(exception);
            }
        }

        private static void HandleLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception)
            {
                return;
            }

            LogCrashlyticsMessage($"{condition}\n{stackTrace}");
        }

        private static Type FindType(string typeName)
        {
            var directType = Type.GetType(typeName);
            if (directType != null)
            {
                return directType;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
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
    }
}
