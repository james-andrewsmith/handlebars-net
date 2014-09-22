// Assign a variable within the context of the current template, very useful 
// for situations which require complicated or repeated logic or concation
// {{assign "test" "123"}}
Handlebars.registerHelper("assign", function (variable, value) {

    if (typeof (variable) == 'function') {
        variable = variable.apply(null, Array.prototype.slice.call(arguments, 1));
    }
    
    if (typeof (value) == 'function') {
        value = value.apply(null, Array.prototype.slice.call(arguments, 2));
    }

    this[variable] = value;

    return '';
});