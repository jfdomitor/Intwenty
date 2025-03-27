"use strict";

var getTag = function getTag(id) {
    return document.getElementById(id);
};

var handleIntwentyViewMode = function handleIntwentyViewMode(istoogle) {
    var menucontainer = getTag("main_menu_container");
    var contentcontainer = getTag("main_content_container");
    if (!menucontainer) return;
    if (!contentcontainer) return;

    if (istoogle) {

        if (menucontainer.hasClass("container-fluid")) {
            menucontainer.removeClass("container-fluid");
            menucontainer.addClass("container");
            setCookie('IntwentyViewMode', 'container', 365);
        } else {
            menucontainer.removeClass("container");
            menucontainer.addClass("container-fluid");
            setCookie('IntwentyViewMode', 'container-fluid', 365);
        }

        if (contentcontainer.hasClass("container-fluid")) {
            contentcontainer.removeClass("container-fluid");
            contentcontainer.addClass("container");
        } else {
            contentcontainer.removeClass("container");
            contentcontainer.addClass("container-fluid");
        }
    } else {
        var cookieval = getCookie('IntwentyViewMode');
        if (!cookieval) return;
        if (cookieval == '') return;

        if (menucontainer.hasClass("container-fluid") && cookieval == 'container') {
            menucontainer.removeClass("container-fluid");
            menucontainer.addClass("container");
        }

        if (menucontainer.hasClass("container") && cookieval == 'container-fluid') {
            menucontainer.removeClass("container");
            menucontainer.addClass("container-fluid");
        }

        if (contentcontainer.hasClass("container-fluid") && cookieval == 'container') {
            contentcontainer.removeClass("container-fluid");
            contentcontainer.addClass("container");
        }

        if (contentcontainer.hasClass("container") && cookieval == 'container-fluid') {
            contentcontainer.removeClass("container");
            contentcontainer.addClass("container-fluid");
        }
    }
};

var setCookie = function setCookie(cname, cvalue, exdays) {
    var d = new Date();
    d.setTime(d.getTime() + exdays * 24 * 60 * 60 * 1000);
    var expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
};

var getCookie = function getCookie(cname) {
    var name = cname + "=";
    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
};

var raiseYesNoModal = function raiseYesNoModal(title, message, callback) {
    document.getElementById("confirmModalTitle").textContent = title;
    document.getElementById("confirmModalBody").textContent = message;

    var modal = new bootstrap.Modal(document.getElementById("yesNoModal"));

    document.getElementById("confirmModalOkBtn").onclick = function () {
        modal.hide();
        if (typeof callback === "function") callback();
    };

    modal.show();
};

