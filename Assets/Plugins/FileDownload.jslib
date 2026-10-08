mergeInto(LibraryManager.library, {
  DownloadZipAndStoreLocally: function () {

    var MOUNT_PATH = "/data";

    // On WebGL, SaveDataAsJson writes to Temp_Save_data and nothing copies it to Save_data.
    // true  = put Temp_Save_data contents into the zip under "Save_data/" (what the loader reads)
    // false = zip folders exactly as they are on disk
    var REMAP_TEMP_TO_SAVE = true;

    function notify(method) {
      try {
        if (typeof unityInstance !== "undefined" && unityInstance) unityInstance.SendMessage("Save_Manager", method, "");
        else if (typeof Module !== "undefined" && Module.SendMessage) Module.SendMessage("Save_Manager", method, "");
        else console.error("No Unity instance available for SendMessage");
      } catch (e) { console.error("SendMessage failed:", e); }
    }

    if (typeof JSZip === "undefined") {
      console.error("JSZip is not loaded - add it to index.html");
      notify("OnDownloadFailed");
      return;
    }
    if (!FS.analyzePath(MOUNT_PATH).exists) {
      console.error(MOUNT_PATH + " is not mounted");
      notify("OnDownloadFailed");
      return;
    }

    // populate=false: write in-memory FS out to IndexedDB
    FS.syncfs(false, function (err) {
      if (err) {
        console.error("Sync before download failed:", err);
        notify("OnDownloadFailed");
        return;
      }

      console.log("Sync completed. Zipping...");
      var zip = new JSZip();
      var fileCount = 0;

      function addDir(dirPath, zipPrefix) {
        FS.readdir(dirPath).forEach(function (entry) {
          if (entry === "." || entry === "..") return;
          var fullPath = dirPath + "/" + entry;
          var zipPath = zipPrefix + entry;
          if (FS.isDir(FS.stat(fullPath).mode)) {
            addDir(fullPath, zipPath + "/");
          } else {
            zip.file(zipPath, FS.readFile(fullPath)); // Uint8Array
            fileCount++;
          }
        });
      }

      try {
        var savePath = MOUNT_PATH + "/Save_data";
        var tempPath = MOUNT_PATH + "/Temp_Save_data";
        if (FS.analyzePath(savePath).exists) addDir(savePath, "Save_data/");
        if (FS.analyzePath(tempPath).exists) {
          // same zip path overwrites, so fresh temp files win over old Save_data files
          addDir(tempPath, REMAP_TEMP_TO_SAVE ? "Save_data/" : "Temp_Save_data/");
        }
      } catch (e) {
        console.error("Zip traversal failed:", e);
        notify("OnDownloadFailed");
        return;
      }

      if (fileCount === 0) {
        console.error("No files found to zip");
        notify("OnDownloadFailed");
        return;
      }

      zip.generateAsync({ type: "blob" }).then(function (blob) {
        var url = URL.createObjectURL(blob);
        var a = document.createElement("a");
        a.href = url;
        a.download = "Save_data.zip";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(function () { URL.revokeObjectURL(url); }, 1000);
        console.log("Download started (" + fileCount + " files).");
        notify("OnDownloadComplete");
      }).catch(function (e) {
        console.error("Zip generation failed:", e);
        notify("OnDownloadFailed");
      });
    });
  }
});
