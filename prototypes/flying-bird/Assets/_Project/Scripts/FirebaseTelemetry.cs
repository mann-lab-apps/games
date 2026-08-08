using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MannLab.Games.FlyingBird
{
    public static class FirebaseTelemetry
    {
        private static readonly Dictionary<string, string> EmptyParameters = new Dictionary<string, string>();
        private static bool initialized;
        private static bool firebaseAvailable;
        private static bool handlingLogMessage;
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

        public static void SetContext(string key, string value)
        {
            Initialize();
            SetCrashlyticsCustomKey(key, value);
        }

        public static void LogException(Exception exception)
        {
            Initialize();
            Debug.LogException(exception);
            InvokeCrashlyticsLogException(exception);
        }

        public static void ForceCrashForTesting()
        {
            Initialize();
            LogCrashlyticsMessage("Crashlytics forced test crash requested.");

            if (TryInvokeCrashlyticsCrash())
            {
                return;
            }

            if (TryForceUnityCrash())
            {
                return;
            }

            throw new InvalidOperationException("Crashlytics forced test crash fallback.");
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

            InvokeFirebaseMethod(method, eventName);
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

            InvokeFirebaseMethod(method, message);
        }

        private static void SetCrashlyticsCustomKey(string key, string value)
        {
            if (crashlyticsType == null)
            {
                return;
            }

            var method = crashlyticsType.GetMethod(
                "SetCustomKey",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string) },
                null);

            InvokeFirebaseMethod(method, key, value ?? string.Empty);
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

            InvokeFirebaseMethod(method, exception);
        }

        private static bool TryInvokeCrashlyticsCrash()
        {
            if (crashlyticsType == null)
            {
                return false;
            }

            var method = crashlyticsType.GetMethod(
                "Crash",
                BindingFlags.Public | BindingFlags.Static,
                null,
                Type.EmptyTypes,
                null);
            if (method == null)
            {
                return false;
            }

            try
            {
                method.Invoke(null, null);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Telemetry] Crashlytics test crash call failed: {exception.GetType().Name}");
                return false;
            }
        }

        private static bool TryForceUnityCrash()
        {
            var diagnosticsType = FindType("UnityEngine.Diagnostics.Utils");
            var categoryType = FindType("UnityEngine.Diagnostics.ForcedCrashCategory");
            if (diagnosticsType == null || categoryType == null)
            {
                return false;
            }

            var method = diagnosticsType.GetMethod(
                "ForceCrash",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { categoryType },
                null);
            if (method == null)
            {
                return false;
            }

            try
            {
                var category = Enum.Parse(categoryType, "Abort");
                method.Invoke(null, new[] { category });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Telemetry] Unity forced crash call failed: {exception.GetType().Name}");
                return false;
            }
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
            if (handlingLogMessage || type != LogType.Exception)
            {
                return;
            }

            try
            {
                handlingLogMessage = true;
                LogCrashlyticsMessage($"{condition}\n{stackTrace}");
            }
            finally
            {
                handlingLogMessage = false;
            }
        }

        private static void InvokeFirebaseMethod(MethodInfo method, params object[] args)
        {
            if (method == null)
            {
                return;
            }

            try
            {
                method.Invoke(null, args);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Telemetry] Firebase call failed: {exception.GetType().Name}");
            }
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
