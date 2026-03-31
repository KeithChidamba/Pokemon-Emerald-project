


mergeInto(LibraryManager.library, {
  CreateDirectories: function (jsonPtr) {




    const MOUNT_PATH = "/data";
    const SAVE_PATH = MOUNT_PATH + "/Save_data";
    const TEMP_PATH = MOUNT_PATH + "/Temp_Save_data";

    if (!FS.analyzePath(MOUNT_PATH).exists) {
      FS.mkdir(MOUNT_PATH);
      FS.mount(IDBFS, {}, MOUNT_PATH);
    }

    FS.syncfs(true, function (err) {
      if (err) {
        console.error("IDBFS initial sync failed:", err);
        return;
      }
    const json = UTF8ToString(jsonPtr);
    const data = JSON.parse(json);

    console.log("Received array:", data.items);

    for (let i = 0; i < data.items.length; i++) {
      console.log("Item " + i + ": " + data.items[i]);
    }
      const allDirs = [
        SAVE_PATH,
        SAVE_PATH + "/Items",
        SAVE_PATH + "/Items/Held_Items",
        SAVE_PATH + "/Items/Storage_Items",
        SAVE_PATH + "/Pokemon",
        SAVE_PATH + "/Player",
        SAVE_PATH + "/Party_Ids",
        SAVE_PATH + "/PC_Storage",
        SAVE_PATH + "/Overworld",
        SAVE_PATH + "/Overworld/Story_Objectives",
        SAVE_PATH + "/Overworld/Berry_Trees",

        TEMP_PATH,
        TEMP_PATH + "/Items",
        TEMP_PATH + "/Items/Held_Items",
        TEMP_PATH + "/Items/Storage_Items",
        TEMP_PATH + "/Pokemon",
        TEMP_PATH + "/Player",
        TEMP_PATH + "/Party_Ids",
        TEMP_PATH + "/PC_Storage",
        TEMP_PATH + "/Overworld",
        TEMP_PATH + "/Overworld/Story_Objectives",
        TEMP_PATH + "/Overworld/Berry_Trees"
      ];

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