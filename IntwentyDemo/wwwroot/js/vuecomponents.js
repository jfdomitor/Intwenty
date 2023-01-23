const buttoncounter =
{
    data() {
        return { count: 0 }
    },
    template: `<div>count is {{ count }}</div>`
};

const radiolist =
{

    template: `<div>
                <template v-for="item in domvalues">
                 <div v-bind:id="controlid + '_' + item.code + '_parent'" v-bind:class="checkBoxClass">
                     <input class="form-check-input" type="radio" v-bind:id="controlid + '_' + item.code" v-bind:name="controlid" v-bind:value="item.code" v-bind:data-textval="item.value" v-model="idfield" v-on:change="radiochanged($event)" />
                     <label class="form-check-label">{{item.value}}</label>
                </div>
                </template>
               </div>`,
    props: {
        idfield: String,
        textfield: String
    },
    emits: ['update:idfield','update:textfield'],
    data() {
        return { domvalues: [], controlid: "", orientation: "HORIZONTAL", domainname: "" }
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
        radiochanged: function (event) {
            if (!event)
                return;
            if (!event.srcElement)
                return;
            if (!event.srcElement.value)
                return;

            var codeoption = event.srcElement.value;
            var textoption = event.srcElement.dataset.textval;
            /*
            var domainvalue = null;
            for (var i = 0; i < this.domvalues.length; i++) {
                if (this.domvalues[i].code == event.srcElement.value) {
                    domainvalue = this.domvalues[i];
                    break;
                }

            }*/

            if (codeoption) {
                this.$emit('update:idfield', codeoption);
            }
            if (textoption) {
                this.$emit('update:textfield', textoption);
            }

        },
       
    }
    , updated: function () {

    },
    watch:
    {

        idfield: function (newval, oldval) {
            if (newval) {
                $("input[name=" + this.controlid + "][value=" + newval + "]").prop('checked', true);
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

};

const searchbox =
{
    template: `<div><div></div></div>`,
    props: {
        idfield: String,
        textfield: String
    },
    emits: ['update:idfield', 'update:textfield'],
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

                if (vm.$root[jsmethod]) {
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
    updated: function () {

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
};

const combobox =
{
    template: '<div></div>',
    props: {
        idfield: String,
        textfield: String
    },
    emits: ['update:idfield', 'update:textfield'],
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

                if (vm.$root[jsmethod]) {
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
    updated: function () {

    },
    watch:
    {

        idfield: function (newval, oldval) {

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
        textfield: function (newval, oldval) {
        }
    },
    destroyed: function () {
        this.selectizeinstance.destroy();
    }
};

const checklist =
{
    template: `<div>
                <template v-for="item in domvalues">
                 <div v-bind:id="controlid + '_' + item.code + '_parent'" v-bind:class="checkBoxClass">
                     <input class="form-check-input" type="checkbox" v-bind:id="controlid + '_' + item.code" v-bind:data-domainvalue="item.code" v-bind:value="item.code" v-bind:data-textval="item.value" v-on:input="checkchanged($event)" />
                     <label class="form-check-label">{{item.value}}</label>
                </div>
                </template>
               </div>`,
    props: {
        idfield: String,
        textfield: String
    },
    emits: ['update:idfield', 'update:textfield'],
    data: function () {
        return { domvalues: [], controlid: "", selecteditems: [], orientation: "HORIZONTAL", domainname: "" };

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
        checkchanged: function (event)
        {
    
            if (!event)
                return;
            if (!event.srcElement)
                return;
            if (!event.srcElement.value)
                return;

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
                for (var i = 0; i < this.domvalues.length; i++) {
                    if (this.domvalues[i].code == domainvalue) {
                        itemtoadd = this.domvalues[i];
                        break;
                    }
                }

                if (itemtoadd) {
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
    },
    updated: function () {
        var vm = this;

        if (vm.idfield && vm.selecteditems.length==0) {
            var persisteditems = vm.idfield.split(",");
            for (var i = 0; i < persisteditems.length; i++) {
                for (var x = 0; x < vm.domvalues.length; x++) {
                    if (vm.domvalues[x].code == persisteditems[i]) {
                        vm.selecteditems.push(vm.domvalues[x]);
                        $("#" + vm.controlid + "_" + vm.domvalues[x].code).prop('checked', true);
                    }

                }

            }
        }
    },
    watch:
    {

        idfield: function (newval, oldval)
        {
            var element = $(this.$el);
            var controlid = $(element).attr('id');

            this.selecteditems = [];

            if (newval) {
                var persisteditems = newval.split(",");
                for (var i = 0; i < persisteditems.length; i++) {
                    for (var x = 0; x < this.domvalues.length; x++) {
                        if (this.domvalues[x].code == persisteditems[i]) {
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
};

const propertytool =
{

    template: `<div>
    <hr />
    <div class="form-inline">

        <div class="mr-2">
            <select id="propcbox"
                    name="propcbox"
                    v-model="propName"
                    v-on:change="propertyChanged($event)"
                    class="form-control form-control-sm">
                <option v-for="item in selectableProperties()" v-bind:value="item.codeName">{{item.displayName}}</option>
            </select>
        </div>
        <div class="mr-2" v-if="currentProperty.isBoolType">
        </div>
        <div class="mr-2" v-if="currentProperty.isStringType">
            <input class="form-control-sm" v-model="propValue" type="text" />
        </div>
        <div class="mr-2" v-if="currentProperty.isNumericType">
            <input class="form-control-sm" v-model="propValue" type="number" />
        </div>
        <div class="mr-2" v-if="currentProperty.isListType">
            <div class="form-group">
                <select id="cboxpropvalues"
                        name="cboxpropvalues"
                        v-model="propValue"
                        class="form-control form-control-sm">
                    <option v-for="item in selectableValues()" v-bind:value="item.codeValue">{{item.displayValue}}</option>
                </select>
            </div>
        </div>
        <button class="btn btn-sm btn-secondary mr-2" v-on:click="addProperty()" style="margin-left:2px"><span class="fa fa-plus"></span> Add</button>
    </div>
    <hr />
    <table class="table table-sm" style="width:90%">
        <thead>
            <tr>
                <th>Property</th>
                <th>Value</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="property in propobject.propertyList">
                <td>{{property.codeName}}</td>
                <td>{{property.displayValue}}</td>
                <td><span v-on:click="deleteProperty(property)" class="fa fa-trash" style="cursor:pointer"></span></td>
            </tr>
        </tbody>
    </table>
</div>`,
    props: {
        propobject: {},
        propcollection: []
    },
    emits: ['update:propobject'],
    data() {
        return {
            metaType: "", currentProperty: { codeName: "", codeValue: "", displayValue: "", displayName: "", isBoolType: false, isStringType: false, isNumericType: false, isListType: false, validValues: [] }, propName: "", propValue: ""
        };
    },
    mounted: function ()
    {
        var vm = this;


        if (!vm.propobject.metaType)
            return;

        vm.metaType = propobject.metaType;
        if (!vm.propobject.propertyList)
             vm.propobject.propertyList = [];

        if (!vm.propobject.hasOwnProperty("showSettings"))
            vm.propobject.showSettings = false;

        vm.propobject.showSettings = !vm.propobject.showSettings;

    },
    methods:
    {
        copyProperty: function (propcode) {

            var vm = this;
            if (!vm.propcollection)
                return;
            if (!propcode)
                return;
            if (propcode == "")
                return;

            var prop = { codeName: "", codeValue: "", displayValue: "", displayName: "", isBoolType: false, isStringType: false, isNumericType: false, isListType: false, validValues: [] };
            for (var i = 0; i < vm.propcollection.length; i++)
            {
                if (vm.propcollection[i].codeName == propcode)
                {
                    prop.codeName = vm.propcollection[i].codeName;
                    prop.codeValue = vm.propcollection[i].codeValue;
                    prop.displayName = vm.propcollection[i].displayName;
                    prop.displayValue = vm.propcollection[i].displayValue;
                    prop.isNumericType = vm.propcollection[i].isNumericType;
                    prop.isStringType = vm.propcollection[i].isStringType;
                    prop.isBoolType = vm.propcollection[i].isBoolType;
                    prop.isListType = vm.propcollection[i].isListType;
                    prop.validValues = vm.propcollection[i].validValues;
                    break;
                }
            }

            return prop;
        },
        selectableProperties: function ()
        {

            var vm = this;
            if (!vm.propcollection)
                return;


            var result = [];
            for (var i = 0; i < vm.propcollection.length; i++) {
                var isincluded = false;
                if (vm.propcollection[i].validFor) {
                    for (var z = 0; z < vm.propcollection[i].validFor.length; z++) {

                        if (vm.metaType === vm.propcollection[i].validFor[z])
                            isincluded = true;
                    }
                }
                if (isincluded)
                    result.push(vm.copyProperty(vm.propcollection[i].codeName));
            }

            return result;
        },
        selectableValues: function ()
        {
            var vm = this;

            if (!vm.propobject)
                return;

            if (!vm.propName)
                return;

            if (vm.propName=="")
                return;

            var prop = vm.copyProperty(vm.propName);

            return prop.validValues;
        },
        addProperty : function () {

            var vm = this;

            if (!vm.propobject)
                return;

            if (!vm.propName)
                return;

            if (!vm.propValue)
                return;

            if (!vm.propobject.propertyList)
                return;

            //Property already exists
            var filter = { codeName: vm.propName };
            var existingprop = vm.propobject.propertyList.where(filter);
            if (existingprop)
            {
                if (existingprop.length > 0)
                {
                    return;
                }
            }


            var selprop = vm.copyProperty(vm.propName);

            if (selprop.isListType)
            {
                var filter = { codeValue: vm.propValue };
                var selvalue = selprop.validValues.where(filter);
                if (selvalue) {
                    selprop.displayValue = selvalue[0].displayValue;
                    selprop.codeValue = selvalue[0].codeValue;
                }
            }

            if (selprop.isBoolType) {
                selprop.codeValue = "TRUE";
                selprop.displayValue = "True";
            }

            if (selprop.isStringType || selprop.isNumericType)
            {
                selprop.codeValue = vm.propValue;
                selprop.displayValue = vm.propValue;
            }

            vm.propobject.propertyList.push(selprop);

            this.$emit('update:propobject', vm.propobject);

            //vm.currentProperty = {};

            //context.$forceUpdate();
        },
        deleteProperty : function (property) {

            var vm = this;

            if (!vm.propobject)
                return;

            if (!vm.currentProperty)
                return;

            if (!vm.propobject.propertyList)
                return;

            for (var i = 0; i < vm.propobject.propertyList.length; i++) {
                if (vm.propobject.propertyList[i].codeName === property.codeName) {
                    vm.propobject.propertyList.splice(i, 1);
                    break;
                }
            }

            this.$emit('update:propobject', vm.propobject);
        },
        propertyChanged: function (event)
        {
            
            var vm = this;

            if (!event)
                return;
            if (!event.srcElement)
                return;
            if (!event.srcElement.value)
                return;
            if (!event.srcElement.selectedOptions)
                return;

            //var test = vm.currentProperty.codeName;
            var selectedcodename = event.srcElement.selectedOptions[0]._value;
            vm.currentProperty = vm.copyProperty(selectedcodename);
            vm.propValue = "";
          

        }


    }
    ,updated: function () {

    },
    watch:
    {
        
        propobject: function (newval, oldval) {
          
            var vm = this;

            if (!newval.metaType)
                return;

            vm.metaType = newval.metaType;

            if (!newval.propertyList)
                newval.propertyList = [];

            if (!newval.hasOwnProperty("showSettings"))
                newval.showSettings = false;

            newval.showSettings = !newval.showSettings;
        }
        
        
    },
    destroyed: function () {
    },
    computed:
    {
       

    }

};

export { buttoncounter, radiolist, searchbox, combobox, checklist, propertytool }; 

