function handleIntwentyViewMode(istoogle) {
    var menucontainer = $("#main_menu_container");
    var contentcontainer = $("#main_content_container");
    if (!menucontainer)
        return;
    if (!contentcontainer)
        return;

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
    }
    else {
        var cookieval = getCookie('IntwentyViewMode');
        if (!cookieval)
            return;
        if (cookieval == '')
            return;

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

function setCookie(cname, cvalue, exdays)
{
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));
    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";path=/";
}

function getCookie(cname)
{
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

function raiseInformationModal(headertext, bodytext, close_callback) {
    $('#msg_dlg_modal_hdr').text(headertext);
    $('#msg_dlg_modal_text').text(bodytext);
    if (close_callback) {
        $('#msg_dlg_modal_closebtn').off('click', close_callback);
        $('#msg_dlg_modal_closebtn').off().on('click', close_callback);
    }
    $('#msg_dlg_modal').modal();

};


function raiseValidationErrorModal(message) {
    $('#msg_dlg_modal_hdr').text('Error');
    $('#msg_dlg_modal_text').text(message);
    $('#msg_dlg_modal').modal();

};

function raiseErrorModal(operationresult) {
    $('#msg_dlg_modal_hdr').text('Error');
    $('#msg_dlg_modal_text').text(operationresult.userError);
    $('#msg_dlg_modal').modal();

};

function raiseYesNoModal(headertxt, bodytext, yes_callback) {
    $('#yesno_dlg_modal_hdr').text(headertxt);
    $('#yesno_dlg_modal_text').text(bodytext);
    $('#yesno_dlg_modal_yesbtn').off('click', yes_callback);
    $('#yesno_dlg_modal_yesbtn').off().on('click', yes_callback);
    $('#yesno_dlg_modal').modal();

};

function hasRequiredValues(datalist, requiredlist) {

    for (var i = 0; i < datalist.length; i++) {
        for (var z = 0; z < requiredlist.length; z++) {
            var fld = requiredlist[z];
            if (!datalist[i][fld])
                return false;
            if (!datalist[i][fld] === "")
                return false;

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
                if (!filter.hasOwnProperty(property))
                    continue; // ignore inherited properties

                collection = $.grep(collection, function (item) {
                    return item[property] === filter[property];
                });
            }
            return collection.slice(0); // copy the array 
        // (in case of empty object filter)

        default:
            throw new TypeError('func must be either a' +
                'function or an object of properties and values to filter by');
    }
};


Array.prototype.firstOrDefault = function (func) {
    return this.where(func)[0] || null;
};



Vue.prototype.selectableProperties = function (item) {
    var context = this;

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


Vue.prototype.addProperty = function (modelitem) {

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

    this.$forceUpdate();
};

Vue.prototype.deleteProperty = function (property, modelitem) {

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

Vue.prototype.initializePropertyUI = function (modelitem) {
    if (!modelitem)
        return;

    modelitem.currentProperty = { isBoolType: false, isStringType: false, isNumericType: false, isListType: false };
    if (!modelitem.propertyList)
        modelitem.propertyList = [];

    if (!modelitem.hasOwnProperty("showSettings"))
        modelitem.showSettings = false;

    modelitem.showSettings = !modelitem.showSettings;

    this.$forceUpdate();

};

Vue.prototype.propertyChanged = function ()
{
    this.$forceUpdate();
};

Vue.component("searchbox", {
    template: '<input><slot></slot></input>',
    props: ['idfield', 'textfield'],
    data: function () {
        return { selectizeinstance: null };

    },
    mounted: function () {
        var vm = this;
        var element = $(this.$el);

        var jsmethod = $(element).data('jsmethod');
        var domainname = $(element).data('domain');
        var usearch = $(element).data('usesearch');
        var mselect = $(element).data('multiselect');
        var acreate = $(element).data('allowcreate');
        var allowcreate = (acreate == "TRUE");
        var usesearch = (usearch == "TRUE");
        var usemultiselect = (mselect == "TRUE");

        var plugs = null;
        var maxitems = 1;
        if (usemultiselect) {
            maxitems = 10;
            plugs = ['remove_button'];
        }

        if (!jsmethod)
            jsmethod = 'getDomain';

        element.selectize({
            plugins: plugs
            , delimiter: ','
            , maxItems: maxitems
            , valueField: 'code'
            , labelField: 'display'
            , searchField: 'display'
            , options: []
            , create: allowcreate
            , preload: true
            , load: function (query, callback) {

                if (!query || !usesearch)
                    query = 'ALL';
                if (usesearch && query == 'ALL')
                    query = 'PRELOAD';

                if (!domainname) return callback();

                if (vm.$root[jsmethod])
                {
                    vm.$root[jsmethod](domainname, query, function (response) {
                        callback(response);
                        if (vm.idfield) {
                            var persisteditems = vm.idfield.split(",");
                            for (var i = 0; i < persisteditems.length; i++) {
                                element[0].selectize.addItem(persisteditems[i], true);
                            }
                        }
                    });
                }
                
          
            }

        }).on('change', function () {

            var selected_objects = $.map(element[0].selectize.items, function (value) {
                return element[0].selectize.options[value];
            });

            var codestr = "";
            var valstr = "";
            var delim = "";
            for (var i = 0; i < selected_objects.length; i++) {
                var code = selected_objects[i].code;
                var val = selected_objects[i].value;
                codestr += delim + code;
                valstr += delim + val;
                delim = ",";

            }
            vm.$emit('update:idfield', codestr);
            vm.$emit('update:textfield', valstr);

        });

        vm.selectizeinstance = element[0].selectize;
    },
    updated: function ()
    {
       
    },
    watch:
    {
        idfield: function (newval, oldval) {
            var t = "";


            if (this.selectizeinstance) {
                this.selectizeinstance.clear(true);

                if (newval) {
                    var persisteditems = newval.split(",");
                    for (var i = 0; i < persisteditems.length; i++) {
                        //ADD ITEM BUT DONT TRIGGER CHANGE
                        this.selectizeinstance.addItem(persisteditems[i], true);

                    }
                }
            }


        },
        textfield: function (newval, oldval)
        {
          
        }


    },
    destroyed: function () {
        this.selectizeinstance.destroy();
    }
});

Vue.component("combobox", {
    template: '<select><slot></slot></select>',
    props: ['idfield', 'textfield'],
    data: function () {
        return { selectizeinstance: null };

    },
    mounted: function () {
        var vm = this;
        var element = $(this.$el);

        var domainname = $(element).data('domain');
        var jsmethod = $(element).data('jsmethod');
        if (!jsmethod)
            jsmethod = 'getDomain';

        element.selectize({
            delimiter: ','
            , maxItems: 1
            , valueField: 'code'
            , labelField: 'display'
            , searchField: 'display'
            , options: []
            , create: false
            , preload: true
            , load: function (query, callback) {

                if (!domainname) return callback();

                if (vm.$root[jsmethod])
                {
                    vm.$root[jsmethod](domainname, 'ALL', function (response) {
                        callback(response);
                        if (vm.idfield) {
                            var persisteditems = vm.idfield.split(",");
                            for (var i = 0; i < persisteditems.length; i++) {
                                element[0].selectize.addItem(persisteditems[i], true);
                            }
                        }
                    });
                }
               

              
            }

        }).on('change', function () {

            var selected_objects = $.map(element[0].selectize.items, function (value) {
                return element[0].selectize.options[value];
            });

            var codestr = "";
            var valstr = "";
            var delim = "";
            for (var i = 0; i < selected_objects.length; i++) {
                var code = selected_objects[i].code;
                var val = selected_objects[i].value;
                codestr += delim + code;
                valstr += delim + val;
                delim = ",";

            }
            vm.$emit('update:idfield', codestr);
            vm.$emit('update:textfield', valstr);

        });

        vm.selectizeinstance = element[0].selectize;


    },
    updated: function ()
    {
       
    },
    watch:
    {

        idfield: function (newval, oldval)
        {
       
            if (this.selectizeinstance) {
                this.selectizeinstance.clear(true);

                if (newval) {
                    var persisteditems = newval.split(",");
                    for (var i = 0; i < persisteditems.length; i++) {
                        //ADD ITEM BUT DONT TRIGGER CHANGE
                        this.selectizeinstance.addItem(persisteditems[i], true);
                    }
                }
            }
        },
        textfield: function (newval, oldval)
        {
        }
    },
    destroyed: function () {
        this.selectizeinstance.destroy();
    }
});

Vue.component("radiolist", {
    template: `<div>
                <template v-for="item in domvalues">
                 <div v-bind:id="controlid + '_' + item.code + '_parent'" v-bind:class="checkBoxClass">
                     <input class="form-check-input" type="radio" v-bind:id="controlid + '_' + item.code" v-bind:name="controlid" v-bind:value="item.code" v-on:change="radiochanged(event)" />
                     <label class="form-check-label">{{item.value}}</label>
                </div>
                </template>
               </div>`,
    props: ['idfield', 'textfield'],
    data: function () {
        return { domvalues: [], controlid: "", orientation: "HORIZONTAL", domainname: "" };

    },
    mounted: function () {
        var vm = this;
        var element = $(this.$el);

        vm.jsmethod = $(element).data('jsmethod');
        vm.domainname = $(element).data('domain');
        vm.orientation = $(element).data('orientation');
        vm.controlid = $(element).attr('id');

        if (!vm.jsmethod)
            vm.jsmethod = 'getDomain';

        if (vm.domainname) {
            if (vm.$root[vm.jsmethod]) {
                vm.$root[vm.jsmethod](vm.domainname, 'ALL', function (response) {
                    vm.domvalues = response;
                });
            }
        }
    },
    methods:
    {
        radiochanged: function (event)
        {
            if (!event)
                return;
            if (!event.srcElement)
                return;
            if (!event.srcElement.value)
                return;

            var domainvalue = null;
            for (var i = 0; i < this.domvalues.length; i++)
            {
                if (this.domvalues[i].code == event.srcElement.value)
                {
                    domainvalue = this.domvalues[i];
                    break;
                }

            }

            if (domainvalue) {
                this.$emit('update:idfield', domainvalue.code);
                this.$emit('update:textfield', domainvalue.value);
            }

        }
    }
    ,updated: function ()
    {
       
    },
    watch:
    {

        idfield: function (newval, oldval)
        {
            if (newval)
            {
                $("input[name=" + this.controlid + "][value=" + newval + "]").prop('checked', true);
            }
          
        },
        textfield: function (newval, oldval)
        {
        }
    },
    destroyed: function ()
    {
    },
    computed:
    {
        checkBoxClass: function () {
            return {
                'form-check form-check-inline': this.orientation === 'HORIZONTAL'
                , 'form-check': this.orientation === 'VERTICAL'
            }
        }
    }
});

Vue.component("checklist", {
    template: `<div>
                <template v-for="item in domvalues">
                 <div v-bind:id="controlid + '_' + item.code + '_parent'" v-bind:class="checkBoxClass">
                     <input class="form-check-input" type="checkbox" v-bind:id="controlid + '_' + item.code" v-bind:data-domainvalue="item.code" v-on:input="checkchanged(event)" />
                     <label class="form-check-label">{{item.value}}</label>
                </div>
                </template>
               </div>`,
    props: ['idfield', 'textfield'],
    data: function () {
        return { domvalues: [], controlid: "", selecteditems:[], orientation:"HORIZONTAL", domainname:"" };

    },
    mounted: function () {
        var vm = this;
        var element = $(this.$el);

        vm.jsmethod = $(element).data('jsmethod');
        vm.domainname = $(element).data('domain');
        vm.orientation = $(element).data('orientation');
        vm.controlid = $(element).attr('id');

        if (!vm.jsmethod)
            vm.jsmethod = 'getDomain';

        if (vm.domainname)
        { 
            if (vm.$root[vm.jsmethod]) {
                vm.$root[vm.jsmethod](vm.domainname, 'ALL', function (response) {
                    vm.domvalues = response;
                });
            } 
        }
    },
    methods:
    {
        checkchanged: function (event)
        {
            var ischecked = $(event.srcElement).is(':checked');
            var domainvalue = $(event.srcElement).data('domainvalue');
           

            if (!ischecked) {
                for (var i = 0; i < this.selecteditems.length; i++) {
                    if (this.selecteditems[i].code == domainvalue) {
                        this.selecteditems.splice(i, 1);
                    }
                }
            }
            else {

                var itemtoadd = null;
                for (var i = 0; i < this.domvalues.length; i++)
                {
                    if (this.domvalues[i].code == domainvalue)
                    {
                        itemtoadd = this.domvalues[i];
                        break;
                    }
                }

                if (itemtoadd)
                { 
                    var exists = false;
                    for (var i = 0; i < this.selecteditems.length; i++) {
                        if (this.selecteditems[i].code == domainvalue) {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                        this.selecteditems.push(itemtoadd);
                }
            }

            var codestr = "";
            var valstr = "";
            var delim = "";
            for (var i = 0; i < this.selecteditems.length; i++) {
                var code = this.selecteditems[i].code;
                var val = this.selecteditems[i].value;
                codestr += delim + code;
                valstr += delim + val;
                delim = ",";

            }

            this.$emit('update:idfield', codestr);
            this.$emit('update:textfield', valstr);

        }
    }
    , updated: function () {

    },
    watch:
    {

        idfield: function (newval, oldval)
        {
            var element = $(this.$el);
            var controlid = $(element).attr('id');

            this.selecteditems=[];

            if (newval)
            {
                var persisteditems = newval.split(",");
                for (var i = 0; i < persisteditems.length; i++)
                {
                    for (var x = 0; x < this.domvalues.length; x++)
                    {
                        if (this.domvalues[x].code == persisteditems[i])
                        {
                            this.selecteditems.push(this.domvalues[x]);
                            $("#" + controlid + "_" + this.domvalues[x].code).prop('checked', true);
                        }

                    }

                }
            }

        },
        textfield: function (newval, oldval) {

        }


    },
    destroyed: function () {

    },
    computed:
    {
        checkBoxClass: function () {
            return {
                'form-check form-check-inline': this.orientation === 'HORIZONTAL'
                ,'form-check': this.orientation === 'VERTICAL'
            }
        }
    }
});


Vue.prototype.onFileUpload = function () {

};

Vue.prototype.uploadImage = function (event) {
    var context = this;
    var endpoint = context.baseurl + 'UploadImage';
    var formData = new FormData();
    formData.append('File', event.srcElement.files[0]);
    formData.append('FileName', event.srcElement.files[0].name);
    formData.append('FileSize', event.srcElement.files[0].size);

    var xhr = new XMLHttpRequest();
    xhr.onreadystatechange = function () {
        if (xhr.readyState === 4) {
            var dbtable = event.srcElement.dataset.dbtable;
            var dbfield = event.srcElement.dataset.dbfield;

            var fileref = JSON.parse(xhr.response);
            context.model[dbtable][dbfield] = "/USERDOC/" + fileref.fileName;
            context.$forceUpdate();
        }
    }
    xhr.open('POST', endpoint, true);
    xhr.send(formData);
};

Vue.prototype.canSave = function () {
    var context = this;
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

            }
            else if (context.model[dbtable][dbfield].length == 0) {
                result = false;
                $(this).addClass('requiredNotValid');
            }
            else {
                if (metatype == "EMAILBOX") {
                    var check = context.model[dbtable][dbfield]
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

Vue.prototype.isRequiredNotValid = function (uiid) {
    return $("#" + uiid).hasClass("requiredNotValid");
};

Vue.prototype.canShowUIControl = function (uiid, tablename, columnname)
{
    return true;
};

Vue.prototype.onUserInput = function (event) {
    if (!event)
        return;

    var elementId = event.srcElement.id;
    if (!elementId)
        return;

    //Remove requiredNotValid if the input is valid
    $("[data-required]").each(function () {
        var required = $(this).data('required');
        var id = $(this).attr('id');
        if (required === "True" && id === elementId) {
            var val = event.srcElement.value;
            if (val) {
                if (val.length > 0)
                    $("#" + elementId).removeClass('requiredNotValid');
            }
        }
    });
};


Vue.prototype.downloadExcel = function () {

};

