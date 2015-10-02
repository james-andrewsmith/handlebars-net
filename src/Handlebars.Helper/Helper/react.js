var _ = _ || require('./../Script/underscore.js');
var Handlebars = Handlebars || require('./../Script/handlebars-1.0.0.js');
var React = React || require('./../Script/react-with-addons.js');


// {{#react '$N.ui.helloMessage' this}}
Handlebars.registerHelper('reactold', function () {

    var HelloMessage = React.createClass({displayName: "HelloMessage",
      render: function() {
        return React.createElement("div", null, "Hello ", this.props.name);
      }
    });

    var html = React.renderToString(React.createElement(HelloMessage, {name: "John"}));

    return new Handlebars.SafeString(html);
});

// {{#react '$N.ui.helloMessage' this}}
Handlebars.registerHelper('react', function (name, options) {


    var component = window[name];
    if (_.isUndefined(component)) {
          return new Handlebars.SafeString('<!-- Could not find React class "' + name + '" -->');
    }

    var context = options;
    if (_.isUndefined(context) || _.isNull(context)) {
        context = {};
    }

    var html = React.renderToString(React.createElement(component, context));
    return new Handlebars.SafeString(html);
});
