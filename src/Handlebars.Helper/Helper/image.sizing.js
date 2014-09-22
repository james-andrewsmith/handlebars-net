var _ = _ || require('./../Script/underscore.js');
var Handlebars = Handlebars || require('./../Script/handlebars-1.0.0.js');
var accounting = accounting || require('./../Script/accounting.min.js');
var moment = moment || require('./../Script/moment.min.js');

Handlebars.registerHelper('calcheight', function (width, height) {
    width = parseInt(width, 10);
    height = parseInt(height, 10);

    var real = Math.ceil((300 / width) * height)
    return real;
});

Handlebars.registerHelper('getoffset', function (picture) {
     
    if (_.isUndefined(picture) || _.isUndefined(picture["original"])) {
        return '';
    }

    var ratio = 0;
    var width = 0, height = 550, left = 0, top = 0;

    ratio = (picture["original"].height >= 550) ?
            (550 / picture["original"].height) :
            (2 - (picture["original"].height / 550));
                        
    width = Math.round(picture["original"].width * ratio);
    left = ((width / 2) * -1) + (312 / 2);  
                        
    // this is a wide image, let's treat it a bit differently
    if (width > 312) {
        width = 312;
        left = 0;
        ratio = (312 / picture["original"].width);
        height = Math.round(picture["original"].height * ratio);
        top = ((height / 2) * -1) + (550 / 2);  
    }

    return 'left: ' + left + 'px;top: ' + top + 'px;height: ' + height + 'px;width: ' + width + 'px;';
          
});