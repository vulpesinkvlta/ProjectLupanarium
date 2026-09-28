// Local QA fixture only. Not part of Assets or the release archive.
(function () {
  const listeners = {};
  function log(value) { console.log('[SDK QA] ' + value); }
  function show(callbacks, rewarded) {
    const cover = document.createElement('div');
    cover.style.cssText = 'position:fixed;inset:0;z-index:9999;background:#222e;color:white;display:flex;align-items:center;justify-content:center;gap:20px;font:24px Arial';
    const title = document.createElement('span'); title.textContent = 'ТЕСТ рекламы'; cover.append(title);
    function finish(grant) {
      if (grant) { callbacks.onRewarded?.(); callbacks.onRewarded?.(); }
      cover.remove(); callbacks.onClose?.(true); listeners.game_api_resume?.(); log('closed, reward=' + grant);
    }
    if (rewarded) {
      const grant = document.createElement('button'); grant.textContent = 'Тест: получить награду';
      grant.onclick = () => finish(true); cover.append(grant);
    }
    const close = document.createElement('button'); close.textContent = 'Закрыть без награды';
    close.onclick = () => finish(false); cover.append(close);
    document.body.append(cover); listeners.game_api_pause?.(); callbacks.onOpen?.(); log('opened');
  }
  window.YaGames = { init: async () => ({
    on: (name, fn) => { listeners[name] = fn; },
    features: { LoadingAPI: { ready: () => log('ready') }, GameplayAPI: { start: () => log('gameplay start'), stop: () => log('gameplay stop') } },
    adv: {
      showRewardedVideo: ({ callbacks }) => show(callbacks, true),
      showFullscreenAdv: ({ callbacks }) => show(callbacks, false),
      showBannerAdv: async () => ({ stickyAdvIsShowing: true }),
      hideBannerAdv: async () => ({ stickyAdvIsShowing: false })
    }
  }) };
})();
