This is a prototype engine which will only perform a subset of handlebars commands, including essential helpers. 

The idea is to see if there can be a performance gain from generating C# using string builders to perform rendering/binding over V8. 

If there is enough of a performance lift then further exploration will go into this engine.