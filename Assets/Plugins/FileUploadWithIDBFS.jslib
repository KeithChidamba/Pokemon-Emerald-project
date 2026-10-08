mergeInto(LibraryManager.library, {
  UploadZipAndStoreToIDBFS: function () {
    var MOUNT_PATH = "/data";

    function notify(method) {
      try {
        if (typeof unityInstance !== "undefined" && unityInstance) unityInstance.SendMessage("Save_Manager", method, "");
        else if (typeof Module !== "undefined" && Module.SendMessage) Module.SendMessage("Save_Manager", method, "");
        else console.error("No Unity instance available for SendMessage");
      } catch (e) { console.error("SendMessage failed:", e); }
    }

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

    if (typeof JSZip === "undefined") {
      console.error("JSZip is not loaded - add it to index.html");
      notify("OnUploadFailed");
      return;
    }

    // Normally already mounted by CreateDirectories (called from C# first)
    if (!FS.analyzePath(MOUNT_PATH).exists) {
      FS.mkdir(MOUNT_PATH);
      FS.mount(IDBFS, {}, MOUNT_PATH);
    }

    // MUST run synchronously inside the click handler that triggered this call
    var input = document.createElement("input");
    input.type = "file";
    input.accept = ".zip";
    input.style.display = "none";

    input.onchange = function (e) {
      var file = e.target.files[0];
      input.remove();
      if (!file) return;

      var reader = new FileReader();
      reader.onerror = function () { console.error("File read failed"); notify("OnUploadFailed"); };
      reader.onload = function (event) {
        JSZip.loadAsync(event.target.result).then(function (zip) {
          var writes = [];

          Object.keys(zip.files).forEach(function (filename) {
            var entry = zip.files[filename];
            if (filename.indexOf("__MACOSX/") === 0) return;

            // Back-compat: zips made before the fix contain Temp_Save_data/
            var target = filename.replace(/^Temp_Save_data\//, "Save_data/");
            var fullPath = MOUNT_PATH + "/" + target;

            if (entry.dir) {
              mkdirp(fullPath);
            } else {
              writes.push(entry.async("uint8array").then(function (data) {
                mkdirp(fullPath.substring(0, fullPath.lastIndexOf("/")));
                FS.writeFile(fullPath, data);
              }));
            }
          });

          return Promise.all(writes);
        }).then(function () {
          FS.syncfs(false, function (err) {
            if (err) {
              console.error("Sync after upload failed:", err);
              notify("OnUploadFailed");
            } else {
              console.log("All files synced to IndexedDB");
              notify("OnIDBFSReady");
            }
          });
        }).catch(function (err) {
          console.error("Zip upload failed:", err);
          notify("OnUploadFailed");
        });
      };
      reader.readAsArrayBuffer(file);
    };

    document.body.appendChild(input);
    input.click();
  }
});
