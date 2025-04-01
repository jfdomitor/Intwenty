/**
 * barea.js
 * 
 * Author: Johan Filipsson
 * License: MIT
 * Description: A lightweight and reactive JavaScript library for modern web applications.
 * 
 * Copyright (c) 2025 Johan Filipsson
 */

"use strict";

export class BareaApp 
{
    #appContent=null; 
    #appElement; 
    #clonedAppElement;
    #bareaId=0;
    #internalSystemCounter = 0;
    #appDataProxy; 
    #appDataProxyCache = new WeakMap();
    #dynamicExpressionRegistry = new Map();
    #computedPropertyNames = [];
    #delayedDirectives = []; //List of directives that can't be tracked until all it's children are tracked
    #appData;
    #methods = {};
    #mounted=false;
    #mountedHandler=null;
    #computedProperties = {};
    #enableBareaId = false;
    #enableHideUnloaded=false;
    #uiDependencyTracker=null;
    #computedPropertiesDependencyTracker =  null;

    constructor(enableInternalId) 
    {
        if (enableInternalId)
            this.#enableBareaId=enableInternalId;

        this.#computedPropertiesDependencyTracker = this.#getComputedPropertiesDependencyTracker();
        this.#uiDependencyTracker = this.#getUserInterfaceTracker(this.#handleUITrackerNofify);
    }

    async mount(element, content) 
    {
        if (!('fetch' in window && 'Promise' in window && 'Symbol' in window && 'Map' in window)) {
            console.error('Your browser and js engine is too old to use barea.js');
            return;
        }

        if (this.#mounted)
            return;

        this.#appContent=content;

        if (!content){
            console.error('Illegal use of mount, please pass a content object with data and methods');
            return;
        }

        if (!content.data){
            console.error('Illegal use of mount, please pass a content object with data and methods');
            return;
        }


        if (!element)
        {
            console.error('Illegal use of mount, please pass an element or an element identifier');
            return;
        }


        if (typeof element === "object") 
            this.#appElement = element;
        else
            this.#appElement = document.getElementById(element);

        this.#clonedAppElement = this.#appElement.cloneNode(true);

      

        //Methods
        if (content.methods)
        {
            this.#methods = content.methods;
            Object.assign(this, this.#methods);
            Object.keys(content.methods).forEach(key => {
                if (typeof this[key] === "function") {
                    this[key] = this[key].bind(this);
                }
            }); 
        }

        //Computed
        if (content.computed) {
            Object.keys(content.computed).forEach(key => {
                if (typeof content.computed[key] === "function") {
                    this.#computedProperties[key] = this.#getNewComputedProperty(content.computed[key], key);
                    this.#computedPropertyNames.push(key);
                }
            });
        
            Object.keys(this.#computedProperties).forEach(key => {
                Object.defineProperty(this, key, {
                    get: () => this.#computedProperties[key].value, 
                    enumerable: true,
                    configurable: true
                });
            });
        }
        

        if (content.mounted)
        {
            //Let user load / modify data before showing the app
            this.#mountedHandler = content.mounted;
            const result = this.#mountedHandler.apply(this, [content.data]);
            if (result instanceof Promise)
            {
                await result;
            }
        }

        //The user my have editited
        this.#appData = content.data;
        this.#appDataProxy = content.data;

        if (this.#enableHideUnloaded)
        {
            document.addEventListener('DOMContentLoaded',this.#loadedHandler);
        }else{
            document.querySelectorAll(".ba-cloak").forEach(el => el.classList.remove("ba-cloak"));
        }
       
      
          
        const proxy = this.#createReactiveProxy((path, reasonobj, reasonkey, reasonvalue, reasonfuncname="") => 
        { 
            //Handles changes in the data and updates the dom
            let log = BareaHelper.getDebugLog(1);
            if (log.active)
                console.log(log.name, path, reasonobj, reasonkey, reasonvalue, reasonfuncname);

            //Tweak for array functions
            //On array assign: reasonobj=parent, reasonkey=arrayname, we will do the same here
            if (Array.isArray(reasonobj) && BareaHelper.ARRAY_FUNCTIONS.includes(reasonfuncname)){
                let objpath = BareaHelper.getLastBareaObjectPath(path);
                reasonkey = BareaHelper.getLastBareaKeyName(path);
                reasonobj=this.getProxifiedPathData(objpath);
                if (!reasonobj)
                    console.error('could not find array on path: ' + path);
            }

     
            this.#uiDependencyTracker.notify(path, reasonobj, reasonkey, reasonvalue, reasonfuncname);
       

        }, this.#appData);

        //The proxy is now active
        this.#appDataProxy = proxy;

        //Track all directives
        this.#trackDirectives();
        this.#trackDelayedDirectives();
        this.#removeMarkUpGenerationNodes();


        this.#mounted = true;

        return this.#appDataProxy;
    }

    /**
     * Force a full reload of barea, except for the proxy
     * Used when new untracked objects in the proxy is detected upon users assigning new objects to the proxy
     */
    refresh() 
    {
        if (!this.#mounted)
            return;

        if (!this.#appElement)
        {
            console.error('The app element is missing in barea.js');
            return;
        }

        //restore templates and clear
        this.#uiDependencyTracker.clear();
        Object.keys(this.#computedProperties).forEach(key => {
            this.#computedProperties[key].clearDependentDirectives();
        });

        this.#dynamicExpressionRegistry.clear();

        if (this.#appContent.computed) 
        {
            this.#computedProperties={};
            Object.keys(this.#appContent.computed).forEach(key => {
                if (typeof this.#appContent.computed[key] === "function") {
                    this.#computedProperties[key] = this.#getNewComputedProperty(this.#appContent.computed[key], key);
                    this.#computedPropertyNames.push(key);
                }
            });
        
            Object.keys(this.#computedProperties).forEach(key => {
                Object.defineProperty(this, key, {
                    get: () => this.#computedProperties[key].value, 
                    enumerable: true,
                    configurable: true
                });
            });
        }

        const freshElement = this.#clonedAppElement.cloneNode(true);
        this.#appElement.replaceWith(freshElement);
        this.#appElement = freshElement;
        
        this.#trackDirectives();
        this.#trackDelayedDirectives();
        this.#removeMarkUpGenerationNodes();
      
    }

    getData()
    {
        if (!this.#mounted)
        {
            console.warn("barea.js is not mounted. Call mount on the instance before using this method.");
            return;
        }
        return this.#appDataProxy;
    }

    enableHideBeforeLoaded(value)
    {
        if (this.#isPrimitive(value))
            this.#enableHideUnloaded = value;
    }

   
    addHandler(functionName, handlerFunction) 
    {
        if (typeof handlerFunction === "function")
        {
            this.#methods[functionName] = handlerFunction;
        } 
        else 
        {
            console.warn(`Handler for "${functionName}" is not a function.`);
        }
    }


    getProxifiedPathData(path, rootobject, varname)
    {
        if (!path && rootobject)
            return rootobject;

        if (!path)
            return this.#appDataProxy;

        const keys = path.match(/[^.[\]]+/g);
        if (!keys && rootobject)
            return rootobject;

        if (!keys)
            return this.#appDataProxy;

        let target = rootobject ? rootobject : this.#appDataProxy;

        for (let i = 0; i < keys.length; i++) {
            if (i === 0 && ((keys[i].toLowerCase() === 'root') || (varname && keys[i].toLowerCase() === varname.toLowerCase())))
                continue;

            if (!target)
                return undefined;

            target = target[keys[i]];
        }

        return target;
    }

    getPathData(path) 
    {
        if (!path) 
            return this.#appData; 

        const keys = path.match(/[^.[\]]+/g);
        if (!keys) 
            return this.#appData; 

        let target = this.#appData;
    
        for (let i = 0; i < keys.length; i++) {
            if (i === 0 && keys[i].toLowerCase() === 'root') continue; 
            if (!target) return undefined; 
            target = target[keys[i]];
        }
    
        return target;
    }

    //Generates a flat object from any object
    getObjectDictionary(obj, parentKey = '') {
        let result = {};
        for (const key in obj) {
            if (obj.hasOwnProperty(key)) {
                const newKey = parentKey ? `${parentKey}.${key}` : key;
                if (typeof obj[key] === 'object' && obj[key] !== null) {
                    if (Array.isArray(obj[key])) {
                        result[newKey] = obj[key]; 
                        obj[key].forEach((item, index) => {
                            result[`${newKey}[${index}]`] = item; 
                            Object.assign(result, this.getObjectDictionary(item, `${newKey}[${index}]`));
                        });
                    } else {
                        result[newKey] = obj[key]; 
                        Object.assign(result, this.getObjectDictionary(obj[key], newKey));
                    }
                } else {
                    result[newKey] = obj[key];  
                }
            }
        }
    
        return result;
    }

    /**
     * The heart, the proxy
     * A recursive proxy for reactivity
     * Strategy: 1. Only proxy on get, 2. Always get data after set (callback)
     */
    #createReactiveProxy(callback, data, currentpath = "root") 
    {

        const handler = {
            get: (target, key, receiver) => {

                let value = Reflect.get(target, key, receiver);

                //Special to support the spread operator ...
                if (key === Symbol.iterator) {
                    return (function* () {
                        let index = 0; 
                        for (let item of target) {
                            let itemPath = `${currentpath}[${index++}]`; 
                
                            if (typeof item === 'object' && item !== null) {
                                if (this.#appDataProxyCache.has(item)) {
                                    yield this.#appDataProxyCache.get(item);
                                } else {
                                    const proxiedItem = this.#createReactiveProxy(callback, item, itemPath);
                                    this.#appDataProxyCache.set(item, proxiedItem);
                                    yield proxiedItem;
                                }
                            } else {
                                yield item;
                            }
                        }
                    }).bind(this);
                }
                
                const newPath = Array.isArray(target) ? `${currentpath}[${key}]` : `${currentpath}.${key}`;
              
                if(typeof value !== "function")
                    this.#computedPropertiesDependencyTracker.track(newPath, null);
               
                if (typeof value === 'object' && value !== null) 
                {
                  
                    if (!Array.isArray(value) && this.#enableBareaId && !value.hasOwnProperty('baId')) 
                    {
                        value.baId = ++this.#bareaId;  
                    }

                    if (this.#appDataProxyCache.has(value)) {
                        return this.#appDataProxyCache.get(value);
                    }
            
                    const proxiedvalue = this.#createReactiveProxy(callback, value, newPath); 
                    this.#appDataProxyCache.set(value, proxiedvalue);
                    return proxiedvalue;

                }else{

                    if(typeof value === "function")
                    {
                        if (BareaHelper.ARRAY_FUNCTIONS.includes(value.name)) {
                            this.#internalSystemCounter++; // Increment the counter
                        
                            // Directly create the wrapped function without caching
                            return (...args) => {
                                let proxyargs = [];
                                args.forEach(t => {
                                    let proxyargobj = null;
                                    if (typeof t === "object") {
                                        proxyargobj = this.#createReactiveProxy(callback, t);
                                        if (!this.#appDataProxyCache.has(t)) 
                                            this.#appDataProxyCache.set(t, proxyargobj);
                                    } else {
                                        proxyargobj = t;
                                    }
                        
                                    proxyargs.push(proxyargobj);
                                });
                        
                                const result = value.apply(target, args);
                        
                                this.#computedPropertiesDependencyTracker.notify(currentpath, value.name);
                        
                                callback(currentpath, receiver, '', proxyargs, value.name);
                        
                                return result;
                            };
                        }
                        
        
                    }
                }
              
                return value;
            },
            set: (target, key, value, receiver) => {

                const newPath = Array.isArray(target) ? `${currentpath}[${key}]` : `${currentpath}.${key}`;

                if(typeof value === "function" && !BareaHelper.ARRAY_FUNCTIONS.includes(value.name))
                    return true;

                if (target[key] === value) 
                    return true;
        
                target[key] = value;

                if(typeof value === "function"  && BareaHelper.ARRAY_FUNCTIONS.includes(value.name))
                    this.#computedPropertiesDependencyTracker.notify(newPath, value.name);
                else
                    this.#computedPropertiesDependencyTracker.notify(newPath, null);

                
                 
                //Out puts raw data (no proxy here)
                callback(newPath, receiver, key, receiver[key]);

                return true;
            }
        };
        
        let proxiedValue =new Proxy(data, handler);
        return proxiedValue;

    }

    /**
    * Remove template nodes from the dom
    */
    #removeMarkUpGenerationNodes()
    {
        this.#uiDependencyTracker.getTemplateDependencies().forEach(dir => {
            if (BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(dir.directivename))
            {
                dir.element.remove();
            }
        });
    }

    /**
    * Some directives may be dependent upon children is tracked first
    * Example: we want to set ba-option-id and ba-option-text before tracking the parent select element
    * 
    */
    #trackDelayedDirectives()
    {
        this.#delayedDirectives.forEach(dir =>
        {
            if (dir.inputtype === BareaHelper.UI_INPUT_SELECT)
            {
                this.#uiDependencyTracker.track('value', dir);

                this.#setSelect(dir);

                let eventtype = (dir.directivename === BareaHelper.DIR_BIND) ? "change" : "blur";
                const handler = function (event) {

                    if (dir.hashandler) {
                        this.#runBindHandler(BareaHelper.VERB_SET_DATA, dir);
                        return;
                    }

                    let log = BareaHelper.getDebugLog(3);
                    if (log.active)
                        console.log(log.name, "select ", "key: " + valuekey, "value: " + el.value);

                    dir.data[dir.key] = dir.element.value;

                }.bind(this);

                dir.element.removeEventListener(eventtype, handler);
                dir.element.addEventListener(eventtype, handler);

            }
        });

        this.#delayedDirectives = [];
    }

    #trackDirectives(tag=this.#appElement, trackcontext={template:null, rendereddata:null, renderedindex:-1, renderedvarname:"", renderedobjid:-1})
    {
      
        //Validate templates before proceeding
        let templateChildren = this.#validateTemplateChildren();
       
        let bareaElements = Array.from(tag.querySelectorAll("*"));
        bareaElements.unshift(tag); 
    
        bareaElements.forEach(el => {


            const bareaAttributes = Array.from(el.attributes).filter(attr => attr.name.startsWith("ba-")).map(attr => ({ name: attr.name, value: attr.value }));

            if (!bareaAttributes)
                return;
            if (!bareaAttributes.length>0)
                return;

            //Skip all children of templates since they will be dealt with later on template rendering
            if (templateChildren.includes(el))
                return;

            let hasMarkUpGenerationAttr = false;
            bareaAttributes.forEach(attr => { if (BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(attr.name)) { hasMarkUpGenerationAttr = true; } });

            bareaAttributes.forEach(attr =>
            {

               if (!attr.value)
                   return;

               //if an attribute also have a render template attribute, skip since it will be rendered in renderTemplates
               if (!BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(attr.name) && hasMarkUpGenerationAttr)
                    return;

                this.#internalSystemCounter++;

            
                if (BareaHelper.DIR_GROUP_BIND_TO_PATH.includes(attr.name))
                {
                    this.#trackBindToPath(tag, trackcontext, el, attr.name, attr.value);      
                }
                else if (BareaHelper.DIR_GROUP_COMPUTED.includes(attr.name))
                {
                    this.#trackComputed(tag, trackcontext, el, attr.name, attr.value);  
                }
                else if (BareaHelper.DIR_GROUP_TRACK_AND_FORGET.includes(attr.name))
                {
                    this.#trackFireAndForget(tag, trackcontext, el, attr.name, attr.value);
                }
                else if (BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(attr.name))
                {
                    this.#trackMarkUpGeneration(tag, trackcontext, el, attr.name, attr.value);   
                }
                  
             });                 
        });


        this.#trackInterpolation(tag, trackcontext);
            
        let log = BareaHelper.getDebugLog(4);
        if (log.active)
        {
            console.log(log.name);
            this.#uiDependencyTracker.getAllDependencies().forEach((value, key) => {
                console.log(key, value);
            });
        }

        log = BareaHelper.getDebugLog(5);
        if (log.active)
        {
            console.log(log.name);
            this.#computedPropertiesDependencyTracker.getAllDependencies().forEach((value, key) => {
                console.log(key, value);
            });
        }

        
    }

    #trackBindToPath(tag = this.#appElement, trackcontext = { template: null, rendereddata: null, renderedindex: -1, renderedvarname: "", renderedobjid: -1 }, element, attributeName, attributeValue)
    {
        const attribute_value_type = this.#getExpressionType(attributeValue, attributeName, trackcontext.renderedvarname);
        if (attribute_value_type === BareaHelper.EXPR_TYPE_INVALID) {
            console.error(`The ${attributeName} directive with value: (${attributeValue}) is invalid.`);
            return;
        }

        let inputtype = "";
        let systeminput = -1;
        if (BareaHelper.DIR_GROUP_UI_DUPLEX.includes(attributeName)) {
            if (element.tagName.toLowerCase() === "select") {
                systeminput = BareaHelper.UI_INPUT_SELECT;
            } else {
                inputtype = element.getAttribute("type");
                if (!inputtype) {
                    systeminput = BareaHelper.UI_INPUT_CUSTOM;

                } else {
                    systeminput = 1;
                    if (inputtype.toLowerCase() === "text")
                        systeminput = BareaHelper.UI_INPUT_TEXT;
                    if (inputtype.toLowerCase() === "checkbox")
                        systeminput = BareaHelper.UI_INPUT_CHECKBOX;
                    if (inputtype.toLowerCase() === "radio")
                        systeminput = BareaHelper.UI_INPUT_RADIO;
                }
            }
        }

        let tracking_obj = this.#createInputDirective(trackcontext, element, attributeName, attributeValue, null, "", systeminput);
        tracking_obj.expressiontype = attribute_value_type;


        //If root is included in the attributeValue, example root.model.users.name
        //Then we will bind to the root even if this is a template node
        if (!trackcontext.rendereddata || attributeValue.startsWith(BareaHelper.ROOT_OBJECT)) {
            let objpath = BareaHelper.getLastBareaObjectPath(attributeValue);
            tracking_obj.data = this.getProxifiedPathData(objpath);
            tracking_obj.key = BareaHelper.getLastBareaKeyName(attributeValue);
        } else {
            tracking_obj.data = trackcontext.rendereddata;
            tracking_obj.key = BareaHelper.getLastBareaKeyName(attributeValue);
        }

        if (!tracking_obj.data || !tracking_obj.key) {
            console.error(`Could not track directive ${attributeName} (${attributeValue}) could not match data at the given path`);
            return;
        }


        let handlername = element.getAttribute(BareaHelper.DIR_BIND_HANDLER);
        if (handlername) {
            const check_handler = this.#getExpressionType(handlername, BareaHelper.DIR_BIND_HANDLER);
            if (check_handler === BareaHelper.EXPR_TYPE_INVALID) {
                console.error(`The ${BareaHelper.DIR_BIND_HANDLER} directive has an invalid value (${handlername}), should be funcname(), or funcname(args..).`);
                return;
            }

            tracking_obj.hashandler = true;
            tracking_obj.handlername = handlername;

        }

        if (tracking_obj.inputtype === BareaHelper.UI_INPUT_SELECT) {
            this.#delayedDirectives.push(tracking_obj);
        } else {
            this.#uiDependencyTracker.track('value', tracking_obj);
        }


        if (BareaHelper.DIR_GROUP_UI_DUPLEX.includes(tracking_obj.directivename)) {
            if (tracking_obj.hashandler)
                this.#runBindHandler(BareaHelper.VERB_SET_UI, tracking_obj);

            if (tracking_obj.inputtype === BareaHelper.UI_INPUT_TEXT) {
                this.#setInputText(tracking_obj);
            }
            else if (tracking_obj.inputtype === BareaHelper.UI_INPUT_CHECKBOX) {
                this.#setInputCheckbox(tracking_obj);
            }
            else if (tracking_obj.inputtype === BareaHelper.UI_INPUT_RADIO) {
                this.#setInputRadio(tracking_obj);
            }


            let eventtype = (tracking_obj.directivename === BareaHelper.DIR_BIND) ? "input" : "blur";
            const handler = function (event) {
                if (tracking_obj.hashandler) {
                    this.#runBindHandler(BareaHelper.VERB_SET_DATA, tracking_obj);
                    return;
                }

                let log = BareaHelper.getDebugLog(3);
                if (tracking_obj.inputtype === BareaHelper.UI_INPUT_CHECKBOX) {
                    if (log.active)
                        console.log(log.name, "type: " + element.type, "key: " + valuekey, "input value: " + element.checked);

                    tracking_obj.data[tracking_obj.key] = element.checked;
                }
                else if (tracking_obj.inputtype === BareaHelper.UI_INPUT_RADIO) {
                    if (log.active)
                        console.log(log.name, "type: " + element.type, "key: " + valuekey, "input value: " + element.value);

                    if (element.checked)
                        tracking_obj.data[tracking_obj.key] = element.value;
                }
                else {
                    if (log.active)
                        console.log(log.name, "type: " + element.type, "key: " + valuekey, "input value: " + element.value);

                    tracking_obj.data[tracking_obj.key] = element.value;
                }
            }.bind(this);

            element.removeEventListener(eventtype, handler);
            element.addEventListener(eventtype, handler);


        }
        else if (tracking_obj.directivename === BareaHelper.DIR_CLASS) {
            let classnames = element.getAttribute('classNames');
            tracking_obj.orgvalue = classnames;
            this.#setClass(tracking_obj);
        }
        else if (tracking_obj.directivename === BareaHelper.DIR_HREF) {
            this.#setHref(tracking_obj);
        }
        else if (tracking_obj.directivename === BareaHelper.DIR_IMAGE_SRC) {
            this.#setSrc(tracking_obj);
        }
        else if (tracking_obj.directivename === BareaHelper.DIR_OPTION_ID) {
            this.#setOptionId(tracking_obj);
        }
        else if (tracking_obj.directivename === BareaHelper.DIR_OPTION_TEXT) {
            this.#setOptionText(tracking_obj);
        }

    }

    #trackComputed(tag = this.#appElement, trackcontext = { template: null, rendereddata: null, renderedindex: -1, renderedvarname: "", renderedobjid: -1 }, element, attributeName, attributeValue) {

        const attribute_value_type = this.#getExpressionType(attributeValue, attributeName, trackcontext.renderedvarname);
        if (attribute_value_type === BareaHelper.EXPR_TYPE_INVALID) {
            console.error(`The ${attributeName} directive has an invalid value (${attributeValue}).`);
            return;
        }

        const tracking_obj = this.#createComputedDirective(trackcontext, element, attributeName, attributeValue);
        tracking_obj.expressiontype = attribute_value_type;

        //If root expression force root data to be executed
        if (!trackcontext.rendereddata) {
            tracking_obj.data = this.#appDataProxy;
        } else {
            tracking_obj.data = trackcontext.rendereddata;
        }
        tracking_obj.key = "";

        let condition = attributeValue.trim();
        if (condition.includes('?'))
            condition = condition.split('?')[0];


        let isnewhandler = false;
        let handlername = this.#dynamicExpressionRegistry.get(attributeValue);
        if (!handlername || trackcontext.rendereddata) //Must use different handlers, since notification doesn't work otherwise.
        {
            isnewhandler = true;
            if ((trackcontext.template)) { //Is template directive
                handlername = `${BareaHelper.META_DYN_TEMPLATE_FUNC_PREFIX}${this.#internalSystemCounter}`;
            } else {
                handlername = `${BareaHelper.META_DYN_FUNC_PREFIX}${this.#internalSystemCounter}`;
            }
            this.#dynamicExpressionRegistry.set(attributeValue, handlername);
        }


        if (attributeName === BareaHelper.DIR_IF) {
            tracking_obj.elementnextsibling = element.nextSibling;
            tracking_obj.parentelement = element.parentElement;
        }

        if (attribute_value_type === BareaHelper.EXPR_TYPE_COMPUTED) {
            tracking_obj.handlername = condition;
            this.#computedProperties[tracking_obj.handlername].addDependentDirective(tracking_obj);
            this.#runComputedFunction(tracking_obj);
        }
        else if (attribute_value_type === BareaHelper.EXPR_TYPE_ROOT_EXPR) {

            const evalobj = this.#appDataProxy;
            const boolRootFunc = function () {
                function evalRootExpr(condition, context) {
                    try {
                        return new Function("contextdata", `return ${condition};`)(context);
                    } catch (error) {
                        console.error(`Error evaluating expression ${condition} on ${element.localName} with attribute ${attributeName}`);
                        return false;
                    }
                }

                condition = condition.replaceAll('root.', 'contextdata.');
                return evalRootExpr(condition, evalobj);
            }

            if (isnewhandler) {
                this.#computedProperties[handlername] = this.#getNewComputedProperty(boolRootFunc, handlername);
                this.#computedPropertyNames.push(handlername);
            }
            tracking_obj.handlername = handlername;
            tracking_obj.iscomputed = true;
            this.#computedProperties[tracking_obj.handlername].addDependentDirective(tracking_obj);
            this.#runComputedFunction(tracking_obj);

        }
        else if (attribute_value_type === BareaHelper.EXPR_TYPE_OBJREF_EXPR) {

            const boolObjFunc = function () {
                function evalObjExpr(condition, context) {
                    try {
                        return new Function("contextdata", `return ${condition};`)(context);
                    } catch (error) {
                        console.error(`Error evaluating expression ${condition} on ${element.localName} with attribute ${attributeName}`);
                        return false;
                    }
                }

                condition = condition.replaceAll(trackcontext.renderedvarname + '.', 'contextdata.');
                return evalObjExpr(condition, tracking_obj.data);
            }

            if (!trackcontext.renderedvarname) {
                console.error(`Invalid expression: ${condition} to use as a dynamicly created function in a template.`);
                return;
            }

            if (isnewhandler) {
                this.#computedProperties[handlername] = this.#getNewComputedProperty(boolObjFunc, handlername);
                this.#computedPropertyNames.push(handlername);
            }
            tracking_obj.handlername = handlername;
            tracking_obj.iscomputed = true;
            this.#computedProperties[tracking_obj.handlername].addDependentDirective(tracking_obj);
            this.#runComputedFunction(tracking_obj);

        }
        else if (attribute_value_type === BareaHelper.EXPR_TYPE_MIX_EXPR) {

            const subobj = tracking_obj.data;
            const rootobj = this.#appDataProxy;

            const boolMixedFunc = function () {
                function evalMixedExpr(condition, subdata, rootdata) {
                    try {
                        return new Function("subdata", "rootdata", `return ${condition};`)(subdata, rootdata);
                    } catch (error) {
                        console.error(`Error evaluating mixed expression ${condition} on ${element.localName} with attribute ${attributeName}`);
                        return false;
                    }
                }

                condition = condition.replaceAll('root.', 'rootdata.');
                condition = condition.replaceAll(trackcontext.renderedvarname + '.', 'subdata.');
                return evalMixedExpr(condition, subobj, rootobj);
            }

            if (!trackcontext.renderedvarname) {
                console.error(`Invalid expression: ${condition} to use as a dynamicly created function in a template.`);
                return;
            }

            if (isnewhandler) {
                this.#computedProperties[handlername] = this.#getNewComputedProperty(boolMixedFunc, handlername);
                this.#computedPropertyNames.push(handlername);
            }
            tracking_obj.handlername = handlername;
            tracking_obj.iscomputed = true;
            this.#computedProperties[tracking_obj.handlername].addDependentDirective(tracking_obj);
            this.#runComputedFunction(tracking_obj);

        }
    }

    #trackFireAndForget(tag = this.#appElement, trackcontext = { template: null, rendereddata: null, renderedindex: -1, renderedvarname: "", renderedobjid: -1 }, element, attributeName, attributeValue)
    {
        const attribute_value_type = this.#getExpressionType(attributeValue, attributeName);
        if (attribute_value_type === BareaHelper.EXPR_TYPE_INVALID) {
            console.error(`Then ${attributeName} directive has an invalid value (${attributeValue}).`);
            return;
        }

        //SPECIAL: NO TRACKING, ONLY REGISTER HANDLER
        if (attribute_value_type === BareaHelper.EXPR_TYPE_HANDLER) {

            const clickHandler = function (event) {
                event.stopPropagation();
                let eventdata = this.#appDataProxy;
                let bapath = element.getAttribute(BareaHelper.META_PATH);

                if (!bapath) {
                    if (trackcontext.rendereddata)
                        eventdata = trackcontext.rendereddata;
                } else {
                    eventdata = this.getProxifiedPathData(bapath);
                }

                let allparams = [event, element, eventdata];
                let pieces = BareaHelper.parseBareaFunctionCall(attributeValue);
                allparams.push(...pieces.params);

                if (this.#methods[pieces.functionName]) {
                    this.#methods[pieces.functionName].apply(this, allparams);
                } else {
                    console.warn(`Handler function '${pieces.functionName}' not found.`);
                }
            }.bind(this);

            // Ensure no duplicate event listener
            if (!element.dataset.listenerAdded) {
                element.removeEventListener("click", clickHandler);
                element.addEventListener("click", clickHandler);
                element.dataset.listenerAdded = "true";
            }

        }
    }

    #trackMarkUpGeneration(tag = this.#appElement, trackcontext = { template: null, rendereddata: null, renderedindex: -1, renderedvarname: "", renderedobjid: -1 }, element, attributeName, attributeValue)
    {
        const attribute_value_type = this.#getExpressionType(attributeValue, attributeName);
        if (attribute_value_type === BareaHelper.EXPR_TYPE_INVALID) {
            console.error(`Then ${attributeName} directive has an invalid value (${attributeValue}).`);
            return;
        }

        let [varname, datapath] = attributeValue.split(" in ").map(s => s.trim());
        if (!varname) {
            console.error(`No variable name was found in the ${attributeName} expression`);
            return;
        }
        if (!datapath) {
            console.error(`No path or computed function was found in the ${attributeName} expression`);
            return;
        }
        if (trackcontext.rendereddata!=null) {
            console.error(`trackMarkUpGeneration was called withn a trackcontext which is not allowed.`);
            return;
        }

        const tracking_obj = this.#createTemplateDirective(trackcontext, element, attributeName, attributeValue, null, "", element.parentElement, element.innerHTML, element.localName);
        tracking_obj.elementnextsibling = element.nextSibling;
        tracking_obj.elementclone = element.cloneNode(true);
        tracking_obj.expressiontype = attribute_value_type;
        if (attribute_value_type !== BareaHelper.EXPR_TYPE_COMPUTED) {
            let objpath = BareaHelper.getLastBareaObjectPath(datapath);
            tracking_obj.data = this.getProxifiedPathData(objpath);
            tracking_obj.key = BareaHelper.getLastBareaKeyName(datapath);
            if (!Array.isArray(tracking_obj.data[tracking_obj.key])) {
                console.error(`could not find array for ${datapath} in  ${attributeName} directive`);
                return;
            }
            this.#uiDependencyTracker.track('value', tracking_obj);

        } else {
            this.#computedProperties[datapath].addDependentDirective(tracking_obj);
            this.#uiDependencyTracker.track('computed', tracking_obj);
        }

        this.#renderTemplates(tracking_obj, BareaHelper.ROOT_OBJECT, tracking_obj.data[tracking_obj.key]);
    }

    #trackInterpolation(tag = this.#appElement, trackcontext = { template: null, rendereddata: null, renderedindex: -1, renderedvarname: "", renderedobjid: -1 })
    {
        let allexpressions = [];
        let nodes_with_expressions = this.#getInterplationNodesAndExpressions(tag);
        nodes_with_expressions.forEach(inode => {

            let nodeexpressions = [];
            let node = inode.textNode;
            inode.expressions.forEach(expr => {

                let path = expr.replaceAll('{', '').replaceAll('}', '').trim();
                const attribute_value_type = this.#getExpressionType(path, BareaHelper.DIR_INTERPOLATION, trackcontext.renderedvarname);
                if (attribute_value_type === BareaHelper.EXPR_TYPE_INVALID) {
                    console.error(`The ${DIR_INTERPOLATION} directive has an invalid expression (${path}).`);
                    return;
                }

                let tracking_obj = this.#createInterpolationDirective(trackcontext, BareaHelper.DIR_INTERPOLATION, path, null, "", node, expr, node.nodeValue);
                tracking_obj.expressiontype = attribute_value_type;
                if (attribute_value_type === BareaHelper.EXPR_TYPE_COMPUTED) {
                    tracking_obj.isrendered = false;
                    tracking_obj.iscomputed = true;
                    nodeexpressions.push(tracking_obj);
                }
                else if ([BareaHelper.EXPR_TYPE_ROOT_PATH, BareaHelper.EXPR_TYPE_OBJREF, BareaHelper.EXPR_TYPE_OBJREF_PATH].includes(attribute_value_type)) {

                    tracking_obj.isobjectexpression = false;
                    let ipolvalue = this.getProxifiedPathData(path, trackcontext.rendereddata, trackcontext.renderedvarname);
                    if (typeof ipolvalue !== "object") {
                        tracking_obj.key = BareaHelper.getLastBareaKeyName(path);
                        let objpath = BareaHelper.getLastBareaObjectPath(path);
                        tracking_obj.data = this.getProxifiedPathData(objpath, trackcontext.rendereddata, trackcontext.renderedvarname);
                    } else {
                        tracking_obj.isobjectexpression = true;
                        tracking_obj.data = ipolvalue;
                    }
                   
                    tracking_obj.isrendered = false;
                    tracking_obj.iscomputed = false;
                    nodeexpressions.push(tracking_obj);

                }
                else if (BareaHelper.INTERPOL_INDEX === attribute_value_type && trackcontext.rendereddata) {
                    //Wont be tracked
                    tracking_obj.isrendered = true;
                    tracking_obj.iscomputed = false;
                    tracking_obj.data = trackcontext.rendereddata;
                    nodeexpressions.push(tracking_obj);
                    if (trackcontext.template)
                        trackcontext.template.usesReRenderedInterpolation = true;
                }

            });

            //OBS Array of arrays, one array per node
            allexpressions.push(nodeexpressions);

        });

        if (allexpressions.length > 0)
        {
            let exist_object_interpolation = false;
            allexpressions.forEach(dir => {
                if (exist_object_interpolation)
                    return;
                dir.forEach(ne => {
                    if (ne.isobjectexpression)
                        exist_object_interpolation = true;
                });
            });

            allexpressions.forEach(dir => {

                let firstexpr = dir[0];
                firstexpr.nodeexpressions = dir;
                firstexpr.nodeexpressions.forEach(ne => {
                    ne.nodeexpressions = dir;
                    if (ne.iscomputed && this.#computedProperties[ne.directivevalue]) {
                        this.#computedProperties[ne.directivevalue].addDependentDirective(ne);
                    } else {
                        //Track as global
                        if (exist_object_interpolation) {
                            this.#uiDependencyTracker.track('global', ne);
                        } else {
                            this.#uiDependencyTracker.track('value', ne);
                        }
                    }
                });
                this.#setInterpolation(firstexpr);
            });

        }
    }

    #getInterplationNodesAndExpressions(root = this.#appElement) 
    {
        let walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT, {
            acceptNode(node) {
                return (node.nodeValue.includes('{{') && node.nodeValue.includes('}}')) ? NodeFilter.FILTER_ACCEPT : NodeFilter.FILTER_REJECT;
            }
        });
    
        let results = [];
        //parentElement: walker.currentNode.parentNode
        while (walker.nextNode()) {
           let inode = { textNode: walker.currentNode, expressions:[] };
           inode.expressions=this.#extractInterpolations(inode.textNode.nodeValue);
           results.push(inode);
        }
        return results;
    }
    

     /**
     * Validates templates and removes them if invalid
     * @returns An array of successfully validated template children
     */
    #validateTemplateChildren(tag=this.#appElement)
    {
        //There can't be templates in generated content
        //No support for dynamic templates :)
        if (tag !== this.#appElement)
            return;

         //Collect children of the template (this is the user generated template, that should be replaced)
         let templateChildren=[];
         let resultChildren=[];
         function collectDescendants(ce, templateroot) 
         {
            templateChildren.push(ce);
            for (let i = 0; i < ce.children.length; i++) 
            {
                   collectDescendants(ce.children[i]);
            }
           
         }

         tag.querySelectorAll(`[${BareaHelper.DIR_FOREACH}]`).forEach(templateroot => 
         {
            let isValidTemplate = true;
            templateChildren=[];
             Array.from(templateroot.children).forEach(child => collectDescendants(child, templateroot));
            
            let templateattr = templateroot.getAttribute(BareaHelper.DIR_FOREACH);
            let [varname, datapath] = templateattr.split(" in ").map(s => s.trim());
            if (!varname){
                isValidTemplate=false;
                console.error(`No variable name was found in the ${BareaHelper.DIR_FOREACH} expression: ${templateattr}`);
                isValidTemplate = false;
            }
            if (!datapath){
                isValidTemplate=false;
                console.error(`No path or computed function was found in the ${BareaHelper.DIR_FOREACH} expression: ${templateattr}`);
                isValidTemplate = false;
            }

            templateChildren.forEach(child=>{
                BareaHelper.DIR_GROUP_BIND_TO_PATH.forEach(dir=>{
                    if (child.hasAttribute(dir)){
                        let bareaattrib = child.getAttribute(dir);
                        if (!bareaattrib.includes(BareaHelper.ROOT_OBJECT)){
                            if (!bareaattrib.includes(varname)){
                                isValidTemplate=false;
                                console.error(`The ${dir} expression ${bareaattrib} used in an element under ${BareaHelper.DIR_FOREACH} does not match the ${BareaHelper.DIR_FOREACH} expression, data should be reference by '${varname}'. or '${BareaHelper.ROOT_OBJECT}'.`);
                                isValidTemplate = false;
                            }
                        }
                    }
                });
            });

             if (!isValidTemplate){
                //Remove the invalid template
                templateroot.parentElement.innerHTML="";
             }else{
                resultChildren.push(...templateChildren);
             }
         });


        return resultChildren;
      
    }

    #createComputedDirective(trackcontext, element, directivename, directivevalue)
    {
        let id = this.#internalSystemCounter++;
        return {id: id, renderedobjid:trackcontext.renderedobjid,  renderedvarname: trackcontext.renderedvarname, template: trackcontext.template, directivename:directivename,  directivevalue:directivevalue, element: element, data:null, key:"", hashandler:false, handlername:"", inputtype:-1, iscomputed:true };
    }

    #createInputDirective(trackcontext, element, directivename, directivevalue, data, key, inputtype)
    {
        let id = this.#internalSystemCounter++;
        return {id: id, renderedobjid:trackcontext.renderedobjid, renderedvarname: trackcontext.renderedvarname, template: trackcontext.template, directivename:directivename,  directivevalue:directivevalue, element: element, data:data, key:key, hashandler:false, handlername:"", inputtype:inputtype,iscomputed:false };
    }
    #createTemplateDirective(trackcontext, element, directivename, directivevalue, data, key,  parentelement, templatemarkup, templatetagname)
    {
        let id = this.#internalSystemCounter++;
        return {id: id, renderedobjid:trackcontext.renderedobjid, renderedvarname: trackcontext.renderedvarname, template: trackcontext.template, directivename:directivename,  directivevalue:directivevalue, element: element, data:data, key:key, hashandler:false, handlername:"", inputtype:-1, iscomputed:false, parentelement : parentelement, templatemarkup : templatemarkup, templatetagname : templatetagname, usesReRenderedInterpolation:false };
    }
    #createInterpolationDirective(trackcontext, directivename, directivevalue, data, key,interpolatednode, expression, nodetemplate)
    {
        let id = this.#internalSystemCounter++;
        return {id: id, renderedobjid:trackcontext.renderedobjid, renderedvarname: trackcontext.renderedvarname, template: trackcontext.template, directivename:directivename,  directivevalue:directivevalue, element: null, data:data, key:key, hashandler:false, handlername:"", inputtype:-1, iscomputed:false, isrendered:false, interpolatednode: interpolatednode, expression: expression, nodetemplate:nodetemplate, nodeexpressions:[], renderedindex: trackcontext.renderedindex  };
    }

    #handleUITrackerNofify = (reasonobj, reasonkey, reasonvalue, path, resultset, reasonfuncname) =>
    {
        resultset.forEach(directive => 
        {
            if (BareaHelper.DIR_GROUP_BIND_TO_PATH.includes(directive.directivename)){

                    if (directive.hashandler){
                        this.#runBindHandler(BareaHelper.VERB_SET_UI, directive);
                        return;
                    }

                    if (BareaHelper.DIR_GROUP_UI_DUPLEX.includes(directive.directivename))
                    {
                        if (directive.inputtype === BareaHelper.UI_INPUT_TEXT){
                            this.#setInputText(directive);
                        }
                        else if (directive.inputtype === BareaHelper.UI_INPUT_CHECKBOX){
                            this.#setInputCheckbox(directive);
                        }
                        else if (directive.inputtype === BareaHelper.UI_INPUT_RADIO){
                            this.#setInputRadio(directive);
                        }
                        else if (directive.inputtype === BareaHelper.UI_INPUT_SELECT) {
                            this.#setSelect(directive);
                        }
                    }
                    else if (directive.directivename===BareaHelper.DIR_CLASS){
                        this.#setClass(directive);
                    }
                    else if (directive.directivename===BareaHelper.DIR_HREF){
                        this.#setHref(directive);
                    }
                    else if (directive.directivename===BareaHelper.DIR_IMAGE_SRC){
                        this.#setSrc(directive);
                    }
                    else if (directive.directivename === BareaHelper.DIR_OPTION_ID) {
                        this.#setOptionId(directive);
                    }
                    else if (directive.directivename === BareaHelper.DIR_OPTION_TEXT) {
                        this.#setOptionText(directive);
                    }
                    
                    
            }
            else if (directive.directivename===BareaHelper.DIR_INTERPOLATION){

                this.#setInterpolation(directive);
            }   
            else if (BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(directive.directivename)){

                if (reasonfuncname && BareaHelper.ARRAY_FUNCTIONS.includes(reasonfuncname))
                    this.#renderTemplates(directive,path,null,reasonfuncname, reasonvalue); 
                else
                    this.#renderTemplates(directive,path,reasonvalue,"", null); 

            }
            else if (BareaHelper.DIR_GROUP_COMPUTED.includes(directive.directivename))
            {
                this.#runComputedFunction(directive);
            }
        });
    }

    
  
    #runComputedFunction(directive)
    {

        let handlername = directive.handlername;
        let boundvalue = false;
        if (this.#computedProperties[handlername]) {
                boundvalue = this.#computedProperties[handlername].value;
        } else {
            console.warn(`Computed boolean function '${handlername}' not found.`);
        }
        
        if (directive.directivename===BareaHelper.DIR_HIDE)
            directive.element.style.display = boundvalue ? "none" : ""
        else if (directive.directivename===BareaHelper.DIR_SHOW)      
            directive.element.style.display = boundvalue ? "" : "none";
        else if (directive.directivename===BareaHelper.DIR_IF) 
        {
          this.#setComputedIf(directive, boundvalue);
        }
        else if (directive.directivename===BareaHelper.DIR_CLASS_IF) 
        {
            let truevalue = directive.directivevalue;
            if (truevalue.includes('?'))
            truevalue=truevalue.split('?')[1];

          this.#setComputedClassIf(directive, boundvalue, truevalue);

        }    
              
    }

    #runBindHandler(verb, directive)
    {

        if (!directive.handlerpieces)
            directive.handlerpieces = BareaHelper.parseBareaFunctionCall(directive.handlername);

        let allparams = [];
        if (verb === BareaHelper.VERB_SET_UI)
            allparams = [verb, directive.element, directive.data[directive.key]];

        if (verb === BareaHelper.VERB_SET_DATA)
            allparams = [verb, directive.element, directive.data, directive.key];

        allparams.push(...directive.handlerpieces.params);

        if (this.#methods[directive.handlerpieces.functionName]) {
            this.#methods[directive.handlerpieces.functionName].apply(this, allparams);
        } else {
            console.warn(`Handler function '${directive.handlerpieces.functionName}' not found.`);
        }
    }

    #setComputedClassIf(directive, boundvalue, truevalue) 
    {
        let classnames = truevalue.split(/[\s,]+/);

        // Add classes if condition is true and remove if false
        if (boundvalue) {
            classnames.forEach(className => {
                if (!directive.element.classList.contains(className)) {
                     directive.element.classList.add(className); // Add class if not already present
                }
                });
        } 
        else 
        {
             classnames.forEach(className => {
                 if (directive.element.classList.contains(className)) {
                    directive.element.classList.remove(className); // Remove class if present
                }
            });
        }
    }

    #setComputedIf(directive, boundvalue) 
    {
        if (boundvalue)
        {
            if (!directive.element.parentNode)
                 if (directive.elementnextsibling)
                    directive.parentelement.insertBefore(directive.element, directive.elementnextsibling);
        }
        else
        {
            if (directive.element.parentNode)
            {
                directive.elementnextsibling = directive.element.nextSibling;
                directive.element.remove();
            }   
        }
    }

    #setInputText(directive) {
        if (directive.element && directive.element.value !== directive.data[directive.key]) 
        {
            if (!directive.data[directive.key])
                directive.element.value = "";
            else
                directive.element.value = directive.data[directive.key];
        }
    }

    #setSelect(directive) {
        if (directive.element && directive.element.value !== directive.data[directive.key]) {
            if (!directive.data[directive.key])
                directive.element.value = "";
            else
                directive.element.value = directive.data[directive.key];
        }
    }

    #setInputCheckbox(directive) 
    {
        if (directive.element && directive.element.checked !== directive.data[directive.key]) 
        {
            if (!directive.data[directive.key])
                directive.element.checked = false;
            else
                directive.element.checked = directive.data[directive.key];
        }
    }

    #setInputRadio(directive)
    {
        if (directive.element && directive.element.type === 'radio' && directive.data[directive.key] !== undefined && directive.element.checked !== (directive.element.value === directive.data[directive.key])) 
        {
            directive.element.checked = directive.element.value === directive.data[directive.key];
        }
    }

    #setClass(directive) 
    {
        if (!directive.data[directive.key])
            return;

        if (!directive.orgvalue)
            directive.orgvalue="";
     
        let classes = directive.data[directive.key];
        if (classes.includes(','))
            classes = classes.replaceAll(',', ' ');
    
        directive.element.className = classes || directive.orgvalue;
    }

    #setSrc(directive) 
    {
        if (!directive.data[directive.key])
            return;
    
        directive.element.src = directive.data[directive.key];
    }

    #setHref(directive) 
    {
        if (!directive.data[directive.key])
            return;

        directive.element.href = directive.data[directive.key];
    }

    #setOptionId(directive)
    {
        if (!directive.data[directive.key])
            return;

        directive.element.value = directive.data[directive.key];
    }

    #setOptionText(directive) {
        if (!directive.data[directive.key])
            return;

        directive.element.textContent = directive.data[directive.key];
    }

    #setInterpolation(directive)
    {
 
        let interpolatednode = directive.interpolatednode;
        let nodetemplate = directive.nodetemplate;
        //We loop individual expressions if many in a node
        directive.nodeexpressions.forEach(ipstmt => 
        {

            let interpol_value = "";
            const attribute_value_type = this.#getExpressionType(ipstmt.directivevalue, BareaHelper.DIR_INTERPOLATION, ipstmt.renderedvarname);
            if (attribute_value_type===BareaHelper.EXPR_TYPE_INVALID)
            {
                    console.error(`The ${BareaHelper.DIR_INTERPOLATION} directive has an invalid expression (${ipstmt.directivevalue}).`);
                    return;
            }
    
            if (attribute_value_type===BareaHelper.EXPR_TYPE_COMPUTED){
                interpol_value=this.#computedProperties[ipstmt.directivevalue].value;
            }else if ([BareaHelper.EXPR_TYPE_ROOT_PATH,BareaHelper.EXPR_TYPE_OBJREF,BareaHelper.EXPR_TYPE_OBJREF_PATH].includes(attribute_value_type)){
               
                if (!ipstmt.isobjectexpression) {
                    interpol_value = ipstmt.data[ipstmt.key];
                }else{
                    interpol_value = JSON.stringify(ipstmt.data); 
                } 

            }
            else if (attribute_value_type === BareaHelper.INTERPOL_INDEX)
            {
                interpol_value = String(directive.renderedindex);
            }
    
            if (!interpol_value) {
                interpol_value = "";
                if (typeof interpol_value === "boolean")
                    interpol_value = "false";

            }
              
            nodetemplate = nodetemplate.replaceAll(ipstmt.expression, interpol_value);

        });
       
      
        interpolatednode.textContent = nodetemplate;
        
    }

    
    /**
     * Render templates = adding new stuf to the DOM
     * @param {string} path - The path affected when proxy updated.
     * @param {any} value - The value changed in the proxy.
     * @param {string} key - The key in the parent affected object.
     * @param {any} target - The parent object of what's changed.
     */
    #renderTemplates(template_directive, path='root', reasonarray, arrayfuncname, arrayfuncargs) 
    {
        let elementsRemoved = false;
        let fullReRender = false;
        let foreacharray = [];

        let [varname, datapath] = template_directive.directivevalue.split(" in ").map(s => s.trim());
        if (!varname)
        {
            console.error(`No variable name was found in the ${template_directive.directivename} expression`);
            return;
        }
        if (!datapath)
        {
            console.error(`No path or computed function was found in the ${template_directive.directivename} expression`);
            return;
        }

        let sourcetype = this.#getExpressionType(datapath, template_directive.directivename);
        if (sourcetype===BareaHelper.EXPR_TYPE_INVALID){
            console.error(`The expression for the directive ${template_directive.directivename} value: ${template_directive.directivevalue} is not valid`);
            return;
        }


        if (arrayfuncname && (arrayfuncname === 'shift' || arrayfuncname === 'pop' || arrayfuncname === 'splice'))
            elementsRemoved=true;    
        if (arrayfuncname && (arrayfuncname === 'sort' || arrayfuncname === 'reverse' || arrayfuncname === 'splice'))
            fullReRender = true;
        if (sourcetype===BareaHelper.EXPR_TYPE_COMPUTED)
            fullReRender = true;
        if (arrayfuncname && !BareaHelper.ARRAY_FUNCTIONS.includes(arrayfuncname))
            fullReRender = true;
        if (reasonarray && !arrayfuncname)
            fullReRender = true;

        if (sourcetype===BareaHelper.EXPR_TYPE_COMPUTED)
        {
            if (this.#computedProperties[datapath])
                foreacharray = this.#computedProperties[datapath].value;
            else
                console.warn(`Could not find computed function name in the ${template_directive.directivename} directive`);

        }
        else
        {
            if (reasonarray){
                foreacharray = reasonarray; 
            }else{
                foreacharray = this.getProxifiedPathData(datapath);
            }      
        }

      
        if (fullReRender)
        {
            let counter=0;
            this.#uiDependencyTracker.removeTemplateDependencies(template_directive.id);

            template_directive.parentelement.innerHTML = "";
            const fragment = document.createDocumentFragment();
            foreacharray.forEach(row => {
   
                let newItem = this.#getNewTemplateElement(template_directive, varname, row, counter);
                fragment.appendChild(newItem);
                counter++;
            });

            if (fragment.childElementCount>0)
                template_directive.parentelement.appendChild(fragment);  

        }
        else
        {
            if (arrayfuncname === "push" && arrayfuncargs) {
                arrayfuncargs.forEach(row => {
                    let newItem = this.#getNewTemplateElement(template_directive, varname, row,-1);
                    template_directive.parentelement.appendChild(newItem);
                });
            } 
            else if (arrayfuncname === "unshift" && arrayfuncargs) {
                arrayfuncargs.reverse().forEach(row => {
                    let newItem = this.#getNewTemplateElement(template_directive, varname, row,-1);
                    template_directive.parentelement.insertBefore(newItem,  template_directive.parentelement.firstChild);
                });
            }
            else if (arrayfuncname === "pop") {
                template_directive.parentelement.removeChild(template_directive.parentelement.lastChild);
            }
            else if (arrayfuncname === "shift") {
                template_directive.parentelement.removeChild(template_directive.parentelement.firstChild);
            }
               
            if (template_directive.usesReRenderedInterpolation)
            {
                let counter=0;
                foreacharray.forEach(row=>{
                    let directives = this.#uiDependencyTracker.getObjectDependencies(row, template_directive.id);
                    directives.forEach(dir =>{
                        if (dir.directivename === BareaHelper.DIR_INTERPOLATION && dir.isrendered)
                        {
                            dir.renderedindex=counter;
                            this.#setInterpolation(dir);
                            counter++; 
                        }
                    });    
                });
            }
 
        }

        if (elementsRemoved)
            this.#uiDependencyTracker.removeDeletedElementDependencies(template_directive.id);
        
    }

    #getNewTemplateElement(template, varname, row, index=-1)
    {
        this.#internalSystemCounter++;
        const newtag = document.createElement(template.templatetagname);
        newtag.innerHTML = template.templatemarkup;

        if (template.elementclone)
        {
            for (let attr of template.elementclone.attributes)
            {
                if (!BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(attr.name))
                    newtag.setAttribute(attr.name, attr.value);
            }
        }
       
        if (newtag.id)
            newtag.id = newtag.id + `-${this.#internalSystemCounter}` 
        else
            newtag.id = `${template.id}-${varname}-${this.#internalSystemCounter}`; 

        let templatechildren = newtag.querySelectorAll("*"); 
        templatechildren.forEach(child => 
        {
           
            if (child.id)
                child.id = child.id + `-${this.#internalSystemCounter}` 
            else
                child.id = `${varname}-${this.#internalSystemCounter}`; 

            let forattrib = child.getAttribute("for");
            if (forattrib)
                child.setAttribute("for", forattrib + `-${this.#internalSystemCounter}`); 
        
        });

        this.#trackDirectives(newtag, {template:template, rendereddata:row, renderedindex:index, renderedvarname:varname, renderedobjid: this.#internalSystemCounter});
        return newtag;
    }

   
    #getExpressionType = function(expression, directive, varname) 
    {
        if (BareaHelper.DIR_GROUP_BIND_TO_PATH.includes(directive))
        {
            if (expression.includes('(') || expression.includes(')'))
                return BareaHelper.EXPR_TYPE_INVALID;

            if (!expression.includes(BareaHelper.ROOT_OBJECT) && !varname)
                return BareaHelper.EXPR_TYPE_INVALID;

            if (!(expression.includes('.')))
                return BareaHelper.EXPR_TYPE_INVALID;

            if (expression.toLowerCase().startsWith('root.'))
                return BareaHelper.EXPR_TYPE_ROOT_PATH;

            return BareaHelper.EXPR_TYPE_OBJREF_PATH;
        }

        if ([BareaHelper.DIR_CLICK, BareaHelper.DIR_BIND_HANDLER].includes(directive))
        {
            if (!(expression.includes('(') && expression.includes(')')))
                return BareaHelper.EXPR_TYPE_INVALID;

            return BareaHelper.EXPR_TYPE_HANDLER;
        }

        if (BareaHelper.DIR_GROUP_COMPUTED.includes(directive))
        {
            if (expression.includes('(') || expression.includes(')'))
                return BareaHelper.EXPR_TYPE_INVALID;

            if ((expression.includes('root.')))
            {
                if (varname && expression.includes(varname+'.'))
                    return BareaHelper.EXPR_TYPE_MIX_EXPR;

                return BareaHelper.EXPR_TYPE_ROOT_EXPR;
            }
            
            if (!(expression.includes('.')))
                return BareaHelper.EXPR_TYPE_COMPUTED;

            return BareaHelper.EXPR_TYPE_OBJREF_EXPR;
        }

        if (directive === BareaHelper.DIR_FOREACH)
        {
            if ((expression.includes('root.')))
                return BareaHelper.EXPR_TYPE_ROOT_PATH;

            if (!(expression.includes('.')))
                return BareaHelper.EXPR_TYPE_COMPUTED;

            return BareaHelper.EXPR_TYPE_INVALID;
        }

        if (directive === BareaHelper.DIR_INTERPOLATION)
        {
            if (expression.includes('(') || expression.includes(')'))
                return BareaHelper.EXPR_TYPE_INVALID;

            if (expression.toLowerCase() === (varname + '.' + BareaHelper.INTERPOL_INDEX))
                return BareaHelper.INTERPOL_INDEX;

            if (expression.toLowerCase().startsWith('root.'))
                return BareaHelper.EXPR_TYPE_ROOT_PATH;

            if (expression.includes('.'))
                return BareaHelper.EXPR_TYPE_OBJREF_PATH;

            if (this.#computedPropertyNames.includes(expression))
                return BareaHelper.EXPR_TYPE_COMPUTED;

            return BareaHelper.EXPR_TYPE_OBJREF;
        }

    
        return BareaHelper.EXPR_TYPE_INVALID;
    }

    #extractInterpolations(text) {
        const regex = /\{\{\s*.*?\s*\}\}/g;
        return text.match(regex) || [];
    }

    
    #isPrimitive(value)
    {
        let result = value !== null && typeof value !== "object" && typeof value !== "function";
        return result;
    }


    #loadedHandler =  (event) => {
       if (this.#enableHideUnloaded)
       {
            document.querySelectorAll(".ba-cloak").forEach(el => el.classList.remove("ba-cloak"));
       }
    }


    #getNewComputedProperty(func, funcname)
    {
        let instance = this;
        let computed_properties_tracker = this.#computedPropertiesDependencyTracker;
        let userinterface_tracker = this.#uiDependencyTracker;
        class BareaComputedProperty
        {
            dependentDirectives = new Set();

            constructor(func, funcname) {
                this.name=funcname;
                this.getter = func;
                this.dirty = true;
                this.dependencyPaths = new Set();
                this.setDirty = (path) => 
                {
                    this.dirty = true;
                    userinterface_tracker.notifyDependentUserInterface(this.dependentDirectives);
                };
            }
        
            track(dep, path) {
                dep.addSubscriber(this.name, this.setDirty); //The computed property tells that tracker: Hi i'm a new subscriber
                this.dependencyPaths.add(path);
            }

            addDependentDirective(directive)
            {
                this.dependentDirectives.add(directive);
            }

            clearDependentDirectives()
            {
                this.dependentDirectives.clear();
            }
        
            get value() 
            {
                if (this.dirty) 
                {
                    computed_properties_tracker.start(this);
                    this._value = this.getter.call(instance);
                    computed_properties_tracker.stop();
                    this.dirty = false;
                }
                return this._value;
            }
        }

        return new BareaComputedProperty(func, funcname);
    }

    #getComputedPropertiesDependencyTracker()
    {
        let instance;

        class  ComputedPropertiesDependencyTracker
        {
            #dependencies = null;
            #activeComputed = null;

            constructor() 
            {
                if (instance)
                    return instance;

                this.#dependencies = new Map();
                instance = this;
            }
        
            start(computed) {
                this.#activeComputed  = computed;
            }
        
            stop() {
                this.#activeComputed  = null;
            }

            getAllDependencies()
            {
                return this.#dependencies;
            } 

            isDepencencyPath(path, funcname)
            {
                if (!path)
                    return false;
                if (!funcname)
                    return false;

                if (!this.#dependencies.has(path))
                    return false;

                for (let childKey of this.#dependencies.keys()) 
                {
                    if (childKey!==path)
                        continue;

                    let childMap = this.#dependencies.get(childKey); 
                
                    for (let key of childMap.subscribers.keys()) {
                        if (key===funcname) {
                            return true;
                        }
                    }
                }

                return false;
            }
        
            //Called on proxy change get
            //If a computed func is active on the singelton tracker
            //Look for a dependency or creates one for the computed func
            track(objpath, funcname) 
            {
                if (!objpath)
                    return;

                if (objpath===BareaHelper.ROOT_OBJECT)
                    return;

                let trackpath ="";
                if (funcname)
                    trackpath = objpath+'.'+funcname.toLowerCase();
                else
                    trackpath=objpath;

                if (this.#activeComputed) {
                    let dep = this.#getDependency(trackpath);
                    this.#activeComputed.track(dep, trackpath);
                }
            }

            /**
            * Called on proxy change (proxy handler set)
            * Sets computed properties dirty if the property is dependent on the objpath
            * Updates directives that are dependent on the computed property that becomes dirty
            * @param {string} objpath - The path in the data that was changed
            * @param {string} funcname - If a function was used on an array, the name of the function
            */
            notify(objpath, funcname) 
            {
                if (!objpath)
                    return;

                if (objpath===BareaHelper.ROOT_OBJECT)
                    return;

                let trackpath ="";
                if (funcname)
                    trackpath = objpath+'.'+funcname.toLowerCase();
                else
                    trackpath=objpath;


                let dep = this.#getDependency(trackpath);
                    dep.notify(trackpath);
            }

            #getDependency(path) 
            {
                let dep = this.#dependencies.get(path);
                if (!dep) {
                    dep = this.#createDependency(path);

                    this.#dependencies.set(path, dep);
                }

                return dep;
            }

            #createDependency(path) 
            {
                let subscribers = new Map();
                return {
                    subscribers : subscribers, 
                    addSubscriber(name, set_dirty_func) {
                        if (!subscribers.has(name))
                        {
                            subscribers.set(name, set_dirty_func);
                        }
                    },
                    notify(path) { 
                        subscribers.forEach((set_dirty_func, name) => {
                            set_dirty_func(path); 
                        });
                    }
                };
            }

        }

        return new ComputedPropertiesDependencyTracker();
    }

    #getUserInterfaceTracker(notifycallback)
    {
        let instance;
        let computedProperties = this.#computedProperties;
        let barea = this;
        class UserInterfaceTracker
        {
            #dependencies = new Map();
            #objectReference = new WeakMap();
            #objectReferenceInfo = new Map();
            #notifycallback = null;
            #objectCounter=0;
            #trackingCalls=0;
            #notificationCalls=0;
            #userTemplates=[];

            constructor(notifycallback) 
            {
                if (instance)
                    return instance;

                this.#notifycallback = notifycallback;
                instance = this;
            }

            #getObjectId(obj) {
                let isnewobj = false;
                let retval = 0;
                if (!this.#objectReference.has(obj)) {
                    isnewobj = true;
                    this.#objectReference.set(obj, ++this.#objectCounter);
                }
                retval = this.#objectReference.get(obj);
                return { id: retval, isnew: isnewobj };
            }

            removeTemplateDependencies(templateid=-1)
            {
                let keystodelete = [];
                this.#dependencies.forEach((value, key) => 
                {
                    for (const directive of value) 
                    {
                        if (directive.template && directive.template.id===templateid && templateid!==-1)
                            keystodelete.push(key);
                    }       
                });

                for (let i = 0; i < keystodelete.length-1; i++)
                    this.#dependencies.delete(keystodelete[i]);

                //Remove directive:s from computed functions too
                Object.keys(computedProperties).forEach(key => {
                    //Create an array before deleting in the set
                    for (let directive of [...computedProperties[key].dependentDirectives]) {
                        if (directive.template && directive.template.id===templateid)
                            computedProperties[key].dependentDirectives.delete(directive);
                    }
                });

            }

            removeDeletedElementDependencies(templateid=-1){

                let keystodelete = [];
                this.#dependencies.forEach((value, key) => 
                {
                    for (const directive of value) 
                    {
                        if (directive.template && directive.template.id===templateid && templateid!==-1 && directive.element && !directive.element.isConnected)
                            keystodelete.push(key);
                    }       
                });

                for (let i = 0; i < keystodelete.length-1; i++)
                    this.#dependencies.delete(keystodelete[i]);

                //Remove directive:s from computed functions too
                Object.keys(computedProperties).forEach(key => {
                    //Create an array before deleting in the set
                    for (let directive of [...computedProperties[key].dependentDirectives]) {
                        if (directive.template && directive.template.id===templateid && directive.element && !directive.element.isConnected)
                            computedProperties[key].dependentDirectives.delete(directive);
                    }
                });

            }

            getObjectDependencies(object, templateid=-1)
            {
                let directives = [];
                this.#dependencies.forEach((value, key) => 
                {
                    for (const directive of value) 
                    {
                        if (directive.template && directive.data && directive.template.id===templateid && Object.is(directive.data, object)) 
                            directives.push(directive);  
                    }
                });

               return directives;

            }

            getAllDependencies()
            {
                return this.#dependencies;
            } 

            getTemplateDependencies() {
                return this.#userTemplates;
            } 

            track(scope, directive) 
            {
                this.#trackingCalls++;

                if (!directive.data && !directive.isrendered && !BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(directive.directivename)){
                    console.error(`Tracked UI has no data`,directive);
                    return;
                }
                if (BareaHelper.DIR_GROUP_COMPUTED.includes(directive.directivename))
                    return;

                const object = directive.data;
              
                let depKey="";
                if (scope==='value')
                {
                    depKey = this.#getObjectId(object).id + ":value:" + directive.key; 
                }
                else if (scope==='global')
                {
                    depKey = "global";  //Used by interpolated objects
                }else{
                    depKey=scope;                   
                }

                if (!depKey)
                    return false;
                
                if (!this.#dependencies.has(depKey)) 
                {
                    this.#dependencies.set(depKey, new Set());
                }
                this.#dependencies.get(depKey).add(directive);

                //Store user templates such as ba-foreach to be able to restore them on refresh
                if (BareaHelper.DIR_GROUP_MARKUP_GENERATION.includes(directive.directivename))
                {
                    if (directive.element && directive.parentelement){
                       this.#userTemplates.push(directive);
                    }
                }

                return true;

            }

            notify(path, reasonobj, reasonkey, reasonvalue, reasonfuncname) 
            {
                this.#notificationCalls++;

                let subobjectcheck = { isnew: false };
                if (reasonvalue)
                {
                    if (reasonvalue != null && typeof reasonvalue === "object")
                    {
                        subobjectcheck = this.#getObjectId(reasonvalue); 
                    }
                }

                let objid = this.#getObjectId(reasonobj);
                if (!objid.isnew && !subobjectcheck.isnew) {
                    let depKey = objid.id + ":value:" + reasonkey;
                    let valueset = this.#dependencies.get(depKey);
                    if (!valueset)
                        valueset = new Set();

                    depKey = "global";
                    let objectset = this.#dependencies.get(depKey);
                    if (!objectset)
                        objectset = new Set();

                    let resultset = new Set([...valueset, ...objectset]);
                    if (resultset.size === 0)
                        return;

                    this.#notifycallback(reasonobj, reasonkey, reasonvalue, path, resultset, reasonfuncname);

                }
                else {

                     barea.refresh();
                     console.warn('UI dependency tracker was notified of an object that was not tracked - barea.refresh() called. (avoid changing the model structure after mount)', reasonobj);
                }
              
            }

            notifyDependentUserInterface(uiset) 
            {
                if (uiset)
                {
                    this.#notifycallback(null, "", null, "", uiset, "");
                }
            }

            clear()
            {
                this.#userTemplates=[];
                this.#dependencies.clear();
            }
           

        }

        return new UserInterfaceTracker(notifycallback);
    
    }


}

export class BareaViewState extends EventTarget
{
    #views = [];
    #appElement = null;
    #dataModel=null;
    #useHistoryAPI=false;

    CurrentState = null;
    CurrentView = null;

    constructor(appelement, datamodel, useHistoryAPI=false) 
    {
        super();
        this.CurrentState = {};
        this.#appElement=appelement;
        this.#dataModel=datamodel;
        this.#buildViews();
        if (useHistoryAPI)
            this.#useHistoryAPI=useHistoryAPI;

        this.#handleClicks();
        this.#handleHashChange();
        this.#handleHistoryAPI();

    }

    
    get EntityId()
    {
        if (!this.CurrentState)
            return "";

        return this.CurrentState.entityId;
    }

    setStateAndNavigate(path,entityid="", force=false) 
    {
        if (!path)
            return;

        if (path.includes("/:"))
            path = path.split("/:")[0];
        
        if (entityid){
            if (!path.includes(`${entityid}`))
            {
                if (!path.endsWith("/"))
                    path+="/";

                path+=`${entityid}`;
            }
        }

        let hash ="";
        if (path.startsWith("/#")){
            path= path.replace("/#","");
            hash=path;
        }
        if (path.startsWith("#")){
            path = path.replace("#","");
            hash=path;
        }

        const url = new URL(path, window.location.origin);
        url.hash=hash;
        this.#registerNavigation(url,entityid,"programatic navigation", force); 
    }

    init()
    {
        const url = new URL(window.location.href);
        let path = "";
        if (url.hash)
            path = url.hash;
        else
            path = url.pathname;

        let entityId = "";
        let viewAtPath = this.#getView(url);
        if (viewAtPath){
            if (viewAtPath.IsPersistedEntityView){
                entityId = this.#getEntityIdFromUrl(url);
            }
        }

        //Fake a last state since this is init
        let state = { prevPath: "/" , currentPath: path, entityId: entityId, type:'init', stateTime:Date.now(), urlPathName: url.pathname, urlHash:url.hash };
        localStorage.setItem("bareaState", JSON.stringify(state));
        this.#registerNavigation(url,entityId,"init"); 
    }


    onChange(callback) {
        this.CurrentState = JSON.parse(localStorage.getItem("bareaState"));
        this.addEventListener("stateChanged", () => callback());
    }



    #buildViews()
    {
      
       let counter=0;
       const templates = document.querySelectorAll("template"); 
       templates.forEach(template=>{
            const views = template.content.querySelectorAll("view"); 
            if (views.length<=0)
                return;

            counter+=1;

            views.forEach(view=>{
               let viewid = view.getAttribute("id");
               if (!viewid){
                    console.error("BareaViewState - A view <view> in a template is missing id");
                    return;
               }
               let viewpath = view.getAttribute("path");
               if (!viewpath){
                    console.error("BareaViewState - A view definition in a template is missing path");
                    return;
               }
               let viewtitle = "";
               let titletag = view.querySelector("title");
               if (titletag)
                  viewtitle=titletag.textContent; 
               let viewdescription = "";
               let viewdescriptiontag = view.querySelector("description");
               if (viewdescriptiontag)
                  viewdescription=viewdescriptiontag.textContent; 
               let datatag = view.querySelector("data");
               let viewInfo = getAttributesAsObject(datatag);
               viewInfo.title=viewtitle;
               viewInfo.description=viewdescription;

               let registeredview = this.#views.find(p=> p.Id== viewid);
               if (!registeredview)
               {
                    registeredview = new BareaView(viewid, viewpath, this.#dataModel); 
                    registeredview.setViewDescription(viewInfo);
                    this.#views.push(registeredview);

               }
               else
               {
                  let currentInfo = registeredview.getViewDescription();
                  if (currentInfo.hasOwnProperty('title') && !currentInfo.title)
                      currentInfo.title=viewtitle;
                  if (currentInfo.hasOwnProperty('description') && !currentInfo.description)
                     currentInfo.description=viewdescription;

                  for (let key in viewInfo){
                    if (!currentInfo.hasOwnProperty(key)){
                         currentInfo[key] = viewInfo[key];
                    }
                  }
                  registeredview.setViewDescription(currentInfo);
               }

               let design = template.content.querySelector("design");
               if (!design)
                   return;

               let substitute = document.createElement("div");
               substitute.setAttribute("id",`${registeredview.Id}_${counter}`);
               template.insertAdjacentElement("afterend", substitute);
               let cloned_design = design.cloneNode(true); 
               const designcontent = document.createElement("div"); // New wrapper
               designcontent.innerHTML = cloned_design.innerHTML; 
               let markupinfo = { parent: substitute, markup:designcontent };

               registeredview.addViewMarkUpInfo(markupinfo);

           });

           template.remove();

       });

       function getAttributesAsObject(element) 
       {
            let attributes = {};
            if (!element)
                return attributes;
            for (let attr of element.attributes) {
                attributes[attr.name] = attr.value;
            }
            return attributes;
        }
    }

  
    #handleClicks()
    {
        let instance = this;
        document.addEventListener("click", (event) => 
        {

            const link = event.target.closest("a"); 
            if (!link) return; 
        
            const isNonNavControl = 
                link.hasAttribute("data-bs-toggle") || 
                link.classList.contains("dropdown-toggle") || 
                link.classList.contains("no-navigation");
        
            if (isNonNavControl) {
                return; 
            }

            if (!event.target.href) 
                return;

            const url = new URL(event.target.href);

            if (!this.#isInternalLink(url))
                return;

            event.preventDefault();
            instance.#registerNavigation(url,"","link navigation");
        });
    }

    #handleHashChange()
    {
        let instance = this;
        window.addEventListener("hashchange", function(){
            instance.#registerNavigation(window.location, "", "hash change navigation");
        });
      
    }

    #handleHistoryAPI()
    {
        if (!this.#useHistoryAPI)
            return;

        let instance = this;
        window.addEventListener("popstate", function(event) 
        {
            instance.#registerNavigation(window.location, "", "pop state navigation");

        });
    }

    
    #getEntityIdFromUrl(url)
    {
        if (!url)
            return "";

        let path = "";
        if (url.hash)
            path=url.hash;
        else
            path = url.pathname;

        let segments = path.split("/");
        if (segments.length>1){
            let entityid = segments[segments.length-1];
            if (entityid)
            {
                let isNotInViewPath = this.#views.some(p=>p.RequestPath.includes(entityid));
                if (!isNotInViewPath)
                    return entityid;

            }
        }

        return "";
    }

    #clearUI(){
        this.#views.forEach(view=>
            {
                let markupinfo = view.getViewMarkUpInfo();
                markupinfo.forEach(templ=>
                {
                    templ.parent.replaceChildren();
                });
            });
    }
  
    /**
     * Set navigation and issue state changed
     */
    #registerNavigation(url,entityid="",type="init", force) 
    {
        
        if (!url){
            return;
        }

        let laststate = this.#getPersistedState();
     
        let viewAtPath = this.#getView(url);
        if (viewAtPath){

            if (viewAtPath.IsPersistedEntityView && !entityid)
                entityid=this.#getEntityIdFromUrl(url);
    
            if (this.#comparePaths(laststate.currentPath,viewAtPath.RequestPath, laststate.entityId, entityid) && this.CurrentView && !force)
            {
                //console.warn("BareaViewState.#registerNavigation returned early since the new state detected was the same as current.");
                return;
            }

            this.CurrentView = viewAtPath;

        }else{
            this.CurrentView = null;
        }

        this.#clearUI();
  
        laststate.prevPath = laststate.currentPath;

        //Don't affect the window if this is an init (infinity loop)
        if (type!=="init"){
            if (url.hash){
                window.location.hash = url.hash;
                laststate.currentPath=url.hash;
            }else{
                laststate.currentPath=url.pathname;
                if (this.#useHistoryAPI){
                    history.pushState(this.CurrentState, type, url);
                }else{
                    window.location.href = url;
                }
            }
        }
       
        laststate.urlPathName = url.pathname;
        laststate.urlHash = url.hash;
        laststate.stateTime = Date.now();
        laststate.type = type;
        laststate.entityId  = entityid;
        this.CurrentState=laststate;
        localStorage.setItem("bareaState", JSON.stringify(this.CurrentState));

        let log = BareaHelper.getDebugLog(10);
        if (log.active)
            console.info(`state engine: ${type}`, this.CurrentState);
         
        this.dispatchEvent(new Event("stateChanged"));
    
    }

   
    #isInternalLink(url) 
    {
        try 
        {
            const linkUrl = new URL(url, window.location.origin);
            return linkUrl.origin === window.location.origin;
        } 
        catch (e) 
        {
            return false; 
        }
    }

    #comparePaths(p1,p2, entityid1, entityid2)
    {
        if (!p1 || !p2)
            return false;

        if (p1.startsWith("/") && !p2.startsWith("/"))
            p2="/"+p2;
        if (p2.startsWith("/") && !p1.startsWith("/"))
            p1="/"+p1;

        if (p1==p2)
            return true;

        if (!entityid1 || !entityid2)
            return false;

        if (entityid1==entityid2){
            if (p1.includes(entityid1))
            {
                if (p1 == p2+"/"+entityid2)
                    return true;

                if (p1 == p2+entityid2)
                    return true;
            }

            if (p2.includes(entityid2))
            {
                    if (p2 == p1+"/"+entityid1)
                        return true;
                    
                    if (p2 == p1+entityid1)
                        return true;
            }
        }

        return false;

    }

   
    #getPersistedState(){
        try{
            return JSON.parse(localStorage.getItem("bareaState"));
        }catch{}

        return { prevPath: "" , currentPath: "", entityId: "", type:"new", stateTime:Date.now() };
    }
   
    #getView(url){
        let viewAtPath = null;
        this.#views.forEach(view=>{
            if (view.validForUrl(url)){
                viewAtPath=view;
            }
        });
        return viewAtPath;
    }

}


export class BareaDataModel {

    #dbTableName = "";
    #dbColumns = [];
    #dataBase = null;

    constructor(dbtablename) {
        if (dbtablename)
            this.#dbTableName = dbtablename;
        else
            console.error('A new instance of BareaDataModel was created without a dbtablename');
    }

    useBareaLocalStorageDb() {
        this.#dataBase = { engine: "BareaLocalStorageDb" };
    }
    useIntwentyAPI() {
        this.#dataBase = { engine: "IntwentyAPI" };
    }

    get DbTableName() {
        return this.#dbTableName;
    }
    get DbColumns() {
        return this.#dbColumns;
    }

    addDbStringColumn(columnname, primarykey=false) {
        this.#dbColumns.push({ name: columnname, datatype: "string", autoincrement: false, primarykey:primarykey });
    }

    addDbIntegerColumn(columnname, autoincrement = false, primarykey = false) {
        this.#dbColumns.push({ name: columnname, datatype: "integer", autoincrement: autoincrement, primarykey: primarykey });
    }

    getBareaDataModel() {
        let model = {};

        model[this.#dbTableName] = {};
        model['entityList'] = [];
        this.#dbColumns.forEach(col => {
            if (col.datatype === "string")
                model[this.#dbTableName][col.name] = "";
            if (col.datatype === "text")
                model[this.#dbTableName][col.name] = "";
            if (col.datatype === "integer")
                model[this.#dbTableName][col.name] = 0;
            if (col.datatype === "boolean")
                model[this.#dbTableName][col.name] = false;
        });

        return model;
    }

    async createEntity(obj) {
        const db = this.#getDataBase();
        if (!db) {
            console.error('No database engine was specified, for mockup call: useBareaLocalStorageDb() (BareaDataModel)');
            return false;
        }
        return db.createEntity(obj);
    }

    async updateEntity(obj, id) {
        const db = this.#getDataBase();
        if (!db) {
            console.error('No database engine was specified, for mockup call: useBareaLocalStorageDb() (BareaDataModel)');
            return false;
        }
        return db.updateEntity(obj, id);
    }

    async getEntities(sql = "") {
        const db = this.#getDataBase();
        if (!db) {
            console.error('No database engine was specified, for mockup call: useBareaLocalStorageDb() (BareaDataModel)');
            return [];
        }
        return db.getEntities(sql);
    }

    async getEntity(id) {
        const db = this.#getDataBase();
        if (!db) {
            console.error('No database engine was specified, for mockup call: useBareaLocalStorageDb() (BareaDataModel)');
            return {};
        }
        return db.getEntity(id);
    }

    async deleteEntity(id) {
        const db = this.#getDataBase();
        if (!db) {
            console.error('No database engine was specified, for mockup call: useBareaLocalStorageDb() (BareaDataModel)');
            return false;
        }
        return db.deleteEntity(id);
    }

    #getDataBase() {
        if (!this.#dataBase)
            return;

        if (this.#dataBase.engine === "BareaLocalStorageDb")
            return this.#getBareaLocalStorageDb();
        if (this.#dataBase.engine === "IntwentyAPI")
            return this.#getIntwentyAPI();

        return null;
    }

    #getBareaLocalStorageDb() {
        const DataModel = this;

        class BareaLocalStorageDb {
            #initializeDb() {
                let db = null;
                let rawdata = localStorage.getItem("BareaDataBase");
                if (rawdata)
                    db = JSON.parse(rawdata);

                if (!db) {
                    db = {};
                    db[DataModel.DbTableName + '_db'] = [];
                    localStorage.setItem("BareaDataBase", JSON.stringify(db));
                }

                return db;
            }

            #getMaxId(arr) {
                let id = 0;
                arr.forEach(p => {
                    if (!BareaHelper.isValue(p,"id"))
                        return;
                    if (BareaHelper.getValue(p, "id") > id)
                        id = BareaHelper.getValue(p, "id");
                });
                return id;
            }

            createEntity(obj) {
                const db = this.#initializeDb();
                const idcol = DataModel.DbColumns.find(p => p.primarykey && p.autoincrement && p.datatype=="integer");
                if (!idcol) {
                    console.error("BareaLocalStorageDb must have an integer autoincremened primary key column");
                    return;
                }

                if (!obj[idcol.name]) {
                    obj[idcol.name] = this.#getMaxId(db[DataModel.DbTableName + '_db']) + 1;
                }

                db[DataModel.DbTableName + '_db'].push(obj);
                localStorage.setItem("BareaDataBase", JSON.stringify(db));
                return true;
            }

            updateEntity(obj, id) {
                const db = this.#initializeDb();
                const idcol = DataModel.DbColumns.find(p => p.primarykey && p.autoincrement && p.datatype == "integer");
                if (!idcol) {
                    console.error("BareaLocalStorageDb must have an integer autoincremened primary key column");
                    return;
                }
                let entityArray = db[DataModel.DbTableName + '_db'];
                let index = entityArray.findIndex(p => BareaHelper.getValue(p, idcol.name) == id);
                if (index !== -1) {
                    entityArray[index] = obj;  // Correctly update the object in the array
                    localStorage.setItem("BareaDataBase", JSON.stringify(db));
                    return true;
                }
                return false;
            }

            getEntities(sql = "") {
                const db = this.#initializeDb();
                const idcol = DataModel.DbColumns.find(p => p.primarykey && p.autoincrement && p.datatype == "integer");
                if (!idcol) {
                    console.error("BareaLocalStorageDb must have an integer autoincremened primary key column");
                    return;
                }
                let entityArray = db[DataModel.DbTableName + '_db'];
                return entityArray;
            }

            getEntity(id) {
                const db = this.#initializeDb();
                const idcol = DataModel.DbColumns.find(p => p.primarykey && p.autoincrement && p.datatype == "integer");
                if (!idcol) {
                    console.error("BareaLocalStorageDb must have an integer autoincremened primary key column");
                    return;
                }
                if (id) {
                    let entityArray = db[DataModel.DbTableName + '_db'];
                    let entityObject = entityArray.find(p => BareaHelper.getValue(p, idcol.name) == id);
                    return entityObject;
                }

                return {};
            }

            deleteEntity(id) {
                const db = this.#initializeDb();
                const idcol = DataModel.DbColumns.find(p => p.primarykey && p.autoincrement && p.datatype == "integer");
                if (!idcol) {
                    console.error("BareaLocalStorageDb must have an integer autoincremened primary key column");
                    return;
                }
                let entityArray = db[DataModel.DbTableName + '_db'];
                let index = entityArray.findIndex(p => BareaHelper.getValue(p, idcol.name) == id);
                if (index !== -1) {
                    entityArray.splice(index, 1);  // Remove the entity at the found index
                    localStorage.setItem("BareaDataBase", JSON.stringify(db));
                    return true;
                }
                return false;
            }

        }
        return new BareaLocalStorageDb();
    }


    #getIntwentyAPI()
    {
        const DataModel = this;
        class IntwentyAPI {



            #initializeDb() {
                let db = {
                    newEntityPath: "/Applications/Api/CreateEntity",
                    editEntityPath: "/Applications/Api/EditEntity",
                    deleteEntityPath: "/Applications/Api/DeleteEntity",
                    getEntityPath: "/Applications/Api/GetEntity",
                    getEntitiesPath: "/Applications/Api/GetEntities"
                };
                return db;
            }

            async createEntity(obj) {
                const db = this.#initializeDb();
                const payload = { model: { dbTableName: DataModel.DbTableName, dbColumns: DataModel.DbColumns }, data: obj, entityId: "", sqlStatement: "" };

                try {
                    const response = await fetch(db.newEntityPath, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(payload)
                    });

                    if (!response.ok) {
                        throw new Error(`HTTP Error: ${response.status} - ${response.statusText}`);
                    }

                    return true;

                }
                catch (error) {
                    console.error('Error:', error);
                }

                return false;
            }

            async updateEntity(obj, id) {
                const db = this.#initializeDb();
                const payload = { model: { dbTableName: DataModel.DbTableName, dbColumns: DataModel.DbColumns }, data: obj, entityId: id, sqlStatement: "" };
                try {
                    const response = await fetch(db.editEntityPath, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(payload) // Convert object to JSON string
                    });

                    if (!response.ok) {
                        throw new Error(`HTTP Error: ${response.status} - ${response.statusText}`);
                    }

                    return true;

                }
                catch (error) {
                    console.error('Error:', error);
                }

                return false;
            }

            async getEntities(sql = "") {
                const db = this.#initializeDb();
                const payload = {
                    model: { dbTableName: DataModel.DbTableName, dbColumns: DataModel.DbColumns }, data: {}, entityId: "", sqlStatement: sql };
                try {
                    const response = await fetch(db.getEntitiesPath, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(payload) // Convert object to JSON string
                    });

                    if (!response.ok) {
                        throw new Error(`HTTP Error: ${response.status} - ${response.statusText}`);
                    }

                    const answer = await response.json();
                    return answer.entities;

                }
                catch (error) {
                    console.error('Error:', error);
                }

                return [];
            }

            async getEntity(id) {
                const db = this.#initializeDb();
                const payload = {
                    model: { dbTableName: DataModel.DbTableName, dbColumns: DataModel.DbColumns }, data: {}, entityId: id, sqlStatement: "" };
                try {
                    const response = await fetch(db.getEntityPath, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(payload) // Convert object to JSON string
                    });

                    if (!response.ok) {
                        throw new Error(`HTTP Error: ${response.status} - ${response.statusText}`);
                    }

                    const answer = await response.json();
                    return answer.entity;

                }
                catch (error) {
                    console.error('Error:', error);
                }

                return {};
            }

            async deleteEntity(id) {
                const db = this.#initializeDb();
                const payload = { model: { dbTableName: DataModel.DbTableName, dbColumns: DataModel.DbColumns }, data: {}, entityId: id, sqlStatement: "" };
                try {
                    const response = await fetch(db.deleteEntityPath, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify(payload) // Convert object to JSON string
                    });

                    if (!response.ok) {
                        throw new Error(`HTTP Error: ${response.status} - ${response.statusText}`);
                    }

                    return true;

                }
                catch (error) {
                    console.error('Error:', error);
                }

                return false;
            }

        }

        return new IntwentyAPI();
    }



}

export class BareaView
{
    #id = "";
    #requestPath = "";
    #viewDescription = {};
    #dataModel = null;
    #viewMarkupInfo = [];
    #isPersistedEntityView=false;

   
    constructor(viewid, requestpath, datamodel=null, dbtablename = null) 
    {
      this.#id=viewid;
      this.#requestPath=requestpath;
      if (this.#requestPath.includes("/:")){
        this.#isPersistedEntityView=true;
        this.#requestPath=this.#requestPath.split("/:")[0];
      }
      if (datamodel){
        this.#dataModel = datamodel;
      }else{
        this.#dataModel = new BareaDataModel(dbtablename);
      }
     
    }

    get Id() {
        return this.#id;
    }

    get IsPersistedEntityView() {
        return this.#isPersistedEntityView;
    }

    get RequestPath(){
        return this.#requestPath;
    }

    getViewMarkUpInfo(){
        return this.#viewMarkupInfo;
    }

    addViewMarkUpInfo(markupinfo)
    {
        this.#viewMarkupInfo.push(markupinfo);
    }

    getDataModel(){
        return this.#dataModel;
    }

    setViewDescription(description)
    {
        this.#viewDescription  = description;
    }

    getViewDescription(){
        return this.#viewDescription;
    }

    addDbStringColumn(columnname, allownull = true)
    {
        this.#dataModel.addDbStringColumn(columnname, allownull);
    }

    addDbIntegerColumn(columnname, autoincrement=false)
    {
        this.#dataModel.addDbIntegerColumn(columnname, autoincrement);
    }

    validForUrl(url)
    {
        if (!url)
            return false;

        if (url.hash)
        {
            let hash = url.hash;
            if (hash.length<2)
                return false;

            if (hash.includes(this.#requestPath))
                return true;

            hash="/"+hash;
            if (hash.includes(this.#requestPath))
                return true;
        }

        if (url.pathname.includes(this.#requestPath))
            return true;

        return false;
    }


    render() 
    {

        this.#viewMarkupInfo.forEach(t=>
        {
            t.parent.replaceChildren(t.markup.cloneNode(true));
        });

    }

}


export class BareaHelper
{

    //Directives
    static DIR_BIND = 'ba-bind';
    static DIR_BIND_BLUR = 'ba-bind-blur';
    static DIR_BIND_HANDLER  = 'ba-bind-handler';
    static DIR_FOREACH = 'ba-foreach';
    static DIR_CLICK = 'ba-click';
    static DIR_CLASS = 'ba-class';
    static DIR_CLASS_IF = 'ba-class-if';
    static DIR_HIDE = 'ba-hide';
    static DIR_SHOW = 'ba-show';
    static DIR_IMAGE_SRC = 'ba-src';
    static DIR_IF = 'ba-if';
    static DIR_HREF = 'ba-href';
    static DIR_INTERPOLATION = 'interpolation';
    static DIR_OPTION_ID = 'ba-option-id';
    static DIR_OPTION_TEXT = 'ba-option-text';

    static DIR_GROUP_BIND_TO_PATH = [BareaHelper.DIR_BIND, BareaHelper.DIR_BIND_BLUR, BareaHelper.DIR_CLASS, BareaHelper.DIR_HREF, BareaHelper.DIR_IMAGE_SRC, BareaHelper.DIR_OPTION_ID, BareaHelper.DIR_OPTION_TEXT];
    static DIR_GROUP_UI_DUPLEX = [BareaHelper.DIR_BIND,BareaHelper.DIR_BIND_BLUR];
    static DIR_GROUP_TRACK_AND_FORGET = [BareaHelper.DIR_CLICK];
    static DIR_GROUP_COMPUTED = [BareaHelper.DIR_CLASS_IF,BareaHelper.DIR_HIDE,BareaHelper.DIR_SHOW, BareaHelper.DIR_IF];
    static DIR_GROUP_MARKUP_GENERATION = [BareaHelper.DIR_FOREACH];

    static UI_INPUT_TEXT = 1;
    static UI_INPUT_CHECKBOX = 2;
    static UI_INPUT_RADIO = 3;
    static UI_INPUT_SELECT = 4;
    static UI_INPUT_CUSTOM = 10;


    //Verbs
    static VERB_SET_DATA = 'SET_DATA';
    static VERB_SET_UI = 'SET_UI';


    //Dom meta data
    static META_PATH = 'ba-path';
    static META_DYN_FUNC_PREFIX = 'dynFunc_';
    static META_DYN_TEMPLATE_FUNC_PREFIX = 'dynTemplFunc_';


    //Special interpolation expressions
    static INTERPOL_INDEX = 'index';


    //Expression types
    static EXPR_TYPE_ROOT_PATH = 'root-path';
    static EXPR_TYPE_HANDLER = 'static-handler';
    static EXPR_TYPE_COMPUTED = 'computed';
    static EXPR_TYPE_OBJREF = 'child-obj';
    static EXPR_TYPE_OBJREF_PATH = 'child-obj-path';
    static EXPR_TYPE_OBJREF_EXPR = 'child-obj-expression';
    static EXPR_TYPE_ROOT_EXPR = 'root-expr';
    static EXPR_TYPE_MIX_EXPR = 'root-and-child-expression';
    static EXPR_TYPE_INVALID = 'invalid-expression';

    //The root of your data
    static ROOT_OBJECT = 'root';

    //Array functions to handle in the proxy
    static ARRAY_FUNCTIONS = ['push', 'pop', 'splice', 'shift', 'unshift','reverse','sort']; 

    static DebugLogs = [
        {id: 1, name: "BareaApp - Proxy call back: ", active:false},
        {id: 2, name: "BareaApp - Update dom on proxy change: ", active:false},
        {id: 3, name: "BareaApp - Update proxy on user input: ", active:false},
        {id: 4, name: "BareaApp - Print tracked UI dependencies: ", active:false},
        {id: 5, name: "BareaApp - Print tracked computed dependencies: ", active:false},
        {id: 10, name: "BareaViewState - On state changed: ", active:false}
    ];

    static deepTraverseObjectToArray = function (obj, parentPath = 'root')
    {
        let result = [];

        if (obj !== null && typeof obj === 'object')
        {
            for (let key in obj) {

                if (obj.hasOwnProperty(key))
                {
                    const currentPath = parentPath ? `${parentPath}.${key}` : key; // Build the full path

                    // Check the type of the current property (object, array, or simple type)
                    if (Array.isArray(obj[key])) {
                        result.push({path: currentPath,type: 'array',value: obj[key]});
                        obj[key].forEach((item, index) => {
                            result = result.concat(BareaHelper.deepTraverseObjectToArray(item, `${currentPath}[${index}]`));
                        });
                    }
                    else if (typeof obj[key] === 'object' && obj[key] !== null) {
                        result.push({path: currentPath,type: 'object',value: obj[key]});
                        result = result.concat(BareaHelper.deepTraverseObjectToArray(obj[key], currentPath));
                    }
                    else {
                        result.push({path: currentPath,type: 'primitive',value: obj[key] });
                    }
                }
            }

        }
        else{
            result.push({ path: parentPath, type: 'primitive',value: null});
        }

        return result;
    }

    static getObjectInfo = function (rootobj, compareobj, parentPath = 'root')
    {
        
        if (rootobj !== null && typeof rootobj === 'object') {
            for (let key in rootobj) {

                if (rootobj.hasOwnProperty(key)) {
                    const currentPath = parentPath ? `${parentPath}.${key}` : key;
                    if (Array.isArray(rootobj[key])) {
                        let cmp = Object.is(compareobj, rootobj[key]);
                        if(cmp)
                            return { path: currentPath, type: 'array', value: rootobj[key] };

                        rootobj[key].forEach((item, index) => {
                            BareaHelper.getObjectInfo(item, `${currentPath}[${index}]`);
                        });

                    }
                    else if (typeof rootobj[key] === 'object' && rootobj[key] !== null) {
                        let cmp = Object.is(compareobj, rootobj[key]);
                        if (cmp)
                            return { path: currentPath, type: 'object', value: rootobj[key] };

                        BareaHelper.getObjectInfo(rootobj[key], currentPath);
                    }
                }
            }
        }
       
        return null;
    }


    static getLastBareaKeyName = function(path)
    {
        if (!path)
            return 'root';

        let keys = path.split('.');

        if (keys.length<2)
            return 'root';
    
        let key = "";
        keys.forEach(t=>{key=t});
        return key;
    }

    static isValue =function(object, key) {
        if (!object || typeof object !== 'object' || !key) return false;
        const lowerKey = key.toLowerCase();
        return Object.keys(object).some(objKey => objKey.toLowerCase() === lowerKey && object[objKey] != null);
    }

    static getValue = function getValue(object, key)
    {
        if (!object || typeof object !== 'object' || !key) return undefined;
        const lowerKey = key.toLowerCase();
        for (const objKey in object) {
            if (objKey.toLowerCase() === lowerKey) {
                return object[objKey];
            }
        }
        return undefined;
    }

    static getLastBareaObjectPath = function(path)
    {
        if (!path) {
            console.error('callk to BareaHelper.getLastBareaObjectPath() without a path');
            return;
        }

        let retval = null;
        let keys = path.split('.');
        for (let i = 0; i < keys.length-1; i++) 
        {
        
            if (!retval)
                retval=keys[i]
            else
                retval+='.'+keys[i];

        };

        if (retval==null)
            retval = path;
        
        return retval;
    }

    static parseBareaFunctionCall = function(str) 
    {
        function convertValue(val) {
            if (/^["'].*["']$/.test(val)) return val.slice(1, -1); // Remove quotes
            if (val === "true") return true;
            if (val === "false") return false;
            if (val === "null") return null;
            if (!isNaN(val)) return val.includes(".") ? parseFloat(val) : parseInt(val, 10);
            if (/^\[.*\]$/.test(val)) return val.slice(1, -1).split(',').map(item => convertValue(item.trim()));
            return val;
        }

        const match = str.match(/^(\w+)\((.*)\)$/);
        if (!match) return null;

        const [_, functionName, paramsString] = match;
        const params = paramsString.trim() 
            ? [...paramsString.matchAll(/'[^']*'|"[^"]*"|[^,]+/g)].map(m => convertValue(m[0].trim())) 
            : [];

        return { functionName, params };
    }

    static hasAnyChar = function(str, chars) 
    {
        return chars.some(char => str.includes(char));
    }

    static enableDebugLog = function(id)
    {
        const logidx = BareaHelper.DebugLogs.findIndex(p=> p.id===id);
        if (logidx!== -1)
            BareaHelper.DebugLogs[logidx].active = true;
    }

    static getDebugLog = function(id) 
    {
        const log = BareaHelper.DebugLogs.find(log => log.id === id);
        if (!log)
            return {id:-1, name:"", active:false};
        return log;
    }

    static printDebugLogs = function()
    {
      console.log('barea: available logs:');
      BareaHelper.DebugLogs.forEach(p=> console.log(p));
    }

}



