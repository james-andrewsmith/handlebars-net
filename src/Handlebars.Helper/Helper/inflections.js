var _ = _ || require('./../Script/underscore.js');
var Handlebars = Handlebars || require('./../Script/handlebars-1.0.0.js');
var accounting = accounting || require('./../Script/accounting.min.js');
var moment = moment || require('./../Script/moment.min.js');

Handlebars.registerHelper('inflect', function (count, singular, plural, include) {
    var word = count > 1 || count === 0 ? plural : singular;
    if (Utils.isUndefined(include) || include === false) {
        return word;
    } else {
        return "" + count + " " + word;
    }
});

Handlebars.registerHelper('ordinalize', function (count, singular, plural, include) {
    var _ref;
    var normal = Math.abs(Math.round(value));
    if (_ref = normal % 100, _indexOf.call([11, 12, 13], _ref) >= 0) {
        return "" + value + "th";
    } else {
        switch (normal % 10) {
            case 1:
                return "" + value + "st";
            case 2:
                return "" + value + "nd";
            case 3:
                return "" + value + "rd";
            default:
                return "" + value + "th";
        }
    }
});
