mergeInto(LibraryManager.library, {
  ClearFileDataStore: function () {

    function notify(method) {
      try {
        if (typeof unityInstance !== "undefined" && unityInstance) unityInstance.SendMessage("Save_Manager", method, "");
        else if (typeof Module !== "undefined" && Module.SendMessage) Module.SendMessage("Save_Manager", method, "");
        else console.error("No Unity instance available for SendMessage");
      } catch (e) { console.error("SendMessage failed:", e); }
    }

    var request = indexedDB.open("/data");

    request.onerror = function () {
      console.error("Failed to open DB: /data");
      notify("OnFSCleared"); // don't leave the C# coroutine waiting forever
    };

    request.onsuccess = function (event) {
      var db = event.target.result;
      var names = Array.prototype.slice.call(db.objectStoreNames);

      // First run: DB can exist with no stores; db.transaction([]) would throw.
      if (names.length === 0) {
        db.close();
        notify("OnFSCleared");
        return;
      }

      var finished = false;
      function finish(msg) {
        if (finished) return;
        finished = true;
        console.log(msg);
        db.close(); // an open connection can block IDBFS from opening/upgrading the DB
        notify("OnFSCleared");
      }

      var tx = db.transaction(names, "readwrite");
      tx.oncomplete = function () { finish("All stores cleared"); };
      tx.onerror = function () { finish("Clear failed (error)"); };
      tx.onabort = function () { finish("Clear failed (abort)"); };
      names.forEach(function (n) { tx.objectStore(n).clear(); });
    };
  }
});
