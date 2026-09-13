mergeInto(LibraryManager.library, {
  OneEqualsOneCanvasDisplayWidth: function () {
    return Module.canvas.getBoundingClientRect().width;
  },
  OneEqualsOneCanvasDisplayHeight: function () {
    return Module.canvas.getBoundingClientRect().height;
  }
});
