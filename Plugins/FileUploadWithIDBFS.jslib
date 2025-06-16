mergeInto(LibraryManager.library, {
  UploadZipAndStoreToIDBFS: function () {
    const MOUNT_PATH = '/data';

    // Ensure IDBFS is mounted once only (e.g. in Start or Awake in Unity)
    if (!FS.analyzePath(MOUNT_PATH).exists) {
      FS.mkdir(MOUNT_PATH);
      FS.mount(IDBFS, {}, MOUNT_PATH);
      FS.syncfs(true, function (err) {
        if (err) console.error("IDBFS mount failed:", err);
        else console.log("IDBFS mounted at", MOUNT_PATH);
      });
    }

    // THIS function should be called directly by a Unity UI button click
    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".zip";
    input.style.display = "none"; // optional

    input.onchange = (e) => {
      const file = e.target.files[0];
      if (!file) return;

      const reader = new FileReader();
      reader.onload = function (event) {
        JSZip.loadAsync(event.target.result).then((zip) => {
          const fileWrites = [];

          Object.keys(zip.files).forEach((filename) => {
            const entry = zip.files[filename];

            if (entry.dir) {
              const dirPath = MOUNT_PATH + '/' + filename;
              try { FS.mkdir(dirPath); } catch (e) {}
            } else {
              const promise = entry.async("uint8array").then((data) => {
                const pathParts = filename.split('/');
                for (let i = 1; i < pathParts.length; i++) {
                  const dir = MOUNT_PATH + '/' + pathParts.slice(0, i).join('/');
                  try { FS.mkdir(dir); } catch (e) {}
                }

                const fullPath = MOUNT_PATH + '/' + filename;
                FS.writeFile(fullPath, data);
                console.log("Saved:", fullPath);
              });

              fileWrites.push(promise);
            }
          });

          Promise.all(fileWrites).then(() => {
            FS.syncfs(false, function (err) {
              if (err) console.error("Sync after upload failed:", err);
              else {
                console.log("All files synced to IndexedDB");
                unityInstance.SendMessage("Save_Manager", "OnIDBFSReady", "");    
           }
            });
          });
        });
      };

      reader.readAsArrayBuffer(file);
    };

    // ✅ This must be called during a Unity click event
    document.body.appendChild(input);
    input.click();
    document.body.removeChild(input); // clean up
  }
});
