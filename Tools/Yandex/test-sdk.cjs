const fs=require('node:fs'), vm=require('node:vm'), assert=require('node:assert/strict');
async function fixture(language='en') {
 const events=[], handlers={}, timers=new Map();let serial=0, callbacks;
 const sdk={environment:{i18n:{lang:language}},on:(n,f)=>handlers[n]=f,features:{LoadingAPI:{ready:()=>events.push(['ready'])},GameplayAPI:{start:()=>events.push(['start']),stop:()=>events.push(['stop'])}},adv:{
 showRewardedVideo:o=>{callbacks=o.callbacks;},showFullscreenAdv:o=>{callbacks=o.callbacks;},showBannerAdv:async()=>({stickyAdvIsShowing:true}),hideBannerAdv:async()=>({stickyAdvIsShowing:false})}};
 const window={YaGames:{init:async()=>sdk},addEventListener:()=>{}};
 const status={textContent:''};
 const document={getElementById:()=>status,hidden:false,addEventListener:()=>{},documentElement:{classList:{toggle:()=>{},remove:()=>{}}}};
 const context={window,document,setTimeout:f=>{timers.set(++serial,f);return serial;},clearTimeout:id=>timers.delete(id)};
 vm.runInNewContext(fs.readFileSync('Assets/WebGLTemplates/Yandex/yandex.js','utf8'),context);
 const api=window.LupaYandex;api.attach('Receiver',(o,m,v)=>events.push([m,v]));await api.init();
 return {api,events,handlers,document,status,timers,callbacks:()=>callbacks};
}
(async()=>{
 for(const lang of ['ru','en','tr']) { const v=await fixture(lang); assert.equal(v.api.language(),lang); assert.equal(v.document.documentElement.lang,lang); assert.ok(v.status.textContent.length>0); v.api.loadingFailed(); assert.ok(v.status.textContent.length>25); }
 let unsupported=await fixture('de'); assert.equal(unsupported.document.documentElement.lang,'en');
 let f=await fixture();f.api.ready();f.api.ready();assert.equal(f.events.filter(e=>e[0]==='ready').length,1);
 f.api.ad(true,1);f.callbacks().onRewarded();f.callbacks().onRewarded();f.callbacks().onClose();f.callbacks().onClose();
 assert.equal(f.events.filter(e=>e[0]==='OnAdReward').length,1);assert.equal(f.events.filter(e=>e[0]==='OnAdClosed').length,1);
 f=await fixture();f.api.ad(true,2);f.callbacks().onError();f.callbacks().onRewarded();assert.equal(f.events.filter(e=>e[0]==='OnAdReward').length,0);
 f=await fixture();f.api.ad(false,3);f.callbacks().onRewarded();f.callbacks().onClose(false);assert.equal(f.events.filter(e=>e[0]==='OnAdReward').length,0);
 f=await fixture();f.document.hidden=true;f.api.ad(true,4);assert.ok(f.events.some(e=>e[0]==='OnAdClosed'&&e[1]==='4'));
 f=await fixture();f.api.ad(true,5);for(const timer of [...f.timers.values()])timer();f.callbacks().onRewarded();assert.equal(f.events.filter(e=>e[0]==='OnAdReward').length,0);
 f.callbacks().onOpen();assert.ok(f.events.some(e=>e[0]==='OnAdVisibility'&&e[1]==='1'));f.callbacks().onClose();assert.ok(f.events.some(e=>e[0]==='OnAdVisibility'&&e[1]==='0'));
 f=await fixture();f.api.ad(true,6);f.callbacks().onOpen();assert.equal(f.timers.size,0);f.handlers.game_api_pause();f.handlers.game_api_resume();assert.ok(f.events.some(e=>e[0]==='OnPlatformPause'&&e[1]==='1'));
 console.log('SDK callback tests passed: ready once, reward once, error, close, fullscreen, hidden page, timeout, visible ad, platform pause.');
})().catch(e=>{console.error(e);process.exitCode=1;});
