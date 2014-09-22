Handlebars.Net
=================

A .net wrapper around existing JavaScript implemented within a number of engines to act as a view engine for MVC or WebAPI. Includes a number of tools to speed up development and deployment of front ends. 

#### Why
Allows a completely .NET backend but a templating language which is compatible with client and server rendering, a completely decoupled front-end which can be deployed seamlessly without a single dropped request.

#### Additional Features

There are a few features from razor which have been implented:

- Sections
- Donut Caching
- Master Template

#### Flow
A command line server which allows for local development of handlebars themes.

- Live Reload
- Uses a local cache of data allowing for completely offline development 
- Preview UI changes using production data
- 

#### Engines
There are currently two engines: 

1. ClearScriptEngine uses .Net Bindings to the V8 engine directly, this is high performance, threads afe and recommended for production situations.

2. NodeEngine create a node server and makes async requests via HTTP. This is mainly included for Mono compatibility with Flow, allowing it to be cross platform. This is recommended for development only and is used.


#### Benchmarks

Usage
-----

``` csharp
using Handlebars;

// Create the template at any point, app startup, or on the fly.
HandleBars.Compile("test", "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>");

// Provide the template name and the context object (which is turned into json)
var html = HandleBars.Run("test", new { title = "My New Post", body = "This is my first post!" });

```

Azure
-----
If deploying to Windows Azure in a Web Role see the example project (ExampleAzureWebRole.csproj), 
managed code requires the install of the VCRedistribute, and copying the JavaScript.NET dependency 
to the IIS directory. For more info: (http://msdn.microsoft.com/en-us/library/windowsazure/hh694038.aspx)