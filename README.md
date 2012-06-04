handlebars.cs
=================

NOTE: This is not a port, this is wrapper of the actual handlebars.js library in C# via the V8
bindings, using http://javascriptdotnet.codeplex.com 

Usage
-----

``` csharp
using handlebars.cs;

// Create the template at any point, app startup, or on the fly.
HandleBars.Compile("test", "<div class=\"entry\"><h1>{{title}}</h1><div class=\"body\">{{{body}}}</div></div>");

// Provide the template name and the context object (which is turned into json)
var html = HandleBars.Run("test", new { title = "My New Post", body = "This is my first post!" });

```

Copyright & License
---------------------

Copyright 2011 PressF12 Pty Ltd

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this work except in compliance with the License.
You may obtain a copy of the License in the LICENSE file, or at:

   http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.