var _ = _ || require('./../Script/underscore.js');
var Handlebars = Handlebars || require('./../Script/handlebars-1.0.0.js');
var accounting = accounting || require('./../Script/accounting.min.js');
var moment = moment || require('./../Script/moment.min.js');

// Technique lifted from here comments here, and adapted to work 
// with the newer compilers. 
// https://github.com/wycats/handlebars.js/issues/222?source=c

/* Work around the compiler protections handlebars has inserted to prevent 
   cascading helpers, this is a very low level which will require further 
   development to add checks for the specific helper being used, we should 
   limit this as the platform sites gain public developers to prevent abuse 
   and potential performance issues. */
Handlebars.JavaScriptCompiler.prototype.lookupOnContext = function (name) {    
    if (name.indexOf(',') === -1) {
        this.push(this.nameLookup('depth' + this.lastContext, name, 'context') + (this.lastContext == 0 ? (' || helpers.' + name) : ''));
    }
    else {
        // this is a hack to make the drop in replacement of "if" work with arrays
        // we can now pass variables like so {{#foo [1,2,3]}}{{/foo}}
        // NOTE: this hack does not work with single variable arrays. but it's a single 
        // variable why do we need an array!
        this.push('[' + name + ']');
    }
};
 

// Setup the helpers
Handlebars.registerHelper('sum', function (left, right) {
    return left + right;
});
 
Handlebars.registerHelper('numFormat', function (num) {
    // Hack around Handlebars by testing for function and using
    // that to calculate the num.
    if (typeof (num) == 'function') {
        num = num.apply(null, Array.prototype.slice.call(arguments, 1));
    }

    // Do that actual work
    return Math.round(num);
});

// Use this template    
// var t = Handlebars.compile("{{ numFormat sum 1 2}} === {{ numFormat 3 }}");
// assert('3 === 3', t());