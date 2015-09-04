var _ = _ || require('./../Script/underscore.js');
var Handlebars = Handlebars || require('./../Script/handlebars-1.0.0.js');
var React = React || require('./../Script/react-with-addons.js');


// {{#react '$N.ui.helloMessage' this}}
Handlebars.registerHelper('react', function () {

    var HelloMessage = React.createClass({displayName: "HelloMessage",
      render: function() {
        return React.createElement("div", null, "Hello ", this.props.name);
      }
    });

    var html = React.renderToString(React.createElement(HelloMessage, {name: "John"}));

    return new Handlebars.SafeString(html);
});
