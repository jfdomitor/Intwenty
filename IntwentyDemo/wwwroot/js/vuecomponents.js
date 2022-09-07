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
                     <input class="form-check-input" type="radio" v-bind:id="controlid + '_' + item.code" v-bind:name="controlid" v-bind:value="item.code" v-on:change="radiochanged(event)" />
                     <label class="form-check-label">{{item.value}}</label>
                </div>
                </template>
               </div>`,
    props: ['idfield', 'textfield'],
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

            var domainvalue = null;
            for (var i = 0; i < this.domvalues.length; i++) {
                if (this.domvalues[i].code == event.srcElement.value) {
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
                , 'form-check': this.orientation === 'VERTICAL'
            }
        }
    }

};

export { buttoncounter, radiolist }; 