var builder = DistributedApplication.CreateBuilder(args);

var presidio = builder.AddPresidio("presidio", 7001);


//builder.AddProject<Projects.RedactorApi>("RedactorApi")
//    .WithExternalHttpEndpoints()
//    .WaitFor(presidio)
//    //.WithHttpEndpoint(port: 5001, targetPort: 5001) // , name: "Container (Dockerfile)", isProxied: true)
//    ;

var api = builder.AddProject<Projects.RedactorApi>("RedactorApi")
    .WithReference(presidio)
    .WithExternalHttpEndpoints()
    .WaitFor(presidio)
    //.WithHttpEndpoint(port: 5001)
    //.WithHttpEndpoint(port: 5001, targetPort: 5001) // , name: "Container (Dockerfile)", isProxied: true)
    ;

//var envPresidio = presidio.Resource.GetEndpoints();
//var envApi = await api.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);



builder.Build().Run();



//var presidio = builder.AddContainer("presidio", "presidio-analyzer")
//    .WithEnvironment("PORT", "5001")
//    .WithEnvironment("PATH", "/usr/local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin")
//    .WithEnvironment("LANG", "C.UTF-8")
//    .WithEnvironment("GPG_KEY", "E3FF2839C048B25C084DEBE9B26995E310250568")
//    .WithEnvironment("PYTHON_VERSION", "3.9.21")
//    .WithEnvironment("PYTHON_SHA256", "3126f59592c9b0d798584755f2bf7b081fa1ca35ce7a6fea980108d752a05bb1")
//    .WithEnvironment("PIP_NO_CACHE_DIR", "1")
//    .WithEnvironment("ANALYZER_CONF_FILE", "presidio_analyzer/conf/default_analyzer.yaml")
//    .WithEnvironment("RECOGNIZER_REGISTRY_CONF_FILE", "presidio_analyzer/conf/default_recognizers.yaml")
//    .WithEnvironment("NLP_CONF_FILE", "presidio_analyzer/conf/default.yaml")
//    .WithHttpEndpoint(port: 5001, targetPort: 5001);

/*

// .WithReference(presidio)
       //.WithReference(presidio)
   //.WaitFor(presidio);
   ; //.WithReference();
   
   
   // .WithEnvironment("ENV_VAR_NAME", "value")
   // .WithBindMount( bm => bm.)
   

docker run -d -p 5002:3000 mcr.microsoft.com/presidio-analyzer:latest

"Path": "/bin/sh",
	"Args": [
		"-c",
		"poetry run python app.py --host 0.0.0.0"
	],

"Cmd": [
			"/bin/sh",
			"-c",
			"poetry run python app.py --host 0.0.0.0"
		],

"Env": [
			"PORT=5001",
			"PATH=/usr/local/bin:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
			"LANG=C.UTF-8",
			"GPG_KEY=E3FF2839C048B25C084DEBE9B26995E310250568",
			"PYTHON_VERSION=3.9.21",
			"PYTHON_SHA256=3126f59592c9b0d798584755f2bf7b081fa1ca35ce7a6fea980108d752a05bb1",
			"PIP_NO_CACHE_DIR=1",
			"ANALYZER_CONF_FILE=presidio_analyzer/conf/default_analyzer.yaml",
			"RECOGNIZER_REGISTRY_CONF_FILE=presidio_analyzer/conf/default_recognizers.yaml",
			"NLP_CONF_FILE=presidio_analyzer/conf/default.yaml"
		],

"Ports": {
			"5001/tcp": [
				{
					"HostIp": "0.0.0.0",
					"HostPort": "7001"
				}
			]
		},
*/
