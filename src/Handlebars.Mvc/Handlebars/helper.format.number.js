
// minus - subtraction e.g. {{ 4 | minus:2 }} #=> 2
// plus - addition e.g. {{ '1' | plus:'1' }} #=> '11', {{ 1 | plus:1 }} #=> 2
// times - multiplication e.g {{ 5 | times:4 }} #=> 20
// divided_by - division e.g. {{ 10 | divided_by:2 }} #=> 5
// modulo - remainder, e.g. {{ 3 | modulo:2 }} #=> 1

Handlebars.registerHelper('debug_sum', function (v1, v2, v3, v4) {
    return 'v1: ' + toString.call(v1) + '\n' +
           'v2: ' + JSON.stringify(v2) + '\n' +
           'v3: ' + JSON.stringify(v3) + '\n' +
           'v4: ' + toString.call(v4);
});

// 
Handlebars.registerHelper('sum', function (v1, v2) {
    // if (_.isNumber(v1) && _.isNumber(v2)) {
    
    return (parseInt(v1) + parseInt(v2)).toString();
    // }

    // return JSON.stringify(v1) + ' ' + JSON.stringify(v2);
});

Handlebars.registerHelper('random', function (min, max) {

    if (_.isUndefined(min)) {
        min = 0;
    }

    if (_.isUndefined(max)) {
        max = 100;
    }

    return Math.floor(Math.random() * (max - min + 1)) + min;
});

Handlebars.registerHelper('base36', function (v1) {
    // if (_.isNumber(v1) && _.isNumber(v2)) {
    return parseInt(v1).toString(36);
    // }

    // return JSON.stringify(v1) + ' ' + JSON.stringify(v2);
});


Handlebars.registerHelper('ceil', function (value) {
    return Math.ceil(value);
});

Handlebars.registerHelper('floor', function (value) {
    return Math.floor(value);
});


/*
var rendered = Handlebars.compile(
    '{{#each values}}' +
    '[{{@index}}]: ' +
    'i+1 = {{math @index 1}}, ' +
    'i-0.5 = {{math @index "+" "-0.5"}}, ' +
    'i/2 = {{math @index "*" 2}}, ' +
    'i%2 = {{math @index "%" 2}}, ' +
    'i*i = {{math @index "*" @index}}\n' +
    '{{/each}}'
)({
    values: ['a', 'b', 'c', 'd', 'e']
});

$("#result").html(rendered);*/
Handlebars.registerHelper("math", function (lvalue, operator, rvalue, options) {
    if (arguments.length < 4) {
        // Operator omitted, assuming "+"
        options = rvalue;
        rvalue = operator;
        operator = "+";
    }

    lvalue = parseFloat(lvalue);
    rvalue = parseFloat(rvalue);

    return {
        "+": lvalue + rvalue,
        "-": lvalue - rvalue,
        "*": lvalue * rvalue,
        "/": lvalue / rvalue,
        "%": lvalue % rvalue
    }[operator];
});

// value = object
// fmt = optional format string
Handlebars.registerHelper('format', function (value, fmt) {

    if (_.isNumber(value)) {
        return accounting.formatNumber(value)
    }

    return value;
});

