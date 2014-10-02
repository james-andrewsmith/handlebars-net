// ?? will this work
// {{raw}}{{/raw}}
Handlebars.registerHelper('raw', function (text) {
    if (_.isUndefined(text)) {
        return '';
    }
    return new Handlebars.SafeString(text);
});


Handlebars.registerHelper("limit", function (text) {
    return (text.length <= 18) ? text : text.substr(0, 18) + '...';
});


Handlebars.registerHelper("stringify", function (optionalValue) {
    if (!_.isUndefined(optionalValue)) {
        return new Handlebars.SafeString(JSON.stringify(optionalValue));
    }
    return 'null';
});

// usage: 
// wrap the underscore strings library
// {{#join list}} {{name}} {{status}} {{/join}}
Handlebars.registerHelper('join', function (items, options) {

    // Hack around Handlebars by testing for function and using
    // that to calculate the num.
    if (typeof (items) == 'function') {
        items = items.apply(null, Array.prototype.slice.call(arguments, 1));
    }

    if (_.isEmpty(items)) {
        return '';
    }

    var out = "";
    for (var i = 0, l = items.length; i < l; i++) {
        out += options.fn(items[i]);
        if (i < l - 1) {
            out += ',';
        }
        // might want to add a newline char or something
    }
    return out;
});

// capitalize - capitalize words in the input sentence
// downcase - convert an input string to lowercase
// upcase|upper - convert an input string to uppercase

Handlebars.registerHelper('capitalize', function (str) {
    if (!_.isUndefined(str)) {
        return new Handlebars.SafeString(_.capitalize(str));
    }
    return '';
});

Handlebars.registerHelper('capitalise', function (str) {
    if (!_.isUndefined(str)) {
        return new Handlebars.SafeString(_.capitalize(str));
    }
    return '';
});

Handlebars.registerHelper('title', function (str) {
    if (!_.isUndefined(str)) {
        return new Handlebars.SafeString(_.capitalize(str));
    }
    return '';
});

Handlebars.registerHelper('upcase', function (str) {
    if (!_.isUndefined(str)) {
        return new Handlebars.SafeString(str.toUpperCase());
    }
    return '';
});

Handlebars.registerHelper('upper', function (str) {
    if (!_.isUndefined(str)) {
        return new Handlebars.SafeString(str.toUpperCase());
    }
    return '';
});

Handlebars.registerHelper('lower', function (str) {
    if (!_.isUndefined(str)) {
        return new Handlebars.SafeString(str.toLowerCase());
    }
    return '';
});

// escape - escape a string
// strip_html -  strip html from string

Handlebars.registerHelper('striptags', function (str) {
    if (str && str != undefined) {

        var tags = /<\/?([a-z][a-z0-9]*)\b[^>]*>/gi,
            commentsAndPhpTags = /<!--[\s\S]*?-->|<\?(?:php)?[\s\S]*?\?>/gi,
            allowed = '';

        if (str.replace) {

            str = str.replace(commentsAndPhpTags, '').replace(tags, function ($0, $1) {
                return allowed.indexOf('<' + $1.toLowerCase() + '>') > -1 ? $0 : '';
            });
        }

        return new Handlebars.SafeString(str);
    }

    return '';
});

// strip_newlines - strip all newlines (\n) from string
// newline_to_br - replace each newline (\n) with html break

Handlebars.registerHelper('br2nl', function (str) {

    if (_.isUndefined(str)) {
        return '';
    }

    return new Handlebars.SafeString(str.replace(/<br\s?\/?>/g, "\n"));
});

Handlebars.registerHelper('nl2br', function (str) {

    if (_.isUndefined(str)) {
        return '';
    }

    return new Handlebars.SafeString(str.replace(/([^>\r\n]?)(\r\n|\n\r|\r|\n)/g, '$1<br />$2'));
});

// urlencode
Handlebars.registerHelper('urlencode', function (str) {
    return new Handlebars.SafeString(encodeURIComponent(str));
});

// replace - replace each occurrence e.g. {{ 'foofoo' | replace:'foo','bar' }} #=> 'barbar'
// replace_first - replace the first occurrence e.g. {{ 'barbar' | replace_first:'bar','foo' }} #=> 'foobar'
// remove - remove each occurrence e.g. {{ 'foobarfoobar' | remove:'foo' }} #=> 'barbar'
// remove_first - remove the first occurrence e.g. {{ 'barbar' | remove_first:'bar' }} #=> 'bar'
// truncate - truncate a string down to x characters
// truncatewords - truncate a string down to x words
// prepend - prepend a string e.g. {{ 'bar' | prepend:'foo' }} #=> 'foobar'
// append - append a string e.g. {{ 'foo' | append:'bar' }} #=> 'foobar'
// split - split a string on a matching pattern e.g. {{ "a~b" | split:~ }} #=> ['a','b']

Handlebars.registerHelper('concat', function (options) {

    var args = [];
    for (var i = 0, len = arguments.length; i <= len; i++) {
        if (typeof (arguments[i]) === 'string') {
            args[args.length] = arguments[i];
        }
        else if (typeof (arguments[i]) === 'boolean') {
            args[args.length] = arguments[i].toString();
        }
        else if (typeof (arguments[i]) === 'number') {
            args[args.length] = arguments[i].toString();
        }
    }
     
    var separator = options.separator && options.separator.limit != null ? options.hash.separator : '';
    return args.join(separator);
});

/**
   * {{dashify}}
   * Replace periods in string with hyphens.
   * @param  {[type]} str [description]
   * @return {[type]}     [description]
   */
Handlebars.registerHelper('dashify', function (str) {
    if (str && typeof str === "string") {
        return _.slugify(str);
    }
});
Handlebars.registerHelper('slugify', function (str) {
    if (str && typeof str === "string") {
        return _.slugify(str);
    }
});
/**
   * {{sentence}}
   * Sentence case
   * @param  {[type]} str [description]
   * @return {[type]}     [description]
   */
Handlebars.registerHelper('sentence', function (str) {
    if (str && typeof str === "string") {
        return str.replace(/((?:\S[^\.\?\!]*)[\.\?\!]*)/g, function (txt) {
            return txt.charAt(0).toUpperCase() + txt.substr(1).toLowerCase();
        });
    }
});

Handlebars.registerHelper('prune', function (str, length) {
    if (str && typeof str === "string") {
        if (_.isUndefined(length)) {
            length = 15;
        }
        return _.prune(str, length);
    }
    return '';
});

Handlebars.registerHelper('humanize', function (str) {
    if (str && typeof str === "string") {
        return _.titleize(_.clean(_.humanize(str)));
    }
    return '';

});
Handlebars.registerHelper("phone", function (phoneNumber) {
    // we can come here if there is a nested call for a property which 
    // does not exist, this is an example of why we want to keep nesting
    // to a minimum
    if (_.isUndefined(phoneNumber)) {
        return '';
    }

    phoneNumber = phoneNumber.toString();
    return "(" + phoneNumber.substr(0, 3) + ") " + phoneNumber.substr(3, 3) + "-" + phoneNumber.substr(6, 4);
});

/**
  * {{urlparse}}
  * Take a URL string, and return an object. Pass true as the
  * second argument to also parse the query string using the
  * querystring module. Defaults to false.
  *
  * @author: Jon Schlinkert <http://github.com/jonschlinkert>
  * @param  {[type]} path  [description]
  * @param  {[type]} type  [description]
  * @param  {[type]} query [description]
  * @return {[type]}       [description]
  */
Handlebars.registerHelper("urlparse", function (path, type, query) {
    path = url.parse(path);
    var result = Utils.stringifyObj(path, type, query);
    return new Utils.safeString(result);
});


Handlebars.registerHelper('replace', function (source, lookup, supplement) {
    if (_.isUndefined(source) || _.isUndefined(lookup) || _.isUndefined(supplement)) {
        return new Handlebars.SafeString(source);
    }

    source = source.replace(new RegExp(lookup, 'g'), supplement);
    return new Handlebars.SafeString(source);
});

Handlebars.registerHelper('substring', function (string, start, end) {

    if (_.isUndefined(end)) {
        end = start;
        start = 0;
    }

    var theString = string.substring(start, end);

    // attach dot dot dot if string greater than suggested end point
    // if (string.length > end) {
    //     theString += '...';
    // }

    return new Handlebars.SafeString(theString);
});

// https://github.com/assemble/handlebars-helpers/blob/master/lib/helpers/helpers-url.js