 

// Drop in replacement of standard each function from handlebars with 
// basic query support
// https://github.com/jessehouchins/handlebars-helpers/blob/master/helpers/each.js
 
// {{#each things 'where' 'prop' '<' foo}}

// INPUT VARIABLES
// limit
// offset
// reversed

// PRIVATE VARIABLES
// index
// index_0
// rindex
// rindex_0
// first
// last
// odd
// even

Handlebars.registerHelper('each', function (context) {

    var j4 = Handlebars.Utils.j4
    var args = Array.prototype.slice.call(arguments)
    var options = args.pop()
    args = j4.conditionArgs(args)
    var fn = options.fn, inverse = options.inverse
    var i = 0, ret = "", data, match

    if (options.data) {
        data = Handlebars.createFrame(options.data);
    }

    if (context && typeof context === 'object') {
        if (context instanceof Array) {
            for (var j = context.length; i < j; i++) {
                if (j4.conditionOK(context[i], args.verb, args.condition)) {
                    match = true
                    if (data) {
                        data.index = i
                        data.rindex = j - i - 1;
                        data.first = (i === 0);
                        data.last = (i === (j - 1));
                        data.length = j;
                        data.odd = ((i + 1) % 2) == 1;
                        data.even = (data.odd == false);
                    }
                    ret = ret + fn(context[i], { data: data })
                }
            }
        } else {
            var size = _.size(context);
            for (var key in context) {
                if (context.hasOwnProperty(key) &&
                    j4.conditionOK(context[key], args.verb, args.condition)) {
                    match = true
                    if (data) {
                        data.key = key
                        data.index = i
                        data.first = (i === 0);
                        data.last = (i === (size - 1));
                        data.odd = ((i + 1) % 2) == 1;
                        data.even = (data.odd == false);
                    }
                    ret = ret + fn(context[key], { data: data })
                    i++
                }
            }
        }
    }

    if (!match) ret = inverse(this)

    return ret
});

// wraps the pluck function from underscore in a wrapper for arrays
// {{chunk some.array limit="6"}}

function partition(a, n) {
    var len = a.length, out = [], i = 0;
    while (i < len) {
        var size = Math.ceil((len - i) / n--);
        out.push(a.slice(i, i += size));
    }
    return out;
}

Handlebars.registerHelper('chunk', function (context, options) {
     

    var toString = Object.prototype.toString,
        functionType = '[object Function]',
        objectType = '[object Object]';

    var fn = options.fn, inverse = options.inverse;
    var i = 0, ret = "", data;

    var limit = options.hash && options.hash.limit != null ? parseInt(options.hash.limit, 10) : 5;
    var capstone = options.hash && options.hash.capstone != null ? parseInt(options.hash.capstone, 10) : 0;

    var type = toString.call(context);
    if (type === functionType) { context = context.call(this); }

    if (options.data) {
        data = Handlebars.createFrame(options.data);
    }

    if (context && typeof context === 'object') {
        if (context instanceof Array) {
            // var lists = _.groupBy(context, function (a, b) {
            //     return Math.floor(b / limit);
            // });
            // lists = _.toArray(lists);

            for (var cs = 0; cs < capstone; cs++) {
                context.push({});
            }

            var lists = partition(context, limit);

            for (var i = 0; i < lists.length; i++) {

                if (data) {
                    data.index = i
                    data.first = (i === 0);
                    data.last = (i === (lists.length - 1));
                }

                ret = ret + fn(lists[i], { data: data });
            }
        }
    }

    if (i === 0) {
        ret = inverse(this);
    }

    return ret;
});


// wraps the pluck function from underscore in a wrapper for arrays
// {{pluck some.array "property_name"}}
Handlebars.registerHelper('pluck', function (array, property, options) {
    return _.pluck(array, property);;
});


// first - get the first element of the passed in array
Handlebars.registerHelper('first', function (context, options) {
    var fn = options.fn, inverse = options.inverse;
    var i = 0, ret = "", data;

    if (options.data) {
        data = Handlebars.createFrame(options.data);
    }

    if (context && typeof context === 'object') {
        if (context instanceof Array) {
            if (context.length > 0) {
                ret = ret + fn(_.first(context), { data: data });
            }
        }
    }

    return ret;
});

// last - get the last element of the passed in array
Handlebars.registerHelper('last', function (context, options) {
    var fn = options.fn, inverse = options.inverse;
    var i = 0, ret = "", data;

    if (options.data) {
        data = Handlebars.createFrame(options.data);
    }

    if (context && typeof context === 'object') {
        if (context instanceof Array) {
            if (context.length > 0) {
                ret = ret + fn(_.last(context), { data: data });
            }
        }
    }

    return ret;
});

// size 
Handlebars.registerHelper('size', function (array) {
    return array.size;
});