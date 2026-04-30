mergeInto(LibraryManager.library, {
    ClearFileDataStore: function () {
        var dbName = "/data";
        var request = indexedDB.open(dbName);

        request.onsuccess = function (event) {
                    var db = event.target.result;

                    var transaction = db.transaction(db.objectStoreNames, "readwrite");

                    transaction.oncomplete = function () {
                        unityInstance.SendMessage("Save_Manager", "OnFSCleared", "");    
                        console.log("All stores cleared");
                    };

                    for (var i = 0; i < db.objectStoreNames.length; i++) {
                        var storeName = db.objectStoreNames[i];
                        var store = transaction.objectStore(storeName);
                        store.clear();
                    }
                };

                request.onerror = function () {
                    console.error("Failed to open DB:", dbName);
                };
    }
});