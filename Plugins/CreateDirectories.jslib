mergeInto(LibraryManager.library, {
  CreateDirectories: function () {
    const MOUNT_PATH = '/data';
    const SAVE_PATH = MOUNT_PATH + '/Save_data';

    // Step 1: Mount IDBFS at /data if not already
    if (!FS.analyzePath(MOUNT_PATH).exists) {
      FS.mkdir(MOUNT_PATH);
      FS.mount(IDBFS, {}, MOUNT_PATH);
    }

    // Step 2: Sync to load existing data (or create if first time)
    FS.syncfs(true, function (err) {
      if (err) {
        console.error("IDBFS initial sync failed:", err);
        return;
      }

      // Step 3: Ensure base Save_data and subdirectories exist
      try {
        FS.mkdir(SAVE_PATH);
      } catch (e) {
        // Directory already exists
      }

      const baseDirs = [
        '/Items',
        '/Pokemon',
        '/Player',
        '/Party_Ids',
        '/Items/Held_Items'
      ];

      for (const dir of baseDirs) {
        try {
          FS.mkdir(SAVE_PATH + dir);
        } catch (e) {
          // Directory may already exist
        }
      }

      // Step 4: Sync updated structure to IndexedDB
      FS.syncfs(false, function (err) {
        if (err) {
          console.error("IDBFS final sync failed:", err);
        } else {
          console.log("Default directory structure created.");
        }
      });
    });
  }
});
