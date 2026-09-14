mergeInto(LibraryManager.library, {
  OneEqualsOneInputInterruptionVersion: function () {
    if (!Module.oneEqualsOneInputState) {
      var state = { version: 0 };
      Module.oneEqualsOneInputState = state;
      window.addEventListener('blur', function () { state.version++; });
      document.addEventListener('visibilitychange', function () {
        if (document.hidden) state.version++;
      });
    }
    return Module.oneEqualsOneInputState.version;
  },
  OneEqualsOneCanvasDisplayWidth: function () {
    return Module.canvas.getBoundingClientRect().width;
  },
  OneEqualsOneCanvasDisplayHeight: function () {
    return Module.canvas.getBoundingClientRect().height;
  }
});
