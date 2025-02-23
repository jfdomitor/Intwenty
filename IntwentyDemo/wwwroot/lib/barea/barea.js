/**
 * barea.js
 * 
 * Author: Johan Filipsson
 * Version: 1.0.0
 * License: MIT
 * Description: A lightweight and reactive JavaScript library for modern web applications.
 * 
 * Copyright (c) 2025 Johan Filipsson
 */
function getBareaApp(enableInternalId){
    return new BareaApp(enableInternalId);
}

//Directives
const DIR_BIND = 'ba-bind';
const DIR_BIND_HANDLER  = 'ba-bind-handler';
const DIR_FOREACH = 'ba-foreach';
const DIR_CLICK = 'ba-click';
const DIR_CLASS = 'ba-class';
const DIR_CLASS_IF = 'ba-class-if';
const DIR_HIDE = 'ba-hide';
const DIR_SHOW = 'ba-show';
const DIR_IMAGE_SRC = 'ba-src';
const DIR_IF = 'ba-if';
const DIR_HREF = 'ba-href';
const DIR_INTERPOLATION = 'interpolation';

//Directive types
const DIR_TYPE_BINDING = 'binding';
const DIR_TYPE_HANDLER = 'uihandler';
const DIR_TYPE_UI_SETTER = 'uisetter';
const DIR_TYPE_BOOLHANDLER = 'boolhandler';
const DIR_TYPE_BOOLEXPR = 'boolexpression';
const DIR_TYPE_TEMPLATE = 'template';


//Verbs
VERB_SET_DATA = 'SET_DATA';
VERB_SET_UI = 'SET_UI';


//Dom meta data
const META_ARRAY_VARNAME = 'ba-varname';
const META_ARRAY_INDEX = 'ba-index';
const META_PATH = 'ba-path';

// Special interpolation expressions
const INTERPOL_INDEX = 'index';
const INTERPOL_ARRCOUNT = 'count';





class BareaApp {

    #bareaId=0;
    #enableBareaId = false;
    #enableInterpolation = true;
    #appElement; 
    #appElementDisplay="";
    #enableHideUnloaded=false;
    #appDataProxy; 
    #eventHandlers = {};
    #consoleLogs = [];
    #domDictionary = []; //Reference dom from paths
    #domDictionaryId=0;
    #mounted=false;
    #mountedHandler=null;

    constructor(enableInternalId) {
        //this.#appDataProxy = data;
        this.#enableBareaId=enableInternalId;
        this.#setConsoleLogs();
       
    }

    mount(element, data) 
    {
        if (this.#mounted)
            return;

        this.#mounted=true;

        if (!element)
        {
                console.error('Illegal use of mount, please pass an element or an element identifier');
                return;
        }

        if (typeof element === "object") 
            this.#appElement = element
        else
            this.#appElement = document.getElementById(element);

        this.#appDataProxy = data

        if (this.#enableHideUnloaded)
        {
            document.addEventListener('DOMContentLoaded',this.#loadedHandler);
        }else{
            document.querySelectorAll(".ba-cloak").forEach(el => el.classList.remove("ba-cloak"));
        }
       
        if (!('fetch' in window && 'Promise' in window && 'Symbol' in window && 'Map' in window))
        {
            console.error('Your browser and js engine is too old to use this script');
            return;
        }
          
        const proxy = this.#createReactiveProxy((path, value, key) => 
        { 
            //Handles changes in the data and updates the dom

            let log = this.#getConsoleLog(1);
            if (log.active)
                console.log(log.name, path, value, key);

            this.#renderTemplates(key, path, value);
            this.#setupBindings(path);
            this.#applyProxyChangesToDOM(path, value);

        }, this.#appDataProxy);

        this.#appDataProxy = proxy;
  
        this.#buildDomDictionary();
        this.#renderTemplates();
        this.#setupBindings();
        this.#applyProxyChangesToDOM();

        if (this.#enableBareaId && ! this.#appDataProxy.hasOwnProperty('baId')) 
            this.#appDataProxy.baId = ++this.#bareaId;  // Assign a new unique ID

        if (this.#mountedHandler)   
            this.#mountedHandler.apply(this, [this.#appDataProxy]);
   
        return this.#appDataProxy;
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

    enableInterpolation(value)
    {
        if (this.#isPrimitive(value))
            this.#enableInterpolation = value;
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
            this.#eventHandlers[functionName] = handlerFunction;
        } 
        else 
        {
            console.warn(`Handler for "${functionName}" is not a function.`);
        }
    }

    addMountedHandler(handlerFunction) 
    {
        if (typeof handlerFunction === "function") 
        {
            this.#mountedHandler = handlerFunction;
        } 
        else 
        {
            console.warn("The object passed to addMountedHandler is not a function.");
        }
    }

    getPathData(path) {
        const keys = path.match(/[^.[\]]+/g);
        let target = this.#appDataProxy;
    
        // Loop with for (faster than forEach)
        for (let i = 0; i < keys.length; i++) {
            if (i === 0 && keys[i].toLowerCase() === 'root') continue; // Skip 'root' only if it's the first key
            if (!target) return undefined; // Exit early if target becomes null/undefined
            target = target[keys[i]];
        }
    
        return target;
    }

    #createReactiveProxy(callback, data, currentpath = "root") {
      
        const handler = {
            get: (target, key) => {
                const value = target[key];
                const newPath = Array.isArray(target) ? `${currentpath}[${key}]` : `${currentpath}.${key}`;
              
              
                // If the value is already a proxy, return it as is to avoid recursive proxying
                if (value && value.__isProxy) {
                    return value;
                }

                if (typeof value === 'object' && value !== null) 
                {
                    if (!Array.isArray(value) && this.#enableBareaId && !value.hasOwnProperty('baId')) 
                    {
                            value.baId = ++this.#bareaId;  
                    }

                    return this.#createReactiveProxy(callback, value, newPath); 

                }else{

                    if(typeof value === "function")
                    {
                        if (['push', 'pop', 'splice', 'shift', 'unshift'].includes(value.name)) 
                        {
                            return (...args) => {
                                const result = Array.prototype[value.name].apply(target, args);
                                callback(currentpath, target, key); // Trigger DOM update on array changes
                                return result;
                            };
                        }    
        
                    }
                }
              
                return value;
            },
            set: (target, key, value) => {

                if (target[key] === value) 
                    return true;
        
                target[key] = value;

                if (['length'].includes(key)) 
                    return true;

                const path = Array.isArray(target) ? `${currentpath}[${key}]` : `${currentpath}.${key}`;
                callback(path, value, key);
                return true;
            }
        };
        
        return new Proxy(data, handler);
    }
    
    #buildDomDictionary(tag = this.#appElement, templateId=-1)
    {
        const templateChildren=[];

        function collectDescendants(ce) {
            templateChildren.push(ce);
            
            for (let i = 0; i < ce.children.length; i++) {
                collectDescendants(ce.children[i]);
            }
        }
        tag.querySelectorAll(`[${DIR_FOREACH}]`).forEach(parent => {
            Array.from(parent.children).forEach(child => collectDescendants(child));
        });


        const bareaelements = [...tag.querySelectorAll("*")].filter(el => 
            [...el.attributes].some(attr => attr.name.startsWith("ba-"))
        );

        bareaelements.forEach(el => {
            const bareaAttributes = el.getAttributeNames()
            .filter(attr => attr.startsWith("ba-"))
            .map(attr => ({ name: attr, value: el.getAttribute(attr) }));

            //Skip all children of templates since they will be dealt with later on template rendering
            if (templateChildren.includes(el))
                return;

            bareaAttributes.forEach(attr =>
            {
   
                if (attr.name===DIR_FOREACH)
                {
                    let templateHtml = el.innerHTML.trim()
                        .replace(/>\s+</g, '><')  // Remove spaces between tags
                        .replace(/(\S)\s{2,}(\S)/g, '$1 $2'); // Reduce multiple spaces to one inside text nodes

                    const odo = this.#createDomDictionaryObject(el, el.parentElement, null, attr.name, attr.value, "template", true, templateHtml, el.localName, -1, null);
                    this.#domDictionary.push(odo);
                    el.remove();
                }

                if ([DIR_HIDE, DIR_SHOW, DIR_IF,DIR_CLASS_IF].includes(attr.name))
                {
                   
                    if (!attr.value)
                        return;

                    let ishandler=false;
                    if (!attr.value.includes('root.'))
                        ishandler=true;

                    if (ishandler && !this.#isValidHandlerName(attr.value))
                        return;

                    let expressions=[];
                    //A DIR_CLASS_IF expression contains both an express/handler and class name(s) 
                    if (attr.name===DIR_CLASS_IF && attr.value.includes('?'))
                    {
                        let attrparts = attr.value.split('?');
                        attrparts.forEach(n=>{expressions.push(n)});
                    }
                    else
                    {
                        expressions.push(attr.value);
                    }

                    if (ishandler){
                        const odo = this.#createDomDictionaryObject(el,el.parentElement,null,attr.name,"", DIR_TYPE_BOOLHANDLER, true,"","",templateId,expressions);
                        this.#domDictionary.push(odo);
                    }
                    else
                    {
                        const odo = this.#createDomDictionaryObject(el,el.parentElement,null,attr.name,"", DIR_TYPE_BOOLEXPR, true,"","",templateId,expressions);
                        this.#domDictionary.push(odo);
                    }
                   
                  
                }

                if ([DIR_CLASS, DIR_IMAGE_SRC,DIR_HREF].includes(attr.name))
                {
                    if (attr.name===DIR_IMAGE_SRC && el.localName.toLowerCase()!=="img")
                        return;

                    if (!attr.value)
                        return;

                    const odo = this.#createDomDictionaryObject(el,el.parentElement,null,attr.name,attr.value, DIR_TYPE_UI_SETTER, true,"","",templateId,null);
                    this.#domDictionary.push(odo);
                    
                }

                if (DIR_BIND===attr.name)
                {
                    if (!attr.value)
                        return;
                    if (!attr.value.includes('root.'))
                    {
                        console.error(`Then ${DIR_BIND} directive has an invalid path (${attr.value}), should begin with root.`);
                        return;
                    }

                    const odo = this.#createDomDictionaryObject(el,el.parentElement,null,attr.name,attr.value, DIR_TYPE_BINDING, true,"","",templateId,null);
                    this.#domDictionary.push(odo);
                }

                if (DIR_CLICK === attr.name)
                {
                    let expressions=[];
                    expressions.push(attr.value);
                    const odo = this.#createDomDictionaryObject(el,el.parentElement,null,attr.name,"", DIR_TYPE_HANDLER, true,"","",templateId, expressions);
                    this.#domDictionary.push(odo);
                }
            });

        });

        if (this.#enableInterpolation)
        {
            const walker = document.createTreeWalker(tag, NodeFilter.SHOW_TEXT, null, false);
            while (walker.nextNode()) {
                if (walker.currentNode.nodeValue.includes("{{") && walker.currentNode.nodeValue.includes("}}"))
                {
                    let paths = this.#getInterpolationPaths(walker.currentNode.nodeValue);
                    const odo = this.#createDomDictionaryObject(walker.currentNode.parentElement, walker.currentNode.parentElement,walker.currentNode,DIR_INTERPOLATION,"", DIR_TYPE_UI_SETTER, true,walker.currentNode.nodeValue,"",templateId, paths);
                    this.#domDictionary.push(odo);
                }
            }
        }

        let log = this.#getConsoleLog(4);
        if (log.active){
            console.log('dom dictionary: ' +  this.#domDictionary.length);
            this.#domDictionary.forEach(t=> {
                console.log(t);
            });
        }
    }

    #removeFromDomDictionaryById(id) 
    {
        this.#domDictionary = this.#domDictionary.filter(item => item.id !== id);
    }

    #removeTemplateChildrenFromDomDictionary(templatedId) 
    {
        this.#domDictionary = this.#domDictionary.filter(item => item.templateId !== templatedId); 
    }

    #createDomDictionaryObject(element, parentElement, node, directive, path, directiveType, isnew, templateMarkup, templateTagName, templateId, expressions)
    {
        if (!expressions)
        {
            expressions = [];
            expressions.push(path);
        }

        let id = this.#domDictionaryId++;

        return {id: id, templateId: templateId, element: element, elementnextsibling:null, parentelement:parentElement, node:node, directive: directive,  path:path, directivetype: directiveType, isnew: isnew, templateMarkup: templateMarkup, templateTagName: templateTagName, expressions: expressions  };
    }
  

    #setupBindings(path='root') 
    {

        let workscope = [];
        if (path.toLowerCase()=='root')
            workscope = this.#domDictionary.filter(p=> p.isnew && [DIR_TYPE_BINDING, DIR_TYPE_HANDLER].includes(p.directivetype));
        else
            workscope= this.#domDictionary.filter(p=> p.isnew && ((p.path.includes(path) && p.directivetype===DIR_TYPE_BINDING) || p.directivetype===DIR_TYPE_HANDLER));

    
        workscope.forEach(item => 
        {

            if (item.directive===DIR_BIND)
            {
                item.isnew = false;
                item.element.addEventListener("input", (event) => {

                    const keys = item.path.match(/[^.[\]]+/g); // Extracts both object keys and array indices
                    let valuekey = null;
                    let target = this.#appDataProxy;

                    keys.forEach((key, index) => {
                        if (key.toLowerCase()!=='root')
                        {
                            if (typeof target[key] === 'object') 
                                target = target[key];
        
                            valuekey=key;
                        }
                    });
        
                    if (!valuekey)
                        return;
                    if (!target)
                        return;


                    const log = this.#getConsoleLog(3);
                    let customhandler = item.element.getAttribute(DIR_BIND_HANDLER);
                    if (customhandler)
                    {
                        if (customhandler.includes('('));
                            customhandler = customhandler.split('(')[0];
                        customhandler=customhandler.trim();

                        if (this.#eventHandlers[customhandler]) {
                            this.#eventHandlers[customhandler].apply(this, [VERB_SET_DATA, item.element, target]);
                        } else {
                            console.warn(`Handler function '${customhandler}' not found.`);
                        }
                        return;
                    }


                    if (item.element.type === "checkbox") 
                    {
                        if (log.active)
                            console.log(log.name, "type: " + item.element.type, "key: " + valuekey, "input value: " + item.element.checked);
                        target[valuekey] =  item.element.checked;
                    } 
                    else if ( item.element.type === "radio") 
                    {
                        if (log.active)
                            console.log(log.name, "type: " + item.element.type, "key: " + valuekey, "input value: " + item.element.value);

                        if (item.element.checked)
                            target[valuekey] =  item.element.value;
                    } 
                    else 
                    {
                        if (log.active)
                            console.log(log.name, "type: " + item.element.type, "key: " + valuekey, "input value: " + item.element.value);

                        target[valuekey] =  item.element.value;
                    }
                });

            }

            if (item.directive=== DIR_CLICK)
            {
                if (!item.expressions)
                    return;
                if (!item.expressions.length===0)
                    return;

                item.isnew = false;
                let handlername = item.expressions[0];
                handlername=handlername.trim();
                if (!this.#isValidHandlerName(item.expressions[0]))
                    return;
              
               
                item.element.addEventListener("click", (event) => {
  
                    const path = this.#getClosestAttribute(event.target, META_PATH); 
                    let data = {};
                    if (path){
                        data = this.getPathData(path);
                    } else {
                        data = this.#appDataProxy;
                    }
                    const args = [event,item.element, data];
                    // Call the function from the handlers object
                    if (this.#eventHandlers[handlername]) {
                        this.#eventHandlers[handlername].apply(this, args);
                    } else {
                        console.warn(`Handler function '${handlername}' not found.`);
                    }
                });
  
            }

        });
    
    }

  
    #renderTemplates(operation='init', path='root', array=this.#appDataProxy) 
    {

        let foreacharray = [];
  
        if (path.includes('['))
            return; //throw new Error('renderTemplates was called with an object in the array');

        let isSinglePush = operation === "push";
        let templates = [];
        if (path.toLowerCase()==='root')
            templates = this.#domDictionary.filter(p=> p.directivetype===DIR_TYPE_TEMPLATE);
        else
            templates = this.#domDictionary.filter(p=> p.path.includes(path) && p.directivetype===DIR_TYPE_TEMPLATE);

        templates.forEach(template => {

            let [varname, datapath] = template.path.split(" in ").map(s => s.trim());
            if (!varname)
                throw new Error(`No variable name was found in the ${DIR_FOREACH} expression`);

            if (!Array.isArray(array))
                foreacharray = this.getPathData(datapath);
            else
                foreacharray= array;

            if (!Array.isArray(foreacharray))
                return; //throw new Error('renderTemplates could not get array, ' + path);

           if ((foreacharray.length - template.element.children.length) !== 1)
                isSinglePush=false;

           let counter=0;
            if (!isSinglePush)
            {
                this.#removeTemplateChildrenFromDomDictionary(template.id);
                template.parentelement.innerHTML = ""; // Clear list
            }
            else
            {
                counter = foreacharray.length-1;
                foreacharray = foreacharray.slice(-1);
            }

            const fragment = document.createDocumentFragment();

            foreacharray.forEach(item => {
                const newtag = document.createElement(template.templateTagName);
                let interpolationpaths = this.#getInterpolationPaths(template.templateMarkup);
                if (interpolationpaths.length>0)
                {
                    let regex = new RegExp(`{{([^}]*)\\b${varname}\\b([^}]*)}}`, "g");
                    newtag.innerHTML = template.templateMarkup.replace(regex, (match, before, after) => {return `{{${before}${datapath}[${counter}]${after}}}`});
                }
                else
                {
                    newtag.innerHTML = template.templateMarkup;
                }
              
                newtag.setAttribute(META_ARRAY_VARNAME, varname);
                newtag.setAttribute(META_PATH, `${datapath}[${counter}]`);
                newtag.setAttribute(META_ARRAY_INDEX, counter);
                if (newtag.id)
                    newtag.id = newtag.id + `-${counter}` 
                else
                    newtag.id = `${template.id}-${varname}-${counter}`; 

                fragment.appendChild(newtag);
               
                //Add references to click handlers
                newtag.querySelectorAll(`[${DIR_CLICK}]`).forEach(el => 
                {
                    el.setAttribute(META_ARRAY_VARNAME, varname);
                    el.setAttribute(META_PATH, `${datapath}[${counter}]`);
                    el.setAttribute(META_ARRAY_INDEX, counter);
                });

                newtag.querySelectorAll(`[${DIR_IF}], [${DIR_HIDE}], [${DIR_SHOW}], [${DIR_CLASS_IF}]`).forEach(el => 
                {
                
                    if (el.hasAttribute(DIR_IF)) {
                        let attrib = el.getAttribute(DIR_IF);

                         //Expression exist in the attribute
                        if (!this.#isValidHandlerName(attrib))
                        {
                            if (!attrib.includes(varname + '.') && !attrib.includes('root.'))
                                console.warn(`The ${DIR_IF} expression ${attrib} used in an element under ${DIR_FOREACH} does not match the ${DIR_FOREACH} expression, should reference '${varname}' or root`);
                            
                            //Convert to fullpath expression example: car.value =>  root.model.cars[4].value
                            let newexpr = attrib;
                            if (attrib.includes(varname + '.')){
                                newexpr = attrib.replaceAll(varname + '.',`${datapath}[${counter}].`);
                                el.setAttribute(DIR_IF, newexpr);
                            }     
                        }
                    }

                    if (el.hasAttribute(DIR_CLASS_IF)) {
                        let attrib = el.getAttribute(DIR_CLASS_IF);

                         //Expression exist in the attribute
                        if (!this.#isValidHandlerName(attrib))
                        {
                            if (!attrib.includes(varname + '.') && !attrib.includes('root.'))
                                console.warn(`The ${DIR_CLASS_IF} expression ${attrib} used in an element under ${DIR_FOREACH} does not match the ${DIR_FOREACH} expression, should reference '${varname}' or root`);
                            
                            //Convert to fullpath expression example: car.value =>  root.model.cars[4].value
                            let newexpr = attrib;
                            if (attrib.includes(varname + '.')){
                                newexpr = attrib.replaceAll(varname + '.',`${datapath}[${counter}].`);
                                el.setAttribute(DIR_CLASS_IF, newexpr);
                            }     
                        }
                    }

                    if (el.hasAttribute(DIR_HIDE)) {
                        let attrib = el.getAttribute(DIR_HIDE);

                         //Expression exist in the attribute
                        if (!this.#isValidHandlerName(attrib))
                        {
                            if (!attrib.includes(varname + '.') && !attrib.includes('root.'))
                                console.warn(`The ${DIR_HIDE} expression ${attrib} used in an element under ${DIR_FOREACH} does not match the ${DIR_FOREACH} expression, should reference '${varname}' or root`);
                            
                            //Convert to fullpath expression example: car.value =>  root.model.cars[4].value
                            let newexpr = attrib;
                            if (attrib.includes(varname + '.')){
                                newexpr = attrib.replaceAll(varname + '.',`${datapath}[${counter}].`);
                                el.setAttribute(DIR_HIDE, newexpr);
                            }     
                        }
                    }

                    if (el.hasAttribute(DIR_SHOW)) {
                        let attrib = el.getAttribute(DIR_SHOW);

                         //Expression exist in the attribute
                        if (!this.#isValidHandlerName(attrib))
                        {
                            if (!attrib.includes(varname + '.') && !attrib.includes('root.'))
                                console.warn(`The  ${DIR_SHOW} expression ${attrib} used in an element under ${DIR_FOREACH} does not match the ${DIR_FOREACH} expression, should reference '${varname}' or root`);
                            
                            //Convert to fullpath expression example: car.value =>  root.model.cars[4].value
                            let newexpr = attrib;
                            if (attrib.includes(varname + '.')){
                                newexpr = attrib.replaceAll(varname + '.',`${datapath}[${counter}].`);
                                el.setAttribute(DIR_SHOW, newexpr);
                            }     
                        }
                    }
                    
                });

                
                //Add references to input bindings
                newtag.querySelectorAll(`[${DIR_BIND}]`).forEach(el => 
                {
                    el.setAttribute(META_ARRAY_VARNAME, varname);
                    el.setAttribute(META_PATH, `${datapath}[${counter}]`);
                    el.setAttribute(META_ARRAY_INDEX, counter);
                    let attrib = el.getAttribute(DIR_BIND);
                    if (!attrib.includes(varname + '.'))
                        console.warn(`The ${DIR_BIND} binding ${attrib} used in an element under ${DIR_FOREACH} does not match the ${DIR_FOREACH} expression, should include '${varname}'`);

                    //Convert to fullpath expression example: car.value =>  root.model.cars[4].value
                    let bindingpath = attrib.replace(varname+'.',`${datapath}[${counter}].`);
                    el.setAttribute(DIR_BIND, bindingpath);

                });

                let templatechildren = newtag.querySelectorAll("*"); 
                templatechildren.forEach(child => 
                {
                    if (child.id)
                        child.id = child.id + `-${counter}` 
                    else
                        child.id = `${varname}-${counter}`; 

                    let forattrib = child.getAttribute("for");
                    if (forattrib)
                        child.setAttribute("for", forattrib + `-${counter}`); 
                   

                });
    

                this.#buildDomDictionary(newtag, template.id);

                counter++;

            });

            if (fragment.childElementCount>0)
                template.parentelement.appendChild(fragment);
           
        });    
    }

    #applyProxyChangesToDOM(path='root', value=this.#appDataProxy) 
    {
      
        //console.log(path, value);
        function interpolate(instance)
        {
            const interpolations = instance.#domDictionary.filter(p=>p.directive===DIR_INTERPOLATION);
            interpolations.forEach(t=>
            {
                let count=0;
                let content= t.templateMarkup;
                t.expressions.forEach(expr=> 
                {
                    //Just to speed up
                    //If primitive (path), example : root.model.user.firstname
                    //Only interpolate root, root.model, root.model.user, root.model.user.firstname
                    if (instance.#isPrimitive(value))
                    {
                        if (!path.includes(expr))
                            return;
                    }

                    let exprvalue = null;

                    if (expr.toLowerCase()=== INTERPOL_INDEX)
                        exprvalue = instance.#getClosestAttribute(t.element, META_ARRAY_INDEX);
                    if (expr.endsWith('.'+INTERPOL_ARRCOUNT))
                    {
                        let arrpath = "";
                        let parts = expr.split('.');
                        parts.forEach(p=>
                        {
                            if (p===INTERPOL_ARRCOUNT)
                                return;

                            if (arrpath==='')
                                arrpath+=p;
                            else
                                arrpath+='.'+p;
                            
                        });
                        if (arrpath)
                        {
                            exprvalue = instance.getPathData(arrpath);
                            if (Array.isArray(exprvalue))
                                exprvalue=exprvalue.length;
                        }
                        
                    }
                    else
                        exprvalue = instance.getPathData(expr);

                    if (!exprvalue)
                        exprvalue ="";

                    if (typeof exprvalue === "object") 
                        exprvalue = JSON.stringify(exprvalue)
                  
                    count++;
                    const regex = new RegExp(`{{\\s*${expr.replace(/[.[\]]/g, '\\$&')}\\s*}}`, 'g');
                    content = content.replace(regex, exprvalue);        
                    
                });
                if (count>0)
                    t.node.textContent = content;

            });

        }

       
        function updateElements(path, value, instance)
        {
                //Boolean handlers (react on any data change)
                const boolhandlers = instance.#domDictionary.filter(p=> p.directivetype === DIR_TYPE_BOOLHANDLER);
                boolhandlers.forEach(t=>
                {
                    const path = instance.#getClosestAttribute(t.element, META_PATH); 
                    let data = {};
                    if (path){
                        data = instance.getPathData(path);
                    } else {
                        data = instance.#appDataProxy;
                    }

                    let customhandler = t.expressions[0];
                    let boundvalue = false;
                    customhandler=customhandler.trim();
                    if (instance.#eventHandlers[customhandler]) {
                            boundvalue = instance.#eventHandlers[customhandler].apply(instance, [data]);
                    } else {
                        console.warn(`Handler function '${customhandler}' not found.`);
                    }
                    
                    if (t.directive===DIR_HIDE)
                        t.element.style.display = boundvalue ? "none" : ""
                    else if (t.directive===DIR_SHOW)      
                        t.element.style.display = boundvalue ? "" : "none";
                    else if (t.directive===DIR_IF) 
                    {
                        if (boundvalue)
                        {
                            if (!t.element.parentNode)
                                if (t.elementnextsibling)
                                    t.parentelement.insertBefore(t.element, t.elementnextsibling);
                        }else
                        {
                            if (t.element.parentNode)
                            {
                                t.elementnextsibling = t.element.nextSibling;
                                t.element.remove();
                            }   
                        }
                    }
                    else if (t.directive===DIR_CLASS_IF && t.expressions.length>1) 
                    {
                       let classnames = t.expressions[1].split(/[\s,]+/);

                        // Add classes if condition is true and remove if false
                        if (boundvalue) {
                            classnames.forEach(className => {
                                if (!t.element.classList.contains(className)) {
                                    t.element.classList.add(className); // Add class if not already present
                                }
                            });
                        } else {
                            classnames.forEach(className => {
                                if (t.element.classList.contains(className)) {
                                    t.element.classList.remove(className); // Remove class if present
                                }
                            });
                        }
                    }    
                          
                });

                //Boolean expressions (react on any data change)
                const boolexpr = instance.#domDictionary.filter(p=> p.directivetype === DIR_TYPE_BOOLEXPR);
                boolexpr.forEach(t=>
                {
                     
                      let expression = t.expressions[0];
                      let boundvalue = false;
                      expression=expression.trim();
                    

                    function  evaluateCondition(condition, context) {
                        try {
                            return new Function("contextdata", `return ${condition};`)(context);
                        } catch (error) {
                            console.error("Error evaluating condition:", error);
                            return false;
                        }
                    }
                    

                     expression=expression.replace('root.','contextdata.');
                     boundvalue = evaluateCondition(expression, instance.#appDataProxy);
                      
                    if (t.directive===DIR_HIDE)
                        t.element.style.display = boundvalue ? "none" : ""
                    else if (t.directive===DIR_SHOW)      
                        t.element.style.display = boundvalue ? "" : "none";
                    else if (t.directive===DIR_IF) 
                    {
                          if (boundvalue)
                          {
                              if (!t.element.parentNode)
                                  if (t.elementnextsibling)
                                      t.parentelement.insertBefore(t.element, t.elementnextsibling);
                          }else
                          {
                              if (t.element.parentNode)
                              {
                                  t.elementnextsibling = t.element.nextSibling;
                                  t.element.remove();
                              }   
                          }
                    } 
                    else if (t.directive===DIR_CLASS_IF && t.expressions.length>1) 
                    {
                       let classnames = t.expressions[1].split(/[\s,]+/);

                        // Add classes if condition is true and remove if false
                        if (boundvalue) {
                            classnames.forEach(className => {
                                if (!t.element.classList.contains(className)) {
                                    t.element.classList.add(className); // Add class if not already present
                                }
                            });
                        } else {
                            classnames.forEach(className => {
                                if (t.element.classList.contains(className)) {
                                    t.element.classList.remove(className); // Remove class if present
                                }
                            });
                        }
                    }     
                            
                });

                const bareabind = instance.#domDictionary.filter(p=>p.directive===DIR_BIND && ((instance.#isPrimitive(value) && (p.path===path)) || (!instance.#isPrimitive(value) && (p.path!==""))));
                bareabind.forEach(t=>
                {
                    let boundvalue = value;
                    if (bareabind.length> 1 || !instance.#isPrimitive(boundvalue))
                        boundvalue = instance.getPathData(t.path);

                    let customhandler = t.element.getAttribute(DIR_BIND_HANDLER);
                    if (customhandler)
                    {
                        customhandler=customhandler.trim();
                        if (!instance.#isValidHandlerName(customhandler))
                            return;

                        if (instance.#eventHandlers[customhandler]) {
                            instance.#eventHandlers[customhandler].apply(instance, [VERB_SET_UI, t.element, boundvalue]);
                        } else {
                            console.warn(`Handler function '${customhandler}' not found.`);
                        }
                        return;
                    }


                    if (t.element.type === "checkbox") 
                    {
                        if (!boundvalue)
                            boundvalue=false;

                        t.element.checked = boundvalue;
                    } 
                    else if (t.element.type === "radio")
                    {
                        t.element.checked = t.element.value === boundvalue;
                    } 
                    else 
                    {
                        if (!boundvalue)
                            boundvalue="";

                        t.element.value = boundvalue;
                    }                     
                });

                //Hide / Show are evaluated every time any value changes
                // const bareahide = instance.#domDictionary.filter(p=>(p.directive===DIR_HIDE || p.directive===DIR_SHOW) && p.directivetype===DIR_TYPE_UI_SETTER && ((instance.#isPrimitive(value) && (p.path===path)) || (!instance.#isPrimitive(value) && (p.path!==""))));
                // bareahide.forEach(t=>
                // {
                //     let boundvalue = value;
                //     if (bareahide.length> 1  || !instance.#isPrimitive(boundvalue))
                //         boundvalue = instance.getPathData(t.path);

                //     if (t.directive===DIR_HIDE)
                //         t.element.style.display = boundvalue ? "none" : ""
                //     else       
                //         t.element.style.display = boundvalue ? "" : "none";       
                // });

                const bareaclass = instance.#domDictionary.filter(p=>p.directive===DIR_CLASS && p.directivetype===DIR_TYPE_UI_SETTER && ((instance.#isPrimitive(value) && (p.path===path)) || (!instance.#isPrimitive(value) && (p.path!==""))));
                bareaclass.forEach(t=>
                {
                    let boundvalue = value;
                    if (bareaclass.length> 1  || !instance.#isPrimitive(boundvalue))
                        boundvalue = instance.getPathData(t.path);

                    if (boundvalue.includes(','))
                        boundvalue = boundvalue.replaceAll(',', ' ');

                    t.element.className = boundvalue || "";
            
                });

                const bareaimgsrc = instance.#domDictionary.filter(p=>p.directive===DIR_IMAGE_SRC && p.directivetype===DIR_TYPE_UI_SETTER && ((instance.#isPrimitive(value) && (p.path===path)) || (!instance.#isPrimitive(value) && (p.path!==""))));
                bareaimgsrc.forEach(t=>
                {
                    let boundvalue = value;
                    if (bareaimgsrc.length> 1  || !instance.#isPrimitive(boundvalue))
                        boundvalue = instance.getPathData(t.path);

                    if (boundvalue)
                    {
                        t.element.src = boundvalue;
                    }
            
                });

                const bahref = instance.#domDictionary.filter(p=>p.directive===DIR_HREF && p.directivetype===DIR_TYPE_UI_SETTER && ((instance.#isPrimitive(value) && (p.path===path)) || (!instance.#isPrimitive(value) && (p.path!==""))));
                bahref.forEach(t=>
                {
                    let boundvalue = value;
                    if (bareaimgsrc.length> 1  || !instance.#isPrimitive(boundvalue))
                        boundvalue = instance.getPathData(t.path);

                    if (boundvalue)
                    {
                        t.element.href = boundvalue;
                    }
            
                });

           
        }

        /****** Interpolation ******/
        interpolate(this);
       
        /******* Element boinding ******/
        updateElements(path,value,this);

    }

    /*** Internal Helpers ***/
    #hasAnyChar(str, chars) {
        return chars.some(char => str.includes(char));
    }
   
    #getClosestAttribute(element, name)
    {
        let val = element.getAttribute(name);
        let p = element.parentElement;
        let safecnt=0;
        while (!val && p)
        {
            safecnt++;
            val = p.getAttribute(name);
            p=p.parentElement;
            if (safecnt>10)
                break;
        }
        return val || "";
    }

    #isValidHandlerName(handlername)
    {
        if (!handlername)
            return false;
        if (this.#hasAnyChar(handlername,['(',',',')','.']))
        {
            return false;
        }

        return true;
    }
   
    //Get interpolation data paths from a string found in the dom
    #getInterpolationPaths(str) {
        const regex = /{{(.*?)}}/g;
        let matches = [];
        let match;
    
        while ((match = regex.exec(str)) !== null) {
            matches.push(match[1].trim()); // Trim spaces inside {{ }}
        }
    
        return matches;
    }


    #isPrimitive(value)
    {
        let result = value !== null && typeof value !== "object" && typeof value !== "function";
        return result;
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
    
    /*  Logging  */
    enableConsoleLog(id, active)
    {
        const logidx = this.#consoleLogs.findIndex(p=> p.id===id);
        if (logidx!== -1)
            this.#consoleLogs[logidx].active = active;
    }

    printConsoleLogs()
    {
      console.log('barea.js available logs:');
      this.#consoleLogs.forEach(p=> console.log(p));
    }

    #setConsoleLogs(){
        this.#consoleLogs = [
            {id: 1, name: "Proxy call back: ", active:false},
            {id: 2, name: "Update dom on proxy change: ", active:false},
            {id: 3, name: "Update proxy on user input: ", active:false},
            {id: 4, name: "Build dom dictionary: ", active:false}
        ];
    } 

    #getConsoleLog(id) 
    {
        const log = this.#consoleLogs.find(log => log.id === id);
        if (!log)
            return {id:-1, name:"", active:false};
        return log;
    }

  
                   
    /* Dom Handler */
    #loadedHandler =  (event) => {
       if (this.#enableHideUnloaded)
       {
            document.querySelectorAll(".ba-cloak").forEach(el => el.classList.remove("ba-cloak"));
       }
    }
   
    

   
    
}
