var getTag = function (id) { return document.getElementById(id) };


function handleIntwentyViewMode(istoogle) {
    var menucontainer = getTag("main_menu_container");
    var contentcontainer = getTag("main_content_container");
    if (!menucontainer || !contentcontainer) return;

    function toggleClass(element, class1, class2) {
        if (element.classList.contains(class1)) {
            element.classList.remove(class1);
            element.classList.add(class2);
        } else {
            element.classList.remove(class2);
            element.classList.add(class1);
        }
    }

    if (istoogle) {
        toggleClass(menucontainer, "container-fluid", "container");
        toggleClass(contentcontainer, "container-fluid", "container");
        setCookie('IntwentyViewMode', menucontainer.classList.contains("container") ? 'container' : 'container-fluid', 365);
    } else {
        var cookieval = getCookie('IntwentyViewMode');
        if (!cookieval) return;

        menucontainer.classList.remove("container", "container-fluid");
        contentcontainer.classList.remove("container", "container-fluid");

        menucontainer.classList.add(cookieval);
        contentcontainer.classList.add(cookieval);
    }
}


var setCookie = function (cname, cvalue, exdays) {
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

var getCookie = function (cname) {
    let name = cname + "=";
    let decodedCookie = decodeURIComponent(document.cookie);
    let ca = decodedCookie.split(';');
    for (let i = 0; i < ca.length; i++) {
        let c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}


var raiseYesNoModal = function(title, message, callback) {
    document.getElementById("confirmModalTitle").textContent = title;
    document.getElementById("confirmModalBody").textContent = message;

    let modal = new bootstrap.Modal(document.getElementById("yesNoModal"));

    document.getElementById("confirmModalOkBtn").onclick = function () {
        modal.hide();
        if (typeof callback === "function") callback();
    };

    modal.show();
}



