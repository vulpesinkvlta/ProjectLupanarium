(function (w) {
  'use strict';
  let sdk, receiver, send, paused = false, loaded = false, readySent = false;
  let running = false, currentAd = null, bannerWanted = false, bannerBusy = false, visibleAds = 0;
  function emit(method, value) { if (send) send(receiver, method, String(value)); }
  function focus() { emit('OnPageFocus', document.hidden ? '0' : '1'); }
  document.addEventListener('visibilitychange', focus);
  w.addEventListener('blur', function () { emit('OnPageFocus','0'); });
  w.addEventListener('focus', focus);
  function notifyReady() {
    if (!sdk || !loaded || readySent) return;
    readySent = true; sdk.features.LoadingAPI.ready();
  }
  function init() {
    return w.YaGames.init().then(function (value) {
      sdk = value;
      sdk.on('game_api_pause', function () { paused = true; emit('OnPlatformPause', '1'); });
      sdk.on('game_api_resume', function () { paused = false; emit('OnPlatformPause', '0'); });
      emit('OnSdkReady', ''); notifyReady();
    }).catch(function () { w.lupaSdkFailed = true; emit('OnSdkError', ''); });
  }
  async function updateBanner() {
    if (!sdk || bannerBusy) return;
    bannerBusy = true;
    const desired = bannerWanted;
    try {
      const result = await (desired ? sdk.adv.showBannerAdv() : sdk.adv.hideBannerAdv());
      document.documentElement.classList.toggle('with-banner', !!result.stickyAdvIsShowing);
    } catch (_) { document.documentElement.classList.remove('with-banner'); }
    finally { bannerBusy = false; if (desired !== bannerWanted) updateBanner(); }
  }
  w.LupaYandex = {
    init: init,
    failed: function () { w.lupaSdkFailed = true; emit('OnSdkError', ''); },
    attach: function (name, sender) {
      receiver = name; send = sender;
      if (sdk) emit('OnSdkReady','');
      else if (w.lupaSdkFailed) emit('OnSdkError','');
      emit('OnPlatformPause', paused ? '1' : '0'); focus();
    },
    ready: function () { loaded = true; notifyReady(); },
    gameplay: function (active) {
      if (!sdk || running === active) return;
      running = active;
      if (active) sdk.features.GameplayAPI.start(); else sdk.features.GameplayAPI.stop();
    },
    banner: function (visible) { bannerWanted = visible; updateBanner(); },
    ad: function (rewarded, id) {
      if (!sdk || currentAd || paused || document.hidden) { emit('OnAdClosed', id); return; }
      const request = { id: id, opened: false, rewarded: false, ended: false };
      currentAd = request;
      function close() {
        if (request.opened) {
          request.opened = false; visibleAds--;
          emit('OnAdVisibility', visibleAds > 0 ? '1' : '0');
        }
        if (request.ended) return;
        request.ended = true; clearTimeout(watchdog);
        if (currentAd === request) currentAd = null;
        emit('OnAdClosed', id);
      }
      // A missing SDK response must not trap the player. Never time out a visible advertisement.
      const watchdog = setTimeout(function () { if (!request.opened) close(); }, 15000);
      const callbacks = {
        onOpen: function () {
          if (!request.opened) { request.opened = true; visibleAds++; emit('OnAdVisibility', '1'); }
          clearTimeout(watchdog);
        },
        onRewarded: function () {
          if (!rewarded || request.ended || request.rewarded) return;
          request.rewarded = true; emit('OnAdReward', id);
        },
        onClose: close,
        onError: close
      };
      try {
        if (rewarded) sdk.adv.showRewardedVideo({ callbacks: callbacks });
        else sdk.adv.showFullscreenAdv({ callbacks: callbacks });
      } catch (_) { close(); }
    }
  };
})(window);

