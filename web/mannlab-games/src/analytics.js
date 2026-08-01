const GOOGLE_ANALYTICS_SCRIPT_ID = "google-analytics-script";

const getMeasurementId = () =>
  import.meta.env.VITE_GA_MEASUREMENT_ID?.trim() ?? "";

export function initGoogleAnalytics(measurementId = getMeasurementId()) {
  const trackingId = measurementId.trim();

  if (
    !trackingId ||
    typeof window === "undefined" ||
    typeof document === "undefined" ||
    window.mannlabGoogleAnalyticsId === trackingId
  ) {
    return;
  }

  window.mannlabGoogleAnalyticsId = trackingId;
  window.dataLayer = window.dataLayer ?? [];
  window.gtag =
    window.gtag ??
    function gtag() {
      window.dataLayer?.push(arguments);
    };

  if (!document.getElementById(GOOGLE_ANALYTICS_SCRIPT_ID)) {
    const script = document.createElement("script");
    script.id = GOOGLE_ANALYTICS_SCRIPT_ID;
    script.async = true;
    script.src = `https://www.googletagmanager.com/gtag/js?id=${encodeURIComponent(
      trackingId
    )}`;
    document.head.append(script);
  }

  window.gtag("js", new Date());
  window.gtag("config", trackingId);
}

export function trackAnalyticsEvent(eventName, params = {}) {
  if (typeof window === "undefined" || typeof window.gtag !== "function") {
    return;
  }

  window.gtag("event", eventName, params);
}
