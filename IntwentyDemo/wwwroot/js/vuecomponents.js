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

export { buttoncounter, radiolist, searchbox, combobox, checklist }; 