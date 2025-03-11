"use strict";

getTag = function (id) {
    return document.getElementById(id);
};

handleIntwentyViewMode = function (istoogle) {
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

setCookie = function (cname, cvalue, exdays) {
    var d = new Date();
    d.setTime(d.getTime() + exdays * 24 * 60 * 60 * 1000);
    var expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
};

getCookie = function (cname) {
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

raiseInformationModal = function (headertext, bodytext, close_callback) {
    getTag('msg_dlg_modal_hdr').textContent = headertext;
    getTag('msg_dlg_modal_text').textContent = bodytext;
    if (close_callback) {
        var closeBtn = getTag('msg_dlg_modal_closebtn');
        closeBtn.replaceWith(closeBtn.cloneNode(true)); // Removes all event listeners
        getTag('msg_dlg_modal_closebtn').addEventListener('click', close_callback);
    }
    var modal = new bootstrap.Modal(getTag('msg_dlg_modal'));
    modal.show();
};

raiseValidationErrorModal = function (message) {
    getTag('msg_dlg_modal_hdr').textContent = 'Error';
    getTag('msg_dlg_modal_text').textContent = message;
    var modal = new bootstrap.Modal(getTag('msg_dlg_modal'));
    modal.show();
};

raiseErrorModal = function (operationresult) {
    getTag('msg_dlg_modal_hdr').textContent = 'Error';
    getTag('msg_dlg_modal_text').textContent = operationresult.userError;
    var modal = new bootstrap.Modal(getTag('msg_dlg_modal'));
    modal.show();
};

raiseYesNoModal = function (headertxt, bodytext, yes_callback) {
    getTag('yesno_dlg_modal_hdr').textContent = headertxt;
    getTag('yesno_dlg_modal_text').textContent = bodytext;
    var yesBtn = getTag('yesno_dlg_modal_yesbtn');
    yesBtn.replaceWith(yesBtn.cloneNode(true)); // Removes all event listeners
    getTag('yesno_dlg_modal_yesbtn').addEventListener('click', yes_callback);

    var modaltag = getTag('yesno_dlg_modal');
    var modal = new bootstrap.Modal(modaltag, { backdrop: 'static', keyboard: true, focus: true });
    modal.show();

    modaltag.addEventListener('hidden.bs.modal', function () {
        document.querySelectorAll('.modal-backdrop').forEach(function (el) {
            return el.remove();
        });
        document.body.classList.remove('modal-open'); // Ensures scrolling is re-enabled
    });
};

hasRequiredValues = function (datalist, requiredlist) {

    for (var i = 0; i < datalist.length; i++) {
        for (var z = 0; z < requiredlist.length; z++) {
            var fld = requiredlist[z];
            if (!datalist[i][fld]) return false;
            if (!datalist[i][fld] === "") return false;
        }
    }

    return true;
};

Array.prototype.where = function (filter) {

    var collection = this;

    switch (typeof filter) {

        case 'function':
            return $.grep(collection, filter);

        case 'object':
            for (var property in filter) {
                if (!filter.hasOwnProperty(property)) continue; // ignore inherited properties

                collection = $.grep(collection, function (item) {
                    return item[property] === filter[property];
                });
            }
            return collection.slice(0); // copy the array
        // (in case of empty object filter)

        default:
            throw new TypeError('func must be either a' + 'function or an object of properties and values to filter by');
    }
};

Array.prototype.firstOrDefault = function (func) {
    return this.where(func)[0] || null;
};

canSave = function (context) {
    var result = true;
    $("[data-required]").each(function () {
        var required = $(this).data('required');
        if (required === "True") {

            var metatype = $(this).data('metatype');
            var dbfield = $(this).data('dbfield');
            var dbtable = $(this).data('dbtable');

            if (!context.model[dbtable][dbfield]) {
                result = false;
                $(this).addClass('requiredNotValid');
            } else if (context.model[dbtable][dbfield].length == 0) {
                result = false;
                $(this).addClass('requiredNotValid');
            } else {
                if (metatype == "EMAILBOX") {
                    var check = context.model[dbtable][dbfield];
                    if (check.indexOf("@") < 1) {
                        result = false;
                        $(this).addClass('requiredNotValid');
                    }
                }
                if (metatype == "PASSWORDBOX") {
                    var check = context.model[dbtable][dbfield].length;
                    if (check > 40) {
                        result = false;
                        $(this).addClass('requiredNotValid');
                    }
                }

                if (result) {
                    $(this).removeClass('requiredNotValid');
                }
            }
        }
    });

    context.$forceUpdate();

    return result;
};

/*
selectableProperties = function (context, item)
{

    if (!item.metaType)
        return [];

    if (!context.propertyCollection)
        return [];

    var result = [];
    for (var i = 0; i < context.propertyCollection.length; i++) {
        var isincluded = false;
        if (context.propertyCollection[i].validFor) {
            for (var z = 0; z < context.propertyCollection[i].validFor.length; z++) {

                if (item.metaType === context.propertyCollection[i].validFor[z])
                    isincluded = true;
            }
        }
        if (isincluded)
            result.push(context.propertyCollection[i]);
    }

    return result;
};


addProperty = function (context, modelitem)
{

    if (!modelitem)
        return;

    if (!modelitem.currentProperty)
        return;

    if (!modelitem.propertyList)
        return;

    if (modelitem.currentProperty.isBoolType) {
        modelitem.currentProperty.codeValue = "TRUE";
        modelitem.currentProperty.displayValue = "True";
    }

    if (modelitem.currentProperty.isStringType || modelitem.currentProperty.isNumericType || modelitem.currentProperty.isListType)
        modelitem.currentProperty.displayValue = modelitem.currentProperty.codeValue;


    var t = modelitem.propertyList.firstOrDefault({ codeName: modelitem.currentProperty.codeName });
    if (t != null)
        return;

    if (!modelitem.currentProperty.codeValue)
        return;


    modelitem.propertyList.push({ codeName: modelitem.currentProperty.codeName, codeValue: modelitem.currentProperty.codeValue, displayValue: modelitem.currentProperty.displayValue });

    modelitem.currentProperty.codeValue = "";

    context.$forceUpdate();
};

deleteProperty = function (property, modelitem)
{

    if (!property)
        return;

    if (!modelitem)
        return;

    if (!modelitem.propertyList)
        return;

    for (var i = 0; i < modelitem.propertyList.length; i++) {
        if (modelitem.propertyList[i].codeName === property.codeName) {
            modelitem.propertyList.splice(i, 1);
            break;
        }
    }
};

initializePropertyUI = function (context, modelitem) {
    if (!modelitem)
        return;

    modelitem.currentProperty = { isBoolType: false, isStringType: false, isNumericType: false, isListType: false };
    if (!modelitem.propertyList)
        modelitem.propertyList = [];

    if (!modelitem.hasOwnProperty("showSettings"))
        modelitem.showSettings = false;

    modelitem.showSettings = !modelitem.showSettings;

    context.$forceUpdate();

};
*/

canShowUIControl = function (uiid, tablename, columnname, context) {
    return true;
};

isRequiredNotValid = function (uiid, context) {
    return $("#" + uiid).hasClass("requiredNotValid");
};

onUserInput = function (event, context) {
    if (!event) return;

    var elementId = event.srcElement.id;
    if (!elementId) return;

    //Remove requiredNotValid if the input is valid
    $("[data-required]").each(function () {
        var required = $(this).data('required');
        var id = $(this).attr('id');
        if (required === "True" && id === elementId) {
            var val = event.srcElement.value;
            if (val) {
                if (val.length > 0) $("#" + elementId).removeClass('requiredNotValid');
            }
        }
    });
};

downloadExcel = function () {};

test = function (context) {
    alert(context.appId);
};

