mergeInto(LibraryManager.library, {
    ClearFileDataStore: function () {
        var dbName = "db:/data";
        var storeName = "FILE_DATA";

        var request = indexedDB.open(dbName);

        request.onsuccess = function (event) {
            var db = event.target.result;

            if (!db.objectStoreNames.contains(storeName)) {
                console.warn("Store not found:", storeName);
                return;
            }

            var transaction = db.transaction([storeName], "readwrite");
            var store = transaction.objectStore(storeName);

            var clearRequest = store.clear();

            clearRequest.onsuccess = function () {
                console.log("FILE_DATA store cleared");
                unityInstance.SendMessage("Save_Manager", "OnFSCleared", ""); 
            };

            clearRequest.onerror = function () {
                console.error("Failed to clear FILE_DATA store");
            };

            transaction.oncomplete = function () {
                db.close();
            };
        };

        request.onerror = function () {
            console.error("Failed to open DB:", dbName);
        };
    }
});