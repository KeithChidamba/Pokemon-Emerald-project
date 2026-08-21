mergeInto(LibraryManager.library, {
  DownloadZipAndStoreLocally: function () {
    FS.syncfs(function (err) {
      if (err) {
        console.error("Sync before download failed:", err);
        return;
      }

      console.log("Sync completed. Zipping...");

      const zip = new JSZip();

      function traverse(path) {
        const entries = FS.readdir(path);
        for (const entry of entries) {
          if (entry === "." || entry === "..") continue;

          const fullPath = `${path}/${entry}`;
          const stat = FS.stat(fullPath);

          if (FS.isDir(stat.mode)) {
            traverse(fullPath);
          } else {
            try {
              const data = FS.readFile(fullPath, { encoding: "binary" });
              zip.file(fullPath.replace("/data/", ""), data);
            } catch (e) {
              console.error("Failed to read file:", fullPath, e);
            }
          }
        }
      }

      traverse("/data");

      zip.generateAsync({ type: "blob" }).then(function (blob) {
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = "Save_data.zip";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        console.log("Download started.");
        unityInstance.SendMessage("Save_Manager", "OnDownloadComplete", "");
      });
    });
  }
});
