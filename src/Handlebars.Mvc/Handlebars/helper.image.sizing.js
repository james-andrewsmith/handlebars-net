 
Handlebars.registerHelper('calcheight', function (width, height) {
    width = parseInt(width, 10);
    height = parseInt(height, 10);

    var real = Math.ceil((300 / width) * height)
    return real;
});

Handlebars.registerHelper('getoffset', function (picture) {
    /*
    var picture = _.first(product.picture);
    var ratio = 0, width = 0, offset = 0;

    if (!_.isUndefined(picture)) {
        var thumb = picture["thumb-192-x-287"];
        if (thumb.width == 184) {
            return (0).toString();
        }

        ratio = (thumb.height >= 276) ?
                (276 / thumb.height) :
                (2 - (thumb.height / 276));

 

        width = thumb.width * ratio;
        if (width === 0) {
            return (0).toString();
        }
        offset = ((width / 2) * -1) + (184 / 2);
        // console.log(offset);
    }

    return (offset).toString();
    */
    
    if (_.isUndefined(picture)) {
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