mergeInto(LibraryManager.library, {
  YG_Init: function (receiver) { window.LupaYandex.attach(UTF8ToString(receiver), function(o,m,v) { SendMessage(o,m,v); }); },
  YG_Ready: function () { window.LupaYandex.ready(); },
  YG_Gameplay: function (active) { window.LupaYandex.gameplay(!!active); },
  YG_Ad: function (rewarded, request) { window.LupaYandex.ad(!!rewarded, request); },
  YG_Banner: function (visible) { window.LupaYandex.banner(!!visible); }
});
