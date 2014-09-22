Handlebars.registerHelper('friendly_date', function (epoch) {
    try {
        if (!_.isNumber(epoch)) {
            return '';
        }

        if (epoch <= 0) {
            return '';
        }

        // return '';
        return moment.unix(epoch).local().format('MMMM Do YYYY');
    }
    catch (e) {
        return e.toString();
    }
});


Handlebars.registerHelper("now", function (format) {
    var today = new Date();

    // These methods need to return a String
    return today.getDay() + "/" + today.getMonth() + "/" + today.getFullYear();
});


Handlebars.registerHelper("hour", function (hour) {
    var shour = '' + hour;
    if (shour.length === 3) {
        shour = '0' + shour;
    }

    var h = Number(shour.substr(0, 2));
    var ampm = h < 12;
    if (!ampm && h != 12) {
        h = (h - 12);        
    }
    h = (h < 10) ? '0' + h : '' + h;
    
    var m = shour.substr(2);

    return h + ':' + m + (ampm ? 'am' : 'pm');

});


Handlebars.registerHelper("dayname", function (day) {
    
    day = parseInt(day);

    switch (day) {
        case 0:
            return 'Sunday';
        case 1:
            return 'Monday';
        case 2:
            return 'Tuesday';
        case 3:
            return 'Wednesday';
        case 4:
            return 'Thursday';
        case 5:
            return 'Friday';
        case 6:
            return 'Saturday';
        default:
            return 'Sunday';
    }
});