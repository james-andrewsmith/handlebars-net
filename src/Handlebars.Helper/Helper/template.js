var _ = _ || require('./../Script/underscore.js');
var Handlebars = Handlebars || require('./../Script/handlebars-1.0.0.js');
var accounting = accounting || require('./../Script/accounting.min.js');
var moment = moment || require('./../Script/moment.min.js');

// a shortcut for {{section 'contents'}}
Handlebars.registerHelper('contents', function () {    
    var result = '\n<!--####section:start:contents####-->' +
                 '\n<!--####section:stop:contents####-->';
    return new Handlebars.SafeString(result);
});

// this needs to detect if it is been executed in an inline or block context
Handlebars.registerHelper('section', function (name, options) {


    // name of the section
    // wrap the contents of the block with 
    // ####section:start:{name}####
    // ####section:stop:{name}####

    if (_.isNull(options.fn) || _.isUndefined(options.fn)) {
        // we are inline
        var result = '\n<!--####section:start:' + name + '####-->' +
                     '\n<!--####section:stop:' + name + '####-->';
        return new Handlebars.SafeString(result);
    }
    else {
        // we are a block
        var out = '\n<!--####section:start:' + name + '####-->\n' +
                  options.fn(this) +
                  '\n<!--####section:stop:' + name + '####-->\n';
        return new Handlebars.SafeString(out);
    }

});

Handlebars.registerHelper('master', function (path) {
    // leave a hint to the actual engine (not the handlebars engine)
    // as to what master template to use

    // we assume master templates are in the root directory, not scurrying around for them
    // {{master "/home/app.handlebars"}}
    // ####master:/home/app.handlebars####
    var result = '####master:' + path + '####';    
    return new Handlebars.SafeString(result);
});

// {{#donut controller="Home" action="RenderCart"}}
// {{#donut "Home.RenderCart"}}
Handlebars.registerHelper('donut', function (controller, action) {

    // get the controller and the action to be executed
    // output:
    // ###donut:Home:Cart###
    controller = Handlebars.Utils.escapeExpression(controller);
    action = Handlebars.Utils.escapeExpression(action);

    var result = '####donut:' + controller + '/' + action + '####';
    return new Handlebars.SafeString(result);
});
