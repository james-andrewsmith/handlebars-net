Handlebars.Net
=================

[![Join the chat at https://gitter.im/james-andrewsmith/handlebars-net](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/james-andrewsmith/handlebars-net?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

A .net wrapper around existing JavaScript implemented within a number of engines to act as a view engine for MVC or WebAPI. Includes a number of tools to speed up development and deployment of front ends. 

#### Why
Allows a completely .NET backend but a templating language which is compatible with client and server rendering, a completely decoupled front-end which can be deployed seamlessly without a single dropped request.

#### Additional Features

There are a few features from razor which have been implented:

- Sections
- Donut Caching
- Master Template

#### Proxy
The Handlebars WebAPI/MVC view engines can be set to skip rendering handlebars and instead return the raw request JSON which is normally passed to the Handlebars Engine for rendering. This command line server combines remote server data/requests with local templates. 

This provides a very light development environment, and is useful for allowing frontend devs to work with production data without setting up databases etc.

I highly recommend using this with browser-sync, live-reload is awesome.

#### Engines
There are currently two engines: 

1. ClearScriptEngine uses .Net Bindings to the V8 engine directly, this is high performance, threads afe and recommended for production situations.

2. NodeEngine create a node server and makes async requests via HTTP. This is mainly included for Mono compatibility with Flow, allowing it to be cross platform. This is recommended for development only and is used.


#### Usage

-----

``` csharp
using Handlebars;

// Create the template at any point, app startup, or on the fly.
HandleBars.Compile("test", "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>");

// Provide the template name and the context object (which is turned into json)
var html = HandleBars.Run("test", new { title = "My New Post", body = "This is my first post!" });

```

#### Azure

If deploying to Windows Azure in a Web Role see the example project (ExampleAzureWebRole.csproj), 
managed code requires the install of the VCRedistribute, and copying the JavaScript.NET dependency 
to the IIS directory. For more info: (http://msdn.microsoft.com/en-us/library/windowsazure/hh694038.aspx)
