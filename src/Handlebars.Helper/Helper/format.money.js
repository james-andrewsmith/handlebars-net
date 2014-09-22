var _ = _ || require('./../Script/underscore.js');
var Handlebars = Handlebars || require('./../Script/handlebars-1.0.0.js');
var accounting = accounting || require('./../Script/accounting.min.js');
var moment = moment || require('./../Script/moment.min.js');

Handlebars.registerHelper("money", function (text, symbol) {

    if (_.isUndefined(text)) {
        return '';
    }

    if (_.isUndefined(symbol)) {
        symbol = '$';
    }

    return accounting.formatMoney(parseInt(text) / 100, symbol);
});
