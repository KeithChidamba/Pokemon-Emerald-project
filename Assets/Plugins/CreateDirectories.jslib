mergeInto(LibraryManager.library, {
  CreateDirectories: function (jsonPtr) {

    var MOUNT_PATH = "/data";
    var SAVE_PATH = MOUNT_PATH + "/Save_data";
    var TEMP_PATH = MOUNT_PATH + "/Temp_Save_data";

    function notify(method) {
      try {
        if (typeof unityInstance !== "undefined" && unityInstance) unityInstance.SendMessage("Save_Manager", method, "");
        else if (typeof Module !== "undefined" && Module.SendMessage) Module.SendMessage("Save_Manager", method, "");
        else console.error("No Unity instance available for SendMessage");
      } catch (e) { console.error("SendMessage failed:", e); }
    }

    // FS.mkdir is NOT recursive (unlike Directory.CreateDirectory in C#), so create each segment.
    function mkdirp(path) {
      var parts = path.split("/").filter(function (p) { return p.length > 0; });
      var cur = "";
      for (var i = 0; i < parts.length; i++) {
        cur += "/" + parts[i];
        if (!FS.analyzePath(cur).exists) {
          try { FS.mkdir(cur); }
          catch (e) { console.error("mkdir failed for", cur, e); }
        }
      }
    }

    var data;
    try {
      data = JSON.parse(UTF8ToString(jsonPtr));
    } catch (e) {
      console.error("CreateDirectories: bad JSON", e);
      notify("OnDirectoryCreationFailed");
      return;
    }

    var allDirs = [SAVE_PATH, TEMP_PATH];
    (data.items || []).forEach(function (item) {
      if (item.charAt(0) !== "/") item = "/" + item;
      allDirs.push(SAVE_PATH + item);
      allDirs.push(TEMP_PATH + item);
    });

    if (!FS.analyzePath(MOUNT_PATH).exists) {
      FS.mkdir(MOUNT_PATH);
      FS.mount(IDBFS, {}, MOUNT_PATH);
    }

    FS.syncfs(true, function (err) {
      if (err) {
        console.error("IDBFS initial sync failed:", err);
        notify("OnDirectoryCreationFailed");
        return;
      }

      allDirs.forEach(mkdirp);

      FS.syncfs(false, function (err) {
        if (err) {
          console.error("IDBFS final sync failed:", err);
          notify("OnDirectoryCreationFailed");
        } else {
          console.log("Default directory structure created (" + allDirs.length + " dirs).");
          notify("OnFileStructureCreated");
        }
      });
    });
  }
});
