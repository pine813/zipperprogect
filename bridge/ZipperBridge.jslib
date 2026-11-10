mergeInto(LibraryManager.library, {
  // Unity 侧调用 WebBridge.NotifyGameComplete() 时会走这里：
  // 通知外层 page（双游戏串联外壳）当前游戏通关，外壳会自动切到下一个游戏。
  ZipperGameComplete: function () {
    try {
      if (window.parent && window.parent !== window) {
        window.parent.postMessage({ type: 'game-complete' }, '*');
      } else {
        console.log('[ZipperBridge] game-complete（没有父窗口，忽略）');
      }
    } catch (e) {
      console.error('[ZipperBridge] postMessage 失败', e);
    }
  }
});
