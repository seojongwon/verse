window.verseCookie = {
    get: function (name) {
        var key = name + "=";
        var parts = document.cookie.split("; ");
        for (var i = 0; i < parts.length; i++) {
            var part = parts[i];
            if (part.indexOf(key) === 0) {
                return decodeURIComponent(part.substring(key.length));
            }
        }
        return null;
    },
    set: function (name, value, days) {
        var expires = "";
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
            expires = "; expires=" + date.toUTCString();
        }
        document.cookie = name + "=" + encodeURIComponent(value || "") + expires + "; path=/";
    }
};
