
Handlebars.registerHelper("money", function (text, symbol) {

    if (_.isUndefined(text)) {
        return '';
    }

    if (_.isUndefined(symbol)) {
        symbol = '$';
    }

    return accounting.formatMoney(parseInt(text) / 100, symbol);
});
