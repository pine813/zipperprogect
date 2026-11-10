mergeInto(LibraryManager.library, {
  // Unity 调用 WebBridge.NotifyGameComplete() 时走到这里，
  // 通知外层 page（双游戏串联外壳）本游戏已通关。
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
