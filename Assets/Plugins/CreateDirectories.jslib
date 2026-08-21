mergeInto(LibraryManager.library, {
  CreateDirectories: function (jsonPtr) {

    const MOUNT_PATH = "/data";
    const SAVE_PATH = MOUNT_PATH + "/Save_data";
    const TEMP_PATH = MOUNT_PATH + "/Temp_Save_data";

    const json = UTF8ToString(jsonPtr);
    const data = JSON.parse(json);

    const allDirs = [];
    allDirs.push(SAVE_PATH);
    allDirs.push(TEMP_PATH);

    data.items.forEach(item => {
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
        return;
      }

      for (const dir of allDirs) {
        try {
          FS.mkdir(dir);
        } catch (e) {
          // already exists
        }
      }

      FS.syncfs(false, function (err) {
        if (err) {
          console.error("IDBFS final sync failed:", err);
        } else {
          console.log("Default directory structure created.");
          unityInstance.SendMessage("Save_Manager", "OnFileStructureCreated", "");
        }
      });
    });
  }
}
);