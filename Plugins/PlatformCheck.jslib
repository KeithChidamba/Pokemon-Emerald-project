mergeInto(LibraryManager.library, {
    CheckIfMobileBrowser: function () {
        console.log("checking device");
        var ua = navigator.userAgent || navigator.vendor || window.opera;
        var isMobile = /android|iphone|ipad|ipod/i.test(ua) ? 1 : 0;

        if(isMobile==1){
             console.log("is mobile");
            SendMessage("Game_Loader", "ConfirmIsMobile", "");
        }else{
              console.log("is not mobile");
            SendMessage("Game_Loader", "DenyIsMobile", "");
        }
        
    }
});