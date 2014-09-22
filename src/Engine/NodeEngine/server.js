var http = require("http"),
    url = require("url"),
    path = require("path"),
    fs = require("fs"),
    qs = require('querystring'),
    actions = actions || {},
    raw = {};

// port = process.argv[2] || 8888;

console.log('Creating Server on port 1337');
http.createServer(function(request, response) {

    var path = url.parse(request.url).pathname;
     
    try
    {
        switch (path.toLowerCase()) {
            case '/compile':
                actions.compile(request, response);
                break;
            case '/render':
                actions.render(request, response);
                break;
            case '/clear':
                actions.clear(request, response);
                break;
            case '/test':
                actions.test(request, response);
            case '/exists':
                actions.exists(request, response);
                break;
            case '/remove':
                actions.remove(request, response);
                break;
            case '/precompile':
                actions.precompile(request, response);
                break;
            case '/precompile-import':
                actions.precompileImport(request, response);
                break;
            default:
                response.writeHead(404, { "Content-Type": "text/html" });
                response.write('Could not find path: ' + path.toLowerCase());
                response.end();
                break;
        }     
    }
    catch (e) {
        response.writeHead(500, { "Content-Type": "application/json" });
        response.write(JSON.stringify(e));
        response.end();
    }

}).listen(1337, '127.0.0.1');

console.log('Creating Compile Action');
actions.compile = function (request, response) {

    var body = '';
    request.on('data', function (chunk) { body += chunk.toString(); });
    request.on('end', function () {

        try 
        {
            // parse the received body data
            var form = qs.parse(body);            
            Handlebars.templates[form['name']] = Handlebars.compile(form['template']);
            raw[form['name']] = form['template'];
            response.writeHead(200);
        }
        catch (e) {
            response.writeHead(500);
        }
                
        response.end();
    });

};

console.log('Creating Clear action');
actions.clear = function (request, response) {

    var body = '';
    request.on('data', function (chunk) { body += chunk.toString(); });

    request.on('end', function () {

        Handlebars.templates = [];
        Handlebars.partial = [];

        response.writeHead(200);      
        response.end();
    });
};

console.log('Creating Precompile action');
actions.precompile = function (request, response) {
    try {
        response.writeHead(200, { "Content-Type": "text/javascript" });
        var keys = Object.keys(Handlebars.templates);
        for (var i = 0; i < keys.length; i++) {
            var pre = Handlebars.precompile(raw[keys[i]]);
            response.write('Handlebars.templates[\'' + keys[i] + '\'] = Handlebars.template(' + pre.toString() + ');');
        }
        // response.write(Handlebars.templates.toString());
        // response.write(Handlebars.partial.toString());
    }
    catch (e) {
        response.writeHead(500, { "Content-Type": "text/plain" });
        response.write(e.message);
        response.write('\n----\n'); 
    }
    finally {
        response.end();
    }
};

console.log('Creating Precompile Import action');
actions.precompileImport = function (request, response) {
    // if the template does not exist, return a 404
    var body = '';
    request.on('data', function (chunk) { body += chunk.toString(); });
    request.on('end', function () {

        try {
            var form = qs.parse(body);
            var name = form['js'];
             
            // evil hack
            eval(js);

            // else render and return any response...
            response.writeHead(200, { "Content-Type": "text/html" });
            // response.write(output);
        }
        catch (e) {
            response.writeHead(500, { "Content-Type": "text/plain" });
            response.write(e.message);
        }
        finally {
            response.end();
        }
    });
};

console.log('Creating Test action');
actions.test = function (request, response) {
    try {
        var query = url.parse(request.url).query;
        var form = qs.parse(query);
        // response.writeHead(typeof Handlebars.templates[form['name']] == 'undefined' ? 404 : 200);
        response.writeHead(200);
        response.write(JSON.stringify(form['name']));
    }
    catch (e) {
        response.writeHead(500, { "Content-Type": "text/plain" });
        response.write(e.message);
    }
    finally {
        response.end();
    }
};

console.log('Creating Exists action');
actions.exists = function (request, response) {
    try { 
        var query = url.parse(request.url).query;
        var form = qs.parse(query);
        response.writeHead(typeof Handlebars.templates[form['name']] == 'undefined' ? 404 : 200);        
        response.write(JSON.stringify(form['name']));
    }
    catch (e) {
        response.writeHead(500, { "Content-Type": "text/plain" });
        response.write(e.message);
    }
    finally {
        response.end();
    }
};

console.log('Creating Remove action');
actions.remove = function (request, response) {
    try { 
        var query = url.parse(request.url).query;
        var form = qs.parse(query);
        delete Handlebars.templates[form['name']];
        response.writeHead(200); 
    }
    catch (e) {
        response.writeHead(500, { "Content-Type": "text/plain" });
        response.write(e.message);
    }
    finally {
        response.end();
    }
};

console.log('Creating Render action');
actions.render = function (request, response) {
    // if the template does not exist, return a 404
    var body = '';
    request.on('data', function (chunk) { body += chunk.toString(); });
    request.on('end', function () {

        // var template = '<div class="entry"><h1>{{title}}</h1><div class="body">{{{body}}}</div></div>';
        // var context = '{title: "My New Post", body: "This is my first post!"}';
        try {
            var form = qs.parse(body);
            var name = form['name'];
            var template = form['template'];
            var context = JSON.parse(form['context']);
            var output = '';
            if (typeof template != 'undefined') {
                output = Handlebars.compile(template)(context);
            }
            else {
                output = Handlebars.templates[name](context);
            }

            // else render and return any response...
            response.writeHead(200, { "Content-Type": "text/html" });
            response.write(output);
        }
        catch (e) {
            response.writeHead(500, { "Content-Type": "text/plain" });
            response.write(e.message); 
        }
        finally {
            response.end();
        }
    });
};

