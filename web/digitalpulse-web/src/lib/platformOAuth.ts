const OAUTH_DONE = "dp-oauth-complete";

export function openOAuthWindow(url: string) {
  const width = 520;
  const height = 720;
  const left = Math.round(window.screenX + (window.outerWidth - width) / 2);
  const top = Math.round(window.screenY + (window.outerHeight - height) / 2);
  return window.open(
    url,
    "dp-platform-login",
    `popup=yes,width=${width},height=${height},left=${left},top=${top}`
  );
}

export function watchOAuthPopup(popup: Window | null, origin: string) {
  return new Promise<string | null>((resolve) => {
    if (!popup) {
      resolve(null);
      return;
    }

    function finish(platform: string | null) {
      window.removeEventListener("message", onMessage);
      window.clearInterval(timer);
      resolve(platform);
    }

    function onMessage(event: MessageEvent) {
      if (event.origin !== origin) return;
      if (event.data?.type !== OAUTH_DONE) return;
      finish(typeof event.data.platform === "string" ? event.data.platform : "");
    }

    const timer = window.setInterval(() => {
      if (popup.closed) finish("");
    }, 400);

    window.addEventListener("message", onMessage);
  });
}

export function notifyOAuthOpener(platform: string) {
  if (!window.opener || window.opener.closed) return false;
  window.opener.postMessage({ type: OAUTH_DONE, platform }, window.location.origin);
  window.close();
  return true;
}
